# Greenward Villagers and Build Wheel Design

## Goal

Make Greenward villagers read as peaceful residents with purposeful routines, make the B key open the same tower picker as the HUD Build button, and prevent tower placement over a villager. Restyle the existing picker from the supplied reference while keeping its art and options editable in Unity.

## Existing flow

`GreenwardVillagerAgent` already selects role-weighted actions and walks to reserved POIs. Resident profiles currently favor wandering, and socialising does not form an intentional pair or turn toward a conversation partner. Villager locomotion uses the `GreenwardVillager` Animator Controller; the project also contains an unarmed Humanoid `walking.fbx` clip.

The Build HUD already has a UI Toolkit radial overlay, but it is a small fixed three-option layout. `TDVerticalSliceBootstrap` handles B by directly entering placement mode, selecting Arcane by default, while the HUD Build button opens the picker. `CanPlaceTower` checks the build surface and nearby towers but not villagers.

## Design

### Villager movement and interaction

- Preview the existing unarmed Humanoid walk clip on the current Greenward villagers and use it for the villager locomotion state if the retargeted pose fits. Preserve guard combat locomotion, flee behavior, walk speed, and root-motion policy.
- Keep the current POI-based routine. Reduce the resident wander weight and let workers/residents favor purposeful destinations such as work, conversation, observation, and sitting spots. Include an observation point near a visible bush so a villager can walk over and look around there.
- Make socialising pair nearby villagers at a conversation spot and orient them toward one another while they talk. Occasionally orient a nearby villager toward the hero and use the existing shared, rate-limited chatter bubble for a greeting.
- Keep role/action weights and reaction distance serialized and Inspector-tunable.

### Build wheel and scene authoring

- Keep the wheel in Unity UI Toolkit. Author its structure and style in the existing UXML/USS, matching the reference's dark metal, brass trim, central Build label, radial tower sectors, icon, price, and drag-to-select hint. Keep the UXML and USS as the visual source of truth and edit them directly; runtime code only binds data and input.
- Open the same HUD wheel from the Build button and B key. Remove the bootstrap's direct B-to-placement path so the key cannot silently choose Arcane.
- Put tower option data in a serialized catalog referenced by the scene's HUD. The data includes display name, icon, price, and availability/placeholder state; option sectors use a shared UXML template and USS classes so adding entries does not require manually duplicating wheel logic. Keep Arcane and Archer selectable. Add two generic locked empty slots with no tower names, theme art, costs, unlock levels, prefabs, or gameplay; the user has not chosen what belongs there. Preserve the existing Frost locked placeholder as-is.
- Author `TDVerticalSliceHUD` and `UIDocument` on the scene camera objects used by Greenward and Main Menu. Assign the existing PanelSettings, UXML, USS, and tower catalog through scene/Inspector references. Remove runtime creation of the HUD/document and fallback-generated panel settings so the scene stays the source of truth. Do not use background authoring scripts to generate or reset visual configuration. Future wheel layout and styling remain editable in UI Builder/USS, and future tower entries remain editable in the catalog Inspector.

### Placement occupancy

- Extend the existing placement validity check to reject a tower footprint overlapping a registered Greenward villager. Use the existing village agent registry and a single shared placement clearance, evaluated on the ground plane.
- An occupied location shows the existing invalid placement preview and cannot charge coins or create a tower. The player can wait for the villager to move and place there afterward.

## Boundaries

No Flame/Tesla tower identities or gameplay are introduced; their two sectors remain generic, unassigned locked slots. No replacement of the existing POI system, chatter system, attack systems, or scene art direction is planned. Existing local edits in the workspace must be preserved.

## Verification

In the Unity Editor, verify the serialized HUD/catalog references and Greenward scene. In Play Mode, check that the chosen walk reads as civilian, villagers visit POIs and converse/notice the hero at close range, B and the Build button open the same wheel, only Arcane and Archer can be selected, and a villager blocks placement until it leaves the tower footprint. Confirm blocked placement does not spend gold and check for compile errors.


