import { describe, expect, it } from 'vitest';
import { createRoomChannelName, createRoomCode } from './supabaseRealtimeBridge';

describe('Supabase co-op bridge boundaries', () => {
  it('uses a TD-specific channel namespace and normalizes room codes', () => {
    expect(createRoomChannelName(' ab12-cd ')).toBe('td-annihilation:v1:room:AB12-CD');
  });

  it('creates a six-character room code without ambiguous characters', () => {
    expect(createRoomCode(() => 0)).toBe('AAAAAA');
    expect(createRoomCode(() => 0.999999)).toBe('999999');
  });

  it('rejects broad or empty channel identifiers', () => {
    expect(() => createRoomChannelName('')).toThrow();
    expect(() => createRoomChannelName('other-project-table')).not.toThrow();
    expect(() => createRoomChannelName('room/with/slashes')).toThrow();
  });
});
