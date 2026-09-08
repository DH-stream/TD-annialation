/// <reference types="vite/client" />

import type { RealtimeChannel, SupabaseClient } from '@supabase/supabase-js';
import type { GameState, NearbyPeer, NetworkTransport, PlayerInput } from '../sim/types';

const CHANNEL_PREFIX = 'td-annihilation:v1:room:';
const LOBBY_CHANNEL = 'td-annihilation:v1:lobby';
const INPUT_EVENT = 'player-input';
const STATE_EVENT = 'game-state';

export type SupabaseBridgeConfig = {
  roomCode: string;
  playerId: string;
  displayName?: string;
  password?: string;
  supabaseUrl?: string;
  publishableKey?: string;
};

export type LobbyPresence = {
  connect(): Promise<void>;
  disconnect(): Promise<void>;
  onPeers(listener: (peers: NearbyPeer[]) => void): () => void;
  onError(listener: (error: Error) => void): () => void;
};

export function createRoomChannelName(roomCode: string): string {
  const normalizedRoomCode = roomCode.trim().toUpperCase();
  if (!/^[A-Z0-9-]{4,32}$/.test(normalizedRoomCode)) {
    throw new Error('Room code must be 4–32 letters, numbers or hyphens.');
  }
  return `${CHANNEL_PREFIX}${normalizedRoomCode}`;
}

export function createRoomCode(random = Math.random): string {
  const alphabet = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
  let code = '';
  for (let index = 0; index < 6; index += 1) {
    code += alphabet[Math.floor(random() * alphabet.length)];
  }
  return code;
}

export async function createProtectedRoomChannelName(roomCode: string, password = ''): Promise<string> {
  const normalizedRoomCode = roomCode.trim().toUpperCase();
  if (!/^[A-Z0-9-]{4,32}$/.test(normalizedRoomCode)) {
    throw new Error('Room code must be 4–32 letters, numbers or hyphens.');
  }
  if (!password) return createRoomChannelName(normalizedRoomCode);
  const digest = await crypto.subtle.digest(
    'SHA-256',
    new TextEncoder().encode(`td-annihilation:v1:${normalizedRoomCode}:${password}`),
  );
  const suffix = Array.from(new Uint8Array(digest), (byte) => byte.toString(16).padStart(2, '0')).join('').slice(0, 24);
  return `${CHANNEL_PREFIX}protected:${suffix}`;
}

function isPresenceRecord(value: unknown): value is Record<string, unknown> {
  return Boolean(value && typeof value === 'object' && !Array.isArray(value));
}

export function normalizePresenceState(state: Record<string, unknown>, localPlayerId: string): NearbyPeer[] {
  return Object.entries(state).flatMap(([presenceKey, values]) => {
    const presence = Array.isArray(values) ? values.find(isPresenceRecord) : null;
    if (presence?.game !== 'td-annihilation' || presence.protocol !== 'v1') return [];
    const playerId = typeof presence.playerId === 'string' ? presence.playerId : presenceKey;
    if (playerId === localPlayerId) return [];
    return [{
      playerId,
      displayName: typeof presence.displayName === 'string' ? presence.displayName : 'Greenward Player',
      roomCode: typeof presence.roomCode === 'string' ? presence.roomCode : '------',
      passwordProtected: presence.passwordProtected === true,
      game: 'td-annihilation' as const,
      protocol: 'v1' as const,
    }];
  }).sort((left, right) => left.displayName.localeCompare(right.displayName));
}

function readConfig(config: SupabaseBridgeConfig): { url: string; key: string } {
  const url = config.supabaseUrl ?? import.meta.env.VITE_SUPABASE_URL;
  const key = config.publishableKey
    ?? import.meta.env.VITE_SUPABASE_PUBLISHABLE_KEY
    ?? import.meta.env.VITE_SUPABASE_ANON_KEY;
  if (!url || !key) {
    throw new Error('Supabase bridge is not configured. Set VITE_SUPABASE_URL and VITE_SUPABASE_PUBLISHABLE_KEY.');
  }
  return { url, key };
}

