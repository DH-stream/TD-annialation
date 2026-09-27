# Skull progression and skill effects

## Goal

Make the Greenward skill tree a persistent single-player meta-progression system: map clears and Endless waves grant skulls, players spend them on nodes, and purchased nodes change actual gameplay stats. Retain the 11 branch identities and 14 functional catalog nodes while rebuilding the graph presentation around the supplied visual reference.

## Player flow

1. Starting a run loads the saved skill purchases and applies their effects to the new run.
2. A first full clear of a map grants its configurable first-clear reward and unlocks Endless for that map on future visits. The player is not pushed into Endless immediately after winning.
3. Later full clears grant a smaller configurable repeat-clear reward. Endless becomes selectable the next time that already-cleared map is launched; Normal remains available.
4. Endless has no final wave. Each cleared wave grants a configurable reward that increases with Endless progress; completed-wave rewards are retained if the player later loses.
5. The result screen reports the current run's skull earnings and updated balance. Rewards cannot be duplicated by repeated result-screen refreshes.
6. In the skill tree, a node displays its cost and effect. The player can purchase an affordable, prerequisite-valid node once. The balance and purchase remain saved after restart.

## Progression and balance

- First-clear reward defaults to 10 skulls per map; repeat-clear reward defaults to 3. Both are editable balance settings.
- Endless reward defaults to 1 skull for the first cleared Endless wave, increasing by 1 per additional cleared Endless wave. Base and increment are editable balance settings.
- Existing root nodes cost 1 skull; prerequisite nodes cost 2. Cost and effect magnitude live alongside each node definition for straightforward tuning.
- Persist the skull balance and unlocked node IDs locally using Unity `PlayerPrefs`; save the balance and unlock together on purchase. Ignore unknown saved node IDs and clamp a missing/negative balance to zero.
- Persist per-map first-clear status. A map's first-clear reward is granted once; subsequent normal clears use the smaller repeat reward.
- Endless wave rewards are granted once on each completed-wave transition, never for a wave that was not cleared. Amount scales with that run's Endless wave number.
- Award each map's first-clear reward and unlock Endless at the normal map's final-wave victory. Offer the mode choice on a later visit to that map.

## Initial node effects

Apply saved effects when a new run starts; do not alter an already-running match. Values below are starting balance values and remain tunable in the catalog.

| Node | Initial effect |
| --- | --- |
| `bastion-foundation` | Archer tower damage +10% |
| `bastion-focus` | Archer tower range +10% |
| `attack-force` | Hero attack damage +10% |
| `arcana-spark` | Arcane tower damage +10% |
| `arcana-echo` | Arcane tower firing interval -10% |
| `economy-fortune` | Starting gold +20 |
| `utility-craft` | Tower build cost -5 gold |
| `support-rally` | Starting lives +2 |
| `crit-opportunity` | Hero hits gain 10% chance to deal double damage |
| `cooldown-tempo` | Hero attack cooldowns -10% |
| `range-sight` | Hero light/heavy attack range +10% |
| `elemental-ember` | Mega attack radius +10% |
| `warden-grit` | Starting lives +2 |
| `warden-edge` | Heavy attack damage multiplier +0.2 |

## Ownership and integration

- `TDSkillTreeCatalog` owns node cost, prerequisite and effect tuning; `TDSkillTreeState` owns current wallet/unlocks and purchase validation.
- The existing bootstrap owns per-run reward timing and applies progression when initializing a run. Existing hero/tower controllers remain the owners of runtime combat values.
- The HUD remains a view: it reads authoritative progression state, invokes a validated purchase, then refreshes the selected node, graph and skull total.
- Rebuild the tree as a full-screen dark-fantasy graph inspired by the supplied image: central fortress/core, multi-color luminous branches, connected circular nodes, and distinct purchased/available/locked/hidden states. Keep the 14 gameplay nodes interactive; extra visible nodes are clearly non-interactive future-path decoration.
- Use original or already-provided free assets only. Place text and controls above the decorative art so nodes and descriptions remain legible at 16:9.
- Keep this single-player and local-save only. No cloud sync, profile selection, refunds, respec, multiplayer authority, new skill branches, or unrelated scene/prefab redesign in this pass.

## Failure and safety behavior

- Reject unknown nodes, repeat purchases, unmet prerequisites, and purchases without enough skulls without spending currency.
- Persist first-clear and wave rewards exactly once; persist purchases immediately so quitting after buying does not lose the transaction.
- Existing saves with no progression keys start at zero skulls and no unlocked nodes.

## Verification

- EditMode tests cover purchase validation, prerequisite/cost handling, save round-trip/defaults, first-clear vs repeat rewards, per-wave Endless scaling/idempotence, and effect aggregation.
- Unity compilation and the full EditMode suite must pass.
- Play Mode smoke test: clear a map twice and verify first/repeat payouts; revisit and enter Endless, verify increasing payouts over several cleared waves and defeat handling; buy a node, restart, confirm persistence and stat effect. Visually compare the rebuilt tree with the supplied reference at 16:9.
