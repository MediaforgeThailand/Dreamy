using UnityEngine;

namespace Dreamy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DreamyCharacterStats))]
    public sealed class DreamyCharacterHitFeedback : MonoBehaviour
    {
        [SerializeField] private bool showDamagePopup = true;
        [SerializeField] private bool flashSprite = true;
        [SerializeField] private Color popupColor = new Color(1f, 0.26f, 0.16f, 1f);
        [SerializeField] private Color flashColor = new Color(1f, 0.42f, 0.38f, 1f);
        [SerializeField] private Vector2 popupOffset = new Vector2(0f, 0.88f);
        [SerializeField] private float flashDuration = 0.18f;
        [SerializeField] private float scalePulse = 0.06f;

        private DreamyCharacterStats stats;
        private SpriteRenderer spriteRenderer;
        private Color baseColor = Color.white;
        private Vector3 baseScale = Vector3.one;
        private float flashEndsAt;
        private bool feedbackActive;

        private void Awake()
        {
            stats = GetComponent<DreamyCharacterStats>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                baseColor = spriteRenderer.color;
            }

            baseScale = transform.localScale;
        }

        private void OnEnable()
        {
            if (stats == null)
            {
                stats = GetComponent<DreamyCharacterStats>();
            }

            if (stats != null)
            {
                stats.Damaged += HandleDamaged;
            }
        }

        private void OnDisable()
        {
            if (stats != null)
            {
                stats.Damaged -= HandleDamaged;
            }

            ResetVisuals();
        }

        public void Configure(
            bool displayDamagePopup,
            bool useSpriteFlash,
            Color damagePopupColor,
            Color hitFlashColor,
            Vector2 damagePopupOffset,
            float hitFlashDuration,
            float hitScalePulse)
        {
            showDamagePopup = displayDamagePopup;
            flashSprite = useSpriteFlash;
            popupColor = damagePopupColor;
            flashColor = hitFlashColor;
            popupOffset = damagePopupOffset;
            flashDuration = Mathf.Max(0f, hitFlashDuration);
            scalePulse = Mathf.Max(0f, hitScalePulse);
        }

        private void HandleDamaged(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            if (showDamagePopup)
            {
                DreamyDamagePopup.Spawn(amount, transform.position + (Vector3)popupOffset, popupColor);
            }

            if (!flashSprite && scalePulse <= 0f)
            {
                return;
            }

            if (spriteRenderer != null && !feedbackActive)
            {
                baseColor = spriteRenderer.color;
            }

            if (!feedbackActive)
            {
                baseScale = transform.localScale;
            }

            feedbackActive = true;
            flashEndsAt = Time.time + flashDuration;
        }

        private void LateUpdate()
        {
            if (!feedbackActive)
            {
                return;
            }

            if (Time.time >= flashEndsAt || flashDuration <= 0f)
            {
                ResetVisuals();
                return;
            }

            float t = Mathf.Clamp01(1f - ((flashEndsAt - Time.time) / flashDuration));
            float pulse = Mathf.Sin(t * Mathf.PI);
            if (flashSprite && spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(baseColor, flashColor, pulse);
            }

            if (scalePulse > 0f)
            {
                transform.localScale = baseScale * (1f + scalePulse * pulse);
            }
        }

        private void ResetVisuals()
        {
            if (!feedbackActive)
            {
                return;
            }

            if (flashSprite && spriteRenderer != null)
            {
                spriteRenderer.color = baseColor;
            }

            if (scalePulse > 0f)
            {
                transform.localScale = baseScale;
            }

            feedbackActive = false;
        }

        private void OnValidate()
        {
            flashDuration = Mathf.Max(0f, flashDuration);
            scalePulse = Mathf.Max(0f, scalePulse);
        }
    }
}
