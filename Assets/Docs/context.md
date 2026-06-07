# Dreamy Asset Context

This file is the working asset map for the Dreamy Unity prototype.

Primary design source:
- `C:/Users/taksi/Desktop/DREAMY Project/GameDesign.md`

Unity project root:
- `C:/Users/taksi/Documents/Unity Project 01`

## Runtime Prototype Usage

Current always-on prototype runtime:
- `Assets/Dreamy/Scripts/DreamyScenePrototypeRuntime.cs`
- Installs player stats, inventory, HUD, pickups, combat feedback, and prototype monsters in any playable scene.
- Loads `Resources/DreamyPrototypeVisualCatalog.asset` for common item sprites.
- Loads `Resources/DreamyMonsterCatalog.asset` for Enemy Pack monsters.
- Uses Tiny Swords UI sprites from the visual catalog for HP, stamina, EXP, action buttons, inventory button, and inventory slots.

Monster runtime:
- `Assets/Dreamy/Scripts/DreamyMonsterController.cs`
- Uses `DreamyMonsterDefinition` when available.
- Fallback is the previous red warrior visual catalog if the monster catalog has not been generated yet.
- Runtime prototype spawns up to 4 combat-ready monster definitions for testing.

## Prototype Life Systems

Runtime farming/crafting/vault prototype:
- `Assets/Dreamy/Scripts/DreamyPrototypeInteraction.cs`
- `Assets/Dreamy/Scripts/DreamyPrototypeInteractionUi.cs`
- `Assets/Dreamy/Scripts/DreamyPrototypeFarmPlot.cs`
- `Assets/Dreamy/Scripts/DreamyPrototypeCraftingStation.cs`
- `Assets/Dreamy/Scripts/DreamyPrototypeVaultNpc.cs`
- `Assets/Dreamy/Scripts/DreamyItemStack.cs`

Runtime behavior:
- `DreamyScenePrototypeRuntime` adds `DreamyPrototypeInteraction` to the player.
- It creates 4 farm plots, 1 crafting station, and 1 Vault Keeper NPC near the player when the scene starts.
- Press `E` or tap the `USE` button near an interactable to use it.
- Farm flow is seed -> water -> grow timer -> harvest crop into `DreamyInventory`.
- Crafting UI currently includes prototype recipes for seed packs, garden meals, and field tools.
- Vault UI transfers item stacks between player inventory and the NPC storage inventory.
- The prototype grants starter Seed, Wood, Food, and Gold only when the player has none of each so the loop can be tested immediately.

Item ids added for this prototype:
- `Seed`
- `Crop`
- `CraftedMeal`
- `CraftedTool`

Asset usage:
- The prototype reuses Tiny Swords UI slot/panel/button sprites from `DreamyPrototypeVisualCatalog`.
- Farm/crop visuals use existing Wood/Food/UI sprites where available and procedural fallback sprites otherwise.
- The Vault Keeper currently uses the visual catalog enemy idle sheet as placeholder NPC art.

Monster data:
- `Assets/Dreamy/Scripts/DreamyMonsterDefinition.cs`
- `Assets/Dreamy/Scripts/DreamyMonsterCatalog.cs`
- Generated assets target: `Assets/Dreamy/Generated/MonsterDefinitions`
- Runtime catalog target: `Assets/Resources/DreamyMonsterCatalog.asset`
- Refresh menu: `Dreamy > Prototype > Refresh Monster Catalog`

## Tiny Swords Enemy Pack

Imported source:
- `C:/Users/taksi/Downloads/Tiny Swords (Enemy Pack).zip`

Imported destination:
- `Assets/Tiny Swords (Enemy Pack)`

Import notes:
- The zip contained macOS metadata. `__MACOSX` and `.DS_Store` entries were skipped for this import.
- The pack contains 123 PNG files and 28 Aseprite source files.
- Enemy animations are horizontal sprite sheets.
- The import builder sets PNG sheets to Sprite, Single mode, Point filter, no mipmaps, uncompressed, clamp wrap, max texture size 8192, and 192 pixels per unit.

Main folders:
- `Enemy Pack/Enemy Avatars`: 18 portrait/icon images for enemy UI, selection cards, codex entries, or monster panels.
- `Enemy Pack/Enemies`: animated enemies, enemy props, projectiles, and faction objects.

Generated monster definition rules:
- A folder becomes a `DreamyMonsterDefinition` when it contains an `*_Idle.png` sheet.
- It is combat-ready when it also has a movement sheet (`*_Run.png` or `*_Walk.png`) and an attack sheet (`*_Attack*.png`, `*_Throw.png`, or `*_Shoot.png`).
- `*_Hit.png` and `*_Dead.png` / `*_Death.png` are stored for future hit/death animation support.
- Names and ids are derived from folders and filenames, not gameplay hardcoding.

Combat-ready Enemy Pack actors:
- Bear: Idle, Run, Attack
- Bomb Fish: Idle, Run, Shoot
- Gnoll: Idle, Walk, Throw, Hit, Bone projectile
- Gnome: Idle, Run, Attack
- Harpoon Shark: Idle, Run, Throw, Harpoon projectile
- Hex Shaman: Idle, Run, Attack, Projectile, Explosion, Explosion Spell, Transformation Spell
- Lizard: Idle, Run, Attack, Hit
- Minotaur: Idle, Walk, Attack, Guard
- Paddle Shark: Idle, Run, Row, Attack
- Panda: Idle, Run, Attack, Guard
- Pig Rider Spear Goblin: Idle, Run, Attack
- Skull: Idle, Run, Attack, Guard
- Snake: Idle, Run, Attack
- Spear Goblin: Idle, Run, Attack Fast, Attack Strong
- Spider: Idle, Run, Attack
- Thief: Idle, Run, Attack
- Torch Goblin: Idle, Run, Attack
- Troll: Idle, Walk, Attack, Windup, Recovery, Dead, Club parts
- Turtle: Idle, Walk, Attack, Guard In, Guard Out

