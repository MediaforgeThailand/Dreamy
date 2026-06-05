using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dreamy
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class DreamyMobilePlayer : MonoBehaviour
    {
        private const int RuntimeSortingOrder = 11;

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
        private Vector2 movementInput;
        private Rigidbody2D rigidbody2d;
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            rigidbody2d = GetComponent<Rigidbody2D>();
            if (rigidbody2d == null)
            {
                rigidbody2d = gameObject.AddComponent<Rigidbody2D>();
            }

            if (spriteRenderer != null && spriteRenderer.sortingOrder >= 100)
            {
                spriteRenderer.sortingOrder = RuntimeSortingOrder;
            }

            transform.localScale = Vector3.one;
            rigidbody2d.gravityScale = 0f;
            rigidbody2d.freezeRotation = true;
            rigidbody2d.interpolation = RigidbodyInterpolation2D.Interpolate;
            rigidbody2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            ApplyFrame(idleFrames, 0);
        }

        private void Update()
        {
            movementInput = ReadMovementInput();
            Animate(movementInput);
            TryCollectNearbyResource();
        }

        private void Start()
        {
            ConfigureImportedMapColliders();
        }

        private void FixedUpdate()
        {
            Move(movementInput);
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

            Vector2 current = rigidbody2d != null ? rigidbody2d.position : (Vector2)transform.position;
            Vector2 next = current + input * (moveSpeed * Time.fixedDeltaTime);
            next.x = Mathf.Clamp(next.x, minBounds.x, maxBounds.x);
            next.y = Mathf.Clamp(next.y, minBounds.y, maxBounds.y);

            if (rigidbody2d != null)
            {
                rigidbody2d.MovePosition(next);
            }
            else
            {
                transform.position = new Vector3(next.x, next.y, transform.position.z);
            }

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

        private static void ConfigureImportedMapColliders()
        {
            TilemapCollider2D[] tilemapColliders = FindObjectsByType<TilemapCollider2D>(FindObjectsInactive.Exclude);
            for (int i = 0; i < tilemapColliders.Length; i++)
            {
                bool shouldBlockMovement = DreamyLevelTileRules.LayerBlocksMovement(tilemapColliders[i].gameObject.name);
                tilemapColliders[i].enabled = shouldBlockMovement;
                tilemapColliders[i].isTrigger = false;

                CompositeCollider2D composite = tilemapColliders[i].GetComponent<CompositeCollider2D>();
                if (composite != null)
                {
                    composite.enabled = shouldBlockMovement;
                    composite.isTrigger = false;
                }
            }
        }

    }
}
