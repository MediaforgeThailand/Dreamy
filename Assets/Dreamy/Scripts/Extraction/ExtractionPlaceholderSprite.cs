using System.Collections.Generic;
using UnityEngine;

namespace Dreamy.Extraction
{
    public static class ExtractionPlaceholderSprite
    {
        private static readonly Dictionary<Color32, Sprite> Cache = new Dictionary<Color32, Sprite>();

        public static Sprite Get(Color color)
        {
            Color32 key = color;
            if (Cache.TryGetValue(key, out Sprite sprite))
            {
                return sprite;
            }

            Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            Color[] pixels = new Color[16 * 16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f), 16f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Cache.Add(key, sprite);
            return sprite;
        }
    }
}
