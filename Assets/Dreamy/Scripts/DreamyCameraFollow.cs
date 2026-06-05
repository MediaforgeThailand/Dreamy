using UnityEngine;

namespace Dreamy
{
    public sealed class DreamyCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.16f;
        [SerializeField] private Vector2 minBounds = new Vector2(-5f, -9f);
        [SerializeField] private Vector2 maxBounds = new Vector2(5f, 9f);
        [SerializeField] private bool snapToPixelGrid = true;
        [SerializeField] private float pixelsPerUnit = 64f;
        [SerializeField] private bool lockIntegerPixelScale = true;
        [SerializeField] private float desiredOrthographicSize = 8f;
        [SerializeField] private float followSpeedPercent = 115f;
        [SerializeField] private float minimumFollowSpeed = 4.5f;
        [SerializeField] private float maximumFollowSpeed = 80f;
        [SerializeField, Range(0f, 45f)] private float edgeSafeZonePercent = 20f;
        [SerializeField, Range(0f, 1f)] private float safeZoneBoostStart = 0.55f;
        [SerializeField] private float safeZoneBoostMultiplier = 3.5f;
        [SerializeField] private float emergencyCatchupMultiplier = 8f;

        private Camera followCamera;
        private Vector3 previousTargetPosition;
        private Vector3 followVelocity;
        private bool hasPreviousTargetPosition;
        private float currentFollowSpeed;

        public float SmoothTime
        {
            get => smoothTime;
            set => smoothTime = Mathf.Max(0f, value);
        }

        public float FollowSpeedPercent
        {
            get => followSpeedPercent;
            set => followSpeedPercent = Mathf.Max(0f, value);
        }

        public float EdgeSafeZonePercent
        {
            get => edgeSafeZonePercent;
            set => edgeSafeZonePercent = Mathf.Clamp(value, 0f, 45f);
        }

        public float SafeZoneBoostMultiplier
        {
            get => safeZoneBoostMultiplier;
            set => safeZoneBoostMultiplier = Mathf.Max(1f, value);
        }

        public float OrthographicSize
        {
            get => followCamera != null ? followCamera.orthographicSize : desiredOrthographicSize;
            set
            {
                desiredOrthographicSize = Mathf.Max(0.1f, value);
                if (followCamera != null && followCamera.orthographic)
                {
                    followCamera.orthographicSize = desiredOrthographicSize;
                }
            }
        }

        public Transform Target
        {
            get => target;
            set
            {
                target = value;
                hasPreviousTargetPosition = false;
                currentFollowSpeed = 0f;
                followVelocity = Vector3.zero;
            }
        }

        public void SetBounds(Vector2 minimum, Vector2 maximum)
        {
            minBounds = minimum;
            maxBounds = maximum;
        }

        public void Configure(float orthographicSize, bool pixelGridSnap, bool integerPixelScale, float smoothing, float cameraFollowSpeedPercent = 115f)
        {
            desiredOrthographicSize = orthographicSize;
            snapToPixelGrid = pixelGridSnap;
            lockIntegerPixelScale = integerPixelScale;
            smoothTime = Mathf.Max(0f, smoothing);
            followSpeedPercent = Mathf.Max(0f, cameraFollowSpeedPercent);

            if (followCamera != null && followCamera.orthographic)
            {
                followCamera.orthographicSize = orthographicSize;
            }
        }

        private void Awake()
        {
            followCamera = GetComponent<Camera>();
        }

        private void OnValidate()
        {
            smoothTime = Mathf.Max(0f, smoothTime);
            desiredOrthographicSize = Mathf.Max(0.1f, desiredOrthographicSize);
            followSpeedPercent = Mathf.Max(0f, followSpeedPercent);
            minimumFollowSpeed = Mathf.Max(0f, minimumFollowSpeed);
            maximumFollowSpeed = Mathf.Max(minimumFollowSpeed, maximumFollowSpeed);
            edgeSafeZonePercent = Mathf.Clamp(edgeSafeZonePercent, 0f, 45f);
            safeZoneBoostStart = Mathf.Clamp01(safeZoneBoostStart);
            safeZoneBoostMultiplier = Mathf.Max(1f, safeZoneBoostMultiplier);
            emergencyCatchupMultiplier = Mathf.Max(0f, emergencyCatchupMultiplier);
        }

        private void LateUpdate()
        {
            ApplyPixelPerfectCameraSize();

            if (target == null)
            {
                hasPreviousTargetPosition = false;
                return;
            }

            float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 targetPosition = target.position;
            Vector3 desired = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
            desired = ClampCameraCenterToMapBounds(desired);

            float targetSpeed = GetTargetSpeed(targetPosition, deltaTime);
            float safeZonePressure = CalculateSafeZonePressure(transform.position, targetPosition);
            float followSpeed = CalculateFollowSpeed(targetSpeed, safeZonePressure, deltaTime);
            float damping = Mathf.Max(0.001f, smoothTime);
            Vector3 next = Vector3.SmoothDamp(transform.position, desired, ref followVelocity, damping, followSpeed, deltaTime);
            next.z = transform.position.z;
            next = KeepTargetInsideSafeZone(next, targetPosition);
            next = ClampCameraCenterToMapBounds(next);
            if (snapToPixelGrid && pixelsPerUnit > 0f)
            {
                next.x = Mathf.Round(next.x * pixelsPerUnit) / pixelsPerUnit;
                next.y = Mathf.Round(next.y * pixelsPerUnit) / pixelsPerUnit;
                next = ClampCameraCenterToMapBounds(next);
            }

            transform.position = next;
        }

