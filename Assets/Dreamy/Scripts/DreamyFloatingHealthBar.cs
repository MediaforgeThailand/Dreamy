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

        [SerializeField] private string displayName = "Monster";
        [SerializeField] private int level = 1;
        [SerializeField] private Vector2 worldOffset = new Vector2(0f, 1.18f);
        [SerializeField] private Vector2 panelSize = new Vector2(158f, 42f);
        [SerializeField] private Color barFillColor = new Color(0.92f, 0.16f, 0.12f, 1f);

        private DreamyCharacterStats stats;
        private Canvas canvas;
        private RectTransform rootRect;
        private Text titleLabel;
        private Text healthLabel;
        private DreamySegmentedBar healthBar;
        private Sprite barBaseSprite;
        private Sprite barFillSprite;

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

        public void Configure(string monsterName, int monsterLevel, Vector2 offset, Sprite baseSprite = null, Sprite fillSprite = null)
        {
            displayName = string.IsNullOrEmpty(monsterName) ? displayName : monsterName;
            level = Mathf.Max(1, monsterLevel);
            worldOffset = offset;
            barBaseSprite = baseSprite;
            barFillSprite = fillSprite;
            Rebuild();
            ApplyBarSprites();
            Refresh();
        }

        private void Rebuild()
        {
            ClearExistingRoot();
            canvas = null;
            rootRect = null;
            titleLabel = null;
            healthLabel = null;
            healthBar = null;
            Build();
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

            GameObject barObject = new GameObject("Health Bar", typeof(RectTransform));
            barObject.transform.SetParent(root.transform, false);
            RectTransform barRect = barObject.GetComponent<RectTransform>();
            ConfigureRect(barRect, new Vector2(0f, -9f), new Vector2(panelSize.x - 18f, 14f));
            healthBar = barObject.AddComponent<DreamySegmentedBar>();
            healthBar.Build(barRect.sizeDelta, barFillColor);
            ApplyBarSprites();

            GameObject healthTextObject = new GameObject("Health Text");
            healthTextObject.transform.SetParent(barObject.transform, false);
            healthLabel = healthTextObject.AddComponent<Text>();
            healthLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            healthLabel.fontSize = 10;
            healthLabel.alignment = TextAnchor.MiddleCenter;
            healthLabel.color = Color.white;
            healthLabel.raycastTarget = false;
            ConfigureRect(healthLabel.rectTransform, Vector2.zero, new Vector2(panelSize.x - 18f, 12f));
        }

        private void ApplyBarSprites()
        {
            if (healthBar != null)
            {
                healthBar.ApplySprites(barBaseSprite, barFillSprite, Color.white);
            }
        }

        private void ClearExistingRoot()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name != "Floating Health Bar")
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
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
            if (healthBar != null)
            {
                healthBar.SetFill(healthPercent);
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

        private void OnValidate()
        {
            level = Mathf.Max(1, level);
        }
    }
}
