# Dreamy Asset Context

This file is the working asset map for the Dreamy Unity prototype.

Primary design source:
- `C:/Users/taksi/Desktop/DREAMY Project/GameDesign.md`

Unity project root:
- `C:/Users/taksi/Documents/Unity Project 01`

## Runtime Prototype Usage

Current always-on prototype runtime:
- `Assets/Dreamy/Scripts/DreamyScenePrototypeRuntime.cs`
- Installs player stats, inventory, HUD, pickups, combat feedback, and a training dummy in any playable scene.
- Loads `Resources/DreamyPrototypeVisualCatalog.asset` for common item sprites.
- Loads `Resources/DreamyMonsterCatalog.asset` for Enemy Pack monsters.
- Uses Tiny Swords UI sprites from the visual catalog for HP, stamina, EXP, action buttons, inventory button, and inventory slots.
- Player attacks use a Unity-style one-shot animation flow: attack plays through before the next command starts, movement is briefly slowed, and damage resolves from a forward slash hitbox instead of a full circle around the player.
- Debug tools are centralized at `Dreamy > Debug > Debug Hub`. Use it first for full audit checks, one-click access to tuning/build tools, runtime player/dummy selection helpers, and saving `Assets/Docs/DebugToolsAudit.md` after a debugging pass.
- Combat tuning is data-driven through `Assets/Resources/DreamyCombatTuningProfile.asset`. Open `Dreamy > Debug > Combat Tuning` to adjust A1, A2, A3, and Super Smash frame ranges, hit marker timing, hitbox length/width/origin, damage/status values, and optional VFX/action events. The window includes `Save Profile`, `Apply To Live Player`, `Regenerate Animator`, player/dummy selection helpers, and a Scene view hitbox/event preview.
- Little Axion is the default player visual when the runtime visual catalog has the Luneblade Premium references. It uses the generated `LittleAxionPrototype.controller` for Idle, Run, Dash, Hurt, Attack 1, Attack 2, Attack 3, and Super Smash, with manual frame animation kept as fallback if the controller reference is missing. Normal attacks use `Attack 3.png` as the source combo sheet and slice it into A1, A2, and A3 sections as 9 + 6 + 8 frames. The runtime sequence is A1 -> A1 -> A2 -> A1 -> A2 -> A3, so the player needs 6 attack presses to complete the current full combo pattern: Attack 1, then Attack 1+2, then Attack 1+2+3. Attack input buffers only during the final 0.2 seconds of the current normal attack, and the next attack must be pressed within 0.4 seconds after the animation ends to continue the sequence. A3 is intentionally heavier, deals higher damage, plays at 50% attack speed, and applies a 50% slow status to the target. Damage is applied at the animation hit marker/fallback marker to every combat target whose collider overlaps the forward slash hitbox; A2 and A3 use earlier hit markers than A1 so damage lands when the visible slash appears even with A3's slower animation. No extra procedural slash effect is spawned because the Axion attack animation already includes the weapon slash visual. Super Smash is bound to the runtime HUD `SKL` button and keyboard `K`.
- Combat debug mode removes runtime-spawned monsters and creates one `Training Dummy` near the player. The dummy has infinite health, receives player slash hits through `IDreamyCombatTarget`, flashes on hit, and reports each hit to the right-side HUD combat log with timestamp, damage, total damage, hit count, and slow/stun status details. The log panel includes a `RESET` button that clears dummy hit/damage counters and the visible history without respawning the dummy.
- `DreamyCharacterStats` now includes prototype combat stats: Damage, Str, Agi, Attack Speed, Crit Rate, Crit Damage, Status Resist, plus runtime Slow and Stun status helpers. Str increases outgoing damage, Agi increases attack speed, Attack Speed multiplies animation/combat timing, Crit can multiply damage, Slow reduces movement/action speed, and Stun stops movement/attacks while active.

Monster runtime:
- `Assets/Dreamy/Scripts/DreamyMonsterController.cs`
- Uses `DreamyMonsterDefinition` when available.
- Fallback is the previous red warrior visual catalog if the monster catalog has not been generated yet.
- The always-on runtime currently removes spawned monsters for combat debugging and uses `DreamyTrainingDummy` instead. Monster catalog spawning code remains available for re-enabling enemy tests later.
- Monster attacks now use a normalized hit marker, forward attack cone, and selected-object Scene gizmo so enemy hit timing/range can be tuned like the player prototype.

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

