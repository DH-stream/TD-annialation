import { describe, expect, it } from 'vitest';
import { createProtectedRoomChannelName, createRoomChannelName, createRoomCode, normalizePresenceState } from './supabaseRealtimeBridge';

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

  it('keeps password-protected sessions off the readable room channel name', async () => {
    const protectedName = await createProtectedRoomChannelName(' ab12-cd ', 'shared-secret');
    expect(protectedName).toMatch(/^td-annihilation:v1:room:protected:[a-f0-9]{24}$/);
    expect(protectedName).not.toContain('shared-secret');
    expect(await createProtectedRoomChannelName('ab12-cd')).toBe(createRoomChannelName('ab12-cd'));
  });

  it('only surfaces TD v1 peers and never exposes the local player', () => {
    const peers = normalizePresenceState({
      local: [{ playerId: 'local', game: 'td-annihilation', protocol: 'v1', displayName: 'Me', roomCode: 'LOCAL' }],
      friend: [{ playerId: 'friend', game: 'td-annihilation', protocol: 'v1', displayName: 'Knight', roomCode: 'AB12CD', passwordProtected: true }],
      unrelated: [{ playerId: 'other-app', game: 'other-project', protocol: 'v1', displayName: 'Other' }],
    }, 'local');
    expect(peers).toEqual([{
      playerId: 'friend',
      displayName: 'Knight',
      roomCode: 'AB12CD',
      passwordProtected: true,
      game: 'td-annihilation',
      protocol: 'v1',
    }]);
  });
});
