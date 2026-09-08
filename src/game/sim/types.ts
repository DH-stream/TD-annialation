export type GamePhase = 'foundation' | 'playing' | 'victory' | 'defeat';

export type PlayerInput = {
  playerId: string;
  moveX: number;
  moveZ: number;
  basicAttack: boolean;
  specialAttack: boolean;
  issuedAt: number;
};

export type GameState = {
  phase: GamePhase;
  stageId: string;
  cameraTarget: { x: number; y: number; z: number };
  players: Record<string, { x: number; y: number; z: number; health: number }>;
};

export type NearbyPeer = {
  playerId: string;
  displayName: string;
  roomCode: string;
  passwordProtected: boolean;
  game: 'td-annihilation';
  protocol: 'v1';
};

export type NetworkTransport = {
  connect(): Promise<void>;
  disconnect(): Promise<void>;
  sendInput(input: PlayerInput): void;
  sendState(state: GameState): void;
  onState(listener: (state: GameState) => void): () => void;
  onInput(listener: (input: PlayerInput) => void): () => void;
  onPeers(listener: (peers: NearbyPeer[]) => void): () => void;
  onError(listener: (error: Error) => void): () => void;
};