export function createSupabaseRealtimeBridge(config: SupabaseBridgeConfig): NetworkTransport {
  const listeners = new Set<(state: GameState) => void>();
  const inputListeners = new Set<(input: PlayerInput) => void>();
  const peerListeners = new Set<(peers: NearbyPeer[]) => void>();
  const errorListeners = new Set<(error: Error) => void>();
  let client: SupabaseClient | null = null;
  let channel: RealtimeChannel | null = null;
  let lobbyChannel: RealtimeChannel | null = null;

  const reportError = (error: unknown): void => {
    const normalized = error instanceof Error ? error : new Error(String(error));
    errorListeners.forEach((listener) => listener(normalized));
  };

  return {
    connect: async () => {
      if (channel) return;
      const { url, key } = readConfig(config);
      const { createClient } = await import('@supabase/supabase-js');
      client = createClient(url, key, {
        auth: {
          persistSession: false,
          autoRefreshToken: false,
          detectSessionInUrl: false,
        },
        realtime: { params: { eventsPerSecond: 30 } },
      });
      channel = client.channel(await createProtectedRoomChannelName(config.roomCode, config.password), {
        config: { private: false },
      });
      lobbyChannel = client.channel(LOBBY_CHANNEL, {
        config: { private: false, presence: { key: config.playerId } },
      });
      const emitPeers = (): void => {
        const state = lobbyChannel?.presenceState() as unknown as Record<string, unknown> | undefined;
        peerListeners.forEach((listener) => listener(normalizePresenceState(state ?? {}, config.playerId)));
      };
      lobbyChannel
        .on('presence', { event: 'sync' }, emitPeers)
        .on('presence', { event: 'join' }, emitPeers)
        .on('presence', { event: 'leave' }, emitPeers);
      channel
        .on('broadcast', { event: STATE_EVENT }, ({ payload }) => {
          if (payload && typeof payload === 'object') {
            listeners.forEach((listener) => listener(payload as GameState));
          }
        })
        .on('broadcast', { event: INPUT_EVENT }, ({ payload }) => {
          if (payload && typeof payload === 'object' && 'playerId' in payload) {
            inputListeners.forEach((listener) => listener(payload as PlayerInput));
          }
        })
        .on('broadcast', { event: 'error' }, ({ payload }) => {
          reportError(payload instanceof Error ? payload : new Error('Remote co-op bridge error.'));
        });

      const subscribe = (target: RealtimeChannel): Promise<void> => new Promise<void>((resolve, reject) => {
        target.subscribe((status) => {
          if (status === 'SUBSCRIBED') resolve();
          if (status === 'CHANNEL_ERROR' || status === 'TIMED_OUT') {
            reject(new Error(`Supabase Realtime subscription failed: ${status}`));
          }
        });
      });
      await Promise.all([subscribe(channel), subscribe(lobbyChannel)]).then(async () => {
        const result = await lobbyChannel?.track({
          playerId: config.playerId,
          displayName: config.displayName ?? 'Greenward Player',
          roomCode: config.roomCode.trim().toUpperCase(),
          passwordProtected: Boolean(config.password),
          game: 'td-annihilation',
          protocol: 'v1',
        });
        if (result !== 'ok') throw new Error(`Supabase Presence tracking failed: ${String(result)}`);
        emitPeers();
      }).catch((error: unknown) => {
        reportError(error);
        channel = null;
        lobbyChannel = null;
        client = null;
        throw error;
      });
    },
    disconnect: async () => {
      if (client && lobbyChannel) {
        await lobbyChannel.untrack();
      }
      if (client) {
        await Promise.all([channel, lobbyChannel].filter((item): item is RealtimeChannel => Boolean(item)).map((item) => client!.removeChannel(item)));
      }
      channel = null;
      lobbyChannel = null;
      client = null;
    },
    sendInput: (input: PlayerInput) => {
      if (!channel) return;
      // Gameplay intent only. Local blood, decals and dismemberment never cross this boundary.
      void channel.send({
        type: 'broadcast',
        event: INPUT_EVENT,
        payload: { ...input, playerId: config.playerId },
      }).catch(reportError);
    },
    sendState: (state: GameState) => {
      if (!channel) return;
      // Snapshot only. Presentation effects such as gore stay local to each client.
      void channel.send({
        type: 'broadcast',
        event: STATE_EVENT,
        payload: state,
      }).catch(reportError);
    },
    onState: (listener) => {
      listeners.add(listener);
      return () => listeners.delete(listener);
    },
    onInput: (listener) => {
      inputListeners.add(listener);
      return () => inputListeners.delete(listener);
    },
    onPeers: (listener) => {
      peerListeners.add(listener);
      return () => peerListeners.delete(listener);
    },
    onError: (listener) => {
      errorListeners.add(listener);
      return () => errorListeners.delete(listener);
    },
  };
}

