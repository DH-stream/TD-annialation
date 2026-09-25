# Greenward Steam UI/UX Spec

## Goal

Replace the current debug-like menu and HUD with a restrained, authored fantasy strategy interface that keeps the battlefield visible and communicates the current decision clearly.

## Direction

- Keep UI Toolkit and the existing menu flow.
- Use the authored Greenward world as the visual backdrop instead of a centered stack of empty framed boxes.
- Use deep forest green, warm parchment, aged brass and arcane violet as accents.
- Prefer asymmetrical composition, thin separators, icon-like visual anchors and purposeful density.
- No decorative pills, dashboard cards or full-width empty panels.
- Use short transitions: fade/slide for screens and a subtle pulse for the active build state.

## Menu

- Main menu: left navigation rail, title lockup, one primary Play action, quiet secondary actions, small location/build label.
- Stage select: vertical stage list with one active stage, locked stages treated as quieter content, and a right-side stage description.
- Skill tree: three authored columns with clear branch headers, node states and a compact detail pane.
- Result screen: strong victory/defeat title, one primary action and two secondary actions.

## Gameplay HUD

- Top-left: compact stage/resource strip for gold, lives and defeated count.
- Top-center: wave number, phase and one contextual instruction.
- Bottom-center: build/action deck with tower name, cost and input hint.
- Bottom-left: quiet minimap frame.
- Build controls only appear during Build phase; the battlefield remains unobstructed during Wave phase.
- Placement mode changes the deck state and instruction instead of adding another large panel.

## Acceptance Criteria

- No gameplay screenshot should show the current giant empty top boxes or empty action panel.
- Menu and HUD must retain all existing button names/callbacks and menu flow.
- The active information hierarchy is readable at the existing 1280x720 reference resolution.
- The UI remains functional without adding packages or requiring new art assets.
