# TD Annihilation — Supabase co-op bridge decision

## Decision

Supabase is a transport bridge for two game clients, not the game's account, save-data or content backend. The game must work without a visible login flow.

The first co-op pass should use Supabase Realtime Broadcast on a dedicated TD Annihilation channel namespace. It must not create, read, update or delete any existing application table in the shared Supabase project.

Proposed channel shape:

```text
td-annihilation:v1:room:<high-entropy-room-code>
```

The room code is shared directly between the two players. It is not a database record and should expire when both clients leave or after a short idle timeout.

## Strict project isolation

- Never run `db push`, `db pull`, `apply_migration`, `execute_sql` or dashboard schema edits for the initial bridge.
- Never reuse an existing table, storage bucket, RPC function, trigger or Realtime Postgres Changes subscription from the shared project.
- Never expose a Supabase service-role or secret key in the game client.
- If a later feature genuinely needs persistence, create a dedicated TD-only namespace such as `td_annihilation` with explicit RLS, grants and a reviewed migration; do not place TD data beside unrelated public tables by accident.
- Every future Supabase operation must be read-only or demonstrably scoped to the TD namespace before it runs.

## Network payload boundary

Only deterministic gameplay intent/state belongs on the bridge:

- room lifecycle and ready state;
- player input with sequence/timestamp;
- authoritative stage/wave events;
- player and enemy transforms or compact snapshots;
- tower placement and combat outcomes;
- disconnect/reconnect markers.

The following stay local on each computer and are never replicated:

- blood splatter decals;
- dismemberment and gore variants;
- camera movement, screen shake and local particles;
- local audio and accessibility presentation;
- local-only settings and key bindings.

This keeps gore independent per player while keeping actual gameplay deterministic. A host-generated death/event id may still be transmitted as a compact gameplay event; each client can seed its own local gore presentation from that id without sending the gore result itself.

## No-login requirement

There is no email, password, OAuth or account screen. The UI should present only `Play with a Friend`, a host/join room-code flow and connection status.

For the prototype, a random room code plus a public Realtime Broadcast topic is the smallest no-login bridge. This is suitable for development only: anyone who discovers the topic could attempt to send messages. Before a Steam release, replace that trust model with an ephemeral, server-validated room credential (or an invisible anonymous Supabase session if that remains compatible with the no-login UX) and validate host/sequence rules server-side.

## Implementation order

1. Keep the current pure `NetworkTransport` interface and local simulation as the source of truth.
2. Add a Supabase client factory that requires only a public project URL and publishable/anon client key from environment variables.
3. Add a Realtime Broadcast transport behind the interface, with the `td-annihilation:v1:room:` namespace and no database calls.
4. Add a local two-tab test harness before connecting the production menu.
5. Add the authenticated/ephemeral room-hardening pass before advertising online co-op as Steam-ready.

## Acceptance checks

- Starting Solo makes zero Supabase requests.
- A Friend room uses only the dedicated TD Realtime channel.
- No migration or table changes appear in the shared project.
- Gore remains visible locally after a replicated death event but is absent from the network payload.
- Disconnecting one player disposes the channel cleanly and does not affect unrelated Supabase traffic.
