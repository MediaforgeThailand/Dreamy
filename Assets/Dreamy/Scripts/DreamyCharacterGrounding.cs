using UnityEngine;

namespace Dreamy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class DreamyCharacterGrounding : MonoBehaviour
    {
        private const string ShadowObjectName = "Ground Shadow";
        private const string GroundedShaderName = "Dreamy/Pixel Character Grounded";
        private const string FallbackSpriteShaderName = "Sprites/Default";
        private const string PrefsPrefix = "Dreamy.CharacterGrounding.";

        [Header("Manual Shadow Tuning")]
        [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.34f);
        [SerializeField] private float shadowXOffset;
        [SerializeField] private float shadowYOffset = 0f;
        [SerializeField] private float shadowWidthScale = 0.36f;
        [SerializeField] private float shadowHeight = 0.1f;
        [SerializeField] private bool shadowVisible = true;
        [SerializeField] private int shadowSortingOffset = -1;
        [SerializeField] private bool applyGroundedMaterial = true;
        [SerializeField] private bool loadSavedTuningOnAwake = true;

        private static Sprite sharedShadowSprite;
        private static Material sharedCharacterMaterial;
        private static Material sharedShadowMaterial;

        private SpriteRenderer characterRenderer;
        private SpriteRenderer shadowRenderer;
        private Transform shadowTransform;

        public float ShadowXOffset
        {
            get => shadowXOffset;
            set => shadowXOffset = Mathf.Clamp(value, -1.2f, 1.2f);
        }

        public float ShadowYOffset
        {
            get => shadowYOffset;
            set => shadowYOffset = Mathf.Clamp(value, -1.2f, 1.2f);
        }

        public float ShadowWidthScale
        {
            get => shadowWidthScale;
            set => shadowWidthScale = Mathf.Clamp(value, 0.1f, 1.4f);
        }

        public float ShadowHeight
        {
            get => shadowHeight;
            set => shadowHeight = Mathf.Clamp(value, 0.02f, 0.5f);
        }

        public bool ShadowVisible
        {
            get => shadowVisible;
            set => shadowVisible = value;
        }

        private void Awake()
        {
            characterRenderer = GetComponent<SpriteRenderer>();
            if (loadSavedTuningOnAwake)
            {
                LoadSavedTuning();
            }

            EnsureShadow();
            ApplyCharacterMaterial();
            RefreshShadow();
        }

        private void LateUpdate()
        {
            RefreshShadow();
        }

        private void OnValidate()
        {
            ShadowXOffset = shadowXOffset;
            ShadowYOffset = shadowYOffset;
            ShadowWidthScale = shadowWidthScale;
            ShadowHeight = shadowHeight;
        }

        public void SaveTuning()
        {
            PlayerPrefs.SetFloat(PrefsPrefix + "ShadowXOffset", shadowXOffset);
            PlayerPrefs.SetFloat(PrefsPrefix + "ShadowYOffset", shadowYOffset);
            PlayerPrefs.SetFloat(PrefsPrefix + "ShadowWidthScale", shadowWidthScale);
            PlayerPrefs.SetFloat(PrefsPrefix + "ShadowHeight", shadowHeight);
            PlayerPrefs.SetInt(PrefsPrefix + "ShadowVisible", shadowVisible ? 1 : 0);
            PlayerPrefs.SetInt(PrefsPrefix + "HasSavedTuning", 1);
            PlayerPrefs.Save();
        }

        public void LoadSavedTuning()
        {
            if (!HasSavedTuning())
            {
                return;
            }

            ShadowXOffset = PlayerPrefs.GetFloat(PrefsPrefix + "ShadowXOffset", shadowXOffset);
            ShadowYOffset = PlayerPrefs.GetFloat(PrefsPrefix + "ShadowYOffset", shadowYOffset);
            ShadowWidthScale = PlayerPrefs.GetFloat(PrefsPrefix + "ShadowWidthScale", shadowWidthScale);
            ShadowHeight = PlayerPrefs.GetFloat(PrefsPrefix + "ShadowHeight", shadowHeight);
            ShadowVisible = PlayerPrefs.GetInt(PrefsPrefix + "ShadowVisible", shadowVisible ? 1 : 0) != 0;
        }

        public void ClearSavedTuning()
        {
            PlayerPrefs.DeleteKey(PrefsPrefix + "ShadowXOffset");
            PlayerPrefs.DeleteKey(PrefsPrefix + "ShadowYOffset");
            PlayerPrefs.DeleteKey(PrefsPrefix + "ShadowWidthScale");
            PlayerPrefs.DeleteKey(PrefsPrefix + "ShadowHeight");
            PlayerPrefs.DeleteKey(PrefsPrefix + "ShadowVisible");
            PlayerPrefs.DeleteKey(PrefsPrefix + "HasSavedTuning");
            PlayerPrefs.Save();
        }

        public bool HasSavedTuning()
        {
            return PlayerPrefs.GetInt(PrefsPrefix + "HasSavedTuning", 0) != 0;
        }

        private void EnsureShadow()
        {
            Transform existing = transform.Find(ShadowObjectName);
            GameObject shadowObject = existing != null ? existing.gameObject : new GameObject(ShadowObjectName);
            shadowObject.transform.SetParent(transform, false);
            shadowTransform = shadowObject.transform;
            shadowRenderer = shadowObject.GetComponent<SpriteRenderer>();
            if (shadowRenderer == null)
            {
                shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
            }

            shadowRenderer.sprite = GetShadowSprite();
            shadowRenderer.sharedMaterial = GetShadowMaterial();
            shadowRenderer.color = shadowColor;
        }

        private void ApplyCharacterMaterial()
        {
            if (!applyGroundedMaterial || characterRenderer == null)
            {
                return;
            }

            Material material = GetCharacterMaterial();
            if (material != null)
            {
                characterRenderer.sharedMaterial = material;
            }
        }

        private void RefreshShadow()
        {
            if (characterRenderer == null)
            {
                characterRenderer = GetComponent<SpriteRenderer>();
            }

            if (characterRenderer == null || shadowRenderer == null || shadowTransform == null)
            {
                return;
            }

            Sprite sprite = characterRenderer.sprite;
            Bounds bounds = sprite != null ? sprite.bounds : new Bounds(Vector3.zero, new Vector3(0.8f, 1f, 0f));
            float width = Mathf.Clamp(bounds.size.x * shadowWidthScale, 0.22f, 0.72f);

            shadowTransform.localPosition = new Vector3(shadowXOffset, shadowYOffset, 0.02f);
            shadowTransform.localRotation = Quaternion.identity;
            shadowTransform.localScale = new Vector3(width, shadowHeight, 1f);
            shadowRenderer.sortingLayerID = characterRenderer.sortingLayerID;
            shadowRenderer.sortingOrder = characterRenderer.sortingOrder + shadowSortingOffset;
            shadowRenderer.color = shadowColor;
            shadowRenderer.enabled = shadowVisible;
        }

        private static Sprite GetShadowSprite()
        {
            if (sharedShadowSprite != null)
            {
                return sharedShadowSprite;
            }

            const int width = 64;
            const int height = 32;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "Dreamy Runtime Character Shadow";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[width * height];
            Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = (x - center.x) / center.x;
                    float ny = (y - center.y) / center.y;
                    float distance = nx * nx + ny * ny;
                    float alpha = Mathf.SmoothStep(1f, 0f, distance);
                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            sharedShadowSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), width);
            sharedShadowSprite.name = "Dreamy Runtime Character Shadow";
            sharedShadowSprite.hideFlags = HideFlags.HideAndDontSave;
            return sharedShadowSprite;
        }

        private static Material GetCharacterMaterial()
        {
            if (sharedCharacterMaterial != null)
            {
                return sharedCharacterMaterial;
            }

            Shader shader = Shader.Find(GroundedShaderName);
            if (shader == null)
            {
                shader = Shader.Find(FallbackSpriteShaderName);
            }

            if (shader == null)
            {
                return null;
            }

            sharedCharacterMaterial = new Material(shader)
            {
                name = "Dreamy Runtime Grounded Character Material",
                hideFlags = HideFlags.HideAndDontSave
            };
            return sharedCharacterMaterial;
        }

        private static Material GetShadowMaterial()
        {
            if (sharedShadowMaterial != null)
            {
                return sharedShadowMaterial;
            }

            Shader shader = Shader.Find(FallbackSpriteShaderName);
            if (shader == null)
            {
                return null;
            }

            sharedShadowMaterial = new Material(shader)
            {
                name = "Dreamy Runtime Character Shadow Material",
                hideFlags = HideFlags.HideAndDontSave
            };
            return sharedShadowMaterial;
        }
    }
}
