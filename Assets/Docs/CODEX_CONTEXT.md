# CODEX_CONTEXT.md

Primary design source for this prototype:

`C:/Users/taksi/Desktop/DREAMY Project/GameDesign.md`

Codex working agreement:

`AGENTS.md`

Asset context:

`Assets/Docs/context.md`

The current prototype target is a Unity 2D mobile room-based Extraction Survival RPG.

Core rules:
- Build MVP systems first, not production-complete systems.
- Keep gameplay data-driven with ScriptableObject definitions.
- Do not hardcode item, weapon, room, or monster names into gameplay logic.
- Keep gameplay logic separate from UI logic.
- Use placeholder sprites, simple shapes, and debug UI until final assets exist.
- Use mobile-friendly input abstraction while keeping keyboard testing in the editor.
- Run inventory is temporary; base storage is persistent.
- Extract transfers run inventory to base storage.
- Death creates lost loot and keeps base storage safe.
- Room progression offers 2-3 choices with risk/reward preview.
- Boss room drops a map unlock item.
- Farming, market, NPC, quests, recovery, and base systems are scaffolds only for now.

Prototype scenes:
- `Assets/Dreamy/Scenes/Prototype/Prototype_Base.unity`
- `Assets/Dreamy/Scenes/Prototype/Prototype_Run.unity`
