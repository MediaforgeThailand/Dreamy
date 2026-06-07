using UnityEngine;
using UnityEngine.UI;

namespace Dreamy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class DreamySegmentedBar : MonoBehaviour
    {
        private const int SourceCellSize = 64;
        private const int BaseCapWidth = 24;
        private const int BaseLeftX = 40;
        private const int BaseMiddleX = 128;
        private const int BaseRightX = 256;
        private const int FillInsetPixels = 0;

        private static Sprite fallbackSprite;

        private RectTransform rectTransform;
        private RectTransform fillClipRect;
        private RectTransform fillImageRect;
        private Image baseImage;
        private Image fillImage;
        private Sprite generatedBaseSprite;
        private Sprite generatedFillSprite;
        private Texture2D generatedBaseTexture;
        private Texture2D generatedFillTexture;
        private Color fallbackFillColor = Color.red;
        private float width;
        private float height;
        private float normalizedFill = 1f;
        private float fillInsetLeft;
        private float fillFullWidth;

        public void Build(Vector2 size, Color fillColor)
        {
            rectTransform = GetComponent<RectTransform>();
            width = Mathf.Max(1f, size.x);
            height = Mathf.Max(1f, size.y);
            fallbackFillColor = fillColor;
            rectTransform.sizeDelta = new Vector2(width, height);

            ClearGeneratedChildren();
            EnsureChildren();
            ApplySprites(null, null, fillColor);
        }

        public void ApplySprites(Sprite baseSprite, Sprite fillSprite, Color fillTint)
        {
            if (baseImage == null && HasGeneratedChildren())
            {
                ClearGeneratedChildren();
            }

            EnsureChildren();
            ReleaseGeneratedAssets();

            generatedBaseSprite = TryCreateBaseCompositeSprite(baseSprite, width, height);
            generatedFillSprite = TryCreateFillCompositeSprite(fillSprite, width, height);

            ApplyImage(baseImage, generatedBaseSprite, generatedBaseSprite != null ? Color.white : new Color(0f, 0f, 0f, 0.72f));
            ApplyImage(fillImage, generatedFillSprite, generatedFillSprite != null ? fillTint : fallbackFillColor);
            RefreshLayout();
        }

        public void SetFill(float value)
        {
            normalizedFill = Mathf.Clamp01(value);
            RefreshLayout();
        }

        private void EnsureChildren()
        {
            if (baseImage == null)
            {
                GameObject baseObject = new GameObject("Base", typeof(RectTransform));
                baseObject.transform.SetParent(transform, false);
                baseImage = baseObject.AddComponent<Image>();
                baseImage.raycastTarget = false;
            }

            if (fillClipRect == null)
            {
                GameObject fillClip = new GameObject("Fill Clip", typeof(RectTransform), typeof(RectMask2D));
                fillClip.transform.SetParent(transform, false);
                fillClipRect = fillClip.GetComponent<RectTransform>();

                GameObject fill = new GameObject("Fill", typeof(RectTransform));
                fill.transform.SetParent(fillClip.transform, false);
                fillImageRect = fill.GetComponent<RectTransform>();
                fillImage = fill.AddComponent<Image>();
                fillImage.raycastTarget = false;
            }
        }

        private void RefreshLayout()
        {
            if (baseImage == null && HasGeneratedChildren())
            {
                ClearGeneratedChildren();
            }

            EnsureChildren();

            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            if (width <= 0f || height <= 0f)
            {
                Vector2 size = rectTransform.rect.size;
                width = Mathf.Max(1f, size.x);
                height = Mathf.Max(1f, size.y);
            }

            ConfigureLeftCenter(baseImage.rectTransform, 0f, width, height);

            float visibleWidth = Mathf.Max(0f, fillFullWidth * normalizedFill);
            ConfigureLeftCenter(fillClipRect, fillInsetLeft, visibleWidth, height);
            ConfigureLeftCenter(fillImageRect, 0f, fillFullWidth, height);
        }

        private Sprite TryCreateBaseCompositeSprite(Sprite source, float targetWidth, float targetHeight)
        {
            if (source == null)
            {
                return null;
            }

            int targetHeightPixels = SourceCellSize;
            int targetWidthPixels = Mathf.Max(BaseCapWidth * 2 + 1, Mathf.RoundToInt(targetWidth / Mathf.Max(1f, targetHeight) * targetHeightPixels));
            Texture2D texture = new Texture2D(targetWidthPixels, targetHeightPixels, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            ClearTexture(texture);

            try
            {
                CopyPixels(source.texture, texture, new RectInt(BaseLeftX, 0, BaseCapWidth, SourceCellSize), 0, 0);

                int middleEnd = targetWidthPixels - BaseCapWidth;
                for (int x = BaseCapWidth; x < middleEnd; x += SourceCellSize)
                {
                    int copyWidth = Mathf.Min(SourceCellSize, middleEnd - x);
                    CopyPixels(source.texture, texture, new RectInt(BaseMiddleX, 0, copyWidth, SourceCellSize), x, 0);
                }

                CopyPixels(source.texture, texture, new RectInt(BaseRightX, 0, BaseCapWidth, SourceCellSize), targetWidthPixels - BaseCapWidth, 0);
                texture.Apply(false, false);
            }
            catch (System.Exception)
            {
                DestroyGeneratedTexture(texture);
                return null;
            }

            generatedBaseTexture = texture;
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), SourceCellSize, 0, SpriteMeshType.FullRect);
        }

        private Sprite TryCreateFillCompositeSprite(Sprite source, float targetWidth, float targetHeight)
        {
            float pixelsPerUiUnit = SourceCellSize / Mathf.Max(1f, targetHeight);
            fillInsetLeft = FillInsetPixels / pixelsPerUiUnit;
            float fillInsetRight = FillInsetPixels / pixelsPerUiUnit;
            fillFullWidth = Mathf.Max(1f, targetWidth - fillInsetLeft - fillInsetRight);

            if (source == null)
            {
                return null;
            }

            int fillWidthPixels = Mathf.Max(1, Mathf.RoundToInt(fillFullWidth * pixelsPerUiUnit));
            Texture2D texture = new Texture2D(fillWidthPixels, SourceCellSize, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            ClearTexture(texture);

            try
            {
                for (int x = 0; x < fillWidthPixels; x += SourceCellSize)
                {
                    int copyWidth = Mathf.Min(SourceCellSize, fillWidthPixels - x);
                    CopyPixels(source.texture, texture, new RectInt(0, 0, copyWidth, SourceCellSize), x, 0);
                }

                texture.Apply(false, false);
            }
            catch (System.Exception)
            {
                DestroyGeneratedTexture(texture);
                return null;
            }

            generatedFillTexture = texture;
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), SourceCellSize, 0, SpriteMeshType.FullRect);
        }

        private static void CopyPixels(Texture2D source, Texture2D destination, RectInt sourceRect, int destinationX, int destinationY)
        {
            Color[] pixels = source.GetPixels(sourceRect.x, sourceRect.y, sourceRect.width, sourceRect.height);
            destination.SetPixels(destinationX, destinationY, sourceRect.width, sourceRect.height, pixels);
        }

        private static void ClearTexture(Texture2D texture)
        {
            Color[] pixels = new Color[texture.width * texture.height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }

            texture.SetPixels(pixels);
        }

        private void ClearGeneratedChildren()
        {
            baseImage = null;
            fillClipRect = null;
            fillImageRect = null;
            fillImage = null;

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (!IsGeneratedChild(child.name))
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

        private bool HasGeneratedChildren()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                if (IsGeneratedChild(transform.GetChild(i).name))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsGeneratedChild(string childName)
        {
            return childName == "Base"
                || childName == "Fill Clip"
                || childName == "Fill";
        }

        private void ReleaseGeneratedAssets()
        {
            DestroyGeneratedSprite(generatedBaseSprite);
            DestroyGeneratedSprite(generatedFillSprite);
            DestroyGeneratedTexture(generatedBaseTexture);
            DestroyGeneratedTexture(generatedFillTexture);
            generatedBaseSprite = null;
            generatedFillSprite = null;
            generatedBaseTexture = null;
            generatedFillTexture = null;
        }

        private static void DestroyGeneratedSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(sprite);
            }
            else
            {
                DestroyImmediate(sprite);
            }
        }

        private static void DestroyGeneratedTexture(Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(texture);
            }
            else
            {
                DestroyImmediate(texture);
            }
        }

        private static void ConfigureLeftCenter(RectTransform rect, float x, float rectWidth, float rectHeight)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.sizeDelta = new Vector2(rectWidth, rectHeight);
        }

        private static void ApplyImage(Image image, Sprite sprite, Color color)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = sprite != null ? sprite : FallbackSprite;
            image.color = color;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;
        }

        private static Sprite FallbackSprite
        {
            get
            {
                if (fallbackSprite != null)
                {
                    return fallbackSprite;
                }

                Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[16];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = Color.white;
                }

                texture.SetPixels(pixels);
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.Apply();
                fallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
                return fallbackSprite;
            }
        }
    }
}
