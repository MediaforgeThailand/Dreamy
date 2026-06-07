using UnityEngine;

namespace Dreamy
{
    public sealed class DreamyDamagePopup : MonoBehaviour
    {
        private const float DefaultDuration = 0.78f;
        private const float DefaultCharacterSize = 0.055f;
        private const int DefaultFontSize = 54;
        private const int SortingOrder = 260;

        private TextMesh textMesh;
        private MeshRenderer meshRenderer;
        private Vector3 startPosition;
        private Vector3 drift;
        private Color baseColor;
        private float duration = DefaultDuration;
        private float elapsed;
        private float startScale = 0.72f;
        private float endScale = 1.05f;

        public static DreamyDamagePopup Spawn(float amount, Vector3 worldPosition, Color color)
        {
            if (amount <= 0f)
            {
                return null;
            }

            GameObject popupObject = new GameObject("Damage " + FormatDamage(amount));
            popupObject.transform.position = worldPosition + new Vector3(Random.Range(-0.12f, 0.12f), Random.Range(-0.02f, 0.08f), -0.2f);

            TextMesh text = popupObject.AddComponent<TextMesh>();
            text.text = FormatDamage(amount);
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = DefaultFontSize;
            text.characterSize = DefaultCharacterSize;
            text.color = color;

            MeshRenderer renderer = popupObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = SortingOrder;
            }

            DreamyDamagePopup popup = popupObject.AddComponent<DreamyDamagePopup>();
            popup.Initialize(color);
            return popup;
        }

        private static string FormatDamage(float amount)
        {
            float rounded = Mathf.Round(amount);
            return Mathf.Abs(amount - rounded) <= 0.05f ? Mathf.RoundToInt(amount).ToString() : amount.ToString("0.0");
        }

        private void Awake()
        {
            textMesh = GetComponent<TextMesh>();
            meshRenderer = GetComponent<MeshRenderer>();
            startPosition = transform.position;
        }

        private void Initialize(Color color)
        {
            baseColor = color;
            drift = new Vector3(Random.Range(-0.18f, 0.18f), 0.96f, 0f);
            transform.localScale = Vector3.one * startScale;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            float eased = 1f - Mathf.Pow(1f - t, 2f);

            transform.position = startPosition + drift * eased;
            transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, Mathf.Sin(t * Mathf.PI));

            if (textMesh != null)
            {
                Color color = baseColor;
                color.a = Mathf.Clamp01(1f - t);
                textMesh.color = color;
            }

            if (meshRenderer != null)
            {
                meshRenderer.sortingOrder = SortingOrder;
            }

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
