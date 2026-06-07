using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dreamy
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(DreamyCharacterStats))]
    [RequireComponent(typeof(DreamyInventory))]
    [RequireComponent(typeof(DreamyExperience))]
    public sealed class DreamyMobilePlayer : MonoBehaviour
    {
        private const int RuntimeSortingOrder = 100;
        private const float RuntimeSortingUnitsPerWorldUnit = 10f;
        public const float DefaultMoveSpeed = 4.2f;

        [SerializeField] private float moveSpeed = DefaultMoveSpeed;
        [SerializeField] private DreamyVirtualJoystick joystick;
        [SerializeField] private Vector2 minBounds = new Vector2(-3.3f, -5.2f);
        [SerializeField] private Vector2 maxBounds = new Vector2(3.3f, 5.2f);
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private float idleFramesPerSecond = 4f;
        [SerializeField] private float walkFramesPerSecond = 8f;
        [SerializeField] private float dodgeSpeed = 10.5f;
        [SerializeField] private float dodgeDuration = 0.18f;
        [SerializeField] private float dodgeCooldown = 0.55f;
        [SerializeField] private float dodgeStaminaCost = 18f;

        private bool wasMoving;
        private float animationTime;
        private Vector2 movementInput;
        private Vector2 lastMoveDirection = Vector2.down;
        private Vector2 dodgeDirection = Vector2.down;
        private float dodgeEndsAt;
        private float nextDodgeTime;
        private bool queuedDodge;
        private Rigidbody2D rigidbody2d;
        private SpriteRenderer spriteRenderer;
        private DreamyCharacterStats characterStats;
        private DreamyInventory inventory;
        private DreamyExperience experience;
        private Collider2D bodyCollider;
        private float nextCollisionRefresh;

        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = Mathf.Max(0f, value);
        }

        public DreamyCharacterStats CharacterStats => characterStats;
        public DreamyInventory Inventory => inventory;
        public DreamyExperience Experience => experience;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            rigidbody2d = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            characterStats = GetOrAddComponent<DreamyCharacterStats>();
            inventory = GetOrAddComponent<DreamyInventory>();
            experience = GetOrAddComponent<DreamyExperience>();
            if (rigidbody2d == null)
            {
                rigidbody2d = gameObject.AddComponent<Rigidbody2D>();
            }

            if (bodyCollider == null)
            {
                CircleCollider2D circleCollider = gameObject.AddComponent<CircleCollider2D>();
                circleCollider.radius = 0.32f;
                bodyCollider = circleCollider;
            }

            bodyCollider.isTrigger = false;
            if (spriteRenderer != null)
            {
                GetOrAddComponent<DreamyYSortSprite>().Configure(RuntimeSortingOrder, RuntimeSortingUnitsPerWorldUnit);
            }

            transform.localScale = Vector3.one;
            DreamyCharacterCollisionUtility.NormalizeTopDownBody(rigidbody2d);
            ApplyFrame(idleFrames, 0);
        }

        private void Update()
        {
            movementInput = ReadMovementInput();
            if (movementInput.sqrMagnitude >= 0.01f)
            {
                lastMoveDirection = movementInput.normalized;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                QueueDodge();
            }

            if (queuedDodge)
            {
                queuedDodge = false;
                TryStartDodge();
            }

            Animate(IsDodging() ? dodgeDirection : movementInput);
            TryCollectNearbyResource();
            RefreshCharacterCollisionIgnores();
        }

        private void Start()
        {
            ConfigureImportedMapColliders();
        }

        private void FixedUpdate()
        {
            if (IsDodging())
            {
                Move(dodgeDirection, dodgeSpeed, true);
                return;
            }

            Move(movementInput, moveSpeed, false);
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

        public void QueueDodge()
        {
            queuedDodge = true;
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
            dodgeSpeed = Mathf.Max(0f, dodgeSpeed);
            dodgeDuration = Mathf.Max(0f, dodgeDuration);
            dodgeCooldown = Mathf.Max(0f, dodgeCooldown);
            dodgeStaminaCost = Mathf.Max(0f, dodgeStaminaCost);
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

        private bool TryStartDodge()
        {
            if (Time.time < nextDodgeTime || characterStats == null || !characterStats.IsAlive)
            {
                return false;
            }

            if (!characterStats.TrySpendStamina(dodgeStaminaCost))
            {
                return false;
            }

            dodgeDirection = movementInput.sqrMagnitude >= 0.01f ? movementInput.normalized : lastMoveDirection;
            if (dodgeDirection.sqrMagnitude < 0.01f)
            {
                dodgeDirection = Vector2.down;
            }

            dodgeEndsAt = Time.time + dodgeDuration;
            nextDodgeTime = Time.time + dodgeCooldown;
            return true;
        }

        private bool IsDodging()
        {
            return Time.time < dodgeEndsAt;
        }

        private void Move(Vector2 input, float speed, bool normalizeInput)
        {
            if (input.sqrMagnitude < 0.0025f)
            {
                DreamyCharacterCollisionUtility.StopBodyDrift(rigidbody2d);
                return;
            }

            Vector2 motion = normalizeInput ? input.normalized : input;
            Vector2 current = rigidbody2d != null ? rigidbody2d.position : (Vector2)transform.position;
            Vector2 next = current + motion * (speed * Time.fixedDeltaTime);
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

        private void RefreshCharacterCollisionIgnores()
        {
            if (Time.time < nextCollisionRefresh)
            {
                return;
            }

            nextCollisionRefresh = Time.time + 0.45f;
            DreamyCharacterCollisionUtility.IgnoreCollisionWithAll<DreamyMonsterController>(this);
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

            DreamyResourcePickup[] pickups = FindObjectsByType<DreamyResourcePickup>(FindObjectsInactive.Exclude);
            for (int i = 0; i < pickups.Length; i++)
            {
                pickups[i].TryPickup(transform);
            }
        }

        private T GetOrAddComponent<T>() where T : Component
        {
            T component = GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
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
