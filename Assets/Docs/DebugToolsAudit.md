# Dreamy Debug Tools Audit

Generated: 2026-06-09

## Current Debug Tools

- `Dreamy > Debug > Debug Hub`: central launchpad for audit checks, tool shortcuts, generated asset refreshes, docs, and report saving.
- `Dreamy > Debug > Combat Tuning`: edits A1, A2, A3, and Super Smash frame ranges, hit marker timing, hitboxes, damage/status, and optional VFX/action events.
- `Dreamy > Level Blocking Tool`: places and edits prototype blockers directly in the Scene view.
- `Dreamy > Prototype > Refresh Little Axion Animator`: rebuilds generated Axion clips/controller from the combat tuning profile.
- `Dreamy > Prototype > Refresh Runtime Visual Catalog`: rebuilds the runtime visual catalog for player, UI, items, and Axion references.
- `Dreamy > Prototype > Refresh Monster Catalog`: rebuilds generated monster definitions from the enemy pack.
- `Dreamy > Apply Mobile Landscape Layout`: reapplies mobile landscape project/player settings.

## Full Audit Summary

The debug foundation now covers the current combat tuning loop: profile editing, generated Axion animation clips, dummy hit logging, catalog refreshes, and scene blocker placement. The biggest remaining need is runtime visibility while Play Mode is running, especially for combo state, hitbox overlap, current animation marker, target count, and active status effects.

## Recommended Next Debug Tools

- Runtime overlay toggle for player stats, current combo step, active hitbox, target count, and cooldowns.
- Combat scenario runner that resets the dummy, places targets, and steps through the six-hit combo.
- VFX event library with named presets that can be attached to Combat Tuning events without code edits.
- Monster tuning window that mirrors player hit markers, ranges, damage, stagger, and movement speed.
- Mobile viewport checker for safe combat UI placement, touch button reach, and blocked-screen warnings.
- Economy and inventory debug tab for item grants, crafting inputs, storage, and extraction reward checks.

## How To Use

- Open `Dreamy > Debug > Debug Hub` first when tuning combat, animation, hitboxes, generated assets, or prototype scenes.
- Press `Run Full Audit` after importing assets or changing generated systems.
- Press `Save Audit Report` after a tuning pass so the next debugging pass starts from current evidence.
- Use `Combat Tuning` for animation frame ranges, hit markers, hitboxes, damage/status, and optional VFX/action events.
