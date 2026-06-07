using UnityEngine;

namespace Dreamy
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class DreamyAttackSlashEffect : MonoBehaviour
    {
        private const int TextureWidth = 32;
        private const int TextureHeight = 20;
        private const float PixelsPerUnit = 32f;

        private static Sprite slashSprite;

        private SpriteRenderer spriteRenderer;
        private Color startColor = Color.white;
        private float duration = 0.15f;
        private float startedAt;

        public static Sprite SlashSprite
        {
            get
            {
                if (slashSprite == null)
                {
                    slashSprite = CreateSlashSprite();
                }

                return slashSprite;
            }
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            startedAt = Time.time;
            if (spriteRenderer != null)
            {
                startColor = spriteRenderer.color;
            }
        }

        public void Configure(float lifetime)
        {
            duration = Mathf.Max(0.03f, lifetime);
            startedAt = Time.time;
        }

        private void Update()
        {
            float progress = Mathf.Clamp01((Time.time - startedAt) / duration);
            transform.localScale = Vector3.one * Mathf.Lerp(0.72f, 1.22f, progress);
            if (spriteRenderer != null)
            {
                Color color = startColor;
                color.a *= 1f - progress;
                spriteRenderer.color = color;
            }

            if (progress >= 1f)
            {
                Destroy(gameObject);
            }
        }

        private static Sprite CreateSlashSprite()
        {
            Texture2D texture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false);
            Color clear = new Color(1f, 1f, 1f, 0f);
            Color edge = new Color(1f, 0.86f, 0.36f, 1f);
            Color core = new Color(1f, 1f, 0.88f, 1f);

            for (int y = 0; y < TextureHeight; y++)
            {
                for (int x = 0; x < TextureWidth; x++)
                {
                    float nx = (x + 0.5f) / TextureWidth;
                    float ny = (y + 0.5f) / TextureHeight;
                    float curve = 0.5f + Mathf.Sin(nx * Mathf.PI) * 0.26f;
                    float distance = Mathf.Abs(ny - curve);
                    bool inside = nx > 0.08f && nx < 0.94f && distance < Mathf.Lerp(0.035f, 0.13f, Mathf.Sin(nx * Mathf.PI));
                    if (!inside)
                    {
                        texture.SetPixel(x, y, clear);
                        continue;
                    }

                    texture.SetPixel(x, y, distance < 0.035f ? core : edge);
                }
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, TextureWidth, TextureHeight), new Vector2(0.5f, 0.5f), PixelsPerUnit, 0, SpriteMeshType.FullRect);
        }
    }
}
