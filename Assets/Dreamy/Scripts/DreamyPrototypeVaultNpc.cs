using UnityEngine;

namespace Dreamy
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(DreamyInventory))]
    public sealed class DreamyPrototypeVaultNpc : DreamyPrototypeInteractable
    {
        private const int RuntimeSortingOrder = 100;
        private const float RuntimeSortingUnitsPerWorldUnit = 10f;

        [SerializeField] private string npcDisplayName = "Vault Keeper";
        [SerializeField] private Texture2D npcIdleSheet;
        [SerializeField] private int idleFrameCount = 8;
        [SerializeField] private float pixelsPerUnit = 128f;

        private SpriteRenderer spriteRenderer;
        private DreamyInventory storage;
        private TextMesh label;

        public DreamyInventory Storage => storage;
        public string DisplayName => string.IsNullOrWhiteSpace(npcDisplayName) ? "Vault Keeper" : npcDisplayName;
        public override string InteractionLabel => "Talk to " + DisplayName;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            storage = GetComponent<DreamyInventory>();
            storage.MaxSlots = Mathf.Max(storage.MaxSlots, 80);
            EnsureYSort();
            EnsureLabel();
            RefreshVisual();
        }

        public void Configure(DreamyPrototypeVisualCatalog catalog)
        {
            npcIdleSheet = catalog != null ? catalog.EnemyIdleSheet : npcIdleSheet;
            RefreshVisual();
        }

        public override bool Interact(DreamyMobilePlayer player)
        {
            if (player == null)
            {
                return false;
            }

            DreamyPrototypeInteractionUi.OpenVault(this, player);
            return true;
        }

        private void RefreshVisual()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = npcIdleSheet != null ? CreateFirstFrame(npcIdleSheet, idleFrameCount, pixelsPerUnit) : CreateFallbackNpcSprite();
            spriteRenderer.color = Color.white;
            EnsureYSort();
        }

        private void EnsureYSort()
        {
            DreamyYSortSprite ySort = GetComponent<DreamyYSortSprite>();
            if (ySort == null)
            {
                ySort = gameObject.AddComponent<DreamyYSortSprite>();
            }

            ySort.Configure(RuntimeSortingOrder, RuntimeSortingUnitsPerWorldUnit);
        }

        private void EnsureLabel()
        {
            if (label != null)
            {
                return;
            }

            Transform existing = transform.Find("Vault Label");
            GameObject labelObject = existing != null ? existing.gameObject : new GameObject("Vault Label");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            label = labelObject.GetComponent<TextMesh>();
            if (label == null)
            {
                label = labelObject.AddComponent<TextMesh>();
            }

            label.text = DisplayName;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.12f;
            label.fontSize = 28;
            label.color = Color.white;
            MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 245;
            }
        }

        private static Sprite CreateFirstFrame(Texture2D texture, int frameCount, float pixelsPerUnit)
        {
            if (texture == null)
            {
                return CreateFallbackNpcSprite();
            }

            texture.filterMode = FilterMode.Point;
            int frames = Mathf.Max(1, frameCount);
            int width = Mathf.Max(1, texture.width / frames);
            Rect rect = new Rect(0f, 0f, width, texture.height);
            return Sprite.Create(texture, rect, new Vector2(0.5f, 0.28f), Mathf.Max(1f, pixelsPerUnit), 0, SpriteMeshType.FullRect);
        }

        private static Sprite CreateFallbackNpcSprite()
        {
            Texture2D texture = new Texture2D(14, 18);
            Color robe = new Color(0.2f, 0.44f, 0.82f, 1f);
            Color face = new Color(0.95f, 0.72f, 0.48f, 1f);
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, y > 10 && x > 3 && x < 10 ? face : robe);
                }
            }

            texture.filterMode = FilterMode.Point;
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.2f), 18f);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            idleFrameCount = Mathf.Max(1, idleFrameCount);
            pixelsPerUnit = Mathf.Max(1f, pixelsPerUnit);
        }
    }
}
