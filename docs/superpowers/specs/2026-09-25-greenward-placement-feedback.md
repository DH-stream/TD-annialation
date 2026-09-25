# Greenward Placement Feedback Spec

## Goal

Make tower placement trustworthy and tactile: the cursor must sample the actual Greenward ground, valid meadow must read as valid, grass must remain visible in gameplay, and a new tower must feel like it rises from the soil.

## Placement Rules

- Sample the scene ground with a raycast first; use the existing Greenward height function only as a fallback for scripted previews.
- Meadow is buildable unless occupied by a placed tower or an explicit gameplay blocker.
- Road, riverbank, corruption and castle remain invalid.
- World decoration, grass, road stones and non-blocking scenery must not invalidate a site.
- Valid and invalid ghost states must be visibly distinct and use the existing green/red material approach.

## Tower Arrival

- Spawn the tower slightly buried and compressed.
- Animate to full height in roughly 0.35 seconds with a quick overshoot and settle.
- Emit a short warm earth/dust burst at the base; it must stop and destroy itself automatically.
- Do not change tower combat, targeting, cost or placement state.

## Grass

- Do not toggle baked grass off when gameplay begins.
- Preserve the active serialized grass batches.
- Add no runtime rebuild or duplicate scattering unless Play Mode verification proves the baked content is absent.

## Acceptance Criteria

- A clean meadow point outside the explicit forbidden regions produces a green ghost and can be placed.
- A road or castle point remains invalid.
- Placed towers still fire normally after the arrival animation.
- Grass remains visible in Play Mode wherever it is visible in the authored scene.