export function createSupabaseLobbyPresence(config: Pick<SupabaseBridgeConfig, 'playerId' | 'displayName' | 'roomCode' | 'password' | 'supabaseUrl' | 'publishableKey'>): LobbyPresence {
  const peerListeners = new Set<(peers: NearbyPeer[]) => void>();
  const errorListeners = new Set<(error: Error) => void>();
  let client: SupabaseClient | null = null;
  let channel: RealtimeChannel | null = null;
  const reportError = (error: unknown): void => {
    const normalized = error instanceof Error ? error : new Error(String(error));
    errorListeners.forEach((listener) => listener(normalized));
  };
  return {
    connect: async () => {
      if (channel) return;
      const { url, key } = readConfig({
        roomCode: config.roomCode ?? 'LOBBY',
        playerId: config.playerId,
        displayName: config.displayName,
        password: config.password,
        supabaseUrl: config.supabaseUrl,
        publishableKey: config.publishableKey,
      });
      const { createClient } = await import('@supabase/supabase-js');
      client = createClient(url, key, { auth: { persistSession: false, autoRefreshToken: false, detectSessionInUrl: false } });
      channel = client.channel(LOBBY_CHANNEL, { config: { private: false, presence: { key: config.playerId } } });
      const emitPeers = (): void => {
        const state = channel?.presenceState() as unknown as Record<string, unknown> | undefined;
        peerListeners.forEach((listener) => listener(normalizePresenceState(state ?? {}, config.playerId)));
      };
      channel.on('presence', { event: 'sync' }, emitPeers).on('presence', { event: 'join' }, emitPeers).on('presence', { event: 'leave' }, emitPeers);
      await new Promise<void>((resolve, reject) => {
        channel?.subscribe((status) => {
          if (status === 'SUBSCRIBED') resolve();
          if (status === 'CHANNEL_ERROR' || status === 'TIMED_OUT') reject(new Error(`Supabase Realtime subscription failed: ${status}`));
        });
      });
      const result = await channel.track({
        playerId: config.playerId,
        displayName: config.displayName ?? 'Greenward Player',
        roomCode: config.roomCode?.trim().toUpperCase() || '------',
        passwordProtected: Boolean(config.password),
        game: 'td-annihilation',
        protocol: 'v1',
      });
      if (result !== 'ok') throw new Error(`Supabase Presence tracking failed: ${String(result)}`);
      emitPeers();
    },
    disconnect: async () => {
      if (client && channel) {
        await channel.untrack();
        await client.removeChannel(channel);
      }
      channel = null;
      client = null;
    },
    onPeers: (listener) => { peerListeners.add(listener); return () => peerListeners.delete(listener); },
    onError: (listener) => { errorListeners.add(listener); return () => errorListeners.delete(listener); },
  };
}
