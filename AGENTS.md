# TD Annihilation — Codex / Agent Rules

## Unity-native project
TD Annihilation is a native Unity game. Gameplay and runtime UI must use Unity-native systems and C#. Do not introduce browser/runtime web stacks, localhost servers, HTML, JavaScript, React, WebViews, or equivalent unless explicitly requested.

## Authoring source of truth
Artist- and designer-tunable presentation values belong in serialized Unity assets and scenes, not in runtime bootstrap code.

Use the Unity Editor / Unity MCP to author and persist:
- scene hierarchy and transforms
- Directional Lights and local lights
- skybox / environment lighting
- fog
- Global Volume and Volume Profiles
- post-processing
- Reflection Probes
- materials and shader parameters
- camera composition and FOV
- particle/VFX component settings
- prefab presentation settings
- UI layout/style assets

These values must remain directly editable in the Inspector after the agent is finished.

## Do not overwrite manual tuning at runtime
Do not set presentation values from Awake(), Start(), Update(), or bootstrap code when those values can be serialized in the scene or an asset.

Examples of values that must not be hard-coded at runtime:
- sun rotation, color or intensity
- ambient sky/equator/ground colors
- camera background/clear flags used for art direction
- fog color/density/distances
- bloom, contrast, saturation, vignette or tonemapping
- Reflection Probe placement/intensity
- static environment object transforms

Runtime code may modify these only when the gameplay itself requires a dynamic change (for example a day/night cycle, weather transition, damage effect, scripted event, or temporary combat feedback).

## Editor tools
Editor scripts may generate or initialize content when useful, but:
1. they must be explicitly invoked by the user,
2. the generated result must be serialized into the scene/prefab/asset,
3. re-running an unrelated bake must not reset manually tuned presentation values.

A world-geometry bake must not also reset lighting, camera, post-processing, UI styling, or other art-direction settings.

## Use Unity MCP for visual work
For visual/presentation tasks, inspect the actual scene in the Unity Editor first. Prefer changing scene objects and serialized assets through the editor/MCP over generating C# that reconstructs the same state at runtime.

After changes, verify the result through the real gameplay camera in Game view.

## Visual direction
Treat the existing TD Annihilation skill-tree direction as the UI/theme anchor:
- dark heroic/gothic fantasy
- forged black metal
- aged brass/gold trim
- heraldic/fortress motifs
- selective ember and magical glow
- strong silhouette and readability

Skill tree = ceremonial.
Main menu = atmospheric.
Gameplay HUD = tactical and compact.

Do not invent a separate visual language for individual screens or features.
