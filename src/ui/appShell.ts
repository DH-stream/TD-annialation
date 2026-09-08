import {
  DEFAULT_KEYBOARD_BINDINGS,
  type KeyboardBindings,
  type KeyboardInputState,
} from '../game/input/keyboardInput';
import {
  canPurchaseSkill,
  GREENWARD_SKILL_TREE,
  purchaseSkill,
} from '../game/progression/skillTree';
import {
  createInitialMenuState,
  selectGameMode,
  selectPlayMode,
  type GameMode,
  type MenuState,
  type PlayMode,
} from './menuState';
import { createRoomCode } from '../game/network/supabaseRealtimeBridge';

const BINDINGS_STORAGE_KEY = 'td-annihilation.keyboard-bindings.v1';

export type GameSelection = {
  gameMode: GameMode;
  playMode: PlayMode;
  roomCode?: string;
  password?: string;
};

type AppShellCallbacks = {
  onStartGame(selection: GameSelection): void;
  onFriendLobbyReady?(element: HTMLElement, getSession: () => { roomCode: string; password: string }): (() => void) | void;
};

type AppShell = {
  dispose(): void;
};

function isKeyboardBindings(value: unknown): value is KeyboardBindings {
  if (!value || typeof value !== 'object') {
    return false;
  }
  const candidate = value as Record<string, unknown>;
  return ['up', 'down', 'left', 'right'].every((key) => typeof candidate[key] === 'string' && candidate[key] !== '');
}

export function loadStoredKeyboardBindings(storage: Storage | undefined = window.localStorage): KeyboardBindings {
  try {
    const saved = storage.getItem(BINDINGS_STORAGE_KEY);
    const parsed: unknown = saved ? JSON.parse(saved) : null;
    return isKeyboardBindings(parsed) ? parsed : { ...DEFAULT_KEYBOARD_BINDINGS };
  } catch {
    return { ...DEFAULT_KEYBOARD_BINDINGS };
  }
}

function saveKeyboardBindings(bindings: KeyboardBindings, storage: Storage | undefined = window.localStorage): void {
  try {
    storage.setItem(BINDINGS_STORAGE_KEY, JSON.stringify(bindings));
  } catch {
    // Settings remain usable when localStorage is unavailable (for example in private mode).
  }
}

function modeLabel(mode: GameMode | null): string {
  return mode === 'friend' ? 'PLAY WITH A FRIEND' : 'SOLO';
}

function playModeLabel(mode: PlayMode | null): string {
  return mode === 'endless' ? 'ENDLESS' : 'STAGES';
}

function skillNodePosition(depth: number, angleDegrees: number): { x: number; y: number } {
  if (depth === 0) {
    return { x: 50, y: 50 };
  }
  const radius = depth === 1 ? 25 : 40;
  const angle = (angleDegrees * Math.PI) / 180;
  return {
    x: 50 + Math.cos(angle) * radius,
    y: 50 + Math.sin(angle) * radius,
  };
}