Enemy Pack elements and props:
- Boat: animated idle enemy boat/prop.
- Bomb: idle, fuse lit, spinning.
- Cave: idle cave spawner/prop.
- Fish Hut: static or animated enemy building.
- Goblin Hut: static or animated enemy building.
- Pig: idle and run mount/ambient actor without attack sheet.
- Seahorse Boat: boat variants for Bomb Fish, Harpoon Shark, and Paddle Shark.
- Cannon: directional cannon sprites and cannon ball projectile.
- Root Troll folder: dead tree, skull spikes, and bone props.
- Wooden Fence: 64x64 tile prop.

## Existing Tiny Swords Assets

Free Pack path:
- `Assets/Tiny Swords (Free Pack)`

Free Pack folders:
- `Buildings`: faction-colored buildings such as castles, towers, barracks, houses, monastery, archery.
- `Particle FX`: effects from the original free pack.
- `Terrain`: terrain tiles and resource objects.
- `UI Elements`: older UI pieces.
- `Units`: colored unit sets, including warrior and monk animation sheets.

Update 010 path:
- `Assets/Tiny Swords (Update 010)`

Update 010 folders:
- `Deco`: decorative world objects.
- `Effects`: animated effects such as fire and related VFX.
- `Factions`: larger faction/unit/building content.
- `Resources`: resource object sprites.
- `Terrain`: additional terrain and tile assets.
- `UI`: 188 UI files including buttons, banners, ribbons, pointers, and regular/pressed/disabled icon states.

Useful UI paths:
- `Assets/Tiny Swords (Update 010)/UI/Buttons`
- `Assets/Tiny Swords (Update 010)/UI/Banners`
- `Assets/Tiny Swords (Update 010)/UI/Ribbons`
- `Assets/Tiny Swords (Update 010)/UI/Pointers`
- `Assets/Tiny Swords (Update 010)/UI/Icons`
- `Assets/Tiny Swords (Free Pack)/UI Elements/UI Elements/Bars`
- `Assets/Tiny Swords (Free Pack)/UI Elements/UI Elements/Buttons`
- `Assets/Tiny Swords (Free Pack)/UI Elements/UI Elements/Icons`
- `Assets/Tiny Swords (Free Pack)/UI Elements/UI Elements/Papers`
- `Assets/Tiny Swords (Free Pack)/UI Elements/UI Elements/Wood Table`

Runtime HUD UI bindings:
- HP, stamina, and EXP backgrounds use `BigBar_Base.png`.
- HP, stamina, and EXP fills use `BigBar_Fill.png`; HP stays red, stamina is tinted green, EXP is tinted blue.
- Attack uses `SmallRedRoundButton_Regular/Pressed.png` plus `Icon_05.png`.
- Dodge/Roll uses `SmallBlueRoundButton_Regular/Pressed.png` plus `Icon_06.png`.
- Inventory uses `SmallBlueRoundButton_Regular/Pressed.png` plus `Icon_11.png`.
- Inventory panel/slots use `RegularPaper.png` and `WoodTable_Slots.png`.

Useful VFX paths:
- `Assets/Tiny Swords (Update 010)/Effects`
- `Assets/Tiny Swords (Free Pack)/Particle FX`
- Unit-specific heal effects exist under each colored Monk folder in the Free Pack.

## Map And Tile Assets

Spritefusion:
- `Assets/Spritefusion`
- Contains imported Sprite Fusion map data/assets from earlier map work.
- The Spritefusion import currently includes many `.asset` files, one prefab, one PNG, and helper script metadata.

Dreamy maps:
- `Assets/Dreamy/Maps`
- `Assets/Dreamy/Scenes`
- Current prototype scenes and generated map images live here.

Generated art:
- `Assets/Dreamy/Generated`
- Contains generated pawn frames, simple generated tiles, PixelLab map concepts, and extraction prototype ScriptableObject data.

## Practical Use Guidelines

For monster testing:
- Open or run `Assets/Dreamy/Scenes/DreamyMobilePrototype.unity`.
- Let Unity refresh scripts/assets.
- If the catalog is missing, use `Dreamy > Prototype > Refresh Monster Catalog`.
- Press Play. The runtime should spawn several catalog monsters near the player.

For adding new enemy art:
- Put horizontal sprite sheets under a named enemy folder.
- Use suffixes like `_Idle`, `_Run`, `_Walk`, `_Attack`, `_Throw`, `_Shoot`, `_Hit`, `_Dead`.
- Run `Dreamy > Prototype > Refresh Monster Catalog`.
- Tune HP, damage, speed, level, and resistance on the generated `DreamyMonsterDefinition` asset instead of editing gameplay code.

For UI:
- Prefer Update 010 UI assets for production-like buttons, banners, icons, and stateful button sprites.
- Keep current prototype HUD simple until the gameplay loop is stable.

For effects:
- Use Update 010 effects and Free Pack particle/effect sprites for impact, fire, healing, pickup, and environmental ambience.
- Keep effect logic separate from damage/loot logic so visual polish can iterate without changing combat rules.
