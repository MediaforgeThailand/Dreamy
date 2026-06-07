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
        private const int PrototypeInventorySlotCount = 60;
        private const int PrototypeMonsterSpawnCount = 4;
        private static readonly Color PlayerDamagePopupColor = new Color(1f, 0.2f, 0.16f, 1f);
        private static readonly Color PlayerHitFlashColor = new Color(1f, 0.38f, 0.36f, 1f);

        [SerializeField] private DreamyPrototypeVisualCatalog visualCatalog;
        [SerializeField] private DreamyMonsterCatalog monsterCatalog;
        [SerializeField] private bool spawnStarterPickups = true;

        private DreamyMobilePlayer player;
        private DreamyPrototypeRuntimeHud hud;
        private DreamyPrototypeInteractionUi interactionUi;
        private bool starterPickupsSpawned;
        private bool prototypeLifeSystemsSpawned;

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
            EnsureHud();
            EnsureInteractionUi();
            EnsureStarterPickups();
            EnsurePrototypeMonster();
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

            if (player.GetComponent<DreamyExperience>() == null)
            {
                player.gameObject.AddComponent<DreamyExperience>();
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
                hud = existingHud;
                hud.Bind(player, visualCatalog);
                return;
            }

            GameObject hudRoot = new GameObject(HudObjectName);
            Canvas canvas = hudRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = hudRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            hudRoot.AddComponent<GraphicRaycaster>();

            hud = hudRoot.AddComponent<DreamyPrototypeRuntimeHud>();
            hud.Build(hudRoot.transform);
            hud.Bind(player, visualCatalog);
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
        private const float InventorySlotSize = 76f;
        private const float InventorySlotGap = 8f;

        private Image statusPanelImage;
        private Image resourcePanelImage;
        private Image inventoryWindowImage;
        private Image healthBarBackground;
        private Image healthFill;
        private Image staminaBarBackground;
        private Image staminaFill;
        private Image expBarBackground;
        private Image expFill;
        private Text healthLabel;
        private Text staminaLabel;
        private Text expLabel;
        private Text resourcesLabel;
        private Text messageLabel;
        private Button attackButton;
        private Button dodgeButton;
        private Button inventoryButton;
        private Button closeInventoryButton;
        private Image attackButtonIcon;
        private Image dodgeButtonIcon;
        private Image inventoryButtonIcon;
        private GameObject inventoryWindow;
        private Text inventoryTitleLabel;
        private readonly Image[] inventorySlotBackgrounds = new Image[InventorySlotCount];
        private readonly Image[] inventorySlotIcons = new Image[InventorySlotCount];
        private readonly Text[] inventorySlotNames = new Text[InventorySlotCount];
        private readonly Text[] inventorySlotQuantities = new Text[InventorySlotCount];
        private DreamyMobilePlayer player;
        private DreamyPlayerCombat playerCombat;
        private DreamyPrototypeVisualCatalog visualCatalog;
        private string message;
        private float messageUntil;

        private DreamyCharacterStats Stats => player != null ? player.CharacterStats : null;
        private DreamyInventory Inventory => player != null ? player.Inventory : null;
        private DreamyExperience Experience => player != null ? player.Experience : null;

        private void OnEnable()
        {
            DreamyResourcePickup.PickedUp += HandlePickup;
            DreamyResourcePickup.PickupRejected += HandlePickupRejected;
            DreamyResourceNode.ResourceCollected += HandleResourceCollected;
        }

        private void OnDisable()
        {
            DreamyResourcePickup.PickedUp -= HandlePickup;
            DreamyResourcePickup.PickupRejected -= HandlePickupRejected;
            DreamyResourceNode.ResourceCollected -= HandleResourceCollected;
        }

        public void Build(Transform parent)
        {
            GameObject statusPanel = CreatePanel(parent, "Prototype Status Panel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -22f), new Vector2(470f, 210f));
            statusPanelImage = statusPanel.GetComponent<Image>();
            CreateBar(statusPanel.transform, "HP", new Color(0.9f, 0.18f, 0.22f, 1f), new Vector2(18f, -20f), out healthBarBackground, out healthFill, out healthLabel);
            CreateBar(statusPanel.transform, "STA", new Color(0.22f, 0.8f, 0.4f, 1f), new Vector2(18f, -78f), out staminaBarBackground, out staminaFill, out staminaLabel);
            CreateBar(statusPanel.transform, "EXP", new Color(0.28f, 0.62f, 1f, 1f), new Vector2(18f, -136f), out expBarBackground, out expFill, out expLabel);

            inventoryButton = CreateButton(parent, "BAG", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(92f, 92f));
            inventoryButtonIcon = CreateButtonIcon(inventoryButton.transform, new Vector2(44f, 44f), new Vector2(0f, 8f));
            PlaceButtonLabel(inventoryButton, "BAG", 15, new Vector2(0f, -31f), new Vector2(78f, 22f));
            BuildInventoryWindow(parent);

            GameObject resourcePanel = CreatePanel(parent, "Prototype Resource Panel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(480f, 68f));
            resourcePanelImage = resourcePanel.GetComponent<Image>();
            resourcesLabel = CreateText(resourcePanel.transform, "Resources", 24, TextAnchor.MiddleCenter, new Vector2(0f, -10f), new Vector2(480f, 48f));
            RectTransform resourcesRect = resourcesLabel.GetComponent<RectTransform>();
            resourcesRect.anchorMin = new Vector2(0.5f, 1f);
            resourcesRect.anchorMax = new Vector2(0.5f, 1f);
            resourcesRect.pivot = new Vector2(0.5f, 1f);

            messageLabel = CreateText(parent, string.Empty, 28, TextAnchor.MiddleCenter, new Vector2(0f, -102f), new Vector2(760f, 54f));
            RectTransform messageRect = messageLabel.GetComponent<RectTransform>();
            messageRect.anchorMin = new Vector2(0.5f, 1f);
            messageRect.anchorMax = new Vector2(0.5f, 1f);
            messageRect.pivot = new Vector2(0.5f, 1f);
            messageLabel.color = new Color(1f, 0.95f, 0.68f, 1f);

            attackButton = CreateButton(parent, "ATK", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-160f, 142f), new Vector2(122f, 122f));
            attackButtonIcon = CreateButtonIcon(attackButton.transform, new Vector2(54f, 54f), new Vector2(0f, 13f));
            PlaceButtonLabel(attackButton, "ATK", 18, new Vector2(0f, -39f), new Vector2(96f, 24f));
            dodgeButton = CreateButton(parent, "ROLL", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-302f, 104f), new Vector2(96f, 96f));
            dodgeButtonIcon = CreateButtonIcon(dodgeButton.transform, new Vector2(42f, 42f), new Vector2(0f, 10f));
            PlaceButtonLabel(dodgeButton, "ROLL", 14, new Vector2(0f, -31f), new Vector2(78f, 20f));
        }

        public void Bind(DreamyMobilePlayer targetPlayer, DreamyPrototypeVisualCatalog catalog)
        {
            player = targetPlayer;
            playerCombat = player != null ? player.GetComponent<DreamyPlayerCombat>() : null;
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

            if (inventoryButton != null)
            {
                inventoryButton.onClick.RemoveAllListeners();
                inventoryButton.onClick.AddListener(ToggleInventory);
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
            ApplySolidSprite(statusPanelImage, new Color(0.025f, 0.032f, 0.045f, 0.78f), Image.Type.Simple);
            ApplySolidSprite(resourcePanelImage, new Color(0.025f, 0.032f, 0.045f, 0.78f), Image.Type.Simple);
            ApplySolidSprite(inventoryWindowImage, new Color(0.025f, 0.032f, 0.045f, 0.96f), Image.Type.Simple);

            ApplySolidSprite(healthBarBackground, new Color(0f, 0f, 0f, 0.62f), Image.Type.Simple);
            ApplySolidSprite(staminaBarBackground, new Color(0f, 0f, 0f, 0.62f), Image.Type.Simple);
            ApplySolidSprite(expBarBackground, new Color(0f, 0f, 0f, 0.62f), Image.Type.Simple);
            ApplySolidSprite(healthFill, new Color(0.9f, 0.18f, 0.22f, 1f), Image.Type.Filled);
            ApplySolidSprite(staminaFill, new Color(0.22f, 0.8f, 0.4f, 1f), Image.Type.Filled);
            ApplySolidSprite(expFill, new Color(0.28f, 0.62f, 1f, 1f), Image.Type.Filled);

            for (int i = 0; i < inventorySlotBackgrounds.Length; i++)
            {
                ApplySolidSprite(inventorySlotBackgrounds[i], new Color(0.05f, 0.06f, 0.075f, 0.9f), Image.Type.Simple);
            }

            if (visualCatalog == null)
            {
                return;
            }

            ApplyButtonSprites(attackButton, visualCatalog.UiRedButtonSprite, visualCatalog.UiRedButtonPressedSprite);
            ApplyButtonSprites(dodgeButton, visualCatalog.UiBlueButtonSprite, visualCatalog.UiBlueButtonPressedSprite);
            ApplyButtonSprites(inventoryButton, visualCatalog.UiBlueButtonSprite, visualCatalog.UiBlueButtonPressedSprite);
            ApplyButtonSprites(closeInventoryButton, visualCatalog.UiRedButtonSprite, visualCatalog.UiRedButtonPressedSprite);

            ApplySprite(attackButtonIcon, visualCatalog.UiAttackIconSprite, Color.white, Image.Type.Simple);
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
            RefreshMessage();
        }

        private void RefreshStats()
        {
            DreamyCharacterStats stats = Stats;
            if (stats != null)
            {
                SetFill(healthFill, stats.MaxHealth > 0f ? stats.CurrentHealth / stats.MaxHealth : 0f);
                SetFill(staminaFill, stats.MaxStamina > 0f ? stats.CurrentStamina / stats.MaxStamina : 0f);
                SetText(healthLabel, "HP " + Mathf.CeilToInt(stats.CurrentHealth) + "/" + Mathf.CeilToInt(stats.MaxHealth));
                SetText(staminaLabel, "STA " + Mathf.CeilToInt(stats.CurrentStamina) + "/" + Mathf.CeilToInt(stats.MaxStamina));
            }

            DreamyExperience experience = Experience;
            if (experience != null)
            {
                SetFill(expFill, experience.ExpToNextLevel > 0 ? (float)experience.CurrentExp / experience.ExpToNextLevel : 0f);
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
        }

        private void RefreshResources()
        {
            DreamyGameState state = DreamyGameState.Instance;
            if (resourcesLabel == null || state == null)
            {
                return;
            }

            resourcesLabel.text = "Wood " + state.Wood + "    Gold " + state.Gold + "    Food " + state.Food;
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

            return value.Length <= 9 ? value : value.Substring(0, 8) + ".";
        }

        private void BuildInventoryWindow(Transform parent)
        {
            inventoryWindow = CreatePanel(parent, "Prototype Inventory Window", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 660f));
            Image panelImage = inventoryWindow.GetComponent<Image>();
            if (panelImage != null)
            {
                inventoryWindowImage = panelImage;
                panelImage.color = new Color(0.025f, 0.032f, 0.045f, 0.96f);
                panelImage.raycastTarget = true;
            }

            inventoryTitleLabel = CreateText(inventoryWindow.transform, "Inventory 0/" + InventorySlotCount, 30, TextAnchor.MiddleLeft, new Vector2(28f, -22f), new Vector2(520f, 46f));
            closeInventoryButton = CreateButton(inventoryWindow.transform, "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -22f), new Vector2(54f, 46f));

            GameObject gridRoot = new GameObject("Inventory Grid");
            gridRoot.transform.SetParent(inventoryWindow.transform, false);
            RectTransform gridRect = gridRoot.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0f, 1f);
            gridRect.anchorMax = new Vector2(0f, 1f);
            gridRect.pivot = new Vector2(0f, 1f);
            gridRect.anchoredPosition = new Vector2(44f, -104f);
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
            iconRect.sizeDelta = new Vector2(42f, 42f);

            Text quantity = CreateText(slot.transform, string.Empty, 14, TextAnchor.UpperRight, new Vector2(6f, -5f), new Vector2(64f, 20f));
            AddTextOutline(quantity);
            inventorySlotQuantities[index] = quantity;

            Text itemName = CreateText(slot.transform, string.Empty, 11, TextAnchor.MiddleCenter, new Vector2(4f, -54f), new Vector2(68f, 18f));
            AddTextOutline(itemName);
            inventorySlotNames[index] = itemName;
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

        private static void CreateBar(Transform parent, string label, Color fillColor, Vector2 position, out Image backgroundImage, out Image fill, out Text text)
        {
            GameObject root = new GameObject(label + " Bar");
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = position;
            rootRect.sizeDelta = new Vector2(434f, 42f);

            GameObject background = new GameObject("Background");
            background.transform.SetParent(root.transform, false);
            backgroundImage = background.AddComponent<Image>();
            backgroundImage.sprite = CreateUiSprite(Color.white);
            backgroundImage.color = new Color(0f, 0f, 0f, 0.45f);
            backgroundImage.raycastTarget = false;
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            GameObject fillObject = new GameObject("Fill");
            fillObject.transform.SetParent(background.transform, false);
            fill = fillObject.AddComponent<Image>();
            fill.sprite = CreateUiSprite(Color.white);
            fill.color = fillColor;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.raycastTarget = false;
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            text = CreateText(root.transform, label, 20, TextAnchor.MiddleLeft, new Vector2(12f, -4f), new Vector2(410f, 34f));
            AddTextOutline(text);
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

        private static void SetFill(Image image, float value)
        {
            if (image != null)
            {
                image.fillAmount = Mathf.Clamp01(value);
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
}
