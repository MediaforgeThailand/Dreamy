using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dreamy
{
    public sealed class DreamyVirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform handle;
        [SerializeField] private float radius = 96f;
        [SerializeField] private bool showOnlyHandle = true;

        private RectTransform rectTransform;
        private Vector2 direction;

        public Vector2 Direction => direction;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            ApplyMinimalVisuals();
            CenterHandle();
        }

        public void Bind(RectTransform handleRectTransform, float joystickRadius)
        {
            handle = handleRectTransform;
            radius = Mathf.Max(joystickRadius, 1f);
            ApplyMinimalVisuals();
            CenterHandle();
        }

        private void OnValidate()
        {
            radius = Mathf.Max(radius, 1f);
            ApplyMinimalVisuals();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            UpdateDirection(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateDirection(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            direction = Vector2.zero;
            CenterHandle();
        }

        private void UpdateDirection(PointerEventData eventData)
        {
            if (rectTransform == null)
            {
                rectTransform = (RectTransform)transform;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                return;
            }

            Vector2 clamped = Vector2.ClampMagnitude(localPoint, radius);
            direction = clamped / radius;

            if (handle != null)
            {
                handle.anchoredPosition = clamped;
            }
        }

        private void CenterHandle()
        {
            if (handle != null)
            {
                handle.anchoredPosition = Vector2.zero;
            }
        }

        private void ApplyMinimalVisuals()
        {
            if (!showOnlyHandle)
            {
                return;
            }

            Image background = GetComponent<Image>();
            if (background != null)
            {
                Color color = background.color;
                color.a = 0f;
                background.color = color;
                background.raycastTarget = true;
            }

            if (handle != null)
            {
                Image handleImage = handle.GetComponent<Image>();
                if (handleImage != null)
                {
                    handleImage.raycastTarget = false;
                    handleImage.preserveAspect = true;
                    Color color = handleImage.color;
                    color.a = Mathf.Max(color.a, 0.92f);
                    handleImage.color = color;
                }
            }
        }
    }
}
