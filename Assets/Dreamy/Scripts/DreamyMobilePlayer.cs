using UnityEngine;

namespace Dreamy
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class DreamyMobilePlayer : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4.2f;
        [SerializeField] private float stopDistance = 0.08f;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Transform touchMarker;

        private Vector3 targetPosition;
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            targetPosition = transform.position;

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }
        }

        private void Update()
        {
            ReadTapInput();
            MoveToTarget();
            TryCollectNearbyResource();
        }

        private void ReadTapInput()
        {
            if (worldCamera == null)
            {
                return;
            }

            bool hasTap = false;
            Vector2 screenPosition = Vector2.zero;

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                hasTap = touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved;
                screenPosition = touch.position;
            }
            else if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))
            {
                hasTap = true;
                screenPosition = Input.mousePosition;
            }

            if (!hasTap)
            {
                return;
            }

            Vector3 world = worldCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -worldCamera.transform.position.z));
            targetPosition = new Vector3(world.x, world.y, transform.position.z);

            if (touchMarker != null)
            {
                touchMarker.position = new Vector3(targetPosition.x, targetPosition.y, touchMarker.position.z);
                touchMarker.gameObject.SetActive(true);
            }
        }

        private void MoveToTarget()
        {
            Vector3 current = transform.position;
            Vector3 delta = targetPosition - current;

            if (delta.magnitude <= stopDistance)
            {
                return;
            }

            Vector3 next = Vector3.MoveTowards(current, targetPosition, moveSpeed * Time.deltaTime);
            transform.position = next;

            if (spriteRenderer != null && Mathf.Abs(delta.x) > 0.01f)
            {
                spriteRenderer.flipX = delta.x < 0f;
            }
        }

        private void TryCollectNearbyResource()
        {
            DreamyResourceNode[] nodes = FindObjectsByType<DreamyResourceNode>(FindObjectsSortMode.None);
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i].TryCollect(transform);
            }
        }
    }
}