export function createAppShell(
  root: HTMLElement,
  keyboard: KeyboardInputState,
  callbacks: AppShellCallbacks,
): AppShell {
  const layer = document.createElement('div');
  layer.className = 'menu-layer';
  root.append(layer);

  let menuState: MenuState = createInitialMenuState();
  let bindings = keyboard.getBindings();
  let capturingBinding: keyof KeyboardBindings | null = null;
  let captureHandler: ((event: KeyboardEvent) => void) | null = null;
  let skillPoints = 5;
  let purchasedSkills = new Set<string>();
  let skillFeedback = 'Choose a node to begin your path.';
  let friendRoomCode = createRoomCode();
  let friendPassword = '';
  let friendLobbyCleanup: (() => void) | undefined;
  const escapeAttribute = (value: string): string => value.replace(/&/g, '&amp;').replace(/"/g, '&quot;').replace(/</g, '&lt;');

  const stopCapture = (): void => {
    if (captureHandler) {
      window.removeEventListener('keydown', captureHandler);
      captureHandler = null;
    }
    capturingBinding = null;
  };

  const renderMain = (): string => `
    <section class="menu-panel menu-main" aria-labelledby="main-menu-title">
      <p class="menu-kicker">THE GREENWARD // v1 FOUNDATION</p>
      <h1 id="main-menu-title">TD ANNIHILATION</h1>
      <p class="menu-lede">Defend the royal heart. Shape the battlefield. Hold the line.</p>
      <div class="menu-rule" aria-hidden="true"></div>
      <p class="menu-section-label">CHOOSE YOUR PATH</p>
      <div class="mode-grid">
        <button class="mode-card ${menuState.gameMode === 'solo' ? 'is-selected' : ''}" data-mode="solo" type="button">
          <span class="mode-card-icon" aria-hidden="true">♜</span>
          <span class="mode-card-title">SOLO</span>
          <span class="mode-card-copy">Command the Greenward alone.</span>
        </button>
        <button class="mode-card ${menuState.gameMode === 'friend' ? 'is-selected' : ''}" data-mode="friend" type="button">
          <span class="mode-card-icon" aria-hidden="true">♜♜</span>
          <span class="mode-card-title">PLAY WITH A FRIEND</span>
          <span class="mode-card-copy">Find open TD sessions automatically, then defend together. No account required.</span>
        </button>
      </div>
      <div class="menu-actions">
        <button class="menu-link" data-screen="settings" type="button">SETTINGS</button>
        <button class="menu-link" data-screen="skill-tree" type="button">SKILL TREE</button>
      </div>
      <p class="menu-footnote">A strategic action tower defense built for the long war.</p>
    </section>
  `;

  const renderPlayMode = (): string => `
    <section class="menu-panel menu-mode" aria-labelledby="play-mode-title">
      <button class="back-link" data-screen="main" type="button">← BACK</button>
      <p class="menu-kicker">${modeLabel(menuState.gameMode)} // GREENWARD</p>
      <h2 id="play-mode-title">SELECT YOUR CAMPAIGN</h2>
      <p class="menu-lede">Choose how the first defense will unfold.</p>
      <div class="play-mode-grid">
        <button class="play-mode-card ${menuState.playMode === 'stages' ? 'is-selected' : ''}" data-play-mode="stages" type="button">
          <span class="play-mode-title">STAGES</span>
          <span class="play-mode-copy">Handmade maps, escalating waves and first-win rewards.</span>
        </button>
        <button class="play-mode-card ${menuState.playMode === 'endless' ? 'is-selected' : ''}" data-play-mode="endless" type="button">
          <span class="play-mode-title">ENDLESS</span>
          <span class="play-mode-copy">Hold the Greenward as long as your defenses survive.</span>
        </button>
      </div>
      ${menuState.gameMode === 'friend' ? `
        <div class="friend-room-panel">
          <label for="friend-room-code">ROOM CODE</label>
          <input id="friend-room-code" data-room-code value="${friendRoomCode}" maxlength="32" autocomplete="off" spellcheck="false" />
          <label for="friend-session-password">SESSION PASSWORD <span>(OPTIONAL)</span></label>
          <input id="friend-session-password" data-session-password type="password" value="${escapeAttribute(friendPassword)}" maxlength="64" autocomplete="new-password" />
          <small>Nearby TD sessions appear automatically. Use the same room code and password to join. No account or login is required.</small>
          <div class="friend-lobby" data-friend-lobby aria-live="polite">SEARCHING FOR TD FRIENDS…</div>
        </div>
      ` : ''}
      <button class="primary-button ${menuState.playMode ? '' : 'is-disabled'}" data-start-game type="button" ${menuState.playMode ? '' : 'disabled'}>
        ENTER THE GREENWARD <span aria-hidden="true">→</span>
      </button>
      ${menuState.gameMode === 'friend' ? '<p class="mode-disclaimer">FRIEND MODE · SUPABASE REALTIME BRIDGE · NO LOGIN</p>' : ''}
    </section>
  `;

  const renderSettings = (): string => {
    const rows: Array<[keyof KeyboardBindings, string]> = [
      ['up', 'MOVE FORWARD'],
      ['down', 'MOVE BACK'],
      ['right', 'MOVE RIGHT'],
      ['left', 'MOVE LEFT'],
    ];
    return `
      <section class="menu-panel settings-panel" aria-labelledby="settings-title">
        <button class="back-link" data-screen="main" type="button">← BACK</button>
        <p class="menu-kicker">COMMAND CONFIGURATION</p>
        <h2 id="settings-title">SETTINGS</h2>
        <p class="menu-lede">Select a binding, then press any key.</p>
        <div class="settings-list">
          ${rows.map(([key, label]) => `
            <div class="settings-row">
              <span>${label}</span>
              <button class="binding-button ${capturingBinding === key ? 'is-capturing' : ''}" data-bind="${key}" type="button">
                ${capturingBinding === key ? 'PRESS KEY' : bindings[key].toUpperCase()}
              </button>
            </div>
          `).join('')}
        </div>
        <div class="settings-actions">
          <button class="secondary-button" data-reset-bindings type="button">RESTORE DEFAULTS</button>
          <p class="settings-note">Bindings save locally on this computer.</p>
        </div>
      </section>
    `;
  };

  const renderSkillTree = (): string => `
    <section class="menu-panel skill-panel" aria-labelledby="skill-tree-title">
      <button class="back-link" data-screen="main" type="button">← BACK</button>
      <div class="skill-heading">
        <div>
          <p class="menu-kicker">ROYAL PROGRESSION</p>
          <h2 id="skill-tree-title">SKILL TREE</h2>
        </div>
        <div class="skill-points"><span>${skillPoints}</span><small>POINTS</small></div>
      </div>
      <p class="menu-lede">The oath opens first. Power grows from the heart outward.</p>
      <div class="skill-tree" aria-label="Animated Greenward skill tree">
        <div class="skill-tree-ring skill-tree-ring-outer" aria-hidden="true"></div>
        <div class="skill-tree-ring skill-tree-ring-inner" aria-hidden="true"></div>
        ${GREENWARD_SKILL_TREE.map((node) => {
          const position = skillNodePosition(node.depth, node.angleDegrees);
          const purchased = purchasedSkills.has(node.id);
          const available = canPurchaseSkill(node, purchasedSkills, skillPoints);
          return `
            <button class="skill-node depth-${node.depth} ${purchased ? 'is-purchased' : ''} ${available ? 'is-available' : ''}" data-skill="${node.id}" style="--node-x:${position.x}%;--node-y:${position.y}%;--node-delay:${node.depth * 140}ms" type="button" aria-label="${node.title}, ${node.cost} skill points">
              <span class="skill-node-core">${purchased ? '✓' : node.depth === 0 ? '✦' : '◇'}</span>
              <span class="skill-node-label">${node.title}</span>
              <span class="skill-node-cost">${node.cost} PT</span>
            </button>
          `;
        }).join('')}
      </div>
      <p class="skill-feedback" aria-live="polite">${skillFeedback}</p>
    </section>
  `;

  const bindEvents = (): void => {
    layer.querySelectorAll<HTMLButtonElement>('[data-mode]').forEach((button) => {
      button.addEventListener('click', () => {
        menuState = selectGameMode(menuState, button.dataset.mode as GameMode);
        render();
      });
    });
    layer.querySelectorAll<HTMLButtonElement>('[data-play-mode]').forEach((button) => {
      button.addEventListener('click', () => {
        menuState = selectPlayMode(menuState, button.dataset.playMode as PlayMode);
        render();
      });
    });
    layer.querySelector<HTMLInputElement>('[data-room-code]')?.addEventListener('input', (event) => {
      friendRoomCode = (event.target as HTMLInputElement).value.toUpperCase().replace(/[^A-Z0-9-]/g, '');
    });
    layer.querySelector<HTMLInputElement>('[data-session-password]')?.addEventListener('input', (event) => {
      friendPassword = (event.target as HTMLInputElement).value;
    });
    const lobbyElement = layer.querySelector<HTMLElement>('[data-friend-lobby]');
    if (lobbyElement && callbacks.onFriendLobbyReady) {
      friendLobbyCleanup?.();
      friendLobbyCleanup = callbacks.onFriendLobbyReady(lobbyElement, () => ({ roomCode: friendRoomCode, password: friendPassword })) ?? undefined;
      lobbyElement.addEventListener('click', (event) => {
        const target = (event.target as HTMLElement).closest<HTMLButtonElement>('[data-nearby-room]');
        if (!target?.dataset.nearbyRoom) return;
        friendRoomCode = target.dataset.nearbyRoom;
        const roomInput = layer.querySelector<HTMLInputElement>('[data-room-code]');
        if (roomInput) roomInput.value = friendRoomCode;
      });
    }
    layer.querySelectorAll<HTMLButtonElement>('[data-screen]').forEach((button) => {
      button.addEventListener('click', () => {
        stopCapture();
        menuState = { ...menuState, screen: button.dataset.screen as MenuState['screen'] };
        render();
      });
    });
    layer.querySelector<HTMLButtonElement>('[data-start-game]')?.addEventListener('click', () => {
      if (menuState.gameMode && menuState.playMode) {
        stopCapture();
        friendLobbyCleanup?.();
        friendLobbyCleanup = undefined;
        layer.classList.add('is-hidden');
        callbacks.onStartGame({
          gameMode: menuState.gameMode,
          playMode: menuState.playMode,
          roomCode: menuState.gameMode === 'friend' ? friendRoomCode : undefined,
          password: menuState.gameMode === 'friend' ? friendPassword : undefined,
        });
      }
    });
    layer.querySelectorAll<HTMLButtonElement>('[data-bind]').forEach((button) => {
      button.addEventListener('click', () => {
        stopCapture();
        capturingBinding = button.dataset.bind as keyof KeyboardBindings;
        captureHandler = (event: KeyboardEvent) => {
          event.preventDefault();
          if (event.key === 'Escape') {
            stopCapture();
            render();
            return;
          }
          bindings = { ...bindings, [capturingBinding!]: event.key.toLowerCase() };
          keyboard.setBindings(bindings);
          saveKeyboardBindings(bindings);
          stopCapture();
          render();
        };
        window.addEventListener('keydown', captureHandler);
        render();
      });
    });
    layer.querySelector<HTMLButtonElement>('[data-reset-bindings]')?.addEventListener('click', () => {
      bindings = { ...DEFAULT_KEYBOARD_BINDINGS };
      keyboard.setBindings(bindings);
      saveKeyboardBindings(bindings);
      skillFeedback = 'Movement controls restored to the Greenward defaults.';
      render();
    });
    layer.querySelectorAll<HTMLButtonElement>('[data-skill]').forEach((button) => {
      button.addEventListener('click', () => {
        const node = GREENWARD_SKILL_TREE.find((candidate) => candidate.id === button.dataset.skill);
        if (!node) return;
        const result = purchaseSkill(node.id, purchasedSkills, skillPoints);
        if (result.purchased.size === purchasedSkills.size) {
          skillFeedback = skillPoints < node.cost
            ? 'Not enough skill points yet.'
            : 'Unlock the inner path before reaching this node.';
        } else {
          purchasedSkills = result.purchased;
          skillPoints = result.remainingPoints;
          skillFeedback = `${node.title} awakened. The next ring is now listening.`;
        }
        render();
      });
    });
  };

  const render = (): void => {
    friendLobbyCleanup?.();
    friendLobbyCleanup = undefined;
    layer.innerHTML = menuState.screen === 'main'
      ? renderMain()
      : menuState.screen === 'play-mode'
        ? renderPlayMode()
        : menuState.screen === 'settings'
          ? renderSettings()
          : renderSkillTree();
    bindEvents();
  };

  render();

  return {
    dispose: () => {
      stopCapture();
      layer.remove();
    },
  };
}