        private float GetTargetSpeed(Vector3 targetPosition, float deltaTime)
        {
            if (!hasPreviousTargetPosition)
            {
                previousTargetPosition = targetPosition;
                hasPreviousTargetPosition = true;
                return 0f;
            }

            float targetSpeed = Vector2.Distance((Vector2)targetPosition, (Vector2)previousTargetPosition) / deltaTime;
            previousTargetPosition = targetPosition;
            return targetSpeed;
        }

        private float CalculateFollowSpeed(float targetSpeed, float safeZonePressure, float deltaTime)
        {
            float targetBasedSpeed = targetSpeed * Mathf.Max(0f, followSpeedPercent) / 100f;
            float minSpeed = Mathf.Max(0f, minimumFollowSpeed);
            float maxSpeed = Mathf.Max(minSpeed, maximumFollowSpeed);
            float boostT = Mathf.InverseLerp(Mathf.Clamp01(safeZoneBoostStart), 1f, safeZonePressure);
            float boostMultiplier = Mathf.Lerp(1f, Mathf.Max(1f, safeZoneBoostMultiplier), Mathf.SmoothStep(0f, 1f, boostT));
            float emergencySpeed = safeZonePressure > 1f
                ? targetSpeed * (safeZonePressure - 1f) * Mathf.Max(0f, emergencyCatchupMultiplier)
                : 0f;
            float targetFollowSpeed = Mathf.Clamp(
                Mathf.Max(minSpeed, targetBasedSpeed * boostMultiplier + emergencySpeed),
                0f,
                maxSpeed);

            if (smoothTime <= 0.001f || currentFollowSpeed <= 0f)
            {
                currentFollowSpeed = targetFollowSpeed;
            }
            else
            {
                float responseTime = targetFollowSpeed > currentFollowSpeed
                    ? Mathf.Max(0.02f, smoothTime * 0.35f)
                    : Mathf.Max(0.05f, smoothTime * 1.8f);
                float t = 1f - Mathf.Exp(-deltaTime / responseTime);
                currentFollowSpeed = Mathf.Lerp(currentFollowSpeed, targetFollowSpeed, t);
            }

            return currentFollowSpeed;
        }

        private float CalculateSafeZonePressure(Vector3 cameraPosition, Vector3 targetPosition)
        {
            if (!TryGetSafeZoneHalfSize(out Vector2 safeHalfSize))
            {
                return 0f;
            }

            Vector2 offset = (Vector2)(targetPosition - cameraPosition);
            float xPressure = safeHalfSize.x > 0.001f ? Mathf.Abs(offset.x) / safeHalfSize.x : 0f;
            float yPressure = safeHalfSize.y > 0.001f ? Mathf.Abs(offset.y) / safeHalfSize.y : 0f;
            return Mathf.Max(xPressure, yPressure);
        }

        private Vector3 KeepTargetInsideSafeZone(Vector3 cameraPosition, Vector3 targetPosition)
        {
            if (!TryGetSafeZoneHalfSize(out Vector2 safeHalfSize))
            {
                return cameraPosition;
            }

            Vector2 offset = (Vector2)(targetPosition - cameraPosition);
            if (offset.x > safeHalfSize.x)
            {
                cameraPosition.x = targetPosition.x - safeHalfSize.x;
            }
            else if (offset.x < -safeHalfSize.x)
            {
                cameraPosition.x = targetPosition.x + safeHalfSize.x;
            }

            if (offset.y > safeHalfSize.y)
            {
                cameraPosition.y = targetPosition.y - safeHalfSize.y;
            }
            else if (offset.y < -safeHalfSize.y)
            {
                cameraPosition.y = targetPosition.y + safeHalfSize.y;
            }

            return cameraPosition;
        }

        private bool TryGetSafeZoneHalfSize(out Vector2 safeHalfSize)
        {
            safeHalfSize = Vector2.zero;
            if (!TryGetViewportHalfSize(out Vector2 viewportHalfSize))
            {
                return false;
            }

            float edgePercent = Mathf.Clamp(edgeSafeZonePercent, 0f, 45f) / 100f;
            float safeScale = Mathf.Max(0.05f, 1f - edgePercent * 2f);
            safeHalfSize = viewportHalfSize * safeScale;
            return safeHalfSize.x > 0f && safeHalfSize.y > 0f;
        }

        private Vector3 ClampCameraCenterToMapBounds(Vector3 cameraPosition)
        {
            if (!TryGetViewportHalfSize(out Vector2 viewportHalfSize))
            {
                cameraPosition.x = Mathf.Clamp(cameraPosition.x, minBounds.x, maxBounds.x);
                cameraPosition.y = Mathf.Clamp(cameraPosition.y, minBounds.y, maxBounds.y);
                return cameraPosition;
            }

            float minX = minBounds.x + viewportHalfSize.x;
            float maxX = maxBounds.x - viewportHalfSize.x;
            float minY = minBounds.y + viewportHalfSize.y;
            float maxY = maxBounds.y - viewportHalfSize.y;

            cameraPosition.x = minX <= maxX
                ? Mathf.Clamp(cameraPosition.x, minX, maxX)
                : (minBounds.x + maxBounds.x) * 0.5f;
            cameraPosition.y = minY <= maxY
                ? Mathf.Clamp(cameraPosition.y, minY, maxY)
                : (minBounds.y + maxBounds.y) * 0.5f;
            return cameraPosition;
        }

        private bool TryGetViewportHalfSize(out Vector2 viewportHalfSize)
        {
            viewportHalfSize = Vector2.zero;
            if (followCamera == null || !followCamera.orthographic)
            {
                return false;
            }

            float halfHeight = followCamera.orthographicSize;
            float halfWidth = halfHeight * followCamera.aspect;
            viewportHalfSize = new Vector2(halfWidth, halfHeight);
            return viewportHalfSize.x > 0f && viewportHalfSize.y > 0f;
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
