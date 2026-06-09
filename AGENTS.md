# AGENTS.md

## Project Role

This repository is the Unity project for Dreamy, a 2D mobile-first, landscape, room-based Extraction Survival RPG prototype.

Use this file as the working agreement for Codex. Game design details belong in `Assets/Docs/CODEX_CONTEXT.md` and asset/system details belong in `Assets/Docs/context.md`.

## Required Context

- For every Dreamy gameplay, Unity, scene, asset, or prototype task, read `Assets/Docs/CODEX_CONTEXT.md` first.
- If the task touches assets, monsters, UI, farming, crafting, inventory, NPCs, or prototype runtime behavior, also read `Assets/Docs/context.md`.
- Treat files in the repository and current Unity scenes as the source of truth. Do not rely on old chat history when the files disagree.
- If a required design detail is missing, make a conservative prototype assumption and document it in the relevant docs file when useful.

## Documentation Maintenance

- Update `Assets/Docs/CODEX_CONTEXT.md` when the game direction, prototype scope, core rules, or primary scenes change.
- Update `Assets/Docs/context.md` when adding, importing, generating, or reassigning assets, monsters, UI elements, runtime systems, item ids, or prototype test flows.
- Update this `AGENTS.md` only when the working rules for Codex should change.
- Do not rewrite these docs for tiny visual tuning unless the change affects future implementation decisions.

## Unity Safety

- Do not edit `Library/`, `Temp/`, `Logs/`, `UserSettings/`, or other Unity-generated cache folders.
- Do not mass-edit `.unity`, `.prefab`, `.asset`, or `.meta` files unless the task requires it.
- Before changing a scene, confirm which scene is being changed.
- Keep edits scoped. Avoid unrelated refactors, metadata churn, and broad scene rewrites.
- When importing or generating assets, avoid staging unrelated temp folders, nested repositories, or reference files at the repo root.

## Prototype Direction

- Build playable MVP systems first, not production-complete systems.
- Farming, market, NPC, quest, recovery, base building, crafting, and repair systems should stay as scaffolds unless explicitly requested.
- Prefer placeholder sprites/UI when final assets are not ready.
- Keep features easy to test in Unity Play Mode.

## Data And Architecture

- Keep gameplay data-driven with ScriptableObject definitions where practical.
- Do not hardcode item names, monster names, weapon names, room names, or map names into gameplay logic.
- Keep gameplay logic separate from UI logic. UI should read and display state, not own core rules.
- Keep systems modular and compile-safe.
- Use clear runtime components instead of one large manager when ownership is naturally separate.

## Mobile First

- Target mobile landscape first.
- Support touch/mobile input, while keeping keyboard and mouse testing available in the Unity Editor.
- Keep UI controls compact and readable on a 16:9 landscape mobile viewport.
- Avoid UI that blocks combat, movement, inventory, or interaction feedback.

## Combat And Physics

- Player and monster bodies should not push each other through normal Rigidbody collisions unless explicitly designed.
- Prefer trigger-based attack ranges, damage zones, and controlled movement logic for prototype combat.
- Use Y-position sorting for characters, monsters, NPCs, and interactables so front objects visually cover objects behind them.
- Keep hit feedback readable: damage numbers, hit flash/effects, knockback/resistance, and health bars should be visible but not noisy.

## Asset Rules

- Use existing project assets before generating or importing new ones.
- If an asset is imported or generated, update `Assets/Docs/context.md` with what it is and how it is used.
- Pixel art sprites should use import settings that preserve crisp pixels unless the asset is intentionally high-resolution painted art.
- Do not assume an asset pack's structure. Inspect folders, spritesheets, animations, and metadata first.

## Verification

- After code changes, make the project compile-safe.
- If Unity or MCP cannot verify Play Mode behavior, say so and report the strongest verification that was possible.
- For gameplay changes, explain how to test the behavior in Unity.
- For UI or visual changes, prefer checking the actual Game view or screenshot when possible.

## Git

- Run `git status` before staging, committing, or pushing.
- Do not commit or push unless the user explicitly asks.
- Do not revert user changes unless the user explicitly asks.
- Keep commits focused on the requested task.

## Token And Context Discipline

- Do not scan the whole project when a task is narrow.
- Prefer `rg` and targeted file reads.
- Limit long command output to the smallest useful range.
- For new unrelated tasks, prefer a new thread that starts from `AGENTS.md`, `Assets/Docs/CODEX_CONTEXT.md`, and `Assets/Docs/context.md`.
