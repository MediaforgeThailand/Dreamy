using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dreamy.Extraction
{
    public sealed class ExtractionPrototypeBootstrap : MonoBehaviour
    {
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private Vector2 playerStart = Vector2.zero;
        [SerializeField] private Vector2 enemyStart = new Vector2(3f, 0f);
        [SerializeField] private Vector2 extractPointPosition = new Vector2(-3f, 0f);
        [SerializeField] private ExtractionMapData mapData;
        [SerializeField] private ExtractionWeaponData startingWeapon;
        [SerializeField] private ExtractionEnemyData enemyData;

        private void Start()
        {
            if (buildOnStart)
            {
                BuildPrototypeIfNeeded();
            }
        }

        [ContextMenu("Build Extraction Prototype")]
        public void BuildPrototypeIfNeeded()
        {
            ExtractionPlayerController existingPlayer = UnityEngine.Object.FindAnyObjectByType<ExtractionPlayerController>();
            if (existingPlayer != null)
            {
                return;
            }

            ExtractionGameSession session = ExtractionGameSession.GetOrCreate();
            ExtractionBaseStorage baseStorage = session.BaseStorage;

            GameObject player = CreatePlayer();
            ExtractionPlayerInput input = player.GetComponent<ExtractionPlayerInput>();
            ExtractionRoomFlowController roomFlow = CreateRoomFlow();
            player.GetComponent<ExtractionDeathLootHandler>().Configure(roomFlow);
            CreateEnemy(player.transform, roomFlow);
            CreateStarterLoot();
            ExtractionExtractPoint extractPoint = CreateExtractPoint(baseStorage);
            CreateRunManager(player.GetComponent<ExtractionHealth>(), extractPoint);
            CreateBaseScaffoldObjects();
            CreateMobileHud(input, player, roomFlow, baseStorage);
        }

        public void Configure(ExtractionMapData map, ExtractionWeaponData weapon, ExtractionEnemyData enemy)
        {
            mapData = map;
            startingWeapon = weapon;
            enemyData = enemy;
        }

        private GameObject CreatePlayer()
        {
            GameObject player = new GameObject("Extraction Player");
            player.transform.position = playerStart;

            SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
            renderer.sprite = ExtractionPlaceholderSprite.Get(new Color(0.25f, 0.68f, 1f, 1f));
            renderer.sortingOrder = 10;

            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;

            CircleCollider2D collider = player.AddComponent<CircleCollider2D>();
            collider.radius = 0.35f;

            player.AddComponent<ExtractionHealth>();
            player.AddComponent<ExtractionStamina>();
            player.AddComponent<ExtractionRunInventory>();
            player.AddComponent<ExtractionDeathLootHandler>();
            ExtractionWeaponController weapon = player.AddComponent<ExtractionWeaponController>();
            ExtractionPlayerInput input = player.AddComponent<ExtractionPlayerInput>();
            ExtractionPlayerController controller = player.AddComponent<ExtractionPlayerController>();
            controller.BindInput(input);

            weapon.SetWeapon(startingWeapon, true);
            return player;
        }

        private void CreateEnemy(Transform playerTarget, ExtractionRoomFlowController roomFlow)
        {
            GameObject enemy = new GameObject("Extraction Enemy Placeholder");
            enemy.transform.position = enemyStart;

            SpriteRenderer renderer = enemy.AddComponent<SpriteRenderer>();
            renderer.sprite = ExtractionPlaceholderSprite.Get(new Color(1f, 0.25f, 0.25f, 1f));
            renderer.sortingOrder = 10;

            Rigidbody2D body = enemy.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;

            CircleCollider2D collider = enemy.AddComponent<CircleCollider2D>();
            collider.radius = 0.35f;

            enemy.AddComponent<ExtractionHealth>();
            enemy.AddComponent<ExtractionLootSpawner>();
            ExtractionEnemyController controller = enemy.AddComponent<ExtractionEnemyController>();
            controller.Configure(enemyData, playerTarget, roomFlow);
        }

        private void CreateStarterLoot()
        {
            ExtractionLootTableData starterLootTable = mapData != null && mapData.StartRoom != null
                ? mapData.StartRoom.RoomRewardTable
                : null;
            if (starterLootTable == null)
            {
                return;
            }

            ExtractionLootSpawner spawner = new GameObject("Extraction Starter Loot Spawner").AddComponent<ExtractionLootSpawner>();
            spawner.SpawnLoot(starterLootTable, playerStart + new Vector2(1.35f, 0.75f));
            Destroy(spawner.gameObject);
        }

        private ExtractionExtractPoint CreateExtractPoint(ExtractionBaseStorage baseStorage)
        {
            GameObject extractPoint = new GameObject("Extraction Extract Point");
            extractPoint.transform.position = extractPointPosition;

            SpriteRenderer renderer = extractPoint.AddComponent<SpriteRenderer>();
            renderer.sprite = ExtractionPlaceholderSprite.Get(new Color(0.25f, 1f, 0.45f, 1f));
            renderer.sortingOrder = 5;

            CircleCollider2D collider = extractPoint.AddComponent<CircleCollider2D>();
            collider.radius = 0.6f;
            collider.isTrigger = true;

            ExtractionExtractPoint point = extractPoint.AddComponent<ExtractionExtractPoint>();
            point.SetBaseStorage(baseStorage);
            return point;
        }

        private static void CreateRunManager(ExtractionHealth playerHealth, ExtractionExtractPoint extractPoint)
        {
            GameObject runManagerObject = new GameObject("Extraction Run Manager");
            ExtractionRunManager runManager = runManagerObject.AddComponent<ExtractionRunManager>();
            runManager.Configure("Prototype_Base", playerHealth, extractPoint);
        }

        private void CreateBaseScaffoldObjects()
        {
            CreateScaffoldObject<ExtractionBaseUpgradeStation>("Extraction Base Upgrade Station", new Vector2(-4.2f, 1.1f), new Color(0.4f, 0.75f, 1f, 1f));
            CreateScaffoldObject<ExtractionCraftingStation>("Extraction Crafting Station", new Vector2(-4.2f, 2f), new Color(0.9f, 0.7f, 0.25f, 1f));
            CreateScaffoldObject<ExtractionRepairStation>("Extraction Repair Station", new Vector2(-4.2f, 2.9f), new Color(0.8f, 0.55f, 0.25f, 1f));
            CreateScaffoldObject<ExtractionFarmPlot>("Extraction Farm Plot", new Vector2(4.2f, 1.1f), new Color(0.35f, 0.9f, 0.35f, 1f));
            CreateScaffoldObject<ExtractionMarketStation>("Extraction Market Station", new Vector2(4.2f, 2f), new Color(0.95f, 0.55f, 0.75f, 1f));
            CreateScaffoldObject<ExtractionNpcAgent>("Extraction NPC Placeholder", new Vector2(4.2f, 2.9f), new Color(0.75f, 0.6f, 1f, 1f));
            CreateScaffoldObject<ExtractionQuestLog>("Extraction Quest Log", new Vector2(0f, 3.8f), new Color(0.65f, 0.9f, 1f, 1f));
        }

        private static T CreateScaffoldObject<T>(string objectName, Vector2 position, Color color) where T : Component
        {
            GameObject scaffold = new GameObject(objectName);
            scaffold.transform.position = position;
            SpriteRenderer renderer = scaffold.AddComponent<SpriteRenderer>();
            renderer.sprite = ExtractionPlaceholderSprite.Get(color);
            renderer.sortingOrder = 4;
            return scaffold.AddComponent<T>();
        }

        private ExtractionRoomFlowController CreateRoomFlow()
        {
            GameObject roomFlow = new GameObject("Extraction Room Flow");
            roomFlow.AddComponent<ExtractionLootSpawner>();
            ExtractionRoomFlowController controller = roomFlow.AddComponent<ExtractionRoomFlowController>();
            controller.Configure(mapData, roomFlow.transform);
            return controller;
        }

        private void CreateMobileHud(
            ExtractionPlayerInput input,
            GameObject player,
            ExtractionRoomFlowController roomFlow,
            ExtractionBaseStorage baseStorage)
        {
            CreateEventSystemIfNeeded();

            GameObject canvasObject = new GameObject("Extraction Mobile HUD");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            canvasObject.AddComponent<DreamySafeArea>();

            DreamyVirtualJoystick joystick = CreateJoystick(canvasObject.transform);
            input.BindJoystick(joystick);

            CreateActionButton(canvasObject.transform, input, ExtractionActionButtonType.Attack, "ATK", new Vector2(-150f, 160f));
            CreateActionButton(canvasObject.transform, input, ExtractionActionButtonType.Dodge, "ROLL", new Vector2(-294f, 120f));
            CreateActionButton(canvasObject.transform, input, ExtractionActionButtonType.Skill, "SKILL", new Vector2(-150f, 304f));
            CreateActionButton(canvasObject.transform, input, ExtractionActionButtonType.Interact, "USE", new Vector2(-438f, 160f));
            CreatePlayerHud(
                canvasObject.transform,
                player.GetComponent<ExtractionHealth>(),
                player.GetComponent<ExtractionStamina>(),
                player.GetComponent<ExtractionWeaponController>(),
                player.GetComponent<ExtractionRunInventory>(),
                baseStorage,
                roomFlow);
            CreateRoomChoicePanel(canvasObject.transform, roomFlow);
        }

        private static void CreatePlayerHud(
            Transform parent,
            ExtractionHealth health,
            ExtractionStamina stamina,
            ExtractionWeaponController weapon,
            ExtractionRunInventory runInventory,
            ExtractionBaseStorage baseStorage,
            ExtractionRoomFlowController roomFlow)
        {
            GameObject hudObject = new GameObject("Extraction Player HUD");
            hudObject.transform.SetParent(parent, false);
            ExtractionPlayerHud hud = hudObject.AddComponent<ExtractionPlayerHud>();

            GameObject statusPanel = CreateHudPanel(
                parent,
                "Extraction Status Panel",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(24f, -24f),
                new Vector2(500f, 220f));

            Image healthFill;
            Text healthLabel;
            CreateHudBar(statusPanel.transform, "Health", new Color(0.95f, 0.18f, 0.22f, 1f), new Vector2(20f, -24f), out healthFill, out healthLabel);

            Image staminaFill;
            Text staminaLabel;
            CreateHudBar(statusPanel.transform, "Stamina", new Color(0.22f, 0.82f, 0.38f, 1f), new Vector2(20f, -84f), out staminaFill, out staminaLabel);

            Image durabilityFill;
            Text weaponLabel;
            CreateHudBar(statusPanel.transform, "Weapon", new Color(0.95f, 0.74f, 0.22f, 1f), new Vector2(20f, -144f), out durabilityFill, out weaponLabel);

            Text roomLabel = CreateHudText(statusPanel.transform, "Room", 20, TextAnchor.MiddleLeft, new Vector2(20f, -190f), new Vector2(460f, 28f));

            GameObject runInventoryPanel = CreateHudPanel(
                parent,
                "Extraction Run Inventory Panel",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-24f, -24f),
                new Vector2(420f, 250f));
            Text runInventoryLabel = CreateHudText(runInventoryPanel.transform, "Run Inventory", 21, TextAnchor.UpperLeft, new Vector2(18f, -18f), new Vector2(384f, 214f));

            GameObject baseStoragePanel = CreateHudPanel(
                parent,
                "Extraction Base Storage Panel",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-24f, -292f),
                new Vector2(420f, 190f));
            Text baseStorageLabel = CreateHudText(baseStoragePanel.transform, "Base Storage", 20, TextAnchor.UpperLeft, new Vector2(18f, -18f), new Vector2(384f, 154f));

            Text messageLabel = CreateHudText(parent, string.Empty, 28, TextAnchor.MiddleCenter, new Vector2(0f, -48f), new Vector2(740f, 60f));
            RectTransform messageRect = messageLabel.GetComponent<RectTransform>();
            messageRect.anchorMin = new Vector2(0.5f, 1f);
            messageRect.anchorMax = new Vector2(0.5f, 1f);
            messageRect.pivot = new Vector2(0.5f, 1f);
            messageLabel.color = new Color(1f, 0.96f, 0.72f, 1f);

            hud.BindViews(
                healthFill,
                staminaFill,
                durabilityFill,
                healthLabel,
                staminaLabel,
                weaponLabel,
                roomLabel,
                runInventoryLabel,
                baseStorageLabel,
                messageLabel);
            hud.Bind(health, stamina, weapon, runInventory, baseStorage, roomFlow);
        }

        private static void CreateDebugPanel(
            Transform parent,
            ExtractionHealth health,
            ExtractionWeaponController weapon,
            ExtractionRunInventory runInventory,
            ExtractionRoomFlowController roomFlow)
        {
            GameObject panel = new GameObject("Extraction Debug Panel");
            panel.transform.SetParent(parent, false);

            Text text = panel.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.alignment = TextAnchor.UpperLeft;
            text.color = Color.white;
            text.raycastTarget = false;

            RectTransform rect = text.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, -24f);
            rect.sizeDelta = new Vector2(520f, 220f);

            ExtractionPrototypeDebugPanel debugPanel = panel.AddComponent<ExtractionPrototypeDebugPanel>();
            debugPanel.Bind(text, health, weapon, runInventory, roomFlow);
        }

        private static DreamyVirtualJoystick CreateJoystick(Transform parent)
        {
            GameObject joystickRoot = new GameObject("Extraction Joystick");
            joystickRoot.transform.SetParent(parent, false);
            Image rootImage = joystickRoot.AddComponent<Image>();
            rootImage.sprite = ExtractionPlaceholderSprite.Get(new Color(1f, 1f, 1f, 0.25f));
            rootImage.color = new Color(1f, 1f, 1f, 0f);
            rootImage.raycastTarget = true;

            RectTransform rootRect = joystickRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.zero;
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = new Vector2(190f, 170f);
            rootRect.sizeDelta = new Vector2(252f, 252f);

            GameObject handle = new GameObject("Extraction Joystick Handle");
            handle.transform.SetParent(joystickRoot.transform, false);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.sprite = ExtractionPlaceholderSprite.Get(new Color(0.4f, 0.7f, 1f, 0.9f));
            handleImage.color = new Color(0.4f, 0.7f, 1f, 0.94f);
            handleImage.raycastTarget = false;
            handleImage.preserveAspect = true;

            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new Vector2(104f, 104f);

            DreamyVirtualJoystick joystick = joystickRoot.AddComponent<DreamyVirtualJoystick>();
            joystick.Bind(handleRect, 96f);
            return joystick;
        }

        private static GameObject CreateHudPanel(
            Transform parent,
            string objectName,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject panel = new GameObject(objectName);
            panel.transform.SetParent(parent, false);
            Image image = panel.AddComponent<Image>();
            image.sprite = ExtractionPlaceholderSprite.Get(new Color(0.03f, 0.04f, 0.05f, 0.82f));
            image.color = new Color(0.03f, 0.04f, 0.05f, 0.82f);
            image.raycastTarget = false;

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return panel;
        }

        private static void CreateHudBar(
            Transform parent,
            string label,
            Color fillColor,
            Vector2 anchoredPosition,
            out Image fillImage,
            out Text labelText)
        {
            GameObject root = new GameObject(label + " Bar");
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = anchoredPosition;
            rootRect.sizeDelta = new Vector2(460f, 42f);

            GameObject background = new GameObject("Background");
            background.transform.SetParent(root.transform, false);
            Image backgroundImage = background.AddComponent<Image>();
            backgroundImage.sprite = ExtractionPlaceholderSprite.Get(new Color(0f, 0f, 0f, 0.48f));
            backgroundImage.color = new Color(0f, 0f, 0f, 0.48f);
            backgroundImage.raycastTarget = false;
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(background.transform, false);
            fillImage = fill.AddComponent<Image>();
            fillImage.sprite = ExtractionPlaceholderSprite.Get(fillColor);
            fillImage.color = fillColor;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.raycastTarget = false;
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            labelText = CreateHudText(root.transform, label, 21, TextAnchor.MiddleLeft, new Vector2(14f, -4f), new Vector2(432f, 34f));
        }

        private static Text CreateHudText(
            Transform parent,
            string value,
            int fontSize,
            TextAnchor alignment,
            Vector2 anchoredPosition,
            Vector2 size)
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
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return text;
        }

        private static void CreateRoomChoicePanel(Transform parent, ExtractionRoomFlowController roomFlow)
        {
            GameObject panel = new GameObject("Extraction Room Choice Panel");
            panel.transform.SetParent(parent, false);
            Image image = panel.AddComponent<Image>();
            image.sprite = ExtractionPlaceholderSprite.Get(new Color(0.03f, 0.04f, 0.05f, 0.85f));
            image.color = new Color(0.03f, 0.04f, 0.05f, 0.85f);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(960f, 360f);

            Button[] buttons = new Button[3];
            Text[] labels = new Text[3];
            for (int i = 0; i < buttons.Length; i++)
            {
                GameObject buttonObject = new GameObject("Room Choice " + (i + 1));
                buttonObject.transform.SetParent(panel.transform, false);
                Image buttonImage = buttonObject.AddComponent<Image>();
                buttonImage.sprite = ExtractionPlaceholderSprite.Get(new Color(0.14f, 0.18f, 0.22f, 0.95f));
                buttonImage.color = new Color(0.14f, 0.18f, 0.22f, 0.95f);
                buttons[i] = buttonObject.AddComponent<Button>();

                RectTransform rect = buttonObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(180f + i * 300f, 0f);
                rect.sizeDelta = new Vector2(260f, 280f);

                GameObject labelObject = new GameObject("Label");
                labelObject.transform.SetParent(buttonObject.transform, false);
                Text label = labelObject.AddComponent<Text>();
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize = 24;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
                label.raycastTarget = false;
                labels[i] = label;

                RectTransform labelRect = label.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(16f, 16f);
                labelRect.offsetMax = new Vector2(-16f, -16f);
            }

            ExtractionRoomChoicePanel choicePanel = panel.AddComponent<ExtractionRoomChoicePanel>();
            choicePanel.Bind(roomFlow, panel, buttons, labels);
        }

        private static void CreateActionButton(Transform parent, ExtractionPlayerInput input, ExtractionActionButtonType action, string label, Vector2 anchoredPosition)
        {
            GameObject buttonObject = new GameObject("Extraction " + action + " Button");
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.AddComponent<Image>();
            image.sprite = ExtractionPlaceholderSprite.Get(new Color(0.05f, 0.06f, 0.08f, 0.8f));
            image.color = new Color(0.05f, 0.06f, 0.08f, 0.8f);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(136f, 136f);

            ExtractionActionButton actionButton = buttonObject.AddComponent<ExtractionActionButton>();
            actionButton.Bind(input, action);

            GameObject textObject = new GameObject("Label");
            textObject.transform.SetParent(buttonObject.transform, false);
            Text text = textObject.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 28;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;

            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        private static void CreateEventSystemIfNeeded()
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }
    }
}
