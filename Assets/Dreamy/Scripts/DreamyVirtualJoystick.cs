using UnityEngine;
using UnityEngine.EventSystems;

namespace Dreamy
{
    public sealed class DreamyVirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform handle;
        [SerializeField] private float radius = 96f;

        private RectTransform rectTransform;
        private Vector2 direction;

        public Vector2 Direction => direction;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            CenterHandle();
        }

        public void Bind(RectTransform handleRectTransform, float joystickRadius)
        {
            handle = handleRectTransform;
            radius = Mathf.Max(joystickRadius, 1f);
            CenterHandle();
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
    }
}
