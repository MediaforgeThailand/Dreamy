using System.Collections.Generic;
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
    [RequireComponent(typeof(DreamyPlayerProgression))]
    public sealed class DreamyMobilePlayer : MonoBehaviour
    {
        private const float DefaultSheetPixelsPerUnit = 128f;
        private const int RuntimeSortingOrder = 100;
        private const float RuntimeSortingUnitsPerWorldUnit = 10f;
        public const float DefaultMoveSpeed = 4.2f;

        [SerializeField] private float moveSpeed = DefaultMoveSpeed;
        [SerializeField] private DreamyVirtualJoystick joystick;
        [SerializeField] private Vector2 minBounds = new Vector2(-3.3f, -5.2f);
        [SerializeField] private Vector2 maxBounds = new Vector2(3.3f, 5.2f);
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private Sprite[] attackFrames;
        [SerializeField] private float idleFramesPerSecond = 4f;
        [SerializeField] private float walkFramesPerSecond = 8f;
        [SerializeField] private float attackFramesPerSecond = 12f;
        [SerializeField] private float dodgeSpeed = 10.5f;
        [SerializeField] private float dodgeDuration = 0.18f;
        [SerializeField] private float dodgeCooldown = 0.55f;
        [SerializeField] private float dodgeStaminaCost = 18f;
        [SerializeField] private bool sourceFacesLeft;

        private bool wasMoving;
        private float animationTime;
        private Vector2 movementInput;
        private Vector2 lastMoveDirection = Vector2.down;
        private Vector2 dodgeDirection = Vector2.down;
        private float dodgeEndsAt;
        private float nextDodgeTime;
        private bool queuedDodge;
        private float attackStartedAt;
        private float attackEndsAt;
        private Sprite[] activeAttackFrames;
        private Sprite[][] attackFrameSets = System.Array.Empty<Sprite[]>();
        private int nextAttackFrameSetIndex;
        private Rigidbody2D rigidbody2d;
        private SpriteRenderer spriteRenderer;
        private DreamyCharacterStats characterStats;
        private DreamyInventory inventory;
        private DreamyExperience experience;
        private DreamyPlayerProgression progression;
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
        public DreamyPlayerProgression Progression => progression != null ? progression : GetComponent<DreamyPlayerProgression>();
        public Vector2 FacingDirection => lastMoveDirection.sqrMagnitude >= 0.01f ? lastMoveDirection.normalized : Vector2.down;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            rigidbody2d = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            characterStats = GetOrAddComponent<DreamyCharacterStats>();
            inventory = GetOrAddComponent<DreamyInventory>();
            experience = GetOrAddComponent<DreamyExperience>();
            progression = GetOrAddComponent<DreamyPlayerProgression>();
            progression.Bind(experience);
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
                GetOrAddComponent<DreamyCharacterGrounding>();
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

        public void ConfigureKnightVisuals(
            Texture2D idleSheet,
            Texture2D runSheet,
            Texture2D attackSheet,
            int idleFrameCount = 8,
            int runFrameCount = 6,
            int attackFrameCount = 4,
            float pixelsPerUnit = DefaultSheetPixelsPerUnit)
        {
            ConfigureCharacterVisuals(
                idleSheet,
                idleFrameCount,
                1,
                runSheet,
                runFrameCount,
                1,
                attackSheet,
                attackFrameCount,
                attackSheet != null ? 1 : 0,
                pixelsPerUnit,
                4f,
                8f,
                12f,
                false);
        }

        public void ConfigureKnightVisuals(
            Texture2D idleSheet,
            Texture2D runSheet,
            Texture2D[] attackSheets,
            float pixelsPerUnit = DefaultSheetPixelsPerUnit)
        {
            ConfigureCharacterVisuals(
                idleSheet,
                8,
                1,
                -1,
                runSheet,
                6,
                1,
                -1,
                attackSheets,
                new[] { 4, 4, 4 },
                new[] { 1, 1, 1 },
                pixelsPerUnit,
                4f,
                8f,
                12f,
                false);
        }

        public void ConfigureCharacterVisuals(
            Texture2D idleSheet,
            int idleColumns,
            int idleRows,
            Texture2D walkSheet,
            int walkColumns,
            int walkRows,
            Texture2D attackSheet,
            int attackColumns,
            int attackRows,
            float pixelsPerUnit,
            float idleFps,
            float walkFps,
            float attackFps,
            bool characterSourceFacesLeft)
        {
            ConfigureCharacterVisuals(
                idleSheet,
                idleColumns,
                idleRows,
                -1,
                walkSheet,
                walkColumns,
                walkRows,
                -1,
                attackSheet != null ? new[] { attackSheet } : System.Array.Empty<Texture2D>(),
                new[] { attackColumns },
                new[] { attackRows },
                pixelsPerUnit,
                idleFps,
                walkFps,
                attackFps,
                characterSourceFacesLeft);
        }

        public void ConfigureCharacterVisuals(
            Texture2D idleSheet,
            int idleColumns,
            int idleRows,
            int idleRowFromTop,
            Texture2D walkSheet,
            int walkColumns,
            int walkRows,
            int walkRowFromTop,
            Texture2D[] attackSheets,
            int[] attackColumns,
            int[] attackRows,
            float pixelsPerUnit,
            float idleFps,
            float walkFps,
            float attackFps,
            bool characterSourceFacesLeft)
        {
            idleFrames = BuildFrames(idleSheet, idleColumns, idleRows, pixelsPerUnit, idleRowFromTop);
            walkFrames = BuildFrames(walkSheet, walkColumns, walkRows, pixelsPerUnit, walkRowFromTop);
            attackFrameSets = BuildAttackFrameSets(attackSheets, attackColumns, attackRows, pixelsPerUnit);
            attackFrames = attackFrameSets.Length > 0 ? attackFrameSets[0] : System.Array.Empty<Sprite>();
            activeAttackFrames = attackFrames;
            idleFramesPerSecond = Mathf.Max(1f, idleFps);
            walkFramesPerSecond = Mathf.Max(1f, walkFps);
            attackFramesPerSecond = Mathf.Max(1f, attackFps);
            sourceFacesLeft = characterSourceFacesLeft;
            wasMoving = false;
            animationTime = 0f;
            attackEndsAt = 0f;
            nextAttackFrameSetIndex = 0;
            if (spriteRenderer != null && Mathf.Abs(lastMoveDirection.x) <= 0.01f)
            {
                spriteRenderer.flipX = false;
            }

            ApplyFacingFlip();

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

        public void PlayAttack(float duration)
        {
            PlayAttack(duration, FacingDirection);
        }

        public void PlayAttack(float duration, Vector2 attackDirection)
        {
            if (attackFrames == null || attackFrames.Length == 0)
            {
                return;
            }

            if (attackDirection.sqrMagnitude >= 0.01f)
            {
                lastMoveDirection = attackDirection.normalized;
            }

            activeAttackFrames = GetNextAttackFrames();
            attackStartedAt = Time.time;
            attackEndsAt = Time.time + Mathf.Max(0.01f, duration);
            animationTime = 0f;
            ApplyFacingFlip();
            ApplyFrame(activeAttackFrames, 0);
        }

        public void ResetAttackCombo()
        {
            nextAttackFrameSetIndex = 0;
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
            dodgeSpeed = Mathf.Max(0f, dodgeSpeed);
            dodgeDuration = Mathf.Max(0f, dodgeDuration);
            dodgeCooldown = Mathf.Max(0f, dodgeCooldown);
            dodgeStaminaCost = Mathf.Max(0f, dodgeStaminaCost);
            attackFramesPerSecond = Mathf.Max(1f, attackFramesPerSecond);
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
                ApplyFacingFlip();
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
            if (Time.time < attackEndsAt && activeAttackFrames != null && activeAttackFrames.Length > 0)
            {
                float attackElapsed = Mathf.Max(0f, Time.time - attackStartedAt);
                int attackFrameIndex = Mathf.Min(activeAttackFrames.Length - 1, Mathf.FloorToInt(attackElapsed * attackFramesPerSecond));
                ApplyFrame(activeAttackFrames, attackFrameIndex);
                return;
            }

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
            ApplyFacingFlip();
        }

        private static Sprite[] BuildFrames(Texture2D texture, int frameCount, float pixelsPerUnit)
        {
            return BuildFrames(texture, frameCount, 1, pixelsPerUnit);
        }

        private static Sprite[] BuildFrames(Texture2D texture, int columns, int rows, float pixelsPerUnit)
        {
            return BuildFrames(texture, columns, rows, pixelsPerUnit, -1);
        }

        private static Sprite[] BuildFrames(Texture2D texture, int columns, int rows, float pixelsPerUnit, int rowFromTop)
        {
            if (texture == null || columns <= 0 || rows <= 0)
            {
                return System.Array.Empty<Sprite>();
            }

            PreparePixelArtTexture(texture);
            int width = Mathf.Max(1, texture.width / columns);
            int height = Mathf.Max(1, texture.height / rows);
            bool useSingleRow = rowFromTop >= 0 && rows > 1;
            int frameCount = useSingleRow ? columns : columns * rows;
            Sprite[] frames = new Sprite[frameCount];
            int frameIndex = 0;
            int startRow = useSingleRow ? Mathf.Clamp(rowFromTop, 0, rows - 1) : 0;
            int endRow = useSingleRow ? startRow + 1 : rows;
            for (int row = startRow; row < endRow; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    float y = (rows - 1 - row) * height;
                    Rect rect = new Rect(column * width, y, width, height);
                    frames[frameIndex] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.28f), Mathf.Max(1f, pixelsPerUnit), 0, SpriteMeshType.FullRect);
                    frameIndex++;
                }
            }

            return frames;
        }

        private static Sprite[][] BuildAttackFrameSets(Texture2D[] textures, int[] columns, int[] rows, float pixelsPerUnit)
        {
            if (textures == null || textures.Length == 0)
            {
                return System.Array.Empty<Sprite[]>();
            }

            List<Sprite[]> sets = new List<Sprite[]>();
            for (int i = 0; i < textures.Length; i++)
            {
                Texture2D texture = textures[i];
                if (texture == null)
                {
                    continue;
                }

                int columnCount = GetArrayValue(columns, i, 1);
                int rowCount = GetArrayValue(rows, i, 1);
                Sprite[] frames = BuildFrames(texture, columnCount, rowCount, pixelsPerUnit);
                if (frames.Length > 0)
                {
                    sets.Add(frames);
                }
            }

            return sets.Count > 0 ? sets.ToArray() : System.Array.Empty<Sprite[]>();
        }

        private static int GetArrayValue(int[] values, int index, int fallback)
        {
            if (values == null || index < 0 || index >= values.Length || values[index] <= 0)
            {
                return Mathf.Max(1, fallback);
            }

            return values[index];
        }

        private Sprite[] GetNextAttackFrames()
        {
            if (attackFrameSets != null && attackFrameSets.Length > 0)
            {
                Sprite[] frames = attackFrameSets[nextAttackFrameSetIndex % attackFrameSets.Length];
                nextAttackFrameSetIndex = (nextAttackFrameSetIndex + 1) % attackFrameSets.Length;
                return frames;
            }

            return attackFrames != null ? attackFrames : System.Array.Empty<Sprite>();
        }

        private void ApplyFacingFlip()
        {
            if (spriteRenderer == null || Mathf.Abs(lastMoveDirection.x) <= 0.01f)
            {
                return;
            }

            spriteRenderer.flipX = sourceFacesLeft ? lastMoveDirection.x > 0f : lastMoveDirection.x < 0f;
        }

        private static void PreparePixelArtTexture(Texture2D texture)
        {
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.anisoLevel = 0;
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
