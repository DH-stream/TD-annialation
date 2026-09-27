# Skull progression and skill effects

## Goal

Make the existing Greenward skill-tree preview a persistent single-player meta-progression system: completed runs grant skulls, players spend them on nodes, and purchased nodes change actual gameplay stats. Keep the current visual tree and its 14 catalog entries; no scene or prefab edits are required.

## Player flow

1. Starting a run loads the saved skill purchases and applies their effects to the new run.
2. A full victory grants a configurable skull amount once; defeat grants none. Returning to or reopening the result screen cannot grant a second reward.
3. The result screen reports the reward and updated skull balance.
4. In the skill tree, a node displays its cost and effect. The player can purchase an affordable, prerequisite-valid node once. The balance and purchase remain saved after restart.

## Progression and balance

- Default reward: 3 skulls per full clear, exposed as a serialized setting on the run bootstrap so it can be rebalanced in the Inspector.
- Existing root nodes cost 1 skull; prerequisite nodes cost 2. Cost and effect magnitude live alongside each node definition for straightforward tuning.
- Persist the skull balance and unlocked node IDs locally using Unity `PlayerPrefs`; save the balance and unlock together on purchase. Ignore unknown saved node IDs and clamp a missing/negative balance to zero.
- Award only on the transition into final Victory for a run, not for intermediate waves, defeat, or repeated result-screen refreshes.

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
- Keep this single-player and local-save only. No cloud sync, profile selection, refunds, respec, multiplayer authority, new skill branches, or scene/prefab redesign in this pass.

## Failure and safety behavior

- Reject unknown nodes, repeat purchases, unmet prerequisites, and purchases without enough skulls without spending currency.
- Persist rewards only once per run; persist purchases immediately so a quit after buying does not lose the transaction.
- Existing saves with no progression keys start at zero skulls and no unlocked nodes.

## Verification

- EditMode tests cover purchase validation, prerequisite/cost handling, serialization round-trip/defaults, victory-only one-time rewards, and effect aggregation.
- Unity compilation and the full EditMode suite must pass.
- Play Mode smoke test: complete a run, confirm exactly one reward, buy a node, restart the project, confirm balance/unlock persist, then verify the corresponding stat changes on a fresh run.
