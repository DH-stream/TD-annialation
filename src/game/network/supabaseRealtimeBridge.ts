/// <reference types="vite/client" />

import type { RealtimeChannel, SupabaseClient } from '@supabase/supabase-js';
import type { GameState, NetworkTransport, PlayerInput } from '../sim/types';

const CHANNEL_PREFIX = 'td-annihilation:v1:room:';
const INPUT_EVENT = 'player-input';
const STATE_EVENT = 'game-state';

export type SupabaseBridgeConfig = {
  roomCode: string;
  playerId: string;
  supabaseUrl?: string;
  publishableKey?: string;
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
      channel = client.channel(createRoomChannelName(config.roomCode), {
        config: { private: false },
      });
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

      await new Promise<void>((resolve, reject) => {
        channel?.subscribe((status) => {
          if (status === 'SUBSCRIBED') resolve();
          if (status === 'CHANNEL_ERROR' || status === 'TIMED_OUT') {
            reject(new Error(`Supabase Realtime subscription failed: ${status}`));
          }
        });
      }).catch((error: unknown) => {
        reportError(error);
        channel = null;
        client = null;
        throw error;
      });
    },
    disconnect: async () => {
      if (client && channel) {
        await client.removeChannel(channel);
      }
      channel = null;
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
    onState: (listener) => {
      listeners.add(listener);
      return () => listeners.delete(listener);
    },
    onInput: (listener) => {
      inputListeners.add(listener);
      return () => inputListeners.delete(listener);
    },
    onError: (listener) => {
      errorListeners.add(listener);
      return () => errorListeners.delete(listener);
    },
  };
}
