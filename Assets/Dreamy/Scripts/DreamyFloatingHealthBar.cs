using UnityEngine;
using UnityEngine.UI;

namespace Dreamy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DreamyCharacterStats))]
    public sealed class DreamyFloatingHealthBar : MonoBehaviour
    {
        private const int SortingOrder = 245;
        private const float CanvasScale = 0.01f;

        private static Sprite whiteSprite;

        [SerializeField] private string displayName = "Monster";
        [SerializeField] private int level = 1;
        [SerializeField] private Vector2 worldOffset = new Vector2(0f, 1.18f);
        [SerializeField] private Vector2 panelSize = new Vector2(158f, 42f);
        [SerializeField] private Color barFillColor = new Color(0.92f, 0.16f, 0.12f, 1f);
        [SerializeField] private Color barBackColor = new Color(0f, 0f, 0f, 0.7f);

        private DreamyCharacterStats stats;
        private Canvas canvas;
        private RectTransform rootRect;
        private Text titleLabel;
        private Text healthLabel;
        private Image healthFill;

        private void Awake()
        {
            stats = GetComponent<DreamyCharacterStats>();
            Build();
            Refresh();
        }

        private void OnEnable()
        {
            if (stats == null)
            {
                stats = GetComponent<DreamyCharacterStats>();
            }

            if (stats != null)
            {
                stats.StatsChanged += Refresh;
                stats.Died += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (stats != null)
            {
                stats.StatsChanged -= Refresh;
                stats.Died -= Refresh;
            }
        }

        private void LateUpdate()
        {
            if (rootRect != null)
            {
                rootRect.localPosition = new Vector3(worldOffset.x, worldOffset.y, -0.08f);
                rootRect.localRotation = Quaternion.identity;
            }
        }

        public void Configure(string monsterName, int monsterLevel, Vector2 offset)
        {
            displayName = string.IsNullOrEmpty(monsterName) ? displayName : monsterName;
            level = Mathf.Max(1, monsterLevel);
            worldOffset = offset;
            Build();
            Refresh();
        }

        private void Build()
        {
            if (canvas != null)
            {
                return;
            }

            GameObject root = new GameObject("Floating Health Bar");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(worldOffset.x, worldOffset.y, -0.08f);
            root.transform.localScale = Vector3.one * CanvasScale;

            rootRect = root.AddComponent<RectTransform>();
            rootRect.sizeDelta = panelSize;

            canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = SortingOrder;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 12f;

            GameObject titleObject = new GameObject("Monster Label");
            titleObject.transform.SetParent(root.transform, false);
            titleLabel = titleObject.AddComponent<Text>();
            titleLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleLabel.fontSize = 17;
            titleLabel.alignment = TextAnchor.MiddleCenter;
            titleLabel.color = Color.white;
            titleLabel.raycastTarget = false;
            Outline titleOutline = titleObject.AddComponent<Outline>();
            titleOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            titleOutline.effectDistance = new Vector2(1f, -1f);
            ConfigureRect(titleLabel.rectTransform, new Vector2(0f, 10f), new Vector2(panelSize.x, 20f));

            GameObject barBack = new GameObject("Health Back");
            barBack.transform.SetParent(root.transform, false);
            Image backImage = barBack.AddComponent<Image>();
            backImage.sprite = WhiteSprite;
            backImage.color = barBackColor;
            backImage.raycastTarget = false;
            ConfigureRect(backImage.rectTransform, new Vector2(0f, -9f), new Vector2(panelSize.x - 18f, 12f));

            GameObject barFill = new GameObject("Health Fill");
            barFill.transform.SetParent(barBack.transform, false);
            healthFill = barFill.AddComponent<Image>();
            healthFill.sprite = WhiteSprite;
            healthFill.color = barFillColor;
            healthFill.type = Image.Type.Filled;
            healthFill.fillMethod = Image.FillMethod.Horizontal;
            healthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            healthFill.raycastTarget = false;
            ConfigureRect(healthFill.rectTransform, Vector2.zero, new Vector2(panelSize.x - 22f, 8f));

            GameObject healthTextObject = new GameObject("Health Text");
            healthTextObject.transform.SetParent(barBack.transform, false);
            healthLabel = healthTextObject.AddComponent<Text>();
            healthLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            healthLabel.fontSize = 10;
            healthLabel.alignment = TextAnchor.MiddleCenter;
            healthLabel.color = Color.white;
            healthLabel.raycastTarget = false;
            ConfigureRect(healthLabel.rectTransform, Vector2.zero, new Vector2(panelSize.x - 18f, 12f));
        }

        private void Refresh()
        {
            if (titleLabel != null)
            {
                titleLabel.text = "[Lv." + level + "] " + displayName;
            }

            if (stats == null)
            {
                return;
            }

            float healthPercent = stats.MaxHealth > 0f ? Mathf.Clamp01(stats.CurrentHealth / stats.MaxHealth) : 0f;
            if (healthFill != null)
            {
                healthFill.fillAmount = healthPercent;
            }

            if (healthLabel != null)
            {
                healthLabel.text = "HP " + Mathf.CeilToInt(stats.CurrentHealth) + "/" + Mathf.CeilToInt(stats.MaxHealth);
            }

            if (canvas != null)
            {
                canvas.enabled = stats.CurrentHealth > 0f;
            }
        }

        private static void ConfigureRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static Sprite WhiteSprite
        {
            get
            {
                if (whiteSprite != null)
                {
                    return whiteSprite;
                }

                Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[16];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = Color.white;
                }

                texture.SetPixels(pixels);
                texture.filterMode = FilterMode.Point;
                texture.Apply();
                whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
                return whiteSprite;
            }
        }

        private void OnValidate()
        {
            level = Mathf.Max(1, level);
        }
    }
}
