using UnityEngine;

namespace Dreamy
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class DreamyPrototypeFarmPlot : DreamyPrototypeInteractable
    {
        private enum FarmState
        {
            Empty,
            NeedsWater,
            Growing,
            Ready
        }

        [SerializeField] private DreamyItemId seedItemId = DreamyItemId.Seed;
        [SerializeField] private string seedDisplayName = "Seed";
        [SerializeField] private DreamyItemId cropItemId = DreamyItemId.Crop;
        [SerializeField] private string cropDisplayName = "Crop";
        [SerializeField] private int harvestQuantity = 2;
        [SerializeField] private float growSeconds = 10f;
        [SerializeField] private Sprite plotSprite;
        [SerializeField] private Sprite seedSprite;
        [SerializeField] private Sprite cropSprite;

        private SpriteRenderer plotRenderer;
        private SpriteRenderer cropRenderer;
        private TextMesh label;
        private FarmState state = FarmState.Empty;
        private float growStartedAt;

        public override string InteractionLabel
        {
            get
            {
                switch (state)
                {
                    case FarmState.Empty:
                        return "Plant seed";
                    case FarmState.NeedsWater:
                        return "Water crop";
                    case FarmState.Ready:
                        return "Harvest crop";
                    default:
                        return "Growing";
                }
            }
        }

        private void Awake()
        {
            plotRenderer = GetComponent<SpriteRenderer>();
            plotRenderer.sortingOrder = 5;
            EnsureCropRenderer();
            EnsureLabel();
            RefreshVisuals();
        }

        private void Update()
        {
            if (state == FarmState.Growing && Time.time >= growStartedAt + growSeconds)
            {
                state = FarmState.Ready;
                RefreshVisuals();
            }
            else if (state == FarmState.Growing)
            {
                RefreshLabel();
            }
        }

        public void Configure(DreamyPrototypeVisualCatalog catalog)
        {
            plotSprite = catalog != null && catalog.UiSlotSprite != null ? catalog.UiSlotSprite : plotSprite;
            seedSprite = catalog != null && catalog.WoodSprite != null ? catalog.WoodSprite : seedSprite;
            cropSprite = catalog != null && catalog.FoodSprite != null ? catalog.FoodSprite : cropSprite;
            RefreshVisuals();
        }

        public override bool Interact(DreamyMobilePlayer player)
        {
            if (player == null || player.Inventory == null)
            {
                return false;
            }

            switch (state)
            {
                case FarmState.Empty:
                    return TryPlant(player);
                case FarmState.NeedsWater:
                    Water();
                    return true;
                case FarmState.Ready:
                    return TryHarvest(player);
                default:
                    DreamyPrototypeInteraction.PublishMessage("Crop is growing " + Mathf.RoundToInt(GetGrowProgress() * 100f) + "%");
                    return true;
            }
        }

        private bool TryPlant(DreamyMobilePlayer player)
        {
            if (player.Inventory.GetQuantity(seedItemId) <= 0)
            {
                DreamyPrototypeInteraction.PublishMessage("Need " + seedDisplayName);
                return false;
            }

            if (!player.Inventory.RemoveItem(seedItemId, 1))
            {
                return false;
            }

            state = FarmState.NeedsWater;
            RefreshVisuals();
            DreamyPrototypeInteraction.PublishMessage("Planted " + seedDisplayName);
            return true;
        }

        private void Water()
        {
            growStartedAt = Time.time;
            state = FarmState.Growing;
            RefreshVisuals();
            DreamyPrototypeInteraction.PublishMessage("Watered crop");
        }

        private bool TryHarvest(DreamyMobilePlayer player)
        {
            if (!player.Inventory.AddItem(cropItemId, harvestQuantity, cropDisplayName))
            {
                DreamyPrototypeInteraction.PublishMessage("Inventory full");
                return false;
            }

            state = FarmState.Empty;
            RefreshVisuals();
            DreamyPrototypeInteraction.PublishMessage("Harvested " + cropDisplayName + " x" + harvestQuantity);
            return true;
        }

        private float GetGrowProgress()
        {
            if (state != FarmState.Growing || growSeconds <= 0f)
            {
                return state == FarmState.Ready ? 1f : 0f;
            }

            return Mathf.Clamp01((Time.time - growStartedAt) / growSeconds);
        }

        private void RefreshVisuals()
        {
            if (plotRenderer == null)
            {
                plotRenderer = GetComponent<SpriteRenderer>();
            }

            plotRenderer.sprite = plotSprite != null ? plotSprite : CreatePlotSprite();
            plotRenderer.color = state == FarmState.Empty ? new Color(0.55f, 0.34f, 0.2f, 1f) : new Color(0.42f, 0.26f, 0.14f, 1f);
            plotRenderer.drawMode = SpriteDrawMode.Sliced;
            plotRenderer.size = new Vector2(1.15f, 0.82f);

            EnsureCropRenderer();
            cropRenderer.sprite = GetCropSprite();
            cropRenderer.enabled = state != FarmState.Empty;
            cropRenderer.color = state == FarmState.NeedsWater ? new Color(0.72f, 0.72f, 0.72f, 1f) : Color.white;
            cropRenderer.transform.localScale = state == FarmState.Ready ? Vector3.one * 0.68f : Vector3.one * 0.42f;

            RefreshLabel();
        }

        private Sprite GetCropSprite()
        {
            return state == FarmState.Ready
                ? cropSprite != null ? cropSprite : CreateCropSprite()
                : seedSprite != null ? seedSprite : CreateSeedSprite();
        }

        private void RefreshLabel()
        {
            EnsureLabel();
            switch (state)
            {
                case FarmState.Empty:
                    label.text = "Plant";
                    break;
                case FarmState.NeedsWater:
                    label.text = "Water";
                    break;
                case FarmState.Ready:
                    label.text = "Harvest";
                    break;
                default:
                    label.text = "Grow " + Mathf.RoundToInt(GetGrowProgress() * 100f) + "%";
                    break;
            }
        }

        private void EnsureCropRenderer()
        {
            if (cropRenderer != null)
            {
                return;
            }

            Transform existing = transform.Find("Crop Visual");
            GameObject cropObject = existing != null ? existing.gameObject : new GameObject("Crop Visual");
            cropObject.transform.SetParent(transform, false);
            cropObject.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            cropRenderer = cropObject.GetComponent<SpriteRenderer>();
            if (cropRenderer == null)
            {
                cropRenderer = cropObject.AddComponent<SpriteRenderer>();
            }

            cropRenderer.sortingOrder = 8;
        }

        private void EnsureLabel()
        {
            if (label != null)
            {
                return;
            }

            Transform existing = transform.Find("Farm Label");
            GameObject labelObject = existing != null ? existing.gameObject : new GameObject("Farm Label");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            label = labelObject.GetComponent<TextMesh>();
            if (label == null)
            {
                label = labelObject.AddComponent<TextMesh>();
            }

            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.12f;
            label.fontSize = 28;
            label.color = Color.white;
            MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 30;
            }
        }

        private static Sprite CreatePlotSprite()
        {
            Texture2D texture = new Texture2D(24, 16);
            Color soil = new Color(0.42f, 0.24f, 0.13f, 1f);
            Color ridge = new Color(0.62f, 0.39f, 0.22f, 1f);
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, y % 5 == 0 ? ridge : soil);
                }
            }

            texture.filterMode = FilterMode.Point;
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 24f);
        }

        private static Sprite CreateSeedSprite()
        {
            return CreateSolidSprite(new Color(0.24f, 0.8f, 0.32f, 1f), 10, 10, 12f);
        }

        private static Sprite CreateCropSprite()
        {
            return CreateSolidSprite(new Color(0.36f, 0.95f, 0.26f, 1f), 14, 14, 14f);
        }

        private static Sprite CreateSolidSprite(Color color, int width, int height, float pixelsPerUnit)
        {
            Texture2D texture = new Texture2D(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }

            texture.filterMode = FilterMode.Point;
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            harvestQuantity = Mathf.Max(1, harvestQuantity);
            growSeconds = Mathf.Max(0.1f, growSeconds);
        }
    }
}
