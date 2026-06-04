using UnityEngine;

namespace Dreamy
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class DreamyMobilePlayer : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4.2f;
        [SerializeField] private DreamyVirtualJoystick joystick;
        [SerializeField] private Vector2 minBounds = new Vector2(-3.3f, -5.2f);
        [SerializeField] private Vector2 maxBounds = new Vector2(3.3f, 5.2f);
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private float idleFramesPerSecond = 4f;
        [SerializeField] private float walkFramesPerSecond = 8f;

        private bool wasMoving;
        private float animationTime;
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            ApplyFrame(idleFrames, 0);
        }

        private void Update()
        {
            Vector2 input = ReadMovementInput();
            Move(input);
            Animate(input);
            TryCollectNearbyResource();
        }

        public void Bind(DreamyVirtualJoystick movementJoystick, Sprite[] idleSprites, Sprite[] walkSprites)
        {
            joystick = movementJoystick;
            idleFrames = idleSprites;
            walkFrames = walkSprites;
            ApplyFrame(idleFrames, 0);
        }

        public void SetMovementBounds(Vector2 minimum, Vector2 maximum)
        {
            minBounds = minimum;
            maxBounds = maximum;
        }

        private Vector2 ReadMovementInput()
        {
            Vector2 input = joystick != null ? joystick.Direction : Vector2.zero;

            if (input.sqrMagnitude < 0.01f)
            {
                input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            }

            return Vector2.ClampMagnitude(input, 1f);
        }

        private void Move(Vector2 input)
        {
            if (input.sqrMagnitude < 0.0025f)
            {
                return;
            }

            Vector3 current = transform.position;
            Vector3 next = current + new Vector3(input.x, input.y, 0f) * (moveSpeed * Time.deltaTime);
            next.x = Mathf.Clamp(next.x, minBounds.x, maxBounds.x);
            next.y = Mathf.Clamp(next.y, minBounds.y, maxBounds.y);
            transform.position = next;

            if (spriteRenderer != null && Mathf.Abs(input.x) > 0.01f)
            {
                spriteRenderer.flipX = input.x < 0f;
            }
        }

        private void Animate(Vector2 input)
        {
            bool isMoving = input.sqrMagnitude >= 0.0025f;
            Sprite[] frames = isMoving && walkFrames != null && walkFrames.Length > 0 ? walkFrames : idleFrames;

            if (frames == null || frames.Length == 0)
            {
                return;
            }

            if (isMoving != wasMoving)
            {
                wasMoving = isMoving;
                animationTime = 0f;
            }

            float framesPerSecond = isMoving ? walkFramesPerSecond : idleFramesPerSecond;
            animationTime += Time.deltaTime * framesPerSecond;
            int frameIndex = Mathf.FloorToInt(animationTime) % frames.Length;
            ApplyFrame(frames, frameIndex);
        }

        private void ApplyFrame(Sprite[] frames, int index)
        {
            if (spriteRenderer == null || frames == null || frames.Length == 0)
            {
                return;
            }

            spriteRenderer.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
        }

        private void TryCollectNearbyResource()
        {
            DreamyResourceNode[] nodes = FindObjectsByType<DreamyResourceNode>(FindObjectsInactive.Exclude);
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i].TryCollect(transform);
            }
        }
    }
}
