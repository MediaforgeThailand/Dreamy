using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dreamy
{
    public sealed class DreamyScenePrototypeRuntime : MonoBehaviour
    {
        private const string RuntimeObjectName = "Dreamy Scene Prototype Runtime";
        private const string HudObjectName = "Dreamy Prototype Runtime HUD";
        private const string CharacterSelectionObjectName = "Dreamy Character Selection UI";
        private const int PrototypeInventorySlotCount = 60;
        private const int PrototypeMonsterSpawnCount = 4;
        private const float SampleCharacterPixelsPerUnit = 48f;
        private const float AxionCharacterPixelsPerUnit = 48f;
        private const int CharacterChoiceKnight = 0;
        private const int CharacterChoiceSample = 1;
        private const int CharacterChoiceAxion = 2;
        private static readonly Color PlayerDamagePopupColor = new Color(1f, 0.2f, 0.16f, 1f);
        private static readonly Color PlayerHitFlashColor = new Color(1f, 0.38f, 0.36f, 1f);

        [SerializeField] private DreamyPrototypeVisualCatalog visualCatalog;
        [SerializeField] private DreamyMonsterCatalog monsterCatalog;
        [SerializeField] private bool spawnStarterPickups = true;

        private DreamyMobilePlayer player;
        private DreamyPrototypeRuntimeHud hud;
        private DreamyPrototypeInteractionUi interactionUi;
        private DreamyQuestDefinition[] runtimeQuestDefinitions;
        private bool starterPickupsSpawned;
        private bool prototypeLifeSystemsSpawned;
        private bool characterSelectionShown;
        private float characterSelectionPreviousTimeScale = 1f;
        private bool existingMobileHudLayoutApplied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeForLoadedScene()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            InstallInActiveScene();
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            InstallInActiveScene();
        }

        private static void InstallInActiveScene()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (FindAnyObjectByType<DreamyScenePrototypeRuntime>() != null)
            {
                return;
            }

            GameObject runtime = new GameObject(RuntimeObjectName);
            runtime.AddComponent<DreamyScenePrototypeRuntime>();
        }

        private void Awake()
        {
            if (visualCatalog == null)
            {
                visualCatalog = Resources.Load<DreamyPrototypeVisualCatalog>("DreamyPrototypeVisualCatalog");
            }

            if (monsterCatalog == null)
            {
                monsterCatalog = Resources.Load<DreamyMonsterCatalog>("DreamyMonsterCatalog");
            }
        }

        private void Start()
        {
            EnsureGameState();
            StartCoroutine(InstallWhenSceneIsReady());
        }

        private void Update()
        {
            if (player == null)
            {
                player = FindAnyObjectByType<DreamyMobilePlayer>();
            }

            if (player != null && hud == null)
            {
                ApplyExistingMobileHudLayout();
                EnsureHud();
            }

            if (player != null && interactionUi == null)
            {
                EnsureInteractionUi();
            }
        }

        private IEnumerator InstallWhenSceneIsReady()
        {
            float timeoutAt = Time.realtimeSinceStartup + 2f;
            while (player == null && Time.realtimeSinceStartup < timeoutAt)
            {
                player = FindAnyObjectByType<DreamyMobilePlayer>();
                yield return null;
            }

            if (player == null)
            {
                yield break;
            }

            EnsurePlayerCoreComponents();
            EnsureEventSystem();
            ApplyExistingMobileHudLayout();
            EnsureHud();
            EnsureInteractionUi();
            EnsureCharacterSelectionUi();
            EnsureStarterPickups();
            EnsureTrainingDummy();
            EnsurePrototypeLifeSystems();
        }

        private void EnsureGameState()
        {
            if (DreamyGameState.Instance != null)
            {
                return;
            }

            DreamyGameState existing = FindAnyObjectByType<DreamyGameState>();
            if (existing != null)
            {
                return;
            }

            new GameObject("Dreamy Game State").AddComponent<DreamyGameState>();
        }

        private void EnsurePlayerCoreComponents()
        {
            if (player == null)
            {
                return;
            }

            if (player.GetComponent<DreamyCharacterStats>() == null)
            {
                player.gameObject.AddComponent<DreamyCharacterStats>();
            }

            DreamyInventory inventory = player.GetComponent<DreamyInventory>();
            if (inventory == null)
            {
                inventory = player.gameObject.AddComponent<DreamyInventory>();
            }

            inventory.MaxSlots = Mathf.Max(inventory.MaxSlots, PrototypeInventorySlotCount);

            DreamyExperience experience = player.GetComponent<DreamyExperience>();
            if (experience == null)
            {
                experience = player.gameObject.AddComponent<DreamyExperience>();
            }

            DreamyPlayerProgression progression = player.GetComponent<DreamyPlayerProgression>();
            if (progression == null)
            {
                progression = player.gameObject.AddComponent<DreamyPlayerProgression>();
            }

            progression.Bind(experience);

            DreamyQuestLog questLog = player.GetComponent<DreamyQuestLog>();
            if (questLog == null)
            {
                questLog = player.gameObject.AddComponent<DreamyQuestLog>();
            }

            questLog.Configure(player, GetRuntimeQuestDefinitions());

            if (visualCatalog != null)
            {
                if (!TryConfigureAxionPlayer())
                {
                    player.ConfigureKnightVisuals(
                        visualCatalog.PlayerIdleSheet,
                        visualCatalog.PlayerRunSheet,
                        visualCatalog.PlayerAttackSheets);
                }
            }

            if (player.GetComponent<DreamyPlayerCombat>() == null)
            {
                player.gameObject.AddComponent<DreamyPlayerCombat>();
            }

            if (player.GetComponent<DreamyPrototypeInteraction>() == null)
            {
                player.gameObject.AddComponent<DreamyPrototypeInteraction>();
            }

            DreamyCharacterHitFeedback feedback = player.GetComponent<DreamyCharacterHitFeedback>();
            if (feedback == null)
            {
                feedback = player.gameObject.AddComponent<DreamyCharacterHitFeedback>();
            }

            feedback.Configure(
                true,
                true,
                PlayerDamagePopupColor,
                PlayerHitFlashColor,
                new Vector2(0f, 0.82f),
                0.2f,
                0.07f);
        }

        private void EnsureHud()
        {
            if (player == null)
            {
                return;
            }

            DreamyPrototypeRuntimeHud existingHud = FindAnyObjectByType<DreamyPrototypeRuntimeHud>();
            if (existingHud != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(existingHud.gameObject);
                }
                else
                {
                    DestroyImmediate(existingHud.gameObject);
                }
            }

            GameObject hudRoot = new GameObject(HudObjectName);
            Canvas canvas = hudRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = hudRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            hudRoot.AddComponent<GraphicRaycaster>();
            hudRoot.AddComponent<DreamySafeArea>();

            hud = hudRoot.AddComponent<DreamyPrototypeRuntimeHud>();
            hud.Build(hudRoot.transform);
            hud.Bind(player, visualCatalog);
        }

        private void ApplyExistingMobileHudLayout()
        {
            if (existingMobileHudLayoutApplied)
            {
                return;
            }

            DreamyVirtualJoystick[] joysticks = FindObjectsByType<DreamyVirtualJoystick>(FindObjectsInactive.Exclude);
            if (joysticks.Length == 0)
            {
                return;
            }

            existingMobileHudLayoutApplied = true;
            for (int i = 0; i < joysticks.Length; i++)
            {
                ApplyJoystickLayout(joysticks[i]);
            }
        }

        private static void ApplyJoystickLayout(DreamyVirtualJoystick joystick)
        {
            if (joystick == null)
            {
                return;
            }

            RectTransform rootRect = joystick.GetComponent<RectTransform>();
            if (rootRect == null)
            {
                return;
            }

            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.zero;
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = new Vector2(190f, 170f);
            rootRect.sizeDelta = new Vector2(252f, 252f);

            RectTransform handleRect = FindJoystickHandle(joystick.transform);
            if (handleRect == null)
            {
                return;
            }

            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new Vector2(104f, 104f);
            joystick.Bind(handleRect, 96f);
        }

        private static RectTransform FindJoystickHandle(Transform joystickRoot)
        {
            if (joystickRoot == null)
            {
                return null;
            }

            for (int i = 0; i < joystickRoot.childCount; i++)
            {
                Transform child = joystickRoot.GetChild(i);
                if (child.name.Contains("Handle"))
                {
                    return child.GetComponent<RectTransform>();
                }
            }

            return joystickRoot.childCount > 0 ? joystickRoot.GetChild(0).GetComponent<RectTransform>() : null;
        }

        private void EnsureInteractionUi()
        {
            if (player == null)
            {
                return;
            }

            DreamyPrototypeInteractionUi existingUi = FindAnyObjectByType<DreamyPrototypeInteractionUi>();
            if (existingUi != null)
            {
                interactionUi = existingUi;
                interactionUi.Configure(player, visualCatalog);
                return;
            }

            GameObject uiRoot = new GameObject("Dreamy Prototype Interaction UI");
            interactionUi = uiRoot.AddComponent<DreamyPrototypeInteractionUi>();
            interactionUi.Configure(player, visualCatalog);
        }

        private void EnsureCharacterSelectionUi()
        {
            if (characterSelectionShown || player == null || GameObject.Find(CharacterSelectionObjectName) != null)
            {
                return;
            }

            characterSelectionShown = true;
            characterSelectionPreviousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;

            GameObject root = new GameObject(CharacterSelectionObjectName);
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 3000;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            GameObject blocker = CreateSelectionPanel(root.transform, "Backdrop", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.55f));
            RectTransform blockerRect = blocker.GetComponent<RectTransform>();
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;

            GameObject panel = CreateSelectionPanel(root.transform, "Character Select Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960f, 430f), new Color(0.035f, 0.044f, 0.06f, 0.95f));
            Text title = CreateSelectionText(panel.transform, "Choose Character", 36, TextAnchor.MiddleCenter, new Vector2(0f, -30f), new Vector2(760f, 54f));
            title.color = new Color(1f, 0.94f, 0.82f, 1f);

            Sprite knightPreview = CreatePreviewSprite(visualCatalog != null ? visualCatalog.PlayerIdleSheet : null, 8, 1, DefaultPreviewPixelsPerUnit());
            Sprite samplePreview = CreatePreviewSprite(visualCatalog != null ? visualCatalog.SampleCharacterIdleSheet : null, 10, 1, SampleCharacterPixelsPerUnit);
            Sprite axionPreview = CreatePreviewSprite(visualCatalog != null ? visualCatalog.AxionCharacterIdleSheet : null, 7, 1, AxionCharacterPixelsPerUnit);

            Button knightButton = CreateCharacterChoiceButton(panel.transform, "Knight", knightPreview, new Vector2(-300f, -214f), true);
            knightButton.onClick.AddListener(() => SelectCharacter(root, CharacterChoiceKnight));

            bool hasSample = visualCatalog != null && visualCatalog.HasSampleCharacter;
            Button sampleButton = CreateCharacterChoiceButton(panel.transform, "Sample", samplePreview, new Vector2(0f, -214f), hasSample);
            sampleButton.onClick.AddListener(() => SelectCharacter(root, CharacterChoiceSample));

            bool hasAxion = visualCatalog != null && visualCatalog.HasAxionCharacter;
            Button axionButton = CreateCharacterChoiceButton(panel.transform, "Little Axion", axionPreview, new Vector2(300f, -214f), hasAxion);
            axionButton.onClick.AddListener(() => SelectCharacter(root, CharacterChoiceAxion));
        }

        private void SelectCharacter(GameObject selectionRoot, int characterChoice)
        {
            if (player != null)
            {
                if (characterChoice == CharacterChoiceSample && visualCatalog != null && visualCatalog.HasSampleCharacter)
                {
                    player.ConfigureCharacterVisuals(
                        visualCatalog.SampleCharacterIdleSheet,
                        10,
                        1,
                        -1,
                        visualCatalog.SampleCharacterWalkSheet,
                        4,
                        6,
                        0,
                        System.Array.Empty<Texture2D>(),
                        System.Array.Empty<int>(),
                        System.Array.Empty<int>(),
                        SampleCharacterPixelsPerUnit,
                        5f,
                        12f,
                        12f,
                        false);
                }
                else if (characterChoice == CharacterChoiceAxion && visualCatalog != null && visualCatalog.HasAxionCharacter)
                {
                    TryConfigureAxionPlayer();
                }
                else if (visualCatalog != null)
                {
                    player.ConfigureKnightVisuals(
                        visualCatalog.PlayerIdleSheet,
                        visualCatalog.PlayerRunSheet,
                        visualCatalog.PlayerAttackSheets);
                }
            }

            Time.timeScale = characterSelectionPreviousTimeScale;
            if (selectionRoot != null)
            {
                Destroy(selectionRoot);
            }
        }

        private bool TryConfigureAxionPlayer()
        {
            if (player == null || visualCatalog == null || !visualCatalog.HasAxionCharacter)
            {
                return false;
            }

            Texture2D[] axionAttackSheets = visualCatalog.AxionCharacterAttackSheets;
            Texture2D comboSheet = axionAttackSheets.Length >= 3 ? axionAttackSheets[2] : visualCatalog.AxionCharacterAttackSheet;
            DreamyCombatTuningProfile combatTuning = DreamyCombatTuningProfile.LoadDefault();
            if (combatTuning != null)
            {
                combatTuning.EnsureDefaults();
            }

            int comboStepCount = combatTuning != null && combatTuning.ComboStepCount > 0 ? combatTuning.ComboStepCount : 6;
            Texture2D[] comboAttackSheets = new Texture2D[comboStepCount];
            int[] sourceFrameCounts = new int[comboStepCount];
            int[] rowCounts = new int[comboStepCount];
            int[] startFrames = new int[comboStepCount];
            int[] frameCounts = new int[comboStepCount];
            int[] animatorStates = new int[comboStepCount];
            int[] attackParts = new int[comboStepCount];
            float[] frameSpeedMultipliers = new float[comboStepCount];
            int[] defaultStartFrames = { 0, 0, 9, 0, 9, 15 };
            int[] defaultFrameCounts = { 9, 9, 6, 9, 6, 8 };
            int[] defaultAnimatorStates = { 0, 0, 1, 0, 1, 2 };
            int[] defaultAttackParts = { 0, 0, 1, 0, 1, 2 };
            float[] defaultFrameSpeedMultipliers = { 1f, 1f, 1f, 1f, 1f, 0.5f };
            for (int i = 0; i < comboStepCount; i++)
            {
                int fallbackIndex = Mathf.Clamp(i, 0, defaultStartFrames.Length - 1);
                DreamyCombatActionTuning actionTuning = combatTuning != null ? combatTuning.GetComboAction(i) : null;
                comboAttackSheets[i] = comboSheet;
                sourceFrameCounts[i] = actionTuning != null ? actionTuning.SourceFrameTotal : 23;
                rowCounts[i] = 1;
                startFrames[i] = actionTuning != null ? actionTuning.SourceFrameStart : defaultStartFrames[fallbackIndex];
                frameCounts[i] = actionTuning != null ? actionTuning.FrameCount : defaultFrameCounts[fallbackIndex];
                animatorStates[i] = actionTuning != null ? actionTuning.AnimatorStateIndex : defaultAnimatorStates[fallbackIndex];
                attackParts[i] = actionTuning != null ? actionTuning.AttackPartIndex : defaultAttackParts[fallbackIndex];
                frameSpeedMultipliers[i] = actionTuning != null ? actionTuning.FrameSpeedMultiplier : defaultFrameSpeedMultipliers[fallbackIndex];
            }

            player.ConfigureCharacterVisuals(
                visualCatalog.AxionCharacterIdleSheet,
                7,
                1,
                -1,
                visualCatalog.AxionCharacterRunSheet,
                8,
                1,
                -1,
                comboAttackSheets,
                sourceFrameCounts,
                rowCounts,
                startFrames,
                frameCounts,
                animatorStates,
                attackParts,
                frameSpeedMultipliers,
                AxionCharacterPixelsPerUnit,
                5f,
                13f,
                24f,
                false);
            player.ConfigureActionVisuals(
                visualCatalog.AxionCharacterDashSheet,
                12,
                1,
                visualCatalog.AxionCharacterHurtSheet,
                3,
                1,
                visualCatalog.AxionCharacterSuperSmashSheet,
                15,
                1,
                AxionCharacterPixelsPerUnit,
                24f,
                12f,
                18f);
            player.ConfigureAnimator(visualCatalog.AxionAnimatorController);
            return true;
        }

        private void EnsureStarterPickups()
        {
            if (!spawnStarterPickups || starterPickupsSpawned || player == null)
            {
                return;
            }

            bool alreadyHasGatherables = FindObjectsByType<DreamyResourcePickup>(FindObjectsInactive.Exclude).Length > 0
                || FindObjectsByType<DreamyResourceNode>(FindObjectsInactive.Exclude).Length > 0;
            if (alreadyHasGatherables)
            {
                starterPickupsSpawned = true;
                return;
            }

            Vector2 origin = player.transform.position;
            SpawnPickup(DreamyItemId.Wood, "Wood", 4, 6, origin + new Vector2(1.3f, 0.55f));
            SpawnPickup(DreamyItemId.Gold, "Gold", 2, 10, origin + new Vector2(-1.2f, 0.85f));
            SpawnPickup(DreamyItemId.Food, "Food", 3, 8, origin + new Vector2(0.25f, -1.15f));
            starterPickupsSpawned = true;
        }

        private void EnsureTrainingDummy()
        {
            if (player == null)
            {
                return;
            }

            RemoveRuntimeMonsters();
            if (FindAnyObjectByType<DreamyTrainingDummy>() != null)
            {
                return;
            }

            GameObject dummy = new GameObject("Training Dummy");
            dummy.transform.position = player.transform.position + new Vector3(1f, -1.15f, 0f);
            dummy.transform.localScale = Vector3.one;

            dummy.AddComponent<SpriteRenderer>();
            dummy.AddComponent<Rigidbody2D>();
            dummy.AddComponent<BoxCollider2D>();
            dummy.AddComponent<DreamyTrainingDummy>().Configure("Training Dummy");
        }

        private static void RemoveRuntimeMonsters()
        {
            DreamyMonsterController[] monsters = FindObjectsByType<DreamyMonsterController>(FindObjectsInactive.Exclude);
            for (int i = 0; i < monsters.Length; i++)
            {
                if (monsters[i] == null)
                {
                    continue;
                }

                GameObject monsterObject = monsters[i].gameObject;
                if (Application.isPlaying)
                {
                    Destroy(monsterObject);
                }
                else
                {
                    DestroyImmediate(monsterObject);
                }
            }
        }

        private void EnsurePrototypeMonster()
        {
            if (player == null || FindAnyObjectByType<DreamyMonsterController>() != null)
            {
                return;
            }

            if (monsterCatalog != null && monsterCatalog.CombatCount > 0)
            {
                int spawnCount = Mathf.Min(PrototypeMonsterSpawnCount, monsterCatalog.CombatCount);
                for (int i = 0; i < spawnCount; i++)
                {
                    DreamyMonsterDefinition definition = monsterCatalog.GetCombatMonster(i);
                    if (definition != null)
                    {
                        SpawnMonster(definition, player.transform.position + GetMonsterSpawnOffset(i), i);
                    }
                }

                return;
            }

            GameObject monster = new GameObject("Prototype Warrior Monster");
            monster.transform.position = player.transform.position + new Vector3(3.1f, 1.25f, 0f);
            monster.transform.localScale = Vector3.one;

            DreamyCharacterStats stats = monster.AddComponent<DreamyCharacterStats>();
            stats.MaxHealth = 55f;
            stats.CurrentHealth = 55f;
            stats.Damage = 9f;

            monster.AddComponent<SpriteRenderer>();
            monster.AddComponent<Rigidbody2D>();
            monster.AddComponent<CircleCollider2D>();
            DreamyMonsterController monsterController = monster.AddComponent<DreamyMonsterController>();
            monsterController.Configure(visualCatalog, player.transform);
            monsterController.ConfigureIdentity("อัศวิน", 25);
        }

        private void EnsurePrototypeLifeSystems()
        {
            if (prototypeLifeSystemsSpawned || player == null)
            {
                return;
            }

            if (FindAnyObjectByType<DreamyPrototypeFarmPlot>() != null
                || FindAnyObjectByType<DreamyPrototypeCraftingStation>() != null
                || FindAnyObjectByType<DreamyPrototypeVaultNpc>() != null)
            {
                prototypeLifeSystemsSpawned = true;
                GrantStarterPrototypeItems();
                return;
            }

            Transform root = new GameObject("Prototype Life Systems").transform;
            Vector3 origin = player.transform.position;

            for (int i = 0; i < 4; i++)
            {
                Vector3 position = origin + new Vector3(-2.1f + i * 0.9f, -1.55f, 0f);
                SpawnFarmPlot(root, position, i);
            }

            SpawnCraftingStation(root, origin + new Vector3(2.1f, -1.15f, 0f));
            SpawnVaultNpc(root, origin + new Vector3(-2.45f, 0.45f, 0f));
            GrantStarterPrototypeItems();
            prototypeLifeSystemsSpawned = true;
        }

        private void SpawnFarmPlot(Transform parent, Vector3 position, int index)
        {
            GameObject plot = new GameObject("Prototype Farm Plot " + (index + 1).ToString("00"));
            plot.transform.SetParent(parent, false);
            plot.transform.position = position;
            plot.AddComponent<SpriteRenderer>();
            DreamyPrototypeFarmPlot farmPlot = plot.AddComponent<DreamyPrototypeFarmPlot>();
            farmPlot.Configure(visualCatalog);
        }

        private void SpawnCraftingStation(Transform parent, Vector3 position)
        {
            GameObject station = new GameObject("Prototype Crafting Keeper");
            station.transform.SetParent(parent, false);
            station.transform.position = position;
            station.AddComponent<SpriteRenderer>();
            DreamyPrototypeCraftingStation craftingStation = station.AddComponent<DreamyPrototypeCraftingStation>();
            craftingStation.Configure(visualCatalog);
        }

        private void SpawnVaultNpc(Transform parent, Vector3 position)
        {
            GameObject npc = new GameObject("Prototype Vault Keeper");
            npc.transform.SetParent(parent, false);
            npc.transform.position = position;
            npc.AddComponent<SpriteRenderer>();
            npc.AddComponent<DreamyInventory>();
            DreamyPrototypeVaultNpc vaultNpc = npc.AddComponent<DreamyPrototypeVaultNpc>();
            vaultNpc.Configure(visualCatalog);
        }

        private void GrantStarterPrototypeItems()
        {
            if (player == null || player.Inventory == null)
            {
                return;
            }

            if (player.Inventory.GetQuantity(DreamyItemId.Seed) <= 0)
            {
                player.Inventory.AddItem(DreamyItemId.Seed, 4, "Seed");
            }

            if (player.Inventory.GetQuantity(DreamyItemId.Wood) <= 0)
            {
                player.Inventory.AddItem(DreamyItemId.Wood, 3, "Wood");
            }

            if (player.Inventory.GetQuantity(DreamyItemId.Food) <= 0)
            {
                player.Inventory.AddItem(DreamyItemId.Food, 2, "Food");
            }

            if (player.Inventory.GetQuantity(DreamyItemId.Gold) <= 0)
            {
                player.Inventory.AddItem(DreamyItemId.Gold, 1, "Gold");
            }
        }

        private DreamyQuestDefinition[] GetRuntimeQuestDefinitions()
        {
            if (runtimeQuestDefinitions != null)
            {
                return runtimeQuestDefinitions;
            }

            DreamyQuestDefinition supplyQuest = ScriptableObject.CreateInstance<DreamyQuestDefinition>();
            supplyQuest.name = "RuntimeQuest_FieldSupplies";
            supplyQuest.hideFlags = HideFlags.DontSave;
            supplyQuest.ConfigureRuntime(
                "runtime.field_supplies",
                "Field Supplies",
                "Bring enough materials to keep the first camp running.",
                new[]
                {
                    new DreamyQuestObjectiveDefinition(DreamyQuestObjectiveKind.CollectItem, DreamyItemId.Wood, string.Empty, "Wood", 5),
                    new DreamyQuestObjectiveDefinition(DreamyQuestObjectiveKind.CollectItem, DreamyItemId.Food, string.Empty, "Food", 3)
                },
                new DreamyQuestRewardDefinition(
                    45,
                    16,
                    0,
                    0,
                    0,
                    "recipe.prototype_meal",
                    new[] { new DreamyItemStack(DreamyItemId.Seed, "Seed", 2) }));

            DreamyQuestDefinition huntQuest = ScriptableObject.CreateInstance<DreamyQuestDefinition>();
            huntQuest.name = "RuntimeQuest_FirstHunt";
            huntQuest.hideFlags = HideFlags.DontSave;
            huntQuest.ConfigureRuntime(
                "runtime.first_hunt",
                "First Hunt",
                "Clear a small threat pack and prove the combat loop.",
                new[]
                {
                    new DreamyQuestObjectiveDefinition(DreamyQuestObjectiveKind.DefeatMonster, DreamyItemId.Custom, string.Empty, "Defeat", 2)
                },
                new DreamyQuestRewardDefinition(
                    70,
                    24,
                    0,
                    1,
                    1,
                    "map.prototype_edge",
                    new[] { new DreamyItemStack(DreamyItemId.UnlockToken, "Unlock Token", 1) }));

            DreamyQuestDefinition levelQuest = ScriptableObject.CreateInstance<DreamyQuestDefinition>();
            levelQuest.name = "RuntimeQuest_LevelReady";
            levelQuest.hideFlags = HideFlags.DontSave;
            levelQuest.ConfigureRuntime(
                "runtime.level_ready",
                "Training Check",
                "Reach the next level to test skill point rewards.",
                new[]
                {
                    new DreamyQuestObjectiveDefinition(DreamyQuestObjectiveKind.ReachLevel, DreamyItemId.Custom, string.Empty, "Level", 2)
                },
                new DreamyQuestRewardDefinition(
                    0,
                    12,
                    0,
                    1,
                    0,
                    "skill.prototype_slot",
                    new[] { new DreamyItemStack(DreamyItemId.SkillBook, "Skill Book", 1) }));

            runtimeQuestDefinitions = new[] { supplyQuest, levelQuest };
            return runtimeQuestDefinitions;
        }

        private void SpawnMonster(DreamyMonsterDefinition definition, Vector3 position, int index)
        {
            GameObject monster = new GameObject("Prototype Monster " + (index + 1).ToString("00") + " - " + definition.DisplayName);
            monster.transform.position = position;
            monster.transform.localScale = Vector3.one;

            monster.AddComponent<DreamyCharacterStats>();
            monster.AddComponent<SpriteRenderer>();
            monster.AddComponent<Rigidbody2D>();
            monster.AddComponent<CircleCollider2D>();
            DreamyMonsterController monsterController = monster.AddComponent<DreamyMonsterController>();
            monsterController.Configure(definition, player.transform);
        }

        private static Vector3 GetMonsterSpawnOffset(int index)
        {
            switch (index % 6)
            {
                case 0:
                    return new Vector3(3.1f, 1.25f, 0f);
                case 1:
                    return new Vector3(-3.4f, 1.55f, 0f);
                case 2:
                    return new Vector3(2.8f, -1.85f, 0f);
                case 3:
                    return new Vector3(-2.9f, -1.7f, 0f);
                case 4:
                    return new Vector3(4.25f, -0.2f, 0f);
                default:
                    return new Vector3(-4.15f, -0.15f, 0f);
            }
        }

        private void SpawnPickup(DreamyItemId itemId, string displayName, int amount, int expReward, Vector2 position)
        {
            GameObject pickup = new GameObject("Prototype " + displayName + " Pickup");
            pickup.transform.position = position;

            SpriteRenderer renderer = pickup.AddComponent<SpriteRenderer>();
            renderer.sprite = visualCatalog != null ? visualCatalog.GetItemSprite(itemId) : null;
            renderer.sortingOrder = 18;
            if (renderer.sprite == null)
            {
                renderer.sprite = CreateFallbackSprite(itemId);
            }

            CircleCollider2D collider = pickup.AddComponent<CircleCollider2D>();
            collider.radius = 0.45f;
            collider.isTrigger = true;

            DreamyResourcePickup resourcePickup = pickup.AddComponent<DreamyResourcePickup>();
            resourcePickup.Configure(itemId, displayName, amount, expReward, 0.9f);
        }

        private static Sprite CreateFallbackSprite(DreamyItemId itemId)
        {
            Color color;
            switch (itemId)
            {
                case DreamyItemId.Gold:
                    color = new Color(1f, 0.82f, 0.18f, 1f);
                    break;
                case DreamyItemId.Food:
                case DreamyItemId.Meat:
                    color = new Color(0.95f, 0.45f, 0.35f, 1f);
                    break;
                default:
                    color = new Color(0.58f, 0.36f, 0.18f, 1f);
                    break;
            }

            Texture2D texture = new Texture2D(8, 8);
            Color[] pixels = new Color[64];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.filterMode = FilterMode.Point;
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 16f);
        }

        private static GameObject CreateSelectionPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            Image image = panel.AddComponent<Image>();
            image.sprite = CreateSelectionSprite(Color.white);
            image.color = color;
            image.raycastTarget = true;

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return panel;
        }

        private static Button CreateCharacterChoiceButton(Transform parent, string label, Sprite previewSprite, Vector2 position, bool interactable)
        {
            GameObject buttonObject = new GameObject(label + " Choice Button");
            buttonObject.transform.SetParent(parent, false);
            Image background = buttonObject.AddComponent<Image>();
            background.sprite = CreateSelectionSprite(Color.white);
            background.color = interactable ? new Color(0.11f, 0.13f, 0.16f, 0.96f) : new Color(0.08f, 0.08f, 0.09f, 0.72f);
            Button button = buttonObject.AddComponent<Button>();
            button.interactable = interactable;

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(260f, 230f);

            GameObject preview = new GameObject("Preview");
            preview.transform.SetParent(buttonObject.transform, false);
            Image previewImage = preview.AddComponent<Image>();
            previewImage.sprite = previewSprite;
            previewImage.preserveAspect = true;
            previewImage.raycastTarget = false;
            previewImage.color = interactable ? Color.white : new Color(1f, 1f, 1f, 0.32f);
            RectTransform previewRect = preview.GetComponent<RectTransform>();
            previewRect.anchorMin = new Vector2(0.5f, 0.5f);
            previewRect.anchorMax = new Vector2(0.5f, 0.5f);
            previewRect.pivot = new Vector2(0.5f, 0.5f);
            previewRect.anchoredPosition = new Vector2(0f, 32f);
            previewRect.sizeDelta = new Vector2(160f, 140f);

            Text text = CreateSelectionText(buttonObject.transform, label, 28, TextAnchor.MiddleCenter, new Vector2(0f, -82f), new Vector2(210f, 42f));
            text.color = interactable ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            return button;
        }

        private static Text CreateSelectionText(Transform parent, string value, int fontSize, TextAnchor alignment, Vector2 position, Vector2 size)
        {
            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;

            RectTransform rect = text.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return text;
        }

        private static Sprite CreatePreviewSprite(Texture2D texture, int columns, int rows, float pixelsPerUnit)
        {
            if (texture == null || columns <= 0 || rows <= 0)
            {
                return CreateSelectionSprite(new Color(0.23f, 0.42f, 0.55f, 1f));
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            int width = Mathf.Max(1, texture.width / columns);
            int height = Mathf.Max(1, texture.height / rows);
            Rect rect = new Rect(0f, (rows - 1) * height, width, height);
            return Sprite.Create(texture, rect, new Vector2(0.5f, 0.28f), Mathf.Max(1f, pixelsPerUnit), 0, SpriteMeshType.FullRect);
        }

        private static float DefaultPreviewPixelsPerUnit()
        {
            return 128f;
        }

        private static Sprite CreateSelectionSprite(Color color)
        {
            Texture2D texture = new Texture2D(4, 4);
            Color[] pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

    }

    public sealed class DreamyPrototypeRuntimeHud : MonoBehaviour
    {
        private const int InventoryRows = 6;
        private const int InventoryColumns = 10;
        private const int InventorySlotCount = InventoryRows * InventoryColumns;
        private const int ConsumableSlotCount = 6;
        private const int QuickSlotCount = 2;
        private const float InventorySlotSize = 76f;
        private const float InventorySlotGap = 10f;
        private const int TrainingLogLineCount = 8;
        private const float ConsumableSlotSize = 96f;
        private const float ConsumableSlotGap = 28f;
        private const float QuickSlotSize = 174f;
        private const float QuickSlotGap = 46f;

        private Image statusPanelImage;
        private Image resourcePanelImage;
        private Image inventoryWindowImage;
        private Image questPanelImage;
        private Image trainingLogPanelImage;
        private DreamySegmentedBar healthBar;
        private DreamySegmentedBar staminaBar;
        private DreamySegmentedBar expBar;
        private Text healthLabel;
        private Text staminaLabel;
        private Text expLabel;
        private Text resourcesLabel;
        private Text progressionLabel;
        private Text questLabel;
        private Text messageLabel;
        private Text trainingLogLabel;
        private Button attackButton;
        private Button specialSkillButton;
        private Button dodgeButton;
        private Button inventoryButton;
        private Button trainingResetButton;
        private Button inventoryTabButton;
        private Button weaponsTabButton;
        private Button closeInventoryButton;
        private Image attackButtonIcon;
        private Image specialSkillButtonIcon;
        private Image dodgeButtonIcon;
        private Image inventoryButtonIcon;
        private Image inventoryTabImage;
        private Image weaponsTabImage;
        private GameObject inventoryWindow;
        private GameObject inventoryTabContent;
        private GameObject weaponsTabContent;
        private GameObject inventoryDragGhost;
        private Image inventoryDragGhostImage;
        private Text inventoryTitleLabel;
        private readonly Image[] inventorySlotBackgrounds = new Image[InventorySlotCount];
        private readonly Image[] inventorySlotIcons = new Image[InventorySlotCount];
        private readonly Text[] inventorySlotNames = new Text[InventorySlotCount];
        private readonly Text[] inventorySlotQuantities = new Text[InventorySlotCount];
        private readonly Image[] consumableSlotBackgrounds = new Image[ConsumableSlotCount];
        private readonly Image[] consumableSlotIcons = new Image[ConsumableSlotCount];
        private readonly Text[] consumableSlotNames = new Text[ConsumableSlotCount];
        private readonly Text[] consumableSlotQuantities = new Text[ConsumableSlotCount];
        private readonly DreamyPrototypeInventoryDragSource[] consumableDragSources = new DreamyPrototypeInventoryDragSource[ConsumableSlotCount];
        private readonly Image[] quickSlotBackgrounds = new Image[QuickSlotCount];
        private readonly Image[] quickSlotIcons = new Image[QuickSlotCount];
        private readonly Text[] quickSlotLabels = new Text[QuickSlotCount];
        private readonly Text[] quickSlotNames = new Text[QuickSlotCount];
        private readonly bool[] quickSlotAssigned = new bool[QuickSlotCount];
        private readonly DreamyItemId[] quickSlotItemIds = new DreamyItemId[QuickSlotCount];
        private readonly string[] quickSlotDisplayNames = new string[QuickSlotCount];
        private DreamyMobilePlayer player;
        private DreamyPlayerCombat playerCombat;
        private DreamyPlayerProgression progression;
        private DreamyQuestLog questLog;
        private DreamyPrototypeVisualCatalog visualCatalog;
        private DreamyItemId draggedItemId;
        private string draggedDisplayName;
        private Sprite draggedSprite;
        private bool hasDraggedItem;
        private string message;
        private float messageUntil;
        private readonly Queue<string> trainingLogEntries = new Queue<string>();
        private int trainingTotalHits;
        private float trainingTotalDamage;

        private DreamyCharacterStats Stats => player != null ? player.CharacterStats : null;
        private DreamyInventory Inventory => player != null ? player.Inventory : null;
        private DreamyExperience Experience => player != null ? player.Experience : null;
        private DreamyPlayerProgression Progression => progression != null ? progression : player != null ? player.GetComponent<DreamyPlayerProgression>() : null;

        private void OnEnable()
        {
            DreamyResourcePickup.PickedUp += HandlePickup;
            DreamyResourcePickup.PickupRejected += HandlePickupRejected;
            DreamyResourceNode.ResourceCollected += HandleResourceCollected;
            DreamyTrainingDummy.HitRecorded += HandleTrainingDummyHitRecorded;
        }

        private void OnDisable()
        {
            DreamyResourcePickup.PickedUp -= HandlePickup;
            DreamyResourcePickup.PickupRejected -= HandlePickupRejected;
            DreamyResourceNode.ResourceCollected -= HandleResourceCollected;
            DreamyTrainingDummy.HitRecorded -= HandleTrainingDummyHitRecorded;
        }

        public void Build(Transform parent)
        {
            GameObject statusPanel = CreatePanel(parent, "Prototype Status Panel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -22f), new Vector2(590f, 160f));
            statusPanelImage = statusPanel.GetComponent<Image>();
            healthBar = CreateBar(statusPanel.transform, "HP", new Color(0.9f, 0.18f, 0.22f, 1f), new Vector2(18f, -16f), new Vector2(521f, 50f), 21, out healthLabel);
            staminaBar = CreateBar(statusPanel.transform, "STA", new Color(0.22f, 0.8f, 0.4f, 1f), new Vector2(18f, -66f), new Vector2(434f, 42f), 18, out staminaLabel);
            expBar = CreateBar(statusPanel.transform, "EXP", new Color(0.28f, 0.62f, 1f, 1f), new Vector2(18f, -112f), new Vector2(434f, 42f), 18, out expLabel);

            inventoryButton = CreateButton(parent, "BAG", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -28f), new Vector2(104f, 104f));
            inventoryButtonIcon = CreateButtonIcon(inventoryButton.transform, new Vector2(50f, 50f), new Vector2(0f, 9f));
            PlaceButtonLabel(inventoryButton, "BAG", 16, new Vector2(0f, -36f), new Vector2(84f, 24f));
            BuildInventoryWindow(parent);

            GameObject resourcePanel = CreatePanel(parent, "Prototype Resource Panel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(760f, 78f));
            resourcePanelImage = resourcePanel.GetComponent<Image>();
            resourcesLabel = CreateText(resourcePanel.transform, "Resources", 22, TextAnchor.MiddleCenter, new Vector2(0f, -7f), new Vector2(760f, 32f));
            RectTransform resourcesRect = resourcesLabel.GetComponent<RectTransform>();
            resourcesRect.anchorMin = new Vector2(0.5f, 1f);
            resourcesRect.anchorMax = new Vector2(0.5f, 1f);
            resourcesRect.pivot = new Vector2(0.5f, 1f);
            progressionLabel = CreateText(resourcePanel.transform, "Progression", 18, TextAnchor.MiddleCenter, new Vector2(0f, -39f), new Vector2(760f, 28f));
            RectTransform progressionRect = progressionLabel.GetComponent<RectTransform>();
            progressionRect.anchorMin = new Vector2(0.5f, 1f);
            progressionRect.anchorMax = new Vector2(0.5f, 1f);
            progressionRect.pivot = new Vector2(0.5f, 1f);

            GameObject questPanel = CreatePanel(parent, "Prototype Quest Panel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -196f), new Vector2(520f, 128f));
            questPanelImage = questPanel.GetComponent<Image>();
            questLabel = CreateText(questPanel.transform, "Quest", 20, TextAnchor.UpperLeft, new Vector2(18f, -14f), new Vector2(486f, 100f));
            AddTextOutline(questLabel);

            GameObject trainingLogPanel = CreatePanel(parent, "Training Dummy Log Panel", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -130f), new Vector2(430f, 318f));
            trainingLogPanelImage = trainingLogPanel.GetComponent<Image>();
            trainingLogLabel = CreateText(trainingLogPanel.transform, "TRAINING DUMMY\nHP INF\nHits 0    Damage 0\n\nNo hits yet.", 18, TextAnchor.UpperLeft, new Vector2(18f, -16f), new Vector2(394f, 286f));
            AddTextOutline(trainingLogLabel);
            trainingResetButton = CreateButton(trainingLogPanel.transform, "RESET", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -14f), new Vector2(96f, 38f));
            PlaceButtonLabel(trainingResetButton, "RESET", 14, Vector2.zero, new Vector2(84f, 26f));

            messageLabel = CreateText(parent, string.Empty, 28, TextAnchor.MiddleCenter, new Vector2(0f, -102f), new Vector2(760f, 54f));
            RectTransform messageRect = messageLabel.GetComponent<RectTransform>();
            messageRect.anchorMin = new Vector2(0.5f, 1f);
            messageRect.anchorMax = new Vector2(0.5f, 1f);
            messageRect.pivot = new Vector2(0.5f, 1f);
            messageLabel.color = new Color(1f, 0.95f, 0.68f, 1f);

            attackButton = CreateButton(parent, "ATK", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-154f, 154f), new Vector2(146f, 146f));
            attackButtonIcon = CreateButtonIcon(attackButton.transform, new Vector2(64f, 64f), new Vector2(0f, 16f));
            PlaceButtonLabel(attackButton, "ATK", 20, new Vector2(0f, -46f), new Vector2(108f, 28f));
            specialSkillButton = CreateButton(parent, "SKL", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-154f, 310f), new Vector2(120f, 120f));
            specialSkillButtonIcon = CreateButtonIcon(specialSkillButton.transform, new Vector2(52f, 52f), new Vector2(0f, 13f));
            PlaceButtonLabel(specialSkillButton, "SKL", 16, new Vector2(0f, -39f), new Vector2(92f, 22f));
            dodgeButton = CreateButton(parent, "ROLL", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-304f, 126f), new Vector2(120f, 120f));
            dodgeButtonIcon = CreateButtonIcon(dodgeButton.transform, new Vector2(52f, 52f), new Vector2(0f, 13f));
            PlaceButtonLabel(dodgeButton, "ROLL", 16, new Vector2(0f, -39f), new Vector2(92f, 22f));
        }

        public void Bind(DreamyMobilePlayer targetPlayer, DreamyPrototypeVisualCatalog catalog)
        {
            player = targetPlayer;
            playerCombat = player != null ? player.GetComponent<DreamyPlayerCombat>() : null;
            progression = player != null ? player.GetComponent<DreamyPlayerProgression>() : null;
            questLog = player != null ? player.GetComponent<DreamyQuestLog>() : null;
            visualCatalog = catalog;
            ApplyCatalogSprites();
            if (attackButton != null)
            {
                attackButton.onClick.RemoveAllListeners();
                attackButton.onClick.AddListener(() =>
                {
                    if (playerCombat != null)
                    {
                        playerCombat.QueueAttack();
                    }
                });
            }

            if (specialSkillButton != null)
            {
                specialSkillButton.onClick.RemoveAllListeners();
                specialSkillButton.onClick.AddListener(() =>
                {
                    if (playerCombat != null)
                    {
                        playerCombat.QueueSpecialSkill();
                    }
                });
            }

            if (inventoryButton != null)
            {
                inventoryButton.onClick.RemoveAllListeners();
                inventoryButton.onClick.AddListener(ToggleInventory);
            }

            if (trainingResetButton != null)
            {
                trainingResetButton.onClick.RemoveAllListeners();
                trainingResetButton.onClick.AddListener(ResetTrainingDummyLog);
            }

            if (inventoryTabButton != null)
            {
                inventoryTabButton.onClick.RemoveAllListeners();
                inventoryTabButton.onClick.AddListener(() => SelectInventoryTab(true));
            }

            if (weaponsTabButton != null)
            {
                weaponsTabButton.onClick.RemoveAllListeners();
                weaponsTabButton.onClick.AddListener(() => SelectInventoryTab(false));
            }

            if (dodgeButton != null)
            {
                dodgeButton.onClick.RemoveAllListeners();
                dodgeButton.onClick.AddListener(() =>
                {
                    if (player != null)
                    {
                        player.QueueDodge();
                    }
                });
            }

            if (closeInventoryButton != null)
            {
                closeInventoryButton.onClick.RemoveAllListeners();
                closeInventoryButton.onClick.AddListener(() => SetInventoryVisible(false));
            }

            Refresh();
        }

        private void ApplyCatalogSprites()
        {
            HideImage(statusPanelImage);
            ApplySolidSprite(resourcePanelImage, new Color(0.025f, 0.032f, 0.045f, 0.78f), Image.Type.Simple);
            ApplySolidSprite(inventoryWindowImage, new Color(0.025f, 0.032f, 0.045f, 0.96f), Image.Type.Simple);
            ApplySolidSprite(questPanelImage, new Color(0.025f, 0.032f, 0.045f, 0.7f), Image.Type.Simple);
            ApplySolidSprite(trainingLogPanelImage, new Color(0.025f, 0.032f, 0.045f, 0.82f), Image.Type.Simple);

            Sprite barBaseSprite = visualCatalog != null ? visualCatalog.UiBarBaseSprite : null;
            Sprite barFillSprite = visualCatalog != null ? visualCatalog.UiBarFillSprite : null;
            ApplySegmentedBar(healthBar, barBaseSprite, barFillSprite, Color.white);
            ApplySegmentedBar(staminaBar, barBaseSprite, barFillSprite, new Color(0.22f, 0.8f, 0.4f, 1f));
            ApplySegmentedBar(expBar, barBaseSprite, barFillSprite, new Color(0.28f, 0.62f, 1f, 1f));

            for (int i = 0; i < inventorySlotBackgrounds.Length; i++)
            {
                ApplySolidSprite(inventorySlotBackgrounds[i], new Color(0.05f, 0.06f, 0.075f, 0.9f), Image.Type.Simple);
            }

            for (int i = 0; i < consumableSlotBackgrounds.Length; i++)
            {
                ApplySolidSprite(consumableSlotBackgrounds[i], new Color(0.055f, 0.068f, 0.085f, 0.92f), Image.Type.Simple);
            }

            for (int i = 0; i < quickSlotBackgrounds.Length; i++)
            {
                ApplySolidSprite(quickSlotBackgrounds[i], new Color(0.07f, 0.084f, 0.105f, 0.94f), Image.Type.Simple);
            }

            if (visualCatalog == null)
            {
                return;
            }

            ApplyButtonSprites(attackButton, visualCatalog.UiRedButtonSprite, visualCatalog.UiRedButtonPressedSprite);
            ApplyButtonSprites(specialSkillButton, visualCatalog.UiBlueButtonSprite, visualCatalog.UiBlueButtonPressedSprite);
            ApplyButtonSprites(dodgeButton, visualCatalog.UiBlueButtonSprite, visualCatalog.UiBlueButtonPressedSprite);
            ApplyButtonSprites(inventoryButton, visualCatalog.UiBlueButtonSprite, visualCatalog.UiBlueButtonPressedSprite);
            ApplyButtonSprites(trainingResetButton, visualCatalog.UiRedButtonSprite, visualCatalog.UiRedButtonPressedSprite);
            ApplyButtonSprites(closeInventoryButton, visualCatalog.UiRedButtonSprite, visualCatalog.UiRedButtonPressedSprite);

            ApplySprite(attackButtonIcon, visualCatalog.UiAttackIconSprite, Color.white, Image.Type.Simple);
            ApplySprite(specialSkillButtonIcon, visualCatalog.UiAttackIconSprite, new Color(0.58f, 0.93f, 1f, 1f), Image.Type.Simple);
            ApplySprite(dodgeButtonIcon, visualCatalog.UiDodgeIconSprite, Color.white, Image.Type.Simple);
            ApplySprite(inventoryButtonIcon, visualCatalog.UiInventoryIconSprite, Color.white, Image.Type.Simple);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                ToggleInventory();
            }

            Refresh();
        }

        private void Refresh()
        {
            RefreshStats();
            RefreshInventory();
            RefreshResources();
            RefreshQuest();
            RefreshTrainingLog();
            RefreshMessage();
        }

        private void RefreshStats()
        {
            DreamyCharacterStats stats = Stats;
            if (stats != null)
            {
                SetFill(healthBar, stats.MaxHealth > 0f ? stats.CurrentHealth / stats.MaxHealth : 0f);
                SetFill(staminaBar, stats.MaxStamina > 0f ? stats.CurrentStamina / stats.MaxStamina : 0f);
                SetText(healthLabel, "HP " + Mathf.CeilToInt(stats.CurrentHealth) + "/" + Mathf.CeilToInt(stats.MaxHealth));
                SetText(staminaLabel, "STA " + Mathf.CeilToInt(stats.CurrentStamina) + "/" + Mathf.CeilToInt(stats.MaxStamina));
            }

            DreamyExperience experience = Experience;
            if (experience != null)
            {
                SetFill(expBar, experience.ExpToNextLevel > 0 ? (float)experience.CurrentExp / experience.ExpToNextLevel : 0f);
                SetText(expLabel, "LV " + experience.Level + " EXP " + experience.CurrentExp + "/" + experience.ExpToNextLevel);
            }
        }

        private void RefreshInventory()
        {
            if (inventoryTitleLabel == null)
            {
                return;
            }

            DreamyInventory inventory = Inventory;
            int itemCount = inventory != null ? inventory.Items.Count : 0;
            inventoryTitleLabel.text = "Inventory " + itemCount + "/" + InventorySlotCount;

            for (int i = 0; i < InventorySlotCount; i++)
            {
                DreamyInventorySlot slot = inventory != null && i < inventory.Items.Count ? inventory.Items[i] : null;
                bool hasItem = slot != null && slot.Quantity > 0;

                if (inventorySlotBackgrounds[i] != null)
                {
                    inventorySlotBackgrounds[i].color = hasItem
                        ? new Color(0.13f, 0.16f, 0.2f, 0.96f)
                        : new Color(0.05f, 0.06f, 0.075f, 0.86f);
                }

                if (inventorySlotIcons[i] != null)
                {
                    inventorySlotIcons[i].sprite = hasItem ? GetItemSprite(slot.ItemId) : null;
                    inventorySlotIcons[i].color = hasItem ? Color.white : new Color(1f, 1f, 1f, 0f);
                }

                SetText(inventorySlotNames[i], hasItem ? ShortenDisplayName(slot.DisplayName) : string.Empty);
                SetText(inventorySlotQuantities[i], hasItem ? "x" + slot.Quantity : string.Empty);
            }

            RefreshConsumableSlots(inventory);
            RefreshQuickSlots(inventory);
        }

        private void RefreshConsumableSlots(DreamyInventory inventory)
        {
            int consumableIndex = 0;
            int itemCount = inventory != null ? inventory.Items.Count : 0;
            for (int i = 0; i < itemCount && consumableIndex < ConsumableSlotCount; i++)
            {
                DreamyInventorySlot slot = inventory.Items[i];
                if (slot == null || slot.Quantity <= 0 || !IsConsumableItem(slot.ItemId))
                {
                    continue;
                }

                SetConsumableSlot(consumableIndex, slot);
                consumableIndex++;
            }

            for (int i = consumableIndex; i < ConsumableSlotCount; i++)
            {
                SetConsumableSlot(i, null);
            }
        }

        private void SetConsumableSlot(int index, DreamyInventorySlot slot)
        {
            if (index < 0 || index >= ConsumableSlotCount)
            {
                return;
            }

            bool hasItem = slot != null && slot.Quantity > 0;
            Sprite sprite = hasItem ? GetItemSprite(slot.ItemId) : null;

            if (consumableSlotBackgrounds[index] != null)
            {
                consumableSlotBackgrounds[index].color = hasItem
                    ? new Color(0.13f, 0.16f, 0.2f, 0.98f)
                    : new Color(0.055f, 0.068f, 0.085f, 0.78f);
            }

            if (consumableSlotIcons[index] != null)
            {
                consumableSlotIcons[index].sprite = sprite;
                consumableSlotIcons[index].color = hasItem ? Color.white : new Color(1f, 1f, 1f, 0f);
            }

            SetText(consumableSlotNames[index], hasItem ? ShortenText(slot.DisplayName, 12) : string.Empty);
            SetText(consumableSlotQuantities[index], hasItem ? "x" + slot.Quantity : string.Empty);

            if (consumableDragSources[index] != null)
            {
                consumableDragSources[index].Bind(
                    this,
                    hasItem ? slot.ItemId : DreamyItemId.Custom,
                    hasItem ? slot.DisplayName : string.Empty,
                    sprite,
                    hasItem);
            }
        }

        private void RefreshQuickSlots(DreamyInventory inventory)
        {
            for (int i = 0; i < QuickSlotCount; i++)
            {
                bool hasItem = quickSlotAssigned[i] && inventory != null && inventory.GetQuantity(quickSlotItemIds[i]) > 0;
                if (!hasItem)
                {
                    quickSlotAssigned[i] = false;
                }

                if (quickSlotBackgrounds[i] != null)
                {
                    quickSlotBackgrounds[i].color = hasItem
                        ? new Color(0.16f, 0.19f, 0.24f, 0.98f)
                        : new Color(0.07f, 0.084f, 0.105f, 0.84f);
                }

                if (quickSlotIcons[i] != null)
                {
                    quickSlotIcons[i].sprite = hasItem ? GetItemSprite(quickSlotItemIds[i]) : null;
                    quickSlotIcons[i].color = hasItem ? Color.white : new Color(1f, 1f, 1f, 0f);
                }

                SetText(quickSlotLabels[i], "Q" + (i + 1));
                SetText(quickSlotNames[i], hasItem ? ShortenText(quickSlotDisplayNames[i], 12) : string.Empty);
            }
        }

        private static bool IsConsumableItem(DreamyItemId itemId)
        {
            switch (itemId)
            {
                case DreamyItemId.Food:
                case DreamyItemId.Meat:
                case DreamyItemId.Potion:
                case DreamyItemId.Crop:
                case DreamyItemId.CraftedMeal:
                    return true;
                default:
                    return false;
            }
        }

        private static string ShortenText(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, Mathf.Max(0, maxLength - 1)) + ".";
        }

        public void BeginInventoryItemDrag(DreamyItemId itemId, string displayName, Sprite sprite, PointerEventData eventData)
        {
            draggedItemId = itemId;
            draggedDisplayName = displayName;
            draggedSprite = sprite;
            hasDraggedItem = true;
            EnsureInventoryDragGhost();
            UpdateInventoryItemDrag(eventData);
        }

        public void UpdateInventoryItemDrag(PointerEventData eventData)
        {
            if (!hasDraggedItem || inventoryDragGhost == null || inventoryWindow == null || eventData == null)
            {
                return;
            }

            RectTransform windowRect = inventoryWindow.GetComponent<RectTransform>();
            RectTransform ghostRect = inventoryDragGhost.GetComponent<RectTransform>();
            if (windowRect == null || ghostRect == null)
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(windowRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                ghostRect.anchoredPosition = localPoint;
            }
        }

        public void EndInventoryItemDrag()
        {
            if (inventoryDragGhost != null)
            {
                Destroy(inventoryDragGhost);
                inventoryDragGhost = null;
                inventoryDragGhostImage = null;
            }

            hasDraggedItem = false;
            draggedSprite = null;
            draggedDisplayName = string.Empty;
        }

        public void AssignDraggedItemToQuickSlot(int quickSlotIndex)
        {
            if (!hasDraggedItem || quickSlotIndex < 0 || quickSlotIndex >= QuickSlotCount)
            {
                return;
            }

            quickSlotAssigned[quickSlotIndex] = true;
            quickSlotItemIds[quickSlotIndex] = draggedItemId;
            quickSlotDisplayNames[quickSlotIndex] = draggedDisplayName;
            RefreshQuickSlots(Inventory);
        }

        private void EnsureInventoryDragGhost()
        {
            if (inventoryWindow == null)
            {
                return;
            }

            if (inventoryDragGhost == null)
            {
                inventoryDragGhost = new GameObject("Inventory Drag Ghost");
                inventoryDragGhost.transform.SetParent(inventoryWindow.transform, false);
                inventoryDragGhostImage = inventoryDragGhost.AddComponent<Image>();
                inventoryDragGhostImage.preserveAspect = true;
                inventoryDragGhostImage.raycastTarget = false;

                CanvasGroup canvasGroup = inventoryDragGhost.AddComponent<CanvasGroup>();
                canvasGroup.blocksRaycasts = false;
                canvasGroup.alpha = 0.86f;

                RectTransform rect = inventoryDragGhost.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(88f, 88f);
            }

            inventoryDragGhost.transform.SetAsLastSibling();
            if (inventoryDragGhostImage != null)
            {
                inventoryDragGhostImage.sprite = draggedSprite;
                inventoryDragGhostImage.color = draggedSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            }
        }

        private void RefreshResources()
        {
            DreamyGameState state = DreamyGameState.Instance;
            if (resourcesLabel == null || state == null)
            {
                return;
            }

            resourcesLabel.text = "Wood " + state.Wood + "    Gold " + state.Gold + "    Food " + state.Food;
            DreamyPlayerProgression playerProgression = Progression;
            if (progressionLabel != null)
            {
                progressionLabel.text = playerProgression != null
                    ? "Coins " + playerProgression.Coins + "    Skill Points " + playerProgression.SkillPoints + "    Unlock Tokens " + playerProgression.UnlockTokens + "    Unlocks " + playerProgression.UnlockCount
                    : "Coins 0    Skill Points 0    Unlock Tokens 0    Unlocks 0";
            }
        }

        private void RefreshQuest()
        {
            if (questLabel == null)
            {
                return;
            }

            if (questLog == null && player != null)
            {
                questLog = player.GetComponent<DreamyQuestLog>();
            }

            questLabel.text = questLog != null ? questLog.BuildHudSummary(3) : "Quest: Not ready";
        }

        private void RefreshTrainingLog()
        {
            if (trainingLogLabel == null)
            {
                return;
            }

            string history = trainingLogEntries.Count > 0
                ? string.Join("\n", trainingLogEntries.ToArray())
                : "No hits yet.";
            trainingLogLabel.text = "TRAINING DUMMY\nHP INF\nHits " + trainingTotalHits
                + "    Damage " + FormatCombatValue(trainingTotalDamage)
                + "\n\n" + history;
        }

        private void RefreshMessage()
        {
            if (messageLabel == null)
            {
                return;
            }

            messageLabel.text = Time.time <= messageUntil ? message : string.Empty;
        }

        private void HandlePickup(DreamyResourcePickup pickup, Transform collector)
        {
            if (collector == null || player == null || collector.GetComponentInParent<DreamyMobilePlayer>() != player)
            {
                return;
            }

            ShowMessage("Picked up " + pickup.DisplayName + " x" + pickup.Amount);
        }

        private void HandlePickupRejected(DreamyResourcePickup pickup, Transform collector)
        {
            if (collector == null || player == null || collector.GetComponentInParent<DreamyMobilePlayer>() != player)
            {
                return;
            }

            ShowMessage("Inventory full");
        }

        private void HandleResourceCollected(DreamyResourceType resourceType, int amount)
        {
            ShowMessage("Collected " + resourceType + " x" + amount);
        }

        private void HandleTrainingDummyHitRecorded(DreamyTrainingDummy dummy, DreamyTrainingDummyHitRecord record)
        {
            trainingTotalHits = record.HitCount;
            trainingTotalDamage = record.TotalDamage;

            string status = string.Empty;
            if (record.SlowDuration > 0f && record.SlowMultiplier < 1f)
            {
                status = "  Slow " + Mathf.RoundToInt((1f - record.SlowMultiplier) * 100f) + "%";
            }

            if (record.StunDuration > 0f)
            {
                status += "  Stun " + FormatCombatValue(record.StunDuration) + "s";
            }

            trainingLogEntries.Enqueue(
                FormatCombatTime(record.Time)
                + "s  #" + record.HitCount
                + "  DMG " + FormatCombatValue(record.Damage)
                + "  TOTAL " + FormatCombatValue(record.TotalDamage)
                + status);
            while (trainingLogEntries.Count > TrainingLogLineCount)
            {
                trainingLogEntries.Dequeue();
            }

            ShowMessage("Dummy hit #" + record.HitCount + " for " + FormatCombatValue(record.Damage));
            RefreshTrainingLog();
        }

        private void ResetTrainingDummyLog()
        {
            DreamyTrainingDummy dummy = FindAnyObjectByType<DreamyTrainingDummy>();
            if (dummy != null)
            {
                dummy.ResetCounters();
            }

            trainingLogEntries.Clear();
            trainingTotalHits = 0;
            trainingTotalDamage = 0f;
            ShowMessage("Dummy log reset");
            RefreshTrainingLog();
        }

        private static string FormatCombatValue(float value)
        {
            return value.ToString("0.#");
        }

        private static string FormatCombatTime(float value)
        {
            return value.ToString("0.00");
        }

        private void ShowMessage(string value)
        {
            message = value;
            messageUntil = Time.time + 2.3f;
        }

        private void ToggleInventory()
        {
            SetInventoryVisible(inventoryWindow == null || !inventoryWindow.activeSelf);
        }

        private void SetInventoryVisible(bool visible)
        {
            if (inventoryWindow != null)
            {
                inventoryWindow.SetActive(visible);
                if (visible)
                {
                    inventoryWindow.transform.SetAsLastSibling();
                }
            }

            RefreshInventory();
        }

        private void SelectInventoryTab(bool showInventory)
        {
            if (inventoryTabContent != null)
            {
                inventoryTabContent.SetActive(showInventory);
            }

            if (weaponsTabContent != null)
            {
                weaponsTabContent.SetActive(!showInventory);
            }

            SetInventoryTabVisual(inventoryTabImage, showInventory);
            SetInventoryTabVisual(weaponsTabImage, !showInventory);
            EndInventoryItemDrag();
        }

        private static void SetInventoryTabVisual(Image image, bool active)
        {
            if (image == null)
            {
                return;
            }

            image.color = active
                ? new Color(0.16f, 0.2f, 0.27f, 0.98f)
                : new Color(0.055f, 0.064f, 0.08f, 0.92f);
        }

        private Sprite GetItemSprite(DreamyItemId itemId)
        {
            Sprite sprite = visualCatalog != null ? visualCatalog.GetItemSprite(itemId) : null;
            return sprite != null ? sprite : CreateUiSprite(new Color(0.42f, 0.52f, 0.62f, 1f));
        }

        private static string ShortenDisplayName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Length <= 12 ? value : value.Substring(0, 11) + ".";
        }

        private void BuildInventoryWindow(Transform parent)
        {
            inventoryWindow = CreatePanel(parent, "Prototype Inventory Window", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Image panelImage = inventoryWindow.GetComponent<Image>();
            if (panelImage != null)
            {
                inventoryWindowImage = panelImage;
                panelImage.color = new Color(0.015f, 0.02f, 0.03f, 0.97f);
                panelImage.raycastTarget = true;
            }

            inventoryTabButton = CreateInventoryTab(inventoryWindow.transform, "INVENTORY", new Vector2(54f, -34f), true);
            inventoryTabImage = inventoryTabButton.GetComponent<Image>();
            weaponsTabButton = CreateInventoryTab(inventoryWindow.transform, "WEAPONS", new Vector2(298f, -34f), false);
            weaponsTabImage = weaponsTabButton.GetComponent<Image>();
            closeInventoryButton = CreateButton(inventoryWindow.transform, "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-46f, -36f), new Vector2(72f, 64f));

            inventoryTabContent = new GameObject("Inventory Tab Content");
            inventoryTabContent.transform.SetParent(inventoryWindow.transform, false);
            RectTransform inventoryContentRect = inventoryTabContent.AddComponent<RectTransform>();
            inventoryContentRect.anchorMin = Vector2.zero;
            inventoryContentRect.anchorMax = Vector2.one;
            inventoryContentRect.offsetMin = Vector2.zero;
            inventoryContentRect.offsetMax = Vector2.zero;

            weaponsTabContent = new GameObject("Weapons Tab Content");
            weaponsTabContent.transform.SetParent(inventoryWindow.transform, false);
            RectTransform weaponsContentRect = weaponsTabContent.AddComponent<RectTransform>();
            weaponsContentRect.anchorMin = Vector2.zero;
            weaponsContentRect.anchorMax = Vector2.one;
            weaponsContentRect.offsetMin = Vector2.zero;
            weaponsContentRect.offsetMax = Vector2.zero;

            GameObject bagPanel = CreateInventorySection(inventoryTabContent.transform, "Inventory Bag Panel", "BAG", new Vector2(54f, -122f), new Vector2(930f, 820f));
            inventoryTitleLabel = CreateText(bagPanel.transform, "Inventory 0/" + InventorySlotCount, 30, TextAnchor.MiddleRight, new Vector2(578f, -24f), new Vector2(304f, 44f));

            GameObject gridRoot = new GameObject("Inventory Grid");
            gridRoot.transform.SetParent(bagPanel.transform, false);
            RectTransform gridRect = gridRoot.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0f, 1f);
            gridRect.anchorMax = new Vector2(0f, 1f);
            gridRect.pivot = new Vector2(0f, 1f);
            gridRect.anchoredPosition = new Vector2(38f, -92f);
            gridRect.sizeDelta = new Vector2(
                InventoryColumns * InventorySlotSize + (InventoryColumns - 1) * InventorySlotGap,
                InventoryRows * InventorySlotSize + (InventoryRows - 1) * InventorySlotGap);

            for (int row = 0; row < InventoryRows; row++)
            {
                for (int column = 0; column < InventoryColumns; column++)
                {
                    int index = row * InventoryColumns + column;
                    CreateInventorySlot(gridRoot.transform, index, row, column);
                }
            }

            GameObject consumablePanel = CreateInventorySection(inventoryTabContent.transform, "Consumable Panel", "CONSUMABLE", new Vector2(1024f, -122f), new Vector2(840f, 236f));
            BuildConsumableLane(consumablePanel.transform);

            GameObject quickPanel = CreateInventorySection(inventoryTabContent.transform, "Quick Slot Panel", "QUICK SLOT", new Vector2(1024f, -654f), new Vector2(840f, 300f));
            BuildQuickSlots(quickPanel.transform);

            CreateInventorySection(inventoryTabContent.transform, "Inventory Middle Draft Panel", "ITEM PREVIEW", new Vector2(1024f, -392f), new Vector2(840f, 214f));
            BuildWeaponsMockup(weaponsTabContent.transform);
            SelectInventoryTab(true);

            inventoryWindow.SetActive(false);
        }

        private void CreateInventorySlot(Transform parent, int index, int row, int column)
        {
            GameObject slot = new GameObject("Inventory Slot " + (index + 1).ToString("00"));
            slot.transform.SetParent(parent, false);
            Image background = slot.AddComponent<Image>();
            background.sprite = CreateUiSprite(Color.white);
            background.color = new Color(0.05f, 0.06f, 0.075f, 0.86f);
            background.raycastTarget = false;
            inventorySlotBackgrounds[index] = background;

            RectTransform rect = slot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(
                column * (InventorySlotSize + InventorySlotGap),
                -row * (InventorySlotSize + InventorySlotGap));
            rect.sizeDelta = new Vector2(InventorySlotSize, InventorySlotSize);

            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(slot.transform, false);
            Image icon = iconObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.color = new Color(1f, 1f, 1f, 0f);
            inventorySlotIcons[index] = icon;

            RectTransform iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, 8f);
            iconRect.sizeDelta = new Vector2(48f, 48f);

            Text quantity = CreateText(slot.transform, string.Empty, 16, TextAnchor.UpperRight, new Vector2(8f, -6f), new Vector2(60f, 22f));
            AddTextOutline(quantity);
            inventorySlotQuantities[index] = quantity;

            Text itemName = CreateText(slot.transform, string.Empty, 12, TextAnchor.MiddleCenter, new Vector2(6f, -56f), new Vector2(64f, 18f));
            AddTextOutline(itemName);
            inventorySlotNames[index] = itemName;
        }

        private Button CreateInventoryTab(Transform parent, string label, Vector2 position, bool active)
        {
            Button button = CreateButton(parent, label, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), position, new Vector2(220f, 64f));
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = active ? new Color(0.16f, 0.2f, 0.27f, 0.98f) : new Color(0.055f, 0.064f, 0.08f, 0.92f);
            }

            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.fontSize = 24;
                text.fontStyle = FontStyle.Bold;
                AddTextOutline(text);
            }

            return button;
        }

        private GameObject CreateInventorySection(Transform parent, string name, string title, Vector2 position, Vector2 size)
        {
            GameObject section = CreatePanel(parent, name, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), position, size);
            Image image = section.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0.035f, 0.044f, 0.058f, 0.88f);
                image.raycastTarget = true;
            }

            Outline outline = section.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.18f);
            outline.effectDistance = new Vector2(2f, -2f);

            Text heading = CreateText(section.transform, title, 30, TextAnchor.MiddleLeft, new Vector2(26f, -22f), new Vector2(size.x - 52f, 46f));
            heading.fontStyle = FontStyle.Bold;
            heading.color = new Color(0.96f, 0.92f, 0.78f, 1f);
            AddTextOutline(heading);
            return section;
        }

        private void BuildConsumableLane(Transform parent)
        {
            for (int i = 0; i < ConsumableSlotCount; i++)
            {
                GameObject slot = CreateDraftSlot(parent, "Consumable Slot " + (i + 1).ToString("00"), new Vector2(36f + i * (ConsumableSlotSize + ConsumableSlotGap), -88f), new Vector2(ConsumableSlotSize, ConsumableSlotSize), out Image background, out Image icon);
                consumableSlotBackgrounds[i] = background;
                consumableSlotIcons[i] = icon;
                consumableSlotQuantities[i] = CreateText(slot.transform, string.Empty, 18, TextAnchor.UpperRight, new Vector2(10f, -8f), new Vector2(74f, 24f));
                AddTextOutline(consumableSlotQuantities[i]);
                consumableSlotNames[i] = CreateText(parent, string.Empty, 16, TextAnchor.MiddleCenter, new Vector2(36f + i * (ConsumableSlotSize + ConsumableSlotGap), -190f), new Vector2(ConsumableSlotSize, 24f));
                AddTextOutline(consumableSlotNames[i]);
                DreamyPrototypeInventoryDragSource dragSource = slot.AddComponent<DreamyPrototypeInventoryDragSource>();
                dragSource.Bind(this, DreamyItemId.Custom, string.Empty, null, false);
                consumableDragSources[i] = dragSource;
            }
        }

        private void BuildQuickSlots(Transform parent)
        {
            for (int i = 0; i < QuickSlotCount; i++)
            {
                GameObject slot = CreateDraftSlot(parent, "Quick Slot " + (i + 1).ToString("00"), new Vector2(64f + i * (QuickSlotSize + QuickSlotGap), -74f), new Vector2(QuickSlotSize, QuickSlotSize), out Image background, out Image icon);
                quickSlotBackgrounds[i] = background;
                quickSlotIcons[i] = icon;

                DreamyPrototypeQuickSlotDropTarget dropTarget = slot.AddComponent<DreamyPrototypeQuickSlotDropTarget>();
                dropTarget.Bind(this, i);

                quickSlotLabels[i] = CreateText(slot.transform, "Q" + (i + 1), 24, TextAnchor.UpperLeft, new Vector2(14f, -12f), new Vector2(88f, 34f));
                quickSlotLabels[i].fontStyle = FontStyle.Bold;
                AddTextOutline(quickSlotLabels[i]);
                quickSlotNames[i] = CreateText(slot.transform, string.Empty, 20, TextAnchor.MiddleCenter, new Vector2(14f, -130f), new Vector2(146f, 30f));
                AddTextOutline(quickSlotNames[i]);
            }
        }

        private void BuildWeaponsMockup(Transform parent)
        {
            GameObject storagePanel = CreateInventorySection(parent, "Weapon Storage Panel", "WEAPON STORAGE", new Vector2(54f, -122f), new Vector2(930f, 820f));
            const int weaponColumns = 7;
            const int weaponRows = 4;
            const float weaponSlotSize = 96f;
            const float weaponSlotGap = 22f;
            for (int row = 0; row < weaponRows; row++)
            {
                for (int column = 0; column < weaponColumns; column++)
                {
                    CreateDraftSlot(storagePanel.transform, "Weapon Draft Slot", new Vector2(48f + column * (weaponSlotSize + weaponSlotGap), -102f - row * (weaponSlotSize + weaponSlotGap)), new Vector2(weaponSlotSize, weaponSlotSize), out _, out _);
                }
            }

            GameObject equipPanel = CreateInventorySection(parent, "Equipped Weapons Panel", "EQUIPPED", new Vector2(1024f, -122f), new Vector2(840f, 820f));
            CreateDraftSlot(equipPanel.transform, "Primary Weapon Slot", new Vector2(72f, -118f), new Vector2(206f, 206f), out _, out _);
            CreateDraftSlot(equipPanel.transform, "Secondary Weapon Slot", new Vector2(326f, -118f), new Vector2(206f, 206f), out _, out _);
            CreateDraftSlot(equipPanel.transform, "Tool Weapon Slot", new Vector2(590f, -118f), new Vector2(152f, 152f), out _, out _);
            CreateDraftSlot(equipPanel.transform, "Weapon Mod Slot A", new Vector2(96f, -398f), new Vector2(138f, 138f), out _, out _);
            CreateDraftSlot(equipPanel.transform, "Weapon Mod Slot B", new Vector2(282f, -398f), new Vector2(138f, 138f), out _, out _);
            CreateDraftSlot(equipPanel.transform, "Weapon Mod Slot C", new Vector2(468f, -398f), new Vector2(138f, 138f), out _, out _);
        }

        private GameObject CreateDraftSlot(Transform parent, string name, Vector2 position, Vector2 size, out Image background, out Image icon)
        {
            GameObject slot = new GameObject(name);
            slot.transform.SetParent(parent, false);
            background = slot.AddComponent<Image>();
            background.sprite = CreateUiSprite(Color.white);
            background.color = new Color(0.055f, 0.068f, 0.085f, 0.82f);
            background.raycastTarget = true;

            Outline outline = slot.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.2f);
            outline.effectDistance = new Vector2(2f, -2f);

            RectTransform rect = slot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(slot.transform, false);
            icon = iconObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.color = new Color(1f, 1f, 1f, 0f);

            RectTransform iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(12f, 12f);
            iconRect.offsetMax = new Vector2(-12f, -12f);
            return slot;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            Image image = panel.AddComponent<Image>();
            image.sprite = CreateUiSprite(Color.white);
            image.color = new Color(0.03f, 0.04f, 0.055f, 0.84f);
            image.raycastTarget = false;

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return panel;
        }

        private static DreamySegmentedBar CreateBar(Transform parent, string label, Color fillColor, Vector2 position, Vector2 size, int fontSize, out Text text)
        {
            GameObject root = new GameObject(label + " Bar");
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = position;
            rootRect.sizeDelta = size;

            DreamySegmentedBar bar = root.AddComponent<DreamySegmentedBar>();
            bar.Build(rootRect.sizeDelta, fillColor);

            text = CreateText(root.transform, label, fontSize, TextAnchor.MiddleLeft, new Vector2(30f, -4f), new Vector2(Mathf.Max(1f, size.x - 52f), Mathf.Max(1f, size.y - 8f)));
            AddTextOutline(text);
            return bar;
        }

        private static Button CreateButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            GameObject buttonObject = new GameObject(label + " Button");
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.AddComponent<Image>();
            image.sprite = CreateUiSprite(new Color(0.08f, 0.1f, 0.13f, 0.88f));
            image.color = new Color(0.08f, 0.1f, 0.13f, 0.88f);
            Button button = buttonObject.AddComponent<Button>();

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Text text = CreateText(buttonObject.transform, label, 27, TextAnchor.MiddleCenter, Vector2.zero, size);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            return button;
        }

        private static Image CreateButtonIcon(Transform parent, Vector2 size, Vector2 position)
        {
            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(parent, false);
            Image icon = iconObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.color = new Color(1f, 1f, 1f, 0f);

            RectTransform rect = icon.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return icon;
        }

        private static void PlaceButtonLabel(Button button, string label, int fontSize, Vector2 position, Vector2 size)
        {
            if (button == null)
            {
                return;
            }

            Text text = button.GetComponentInChildren<Text>();
            if (text == null)
            {
                return;
            }

            text.text = label;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            AddTextOutline(text);

            RectTransform rect = text.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void ApplySprite(Image image, Sprite sprite, Color color, Image.Type imageType)
        {
            if (image == null || sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            image.color = color;
            image.type = imageType;
            image.preserveAspect = false;
            if (imageType == Image.Type.Filled)
            {
                image.fillMethod = Image.FillMethod.Horizontal;
                image.fillOrigin = (int)Image.OriginHorizontal.Left;
            }
        }

        private static void ApplySolidSprite(Image image, Color color, Image.Type imageType)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = CreateUiSprite(Color.white);
            image.color = color;
            image.type = imageType;
            image.preserveAspect = false;
            if (imageType == Image.Type.Filled)
            {
                image.fillMethod = Image.FillMethod.Horizontal;
                image.fillOrigin = (int)Image.OriginHorizontal.Left;
            }
        }

        private static void HideImage(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.enabled = false;
            image.raycastTarget = false;
        }

        private static void ApplySegmentedBar(DreamySegmentedBar bar, Sprite baseSprite, Sprite fillSprite, Color fillTint)
        {
            if (bar != null)
            {
                bar.ApplySprites(baseSprite, fillSprite, fillTint);
            }
        }

        private static void ApplyButtonSprites(Button button, Sprite regularSprite, Sprite pressedSprite)
        {
            if (button == null || regularSprite == null)
            {
                return;
            }

            Image image = button.targetGraphic as Image;
            if (image == null)
            {
                image = button.GetComponent<Image>();
            }

            if (image != null)
            {
                image.sprite = regularSprite;
                image.color = Color.white;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                button.targetGraphic = image;
            }

            button.transition = Selectable.Transition.SpriteSwap;
            SpriteState state = button.spriteState;
            state.pressedSprite = pressedSprite != null ? pressedSprite : regularSprite;
            state.highlightedSprite = pressedSprite != null ? pressedSprite : regularSprite;
            state.selectedSprite = regularSprite;
            button.spriteState = state;
        }

        private static Text CreateText(Transform parent, string value, int fontSize, TextAnchor alignment, Vector2 position, Vector2 size)
        {
            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;

            RectTransform rect = text.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return text;
        }

        private static void AddTextOutline(Text text)
        {
            if (text == null || text.GetComponent<Outline>() != null)
            {
                return;
            }

            Outline outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.82f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        private static Sprite CreateUiSprite(Color color)
        {
            Texture2D texture = new Texture2D(4, 4);
            Color[] pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
        }

        private static void SetFill(DreamySegmentedBar bar, float value)
        {
            if (bar != null)
            {
                bar.SetFill(value);
            }
        }

        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }
    }

    public sealed class DreamyPrototypeInventoryDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private DreamyPrototypeRuntimeHud owner;
        private DreamyItemId itemId;
        private string displayName;
        private Sprite sprite;
        private bool canDrag;

        public void Bind(DreamyPrototypeRuntimeHud hud, DreamyItemId sourceItemId, string sourceDisplayName, Sprite sourceSprite, bool enabled)
        {
            owner = hud;
            itemId = sourceItemId;
            displayName = sourceDisplayName;
            sprite = sourceSprite;
            canDrag = enabled;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!canDrag || owner == null)
            {
                return;
            }

            owner.BeginInventoryItemDrag(itemId, displayName, sprite, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!canDrag || owner == null)
            {
                return;
            }

            owner.UpdateInventoryItemDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (owner != null)
            {
                owner.EndInventoryItemDrag();
            }
        }
    }

    public sealed class DreamyPrototypeQuickSlotDropTarget : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private DreamyPrototypeRuntimeHud owner;
        private int quickSlotIndex;
        private Image background;
        private Color baseColor;

        public void Bind(DreamyPrototypeRuntimeHud hud, int targetQuickSlotIndex)
        {
            owner = hud;
            quickSlotIndex = targetQuickSlotIndex;
            background = GetComponent<Image>();
            if (background != null)
            {
                baseColor = background.color;
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (owner != null)
            {
                owner.AssignDraggedItemToQuickSlot(quickSlotIndex);
            }

            RestoreColor();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (background != null)
            {
                background.color = new Color(0.24f, 0.3f, 0.38f, 0.98f);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            RestoreColor();
        }

        private void RestoreColor()
        {
            if (background != null)
            {
                background.color = baseColor;
            }
        }
    }
}