Runtime quest/progression prototype:
- `Assets/Dreamy/Scripts/DreamyQuestDefinition.cs`
- `Assets/Dreamy/Scripts/DreamyQuestLog.cs`
- `Assets/Dreamy/Scripts/DreamyPlayerProgression.cs`
- Quest definitions are ScriptableObject data definitions; the current scene runtime creates temporary prototype definitions at play time until authored assets are ready.
- Quest objectives currently support collect item, defeat monster, reach level, earn currency, and own unlock token.
- Quest rewards can grant EXP, coins, premium currency, skill points, unlock tokens, unlock ids, and item stacks.
- `DreamyPlayerProgression` grants skill points from `DreamyExperience.LeveledUp`.
- The runtime HUD shows active quest progress plus Coins, Skill Points, Unlock Tokens, and Unlock count.
- Runtime combat debugging hides the previous defeat-monster quest while the training dummy replaces spawned monsters.
- GM Tools can add EXP, Coins, Skill Points, and Unlock Tokens for quick testing.

Item ids added for this prototype:
- `Seed`
- `Crop`
- `CraftedMeal`
- `CraftedTool`
- `Coin`
- `UnlockToken`
- `SkillBook`

Asset usage:
- The prototype reuses Tiny Swords UI slot/panel/button sprites from `DreamyPrototypeVisualCatalog`.
- Farm/crop visuals use existing Wood/Food/UI sprites where available and procedural fallback sprites otherwise.
- The Vault Keeper currently uses the visual catalog enemy idle sheet as placeholder NPC art.

## Luneblade Little Axion Premium

Imported destination:
- `Assets/Luneblade - Little Axion (Premium)`

Sprite sheet rules:
- Readme import guidance says to slice sheets at 144x144 pixels, point filter, max size 4096, and no compression.
- Runtime code slices the horizontal sheets procedurally from the Texture2D references in `DreamyPrototypeVisualCatalog`.
- `DreamyAxionAnimatorBuilder` also slices the same sheets as Multiple sprites for generated AnimationClips and an Animator Controller. The visual catalog builder intentionally does not force Axion sheets back to Single mode.

Runtime frame counts:
- Idle: 7 frames
- Run: 8 frames
- Dash: 12 frames
- Hurt: 3 frames
- Attack source: `Attack 3.png`, 23 source frames
- A1 / Attack 1 clip: frames 0-8
- A2 / Attack 2 clip: frames 9-14
- A3 / Attack 3 clip: frames 15-22
- Normal combo sequence: A1, A1, A2, A1, A2, A3
- Super Smash: 15 frames

Catalog refresh:
- `Assets/Dreamy/Editor/DreamyPrototypeVisualCatalogBuilder.cs`
- `Assets/Dreamy/Editor/DreamyAxionAnimatorBuilder.cs`
- Menu: `Dreamy > Prototype > Refresh Runtime Visual Catalog`
- Menu: `Dreamy > Prototype > Refresh Little Axion Animator`
- The builder configures the Luneblade sheets as crisp pixel-art textures and assigns them to `Assets/Resources/DreamyPrototypeVisualCatalog.asset`.
- `DreamyPrototypeVisualCatalog.HasAxionCharacter` intentionally requires Idle, Run, Dash, Hurt, Attack 1, Attack 2, Attack 3, and Super Smash references so the runtime does not mix the old Axion setup with the premium combat set.
- Generated Animator assets target `Assets/Dreamy/Generated/AxionAnimator`.
- Generated attack clips include `DreamyAnimationHitMarker` Animation Events. `DreamyPlayerCombat` exposes the matching method and also keeps the same normalized marker timings as a fallback if runtime events do not fire. Attack frame ranges and event timings are now read from `DreamyCombatTuningProfile` when refreshing the Axion animator.

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

For general debugging:
- Open `Dreamy > Debug > Debug Hub`.
- Run `Full Audit` before deeper tuning work.
- Use the quick action buttons for combat tuning, level blocking, catalog refreshes, animator rebuilds, and docs.
- Press `Save Audit Report` when the current state should be captured for the next debugging pass.

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
