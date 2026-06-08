using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Dreamy
{
    public sealed class DreamyPrototypeInteractionUi : MonoBehaviour
    {
        private static DreamyPrototypeInteractionUi instance;

        private sealed class RequirementInfoTooltip
        {
            public GameObject Root;
            public Text Title;
            public Text Description;
            public Text Location;
        }

        private DreamyMobilePlayer player;
        private DreamyPrototypeInteraction interaction;
        private DreamyPrototypeVisualCatalog catalog;
        private Canvas canvas;
        private GameObject promptPanel;
        private Text promptLabel;
        private Text messageLabel;
        private Button useButton;
        private GameObject activeWindow;
        private Sprite runtimeCraftingBackgroundSprite;
        private readonly Dictionary<string, Sprite> runtimeCraftingIconSprites = new Dictionary<string, Sprite>();
        private float messageUntil;

        public static DreamyPrototypeInteractionUi Instance => instance;

        private void Awake()
        {
            instance = this;
        }

        private void OnEnable()
        {
            DreamyPrototypeInteraction.PromptChanged += HandlePromptChanged;
            DreamyPrototypeInteraction.InteractionMessage += HandleInteractionMessage;
        }

        private void OnDisable()
        {
            DreamyPrototypeInteraction.PromptChanged -= HandlePromptChanged;
            DreamyPrototypeInteraction.InteractionMessage -= HandleInteractionMessage;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }

            if (runtimeCraftingBackgroundSprite != null)
            {
                Destroy(runtimeCraftingBackgroundSprite);
            }

            foreach (Sprite sprite in runtimeCraftingIconSprites.Values)
            {
                if (sprite != null)
                {
                    Destroy(sprite);
                }
            }

            runtimeCraftingIconSprites.Clear();
        }

        private void Update()
        {
            if (messageLabel != null && Time.time > messageUntil)
            {
                messageLabel.text = string.Empty;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseWindow();
            }
        }

        public void Configure(DreamyMobilePlayer targetPlayer, DreamyPrototypeVisualCatalog visualCatalog)
        {
            player = targetPlayer;
            interaction = player != null ? player.GetComponent<DreamyPrototypeInteraction>() : null;
            catalog = visualCatalog;
            EnsureCanvas();
            BuildPromptControls();
        }

        public static void OpenCrafting(DreamyPrototypeCraftingStation station, DreamyMobilePlayer targetPlayer)
        {
            DreamyPrototypeInteractionUi ui = EnsureInstance(targetPlayer);
            ui.ShowCrafting(station, targetPlayer);
        }

        public static void OpenVault(DreamyPrototypeVaultNpc npc, DreamyMobilePlayer targetPlayer)
        {
            DreamyPrototypeInteractionUi ui = EnsureInstance(targetPlayer);
            ui.ShowVault(npc, targetPlayer);
        }

        private static DreamyPrototypeInteractionUi EnsureInstance(DreamyMobilePlayer targetPlayer)
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<DreamyPrototypeInteractionUi>();
            }

            if (instance == null)
            {
                GameObject uiObject = new GameObject("Dreamy Prototype Interaction UI");
                instance = uiObject.AddComponent<DreamyPrototypeInteractionUi>();
            }

            if (instance.player == null || instance.catalog == null)
            {
                instance.Configure(targetPlayer, Resources.Load<DreamyPrototypeVisualCatalog>("DreamyPrototypeVisualCatalog"));
            }

            return instance;
        }

        private void EnsureCanvas()
        {
            if (canvas != null)
            {
                return;
            }

            canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private void BuildPromptControls()
        {
            if (promptLabel != null)
            {
                return;
            }

            promptPanel = CreatePanel(transform, "Interaction Prompt Panel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 104f), new Vector2(560f, 54f));
            promptLabel = CreateText(promptPanel.transform, string.Empty, 22, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(536f, 42f));
            RectTransform promptTextRect = promptLabel.GetComponent<RectTransform>();
            promptTextRect.anchorMin = Vector2.zero;
            promptTextRect.anchorMax = Vector2.one;
            promptTextRect.offsetMin = new Vector2(12f, 6f);
            promptTextRect.offsetMax = new Vector2(-12f, -6f);

            useButton = CreateButton(transform, "USE", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-420f, 100f), new Vector2(86f, 86f), false);
            useButton.onClick.AddListener(() =>
            {
                if (interaction == null && player != null)
                {
                    interaction = player.GetComponent<DreamyPrototypeInteraction>();
                }

                interaction?.QueueInteract();
            });

            messageLabel = CreateText(transform, string.Empty, 26, TextAnchor.MiddleCenter, new Vector2(0f, -166f), new Vector2(780f, 48f));
            RectTransform messageRect = messageLabel.GetComponent<RectTransform>();
            messageRect.anchorMin = new Vector2(0.5f, 1f);
            messageRect.anchorMax = new Vector2(0.5f, 1f);
            messageRect.pivot = new Vector2(0.5f, 1f);
            messageLabel.color = new Color(1f, 0.96f, 0.72f, 1f);

            ApplyButtonSprites(useButton, catalog != null ? catalog.UiBlueButtonSprite : null, catalog != null ? catalog.UiBlueButtonPressedSprite : null);
            SetPromptVisible(false);
        }

        private void ShowCrafting(DreamyPrototypeCraftingStation station, DreamyMobilePlayer targetPlayer)
        {
            if (station == null || targetPlayer == null)
            {
                return;
            }

            player = targetPlayer;
            CloseWindow();
            SetPromptVisible(false);
            activeWindow = CreateCraftingOverlay("Crafting Window");
            BuildCraftingMenu(activeWindow.transform, station, targetPlayer);
        }

        private void ShowCraftingDetail(DreamyPrototypeCraftingStation station, DreamyMobilePlayer targetPlayer, DreamyPrototypeRecipe recipe)
        {
            if (station == null || targetPlayer == null || recipe == null)
            {
                return;
            }

            player = targetPlayer;
            CloseWindow();
            SetPromptVisible(false);
            activeWindow = CreateCraftingOverlay("Crafting Detail Window");
            BuildCraftingDetail(activeWindow.transform, station, targetPlayer, recipe);
        }

        private GameObject CreateCraftingOverlay(string name)
        {
            GameObject overlay = CreatePanel(transform, name, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            ApplyCraftingBackground(overlay);

            GameObject backgroundWash = CreatePanel(overlay.transform, "Crafting Background Wash", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            SetImageColor(backgroundWash, new Color(0f, 0f, 0f, 0.48f));

            GameObject leftShade = CreatePanel(overlay.transform, "Crafting Left Shade", Vector2.zero, new Vector2(0.42f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            SetImageColor(leftShade, new Color(0f, 0f, 0f, 0.38f));

            GameObject rightShade = CreatePanel(overlay.transform, "Crafting Right Shade", new Vector2(0.42f, 0f), Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            SetImageColor(rightShade, new Color(0f, 0f, 0f, 0.12f));

            Button closeButton = CreatePlainButton(overlay.transform, "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-36f, -28f), new Vector2(58f, 52f), new Color(0.09f, 0.1f, 0.12f, 0.82f));
            closeButton.onClick.AddListener(CloseWindow);
            return overlay;
        }

        private void ApplyCraftingBackground(GameObject overlay)
        {
            Image image = overlay != null ? overlay.GetComponent<Image>() : null;
            if (image == null)
            {
                return;
            }

            Sprite backgroundSprite = GetCraftingBackgroundSprite();

            if (backgroundSprite == null)
            {
                image.color = new Color(0f, 0f, 0f, 0.86f);
                return;
            }

            image.sprite = backgroundSprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
        }

        private Sprite GetCraftingBackgroundSprite()
        {
            if (catalog != null && catalog.CraftingBackgroundSprite != null)
            {
                return catalog.CraftingBackgroundSprite;
            }

            Sprite backgroundSprite = Resources.Load<Sprite>("BG_blacksmith");
            if (backgroundSprite != null)
            {
                return backgroundSprite;
            }

            if (runtimeCraftingBackgroundSprite != null)
            {
                return runtimeCraftingBackgroundSprite;
            }

            Texture2D backgroundTexture = Resources.Load<Texture2D>("BG_blacksmith");
            if (backgroundTexture == null)
            {
                return null;
            }

            runtimeCraftingBackgroundSprite = Sprite.Create(
                backgroundTexture,
                new Rect(0f, 0f, backgroundTexture.width, backgroundTexture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            runtimeCraftingBackgroundSprite.name = "BG_blacksmith_RuntimeSprite";
            return runtimeCraftingBackgroundSprite;
        }

        private void BuildCraftingMenu(Transform root, DreamyPrototypeCraftingStation station, DreamyMobilePlayer targetPlayer)
        {
            GameObject leftPanel = CreatePanel(root, "Crafting Menu Panel", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(44f, 0f), new Vector2(650f, -120f));
            SetImageColor(leftPanel, new Color(0f, 0f, 0f, 0.42f));

            Text title = CreateText(leftPanel.transform, "CRAFTING", 72, TextAnchor.MiddleLeft, new Vector2(48f, -54f), new Vector2(520f, 86f));
            title.fontStyle = FontStyle.Bold;
            Text subtitle = CreateText(leftPanel.transform, station.DisplayName, 24, TextAnchor.MiddleLeft, new Vector2(54f, -140f), new Vector2(460f, 34f));
            subtitle.color = new Color(0.86f, 0.9f, 0.95f, 1f);

            string[] categories = { "ALL", "MEAL", "TOOL", "MATERIAL" };
            for (int i = 0; i < categories.Length; i++)
            {
                Button category = CreatePlainButton(leftPanel.transform, categories[i], new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(54f + i * 124f, -190f), new Vector2(104f, 44f), i == 0 ? new Color(0.22f, 0.28f, 0.36f, 0.94f) : new Color(0.03f, 0.035f, 0.045f, 0.74f));
                category.onClick.AddListener(() => DreamyPrototypeInteraction.PublishMessage("Category filter is a placeholder"));
            }

            CreateText(leftPanel.transform, "Recipes", 30, TextAnchor.MiddleLeft, new Vector2(54f, -254f), new Vector2(360f, 42f));
            CreateDivider(leftPanel.transform, new Vector2(54f, -304f), 520f);

            const int columns = 3;
            const float slotSize = 112f;
            const float columnGap = 54f;
            const float rowGap = 158f;
            Vector2 gridOrigin = new Vector2(68f, -340f);
            int count = Mathf.Min(station.Recipes.Count, 9);
            for (int i = 0; i < count; i++)
            {
                DreamyPrototypeRecipe recipe = station.Recipes[i];
                int row = i / columns;
                int column = i % columns;
                Vector2 slotPosition = gridOrigin + new Vector2(column * (slotSize + columnGap), -row * rowGap);

                Button recipeButton = CreatePlainButton(leftPanel.transform, string.Empty, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), slotPosition, new Vector2(slotSize, slotSize), new Color(0.07f, 0.08f, 0.1f, 0.95f));
                GameObject iconBox = CreatePlaceholderBox(recipeButton.transform, "Item Placeholder", new Vector2(12f, -12f), new Vector2(slotSize - 24f, slotSize - 24f), string.Empty, 18);
                AddIconToBox(iconBox, GetRecipeIconSprite(recipe), 10f);
                Text label = CreateText(leftPanel.transform, ShortenText(recipe.DisplayName, 16), 19, TextAnchor.UpperCenter, slotPosition + new Vector2(-10f, -118f), new Vector2(slotSize + 20f, 50f));
                label.color = new Color(0.93f, 0.95f, 0.98f, 1f);

                DreamyPrototypeRecipe selectedRecipe = recipe;
                recipeButton.onClick.AddListener(() => ShowCraftingDetail(station, targetPlayer, selectedRecipe));
            }

            if (count == 0)
            {
                Text empty = CreateText(leftPanel.transform, "No recipes yet", 24, TextAnchor.MiddleCenter, new Vector2(64f, -340f), new Vector2(500f, 80f));
                empty.color = new Color(0.86f, 0.9f, 0.95f, 1f);
            }

            Text hint = CreateText(root, "SELECT A RECIPE", 28, TextAnchor.MiddleRight, new Vector2(-226f, 84f), new Vector2(360f, 52f));
            RectTransform hintRect = hint.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(1f, 0f);
            hintRect.anchorMax = new Vector2(1f, 0f);
            hintRect.pivot = new Vector2(1f, 0f);

            Button backButton = CreatePlainButton(root, "BACK", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-42f, 40f), new Vector2(140f, 56f), new Color(0.09f, 0.1f, 0.12f, 0.82f));
            backButton.onClick.AddListener(CloseWindow);
        }

        private void BuildCraftingDetail(Transform root, DreamyPrototypeCraftingStation station, DreamyMobilePlayer targetPlayer, DreamyPrototypeRecipe recipe)
        {
            GameObject descriptionPanel = CreatePanel(root, "Crafting Description Panel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(86f, -178f), new Vector2(440f, 230f));
            SetImageColor(descriptionPanel, new Color(0f, 0f, 0f, 0.76f));
            Text title = CreateText(descriptionPanel.transform, recipe.DisplayName, 28, TextAnchor.MiddleLeft, new Vector2(24f, -22f), new Vector2(360f, 42f));
            title.fontStyle = FontStyle.Bold;
            CreateDivider(descriptionPanel.transform, new Vector2(24f, -76f), 388f);
            Text description = CreateText(descriptionPanel.transform, GetRecipeDescription(recipe), 22, TextAnchor.UpperLeft, new Vector2(24f, -94f), new Vector2(388f, 110f));
            description.color = new Color(0.92f, 0.94f, 0.98f, 1f);
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            description.verticalOverflow = VerticalWrapMode.Truncate;

            GameObject largePreview = CreatePlaceholderBox(root, "Large Item Preview Placeholder", new Vector2(640f, -534f), new Vector2(560f, 560f), string.Empty, 32);
            AddIconToBox(largePreview, GetRecipeIconSprite(recipe), 84f);

            GameObject requirementPanel = CreatePanel(root, "Crafting Requirements Panel", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-250f, -6f), new Vector2(420f, 320f));
            SetImageColor(requirementPanel, new Color(0f, 0f, 0f, 0.78f));
            Text requirementsTitle = CreateText(requirementPanel.transform, "Crafting Requirements", 26, TextAnchor.MiddleLeft, new Vector2(24f, -22f), new Vector2(296f, 42f));
            requirementsTitle.fontStyle = FontStyle.Bold;
            GameObject outputIconBox = CreatePlaceholderBox(requirementPanel.transform, "Output Placeholder", new Vector2(334f, -24f), new Vector2(54f, 54f), string.Empty, 12);
            AddIconToBox(outputIconBox, GetRecipeIconSprite(recipe), 6f);
            RequirementInfoTooltip requirementTooltip = CreateRequirementInfoTooltip(requirementPanel.transform);

            int inputCount = Mathf.Min(recipe.Inputs.Count, 4);
            for (int i = 0; i < inputCount; i++)
            {
                CreateRequirementRow(requirementPanel.transform, recipe.Inputs[i], targetPlayer.Inventory, new Vector2(24f, -94f - i * 54f), requirementTooltip);
            }

            if (inputCount == 0)
            {
                Text free = CreateText(requirementPanel.transform, "No materials required", 22, TextAnchor.MiddleLeft, new Vector2(24f, -104f), new Vector2(330f, 36f));
                free.color = new Color(0.86f, 0.9f, 0.95f, 1f);
            }

            CreateDivider(requirementPanel.transform, new Vector2(24f, -244f), 372f);
            Text output = CreateText(requirementPanel.transform, "Craft x" + GetOutputQuantity(recipe) + "  " + GetOutputName(recipe), 22, TextAnchor.MiddleLeft, new Vector2(24f, -262f), new Vector2(260f, 36f));
            output.color = new Color(0.96f, 0.92f, 0.72f, 1f);

            Button craftButton = CreatePlainButton(requirementPanel.transform, "CRAFT", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 22f), new Vector2(128f, 50f), new Color(0.64f, 0.16f, 0.17f, 0.95f));
            craftButton.interactable = CanCraftRecipe(recipe, targetPlayer.Inventory);
            DreamyPrototypeRecipe selectedRecipe = recipe;
            craftButton.onClick.AddListener(() =>
            {
                string message;
                station.TryCraft(selectedRecipe, player != null ? player.Inventory : null, out message);
                DreamyPrototypeInteraction.PublishMessage(message);
                ShowCraftingDetail(station, player, selectedRecipe);
            });

            Button backButton = CreatePlainButton(root, "BACK", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-42f, 40f), new Vector2(140f, 56f), new Color(0.09f, 0.1f, 0.12f, 0.82f));
            backButton.onClick.AddListener(() => ShowCrafting(station, targetPlayer));
        }

        private void ShowVault(DreamyPrototypeVaultNpc npc, DreamyMobilePlayer targetPlayer)
        {
            if (npc == null || targetPlayer == null || npc.Storage == null)
            {
                return;
            }

            player = targetPlayer;
            CloseWindow();
            activeWindow = CreatePanel(transform, "Vault Window", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1040f, 690f));
            CreateText(activeWindow.transform, npc.DisplayName + " Vault", 32, TextAnchor.MiddleLeft, new Vector2(34f, -24f), new Vector2(620f, 48f));
            Button closeButton = CreateButton(activeWindow.transform, "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -24f), new Vector2(56f, 50f), true);
            closeButton.onClick.AddListener(CloseWindow);

            CreateText(activeWindow.transform, "Player", 24, TextAnchor.MiddleLeft, new Vector2(42f, -86f), new Vector2(360f, 36f));
            CreateText(activeWindow.transform, "Vault", 24, TextAnchor.MiddleLeft, new Vector2(548f, -86f), new Vector2(360f, 36f));
            BuildInventoryColumn(activeWindow.transform, targetPlayer.Inventory, npc.Storage, true, new Vector2(42f, -134f), npc, targetPlayer);
            BuildInventoryColumn(activeWindow.transform, npc.Storage, targetPlayer.Inventory, false, new Vector2(548f, -134f), npc, targetPlayer);
        }

        private void BuildInventoryColumn(Transform parent, DreamyInventory source, DreamyInventory target, bool deposit, Vector2 origin, DreamyPrototypeVaultNpc npc, DreamyMobilePlayer targetPlayer)
        {
            if (source == null)
            {
                return;
            }

            int rows = Mathf.Min(9, source.Items.Count);
            for (int i = 0; i < rows; i++)
            {
                DreamyInventorySlot slot = source.Items[i];
                GameObject row = CreatePanel(parent, (deposit ? "Deposit " : "Withdraw ") + i, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), origin + new Vector2(0f, -i * 54f), new Vector2(440f, 46f));
                CreateText(row.transform, slot.DisplayName + " x" + slot.Quantity, 18, TextAnchor.MiddleLeft, new Vector2(16f, -6f), new Vector2(288f, 32f));
                Button moveButton = CreateButton(row.transform, deposit ? ">" : "<", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(56f, 36f), true);
                int slotIndex = i;
                moveButton.onClick.AddListener(() =>
                {
                    bool moved = source.TransferSlotTo(target, slotIndex, int.MaxValue);
                    DreamyPrototypeInteraction.PublishMessage(moved ? "Moved item" : "Cannot move item");
                    ShowVault(npc, targetPlayer);
                });
            }

            if (rows == 0)
            {
                Text empty = CreateText(parent, "Empty", 18, TextAnchor.MiddleLeft, origin + new Vector2(16f, -6f), new Vector2(360f, 32f));
                empty.color = new Color(0.86f, 0.9f, 0.95f, 1f);
            }
        }

        private void CloseWindow()
        {
            if (activeWindow != null)
            {
                Destroy(activeWindow);
                activeWindow = null;
            }

            if (promptLabel != null && !string.IsNullOrWhiteSpace(promptLabel.text))
            {
                SetPromptVisible(true);
            }
        }

        private void HandlePromptChanged(string prompt)
        {
            bool hasPrompt = !string.IsNullOrWhiteSpace(prompt);
            if (promptLabel != null)
            {
                promptLabel.text = prompt;
            }

            if (useButton != null)
            {
                useButton.interactable = hasPrompt;
            }

            SetPromptVisible(hasPrompt);
        }

        private void SetPromptVisible(bool visible)
        {
            if (promptPanel != null)
            {
                promptPanel.SetActive(visible);
            }

            if (useButton != null)
            {
                useButton.gameObject.SetActive(visible);
            }
        }

        private void HandleInteractionMessage(string message)
        {
            if (messageLabel == null)
            {
                return;
            }

            messageLabel.text = message;
            messageUntil = Time.time + 2.4f;
        }

        private Button CreatePlainButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size, Color backgroundColor)
        {
            GameObject buttonObject = new GameObject((string.IsNullOrWhiteSpace(label) ? "Crafting" : label) + " Button");
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.AddComponent<Image>();
            image.sprite = CreateUiSprite(Color.white);
            image.color = backgroundColor;

            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.disabledColor = new Color(0.42f, 0.42f, 0.42f, 0.58f);
            button.colors = colors;

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            if (!string.IsNullOrWhiteSpace(label))
            {
                Text text = CreateText(buttonObject.transform, label, Mathf.RoundToInt(Mathf.Min(size.x, size.y) * 0.34f), TextAnchor.MiddleCenter, Vector2.zero, size);
                RectTransform textRect = text.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
                AddTextOutline(text);
            }

            return button;
        }

        private GameObject CreatePlaceholderBox(Transform parent, string name, Vector2 position, Vector2 size, string label, int fontSize)
        {
            GameObject box = CreatePanel(parent, name, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), position, size);
            SetImageColor(box, new Color(0.08f, 0.095f, 0.12f, 0.96f));

            Outline outline = box.AddComponent<Outline>();
            outline.effectColor = new Color(0.56f, 0.62f, 0.72f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);

            if (!string.IsNullOrWhiteSpace(label))
            {
                Text placeholder = CreateText(box.transform, label, fontSize, TextAnchor.MiddleCenter, Vector2.zero, size);
                placeholder.color = new Color(0.62f, 0.68f, 0.78f, 0.85f);
                RectTransform rect = placeholder.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            return box;
        }

        private static void SetImageColor(GameObject target, Color color)
        {
            Image image = target != null ? target.GetComponent<Image>() : null;
            if (image != null)
            {
                image.color = color;
            }
        }

        private static void CreateDivider(Transform parent, Vector2 position, float width)
        {
            GameObject divider = new GameObject("Divider");
            divider.transform.SetParent(parent, false);
            Image image = divider.AddComponent<Image>();
            image.sprite = CreateUiSprite(Color.white);
            image.color = new Color(1f, 1f, 1f, 0.82f);

            RectTransform rect = divider.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(width, 3f);
        }

        private RequirementInfoTooltip CreateRequirementInfoTooltip(Transform parent)
        {
            GameObject root = CreatePanel(parent, "Requirement Info Tooltip", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(-292f, -116f), new Vector2(292f, 92f));
            SetImageColor(root, new Color(0f, 0f, 0f, 0.9f));
            Image image = root.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = false;
            }

            Outline outline = root.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.32f);
            outline.effectDistance = new Vector2(1f, -1f);

            Text title = CreateText(root.transform, string.Empty, 18, TextAnchor.MiddleLeft, new Vector2(14f, -8f), new Vector2(264f, 24f));
            title.fontStyle = FontStyle.Bold;
            Text description = CreateText(root.transform, string.Empty, 14, TextAnchor.UpperLeft, new Vector2(14f, -34f), new Vector2(264f, 30f));
            description.color = new Color(0.9f, 0.93f, 0.98f, 1f);
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            description.verticalOverflow = VerticalWrapMode.Truncate;
            Text location = CreateText(root.transform, string.Empty, 13, TextAnchor.UpperLeft, new Vector2(14f, -68f), new Vector2(264f, 18f));
            location.color = new Color(0.96f, 0.88f, 0.58f, 1f);
            location.horizontalOverflow = HorizontalWrapMode.Wrap;
            location.verticalOverflow = VerticalWrapMode.Truncate;

            root.SetActive(false);
            return new RequirementInfoTooltip
            {
                Root = root,
                Title = title,
                Description = description,
                Location = location
            };
        }

        private void CreateRequirementRow(Transform parent, DreamyItemStack stack, DreamyInventory inventory, Vector2 position, RequirementInfoTooltip tooltip)
        {
            GameObject placeholder = CreatePlaceholderBox(parent, stack.DisplayName + " Placeholder", position, new Vector2(42f, 42f), string.Empty, 12);
            AddIconToBox(placeholder, GetItemIconSprite(stack.ItemId), 4f);
            Image placeholderImage = placeholder.GetComponent<Image>();
            if (placeholderImage != null)
            {
                placeholderImage.raycastTarget = false;
            }

            Text name = CreateText(parent, stack.DisplayName, 20, TextAnchor.MiddleLeft, position + new Vector2(58f, -2f), new Vector2(180f, 38f));
            name.color = new Color(0.92f, 0.94f, 0.98f, 1f);

            int owned = inventory != null ? inventory.GetQuantity(stack.ItemId) : 0;
            Text count = CreateText(parent, owned + "/" + stack.Quantity, 20, TextAnchor.MiddleRight, position + new Vector2(262f, -2f), new Vector2(100f, 38f));
            count.color = owned >= stack.Quantity ? new Color(0.55f, 0.94f, 0.62f, 1f) : new Color(0.95f, 0.36f, 0.32f, 1f);

            Button infoButton = CreatePlainButton(parent, string.Empty, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), position + new Vector2(0f, 2f), new Vector2(248f, 46f), new Color(1f, 1f, 1f, 0.01f));
            infoButton.gameObject.name = stack.DisplayName + " Requirement Info Button";
            infoButton.transform.SetAsLastSibling();
            DreamyItemStack selectedStack = stack;
            Vector2 selectedPosition = position;
            infoButton.onClick.AddListener(() => ShowRequirementInfo(selectedStack, tooltip, selectedPosition));
        }

        private static void ShowRequirementInfo(DreamyItemStack stack, RequirementInfoTooltip tooltip, Vector2 rowPosition)
        {
            if (stack == null || tooltip == null || tooltip.Root == null)
            {
                return;
            }

            RectTransform tooltipRect = tooltip.Root.GetComponent<RectTransform>();
            if (tooltipRect != null)
            {
                tooltipRect.anchoredPosition = GetRequirementTooltipPosition(rowPosition);
            }

            if (tooltip.Title != null)
            {
                tooltip.Title.text = stack.DisplayName;
            }

            if (tooltip.Description != null)
            {
                tooltip.Description.text = GetItemDescription(stack.ItemId);
            }

            if (tooltip.Location != null)
            {
                tooltip.Location.text = "Find: " + GetItemLocation(stack.ItemId);
            }

            tooltip.Root.SetActive(true);
            tooltip.Root.transform.SetAsLastSibling();
        }

        private static Vector2 GetRequirementTooltipPosition(Vector2 rowPosition)
        {
            return new Vector2(-292f, Mathf.Clamp(rowPosition.y - 18f, -150f, -108f));
        }

        private Sprite GetRecipeIconSprite(DreamyPrototypeRecipe recipe)
        {
            return recipe != null && recipe.Outputs.Count > 0 ? GetItemIconSprite(recipe.Outputs[0].ItemId) : null;
        }

        private Sprite GetItemIconSprite(DreamyItemId itemId)
        {
            string resourcePath = GetCraftingIconResourcePath(itemId);
            Sprite sprite = LoadCraftingIconSprite(resourcePath);
            if (sprite != null)
            {
                return sprite;
            }

            return catalog != null ? catalog.GetItemSprite(itemId) : null;
        }

        private Sprite LoadCraftingIconSprite(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return null;
            }

            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
                if (sprite.texture != null)
                {
                    sprite.texture.filterMode = FilterMode.Point;
                }

                return sprite;
            }

            if (runtimeCraftingIconSprites.TryGetValue(resourcePath, out sprite))
            {
                return sprite;
            }

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return null;
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = resourcePath.Substring(resourcePath.LastIndexOf('/') + 1) + "_RuntimeSprite";
            runtimeCraftingIconSprites[resourcePath] = sprite;
            return sprite;
        }

        private static string GetCraftingIconResourcePath(DreamyItemId itemId)
        {
            switch (itemId)
            {
                case DreamyItemId.Wood:
                    return "Dreamy/Crafting/Icons/Icon_Wood";
                case DreamyItemId.Crop:
                    return "Dreamy/Crafting/Icons/Icon_Crop";
                case DreamyItemId.CraftedMeal:
                    return "Dreamy/Crafting/Icons/Icon_GardenMeal";
                default:
                    return string.Empty;
            }
        }

        private static bool AddIconToBox(GameObject box, Sprite sprite, float padding)
        {
            if (box == null || sprite == null)
            {
                return false;
            }

            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(box.transform, false);
            Image image = iconObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;

            RectTransform rect = iconObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
            return true;
        }

        private static string GetItemDescription(DreamyItemId itemId)
        {
            switch (itemId)
            {
                case DreamyItemId.Wood:
                    return "Basic material for tools and camp crafting.";
                case DreamyItemId.Gold:
                    return "Rare metal used for sturdy recipes.";
                case DreamyItemId.Food:
                case DreamyItemId.Meat:
                    return "Food resource used for simple crafting.";
                case DreamyItemId.Seed:
                    return "Plant this in a farm plot to grow crops.";
                case DreamyItemId.Crop:
                    return "Fresh harvest used for meal recipes.";
                case DreamyItemId.CraftedMeal:
                    return "Prepared food crafted from farm ingredients.";
                case DreamyItemId.CraftedTool:
                    return "Prototype tool made from basic materials.";
                default:
                    return "Prototype material used by crafting recipes.";
            }
        }

        private static string GetItemLocation(DreamyItemId itemId)
        {
            switch (itemId)
            {
                case DreamyItemId.Wood:
                    return "trees, stumps, or wood pickups";
                case DreamyItemId.Gold:
                    return "gold nodes or gold pickups";
                case DreamyItemId.Food:
                case DreamyItemId.Meat:
                    return "food pickups around the field";
                case DreamyItemId.Seed:
                    return "craft from Food or starter supplies";
                case DreamyItemId.Crop:
                    return "plant Seed, water it, then harvest";
                case DreamyItemId.CraftedMeal:
                case DreamyItemId.CraftedTool:
                    return "craft at the Crafting Keeper";
                default:
                    return "prototype sources";
            }
        }

        private static bool CanCraftRecipe(DreamyPrototypeRecipe recipe, DreamyInventory inventory)
        {
            if (recipe == null || inventory == null)
            {
                return false;
            }

            for (int i = 0; i < recipe.Inputs.Count; i++)
            {
                DreamyItemStack input = recipe.Inputs[i];
                if (inventory.GetQuantity(input.ItemId) < input.Quantity)
                {
                    return false;
                }
            }

            return true;
        }

        private static string GetRecipeDescription(DreamyPrototypeRecipe recipe)
        {
            string outputName = GetOutputName(recipe);
            return outputName + " is a prototype craftable item. Use this text area for flavor, gameplay effect, and where the item fits in the player's loop.";
        }

        private static string GetOutputName(DreamyPrototypeRecipe recipe)
        {
            return recipe != null && recipe.Outputs.Count > 0 ? recipe.Outputs[0].DisplayName : recipe != null ? recipe.DisplayName : "Item";
        }

        private static int GetOutputQuantity(DreamyPrototypeRecipe recipe)
        {
            return recipe != null && recipe.Outputs.Count > 0 ? recipe.Outputs[0].Quantity : 1;
        }

        private static string ShortenText(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, Mathf.Max(0, maxLength - 1)) + ".";
        }

        private GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            Image image = panel.AddComponent<Image>();
            image.sprite = CreateUiSprite(Color.white);
            image.color = new Color(0.05f, 0.06f, 0.075f, 0.9f);
            image.raycastTarget = true;
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return panel;
        }

        private Button CreateButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size, bool red)
        {
            GameObject buttonObject = new GameObject(label + " Button");
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.AddComponent<Image>();
            image.sprite = CreateUiSprite(red ? new Color(0.65f, 0.16f, 0.18f, 1f) : new Color(0.16f, 0.32f, 0.62f, 1f));
            image.color = Color.white;
            Button button = buttonObject.AddComponent<Button>();
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Text text = CreateText(buttonObject.transform, label, Mathf.RoundToInt(Mathf.Min(size.x, size.y) * 0.28f), TextAnchor.MiddleCenter, Vector2.zero, size);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            AddTextOutline(text);

            ApplyButtonSprites(
                button,
                red ? catalog != null ? catalog.UiRedButtonSprite : null : catalog != null ? catalog.UiBlueButtonSprite : null,
                red ? catalog != null ? catalog.UiRedButtonPressedSprite : null : catalog != null ? catalog.UiBlueButtonPressedSprite : null);
            return button;
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

        private static void AddTextOutline(Text text)
        {
            if (text == null || text.GetComponent<Outline>() != null)
            {
                return;
            }

            Outline outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.78f);
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
    }
}
