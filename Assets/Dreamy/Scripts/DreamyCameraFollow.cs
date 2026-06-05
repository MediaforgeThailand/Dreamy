using UnityEngine;

namespace Dreamy
{
    public sealed class DreamyCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0f;
        [SerializeField] private Vector2 minBounds = new Vector2(-5f, -9f);
        [SerializeField] private Vector2 maxBounds = new Vector2(5f, 9f);
        [SerializeField] private bool snapToPixelGrid = true;
        [SerializeField] private float pixelsPerUnit = 64f;
        [SerializeField] private bool lockIntegerPixelScale = true;
        [SerializeField] private float desiredOrthographicSize = 8f;

        private Camera followCamera;
        private Vector3 velocity;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        public void SetBounds(Vector2 minimum, Vector2 maximum)
        {
            minBounds = minimum;
            maxBounds = maximum;
        }

        private void Awake()
        {
            followCamera = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            ApplyPixelPerfectCameraSize();

            if (target == null)
            {
                return;
            }

            Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);
            desired.x = Mathf.Clamp(desired.x, minBounds.x, maxBounds.x);
            desired.y = Mathf.Clamp(desired.y, minBounds.y, maxBounds.y);

            Vector3 next = smoothTime > 0.001f && !snapToPixelGrid
                ? Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime)
                : desired;
            if (snapToPixelGrid && pixelsPerUnit > 0f)
            {
                next.x = Mathf.Round(next.x * pixelsPerUnit) / pixelsPerUnit;
                next.y = Mathf.Round(next.y * pixelsPerUnit) / pixelsPerUnit;
            }

            transform.position = next;
        }

        private void ApplyPixelPerfectCameraSize()
        {
            if (!lockIntegerPixelScale || followCamera == null || !followCamera.orthographic || pixelsPerUnit <= 0f)
            {
                return;
            }

            int screenHeight = Mathf.Max(1, Screen.height);
            float desiredPixelScale = screenHeight / (2f * desiredOrthographicSize * pixelsPerUnit);
            int integerPixelScale = Mathf.Max(1, Mathf.RoundToInt(desiredPixelScale));
            followCamera.orthographicSize = screenHeight / (2f * pixelsPerUnit * integerPixelScale);
        }
    }
}
