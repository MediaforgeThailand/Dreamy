using UnityEngine;
using UnityEngine.UI;

namespace Dreamy
{
    public sealed class DreamyPrototypeInteractionUi : MonoBehaviour
    {
        private static DreamyPrototypeInteractionUi instance;

        private DreamyMobilePlayer player;
        private DreamyPrototypeInteraction interaction;
        private DreamyPrototypeVisualCatalog catalog;
        private Canvas canvas;
        private GameObject promptPanel;
        private Text promptLabel;
        private Text messageLabel;
        private Button useButton;
        private GameObject activeWindow;
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
            activeWindow = CreatePanel(transform, "Crafting Window", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 650f));
            CreateText(activeWindow.transform, "Crafting Recipes", 32, TextAnchor.MiddleLeft, new Vector2(34f, -24f), new Vector2(560f, 48f));
            Button closeButton = CreateButton(activeWindow.transform, "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -24f), new Vector2(56f, 50f), true);
            closeButton.onClick.AddListener(CloseWindow);

            for (int i = 0; i < station.Recipes.Count; i++)
            {
                DreamyPrototypeRecipe recipe = station.Recipes[i];
                GameObject row = CreatePanel(activeWindow.transform, "Recipe " + (i + 1), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(42f, -94f - i * 142f), new Vector2(812f, 118f));
                CreateText(row.transform, recipe.DisplayName, 24, TextAnchor.MiddleLeft, new Vector2(22f, -14f), new Vector2(520f, 34f));
                Text summary = CreateText(row.transform, station.GetRecipeSummary(recipe), 18, TextAnchor.MiddleLeft, new Vector2(22f, -54f), new Vector2(560f, 42f));
                summary.color = new Color(0.86f, 0.9f, 0.95f, 1f);
                Button craftButton = CreateButton(row.transform, "CRAFT", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-22f, 0f), new Vector2(150f, 62f), true);
                DreamyPrototypeRecipe selectedRecipe = recipe;
                craftButton.onClick.AddListener(() =>
                {
                    string message;
                    station.TryCraft(selectedRecipe, player != null ? player.Inventory : null, out message);
                    DreamyPrototypeInteraction.PublishMessage(message);
                    ShowCrafting(station, player);
                });
            }
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
