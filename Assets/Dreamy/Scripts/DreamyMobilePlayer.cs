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
        private static readonly int AnimatorIsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int AnimatorAttack1Hash = Animator.StringToHash("Attack1");
        private static readonly int AnimatorAttack2Hash = Animator.StringToHash("Attack2");
        private static readonly int AnimatorAttack3Hash = Animator.StringToHash("Attack3");
        private static readonly int AnimatorSuperSmashHash = Animator.StringToHash("SuperSmash");
        private static readonly int AnimatorDashHash = Animator.StringToHash("Dash");
        private static readonly int AnimatorHurtHash = Animator.StringToHash("Hurt");
        private static readonly int AnimatorAttack1StateHash = Animator.StringToHash("Base Layer.Attack 1");
        private static readonly int AnimatorAttack2StateHash = Animator.StringToHash("Base Layer.Attack 2");
        private static readonly int AnimatorAttack3StateHash = Animator.StringToHash("Base Layer.Attack 3");
        private static readonly int AnimatorSuperSmashStateHash = Animator.StringToHash("Base Layer.Super Smash");
        private const float StopSpeedThreshold = 0.03f;
        public const float DefaultMoveSpeed = 4.2f;

        [Header("Movement Feel")]
        [SerializeField] private float moveSpeed = DefaultMoveSpeed;
        [SerializeField] private float acceleration = 28f;
        [SerializeField] private float deceleration = 34f;
        [SerializeField] private float turnAcceleration = 42f;
        [SerializeField, Range(0f, 0.45f)] private float inputDeadZone = 0.12f;

        [Header("Sprint")]
        [SerializeField, Min(1f)] private float sprintSpeedMultiplier = 1.35f;
        [SerializeField, Min(0f)] private float sprintStaminaPerSecond = 18f;
        [SerializeField, Min(0f)] private float sprintStartStamina = 8f;

        [Header("References")]
        [SerializeField] private DreamyVirtualJoystick joystick;
        [SerializeField] private Vector2 minBounds = new Vector2(-3.3f, -5.2f);
        [SerializeField] private Vector2 maxBounds = new Vector2(3.3f, 5.2f);

        [Header("Animation")]
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private Sprite[] attackFrames;
        [SerializeField] private Sprite[] dodgeFrames;
        [SerializeField] private Sprite[] hurtFrames;
        [SerializeField] private Sprite[] specialAttackFrames;
        [SerializeField] private float idleFramesPerSecond = 4f;
        [SerializeField] private float walkFramesPerSecond = 8f;
        [SerializeField] private float attackFramesPerSecond = 12f;
        [SerializeField] private float dodgeFramesPerSecond = 24f;
        [SerializeField] private float hurtFramesPerSecond = 12f;
        [SerializeField] private float specialAttackFramesPerSecond = 18f;
        [SerializeField] private float attackMoveSpeedMultiplier = 0.35f;

        [Header("Dodge")]
        [SerializeField] private float dodgeSpeed = 10.5f;
        [SerializeField] private float dodgeDuration = 0.18f;
        [SerializeField] private float dodgeCooldown = 0.55f;
        [SerializeField] private float dodgeStaminaCost = 18f;
        [SerializeField] private bool sourceFacesLeft;

        private bool wasMoving;
        private bool isSprinting;
        private float animationTime;
        private Vector2 currentVelocity;
        private Vector2 movementInput;
        private Vector2 lastMoveDirection = Vector2.down;
        private Vector2 dodgeDirection = Vector2.down;
        private float dodgeStartedAt;
        private float dodgeEndsAt;
        private float nextDodgeTime;
        private bool queuedDodge;
        private float attackStartedAt;
        private float attackEndsAt;
        private Sprite[] activeAttackFrames;
        private float activeAttackFramesPerSecond;
        private float hurtStartedAt;
        private float hurtEndsAt;
        private Sprite[][] attackFrameSets = System.Array.Empty<Sprite[]>();
        private int[] attackAnimatorStateIndices = System.Array.Empty<int>();
        private int[] attackPartIndices = System.Array.Empty<int>();
        private float[] attackFrameSpeedMultipliers = System.Array.Empty<float>();
        private int nextAttackFrameSetIndex;
        private Rigidbody2D rigidbody2d;
        private SpriteRenderer spriteRenderer;
        private DreamyCharacterStats characterStats;
        private DreamyInventory inventory;
        private DreamyExperience experience;
        private DreamyPlayerProgression progression;
        private Collider2D bodyCollider;
        private Animator animator;
        private bool useAnimatorController;
        private float nextCollisionRefresh;

        public Vector2 CurrentVelocity => currentVelocity;
        public bool IsSprinting => isSprinting;

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
        public bool IsAttacking => Time.time < attackEndsAt;
        public int LastAttackSequenceIndex { get; private set; }
        public int LastAttackPartIndex { get; private set; }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
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
            useAnimatorController = animator != null && animator.runtimeAnimatorController != null;
            if (!useAnimatorController)
            {
                ApplyFrame(idleFrames, 0);
            }
        }

        private void OnEnable()
        {
            if (characterStats == null)
            {
                characterStats = GetComponent<DreamyCharacterStats>();
            }

            if (characterStats != null)
            {
                characterStats.Damaged += HandleDamaged;
            }
        }

        private void OnDisable()
        {
            if (characterStats != null)
            {
                characterStats.Damaged -= HandleDamaged;
            }
        }

        private void Update()
        {
            bool canAct = characterStats == null || (characterStats.IsAlive && !characterStats.IsStunned);
            movementInput = canAct ? ReadMovementInput() : Vector2.zero;
            if (movementInput.sqrMagnitude >= 0.01f)
            {
                lastMoveDirection = movementInput.normalized;
            }

            if (canAct && Input.GetKeyDown(KeyCode.Space))
            {
                QueueDodge();
            }

            if (canAct && queuedDodge)
            {
                queuedDodge = false;
                TryStartDodge();
            }
            else if (!canAct)
            {
                queuedDodge = false;
            }

            bool isActivelyDodging = canAct && IsDodging();
            Animate(isActivelyDodging ? dodgeDirection * dodgeSpeed : currentVelocity, movementInput);

            if (canAct)
            {
                TryCollectNearbyResource();
                RefreshCharacterCollisionIgnores();
            }
        }

        private void Start()
        {
            ConfigureImportedMapColliders();
        }

        private void FixedUpdate()
        {
            if (characterStats != null && !characterStats.IsAlive)
            {
                isSprinting = false;
                currentVelocity = Vector2.zero;
                DreamyCharacterCollisionUtility.StopBodyDrift(rigidbody2d);
                return;
            }

            float movementSpeedMultiplier = GetMovementSpeedMultiplier();
            if (movementSpeedMultiplier <= 0f)
            {
                isSprinting = false;
                currentVelocity = Vector2.zero;
                DreamyCharacterCollisionUtility.StopBodyDrift(rigidbody2d);
                return;
            }

            if (IsDodging())
            {
                isSprinting = false;
                currentVelocity = dodgeDirection * dodgeSpeed;
                Move(currentVelocity * movementSpeedMultiplier, Time.fixedDeltaTime);
                return;
            }

            if (IsAttacking)
            {
                isSprinting = false;
                UpdateVelocity(movementInput, Time.fixedDeltaTime);
                Move(currentVelocity * attackMoveSpeedMultiplier * movementSpeedMultiplier, Time.fixedDeltaTime);
                return;
            }

            UpdateSprintState(movementInput, Time.fixedDeltaTime);
            UpdateVelocity(movementInput, Time.fixedDeltaTime);
            Move(currentVelocity * movementSpeedMultiplier, Time.fixedDeltaTime);
        }

        public void Bind(DreamyVirtualJoystick movementJoystick, Sprite[] idleSprites, Sprite[] walkSprites)
        {
            DisableAnimatorController();
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
            ConfigureCharacterVisuals(
                idleSheet,
                idleColumns,
                idleRows,
                idleRowFromTop,
                walkSheet,
                walkColumns,
                walkRows,
                walkRowFromTop,
                attackSheets,
                attackColumns,
                attackRows,
                null,
                null,
                null,
                null,
                null,
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
            int[] attackStartFrames,
            int[] attackFrameCounts,
            int[] attackAnimatorStates,
            int[] attackParts,
            float[] attackSpeedMultipliers,
            float pixelsPerUnit,
            float idleFps,
            float walkFps,
            float attackFps,
            bool characterSourceFacesLeft)
        {
            DisableAnimatorController();
            idleFrames = BuildFrames(idleSheet, idleColumns, idleRows, pixelsPerUnit, idleRowFromTop);
            walkFrames = BuildFrames(walkSheet, walkColumns, walkRows, pixelsPerUnit, walkRowFromTop);
            attackFrameSets = BuildAttackFrameSets(attackSheets, attackColumns, attackRows, attackStartFrames, attackFrameCounts, pixelsPerUnit);
            attackAnimatorStateIndices = BuildAttackIntMetadata(attackFrameSets.Length, attackAnimatorStates, 0, true);
            attackPartIndices = BuildAttackIntMetadata(attackFrameSets.Length, attackParts, 0, true);
            attackFrameSpeedMultipliers = BuildAttackFloatMetadata(attackFrameSets.Length, attackSpeedMultipliers, 1f);
            attackFrames = attackFrameSets.Length > 0 ? attackFrameSets[0] : System.Array.Empty<Sprite>();
            activeAttackFrames = attackFrames;
            idleFramesPerSecond = Mathf.Max(1f, idleFps);
            walkFramesPerSecond = Mathf.Max(1f, walkFps);
            attackFramesPerSecond = Mathf.Max(1f, attackFps);
            activeAttackFramesPerSecond = attackFramesPerSecond;
            dodgeFrames = System.Array.Empty<Sprite>();
            hurtFrames = System.Array.Empty<Sprite>();
            specialAttackFrames = System.Array.Empty<Sprite>();
            sourceFacesLeft = characterSourceFacesLeft;
            wasMoving = false;
            animationTime = 0f;
            attackEndsAt = 0f;
            nextAttackFrameSetIndex = 0;
            LastAttackSequenceIndex = 0;
            LastAttackPartIndex = 0;
            if (spriteRenderer != null && Mathf.Abs(lastMoveDirection.x) <= 0.01f)
            {
                spriteRenderer.flipX = false;
            }

            ApplyFacingFlip();

            ApplyFrame(idleFrames, 0);
        }

        public void ConfigureActionVisuals(
            Texture2D dodgeSheet,
            int dodgeColumns,
            int dodgeRows,
            Texture2D hurtSheet,
            int hurtColumns,
            int hurtRows,
            Texture2D specialAttackSheet,
            int specialAttackColumns,
            int specialAttackRows,
            float pixelsPerUnit,
            float dashFps,
            float hurtFps,
            float specialAttackFps)
        {
            dodgeFrames = BuildFrames(dodgeSheet, dodgeColumns, dodgeRows, pixelsPerUnit);
            hurtFrames = BuildFrames(hurtSheet, hurtColumns, hurtRows, pixelsPerUnit);
            specialAttackFrames = BuildFrames(specialAttackSheet, specialAttackColumns, specialAttackRows, pixelsPerUnit);
            dodgeFramesPerSecond = Mathf.Max(1f, dashFps);
            hurtFramesPerSecond = Mathf.Max(1f, hurtFps);
            specialAttackFramesPerSecond = Mathf.Max(1f, specialAttackFps);
        }

        public void ConfigureAnimator(RuntimeAnimatorController controller)
        {
            if (controller == null)
            {
                DisableAnimatorController();
                return;
            }

            if (animator == null)
            {
                animator = GetOrAddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.enabled = true;
            useAnimatorController = true;
            wasMoving = false;
            ResetAnimatorActionTriggers();
            animator.SetBool(AnimatorIsMovingHash, false);
            animator.Rebind();
            animator.Update(0f);
            ApplyFacingFlip();
        }

        public void SetMovementBounds(Vector2 minimum, Vector2 maximum)
        {
            minBounds = minimum;
            maxBounds = maximum;
        }

        public void SetMovementTuning(float maximumSpeed, float accelerationRate, float decelerationRate, float turnAccelerationRate, float deadZone)
        {
            moveSpeed = Mathf.Max(0f, maximumSpeed);
            acceleration = Mathf.Max(0f, accelerationRate);
            deceleration = Mathf.Max(0f, decelerationRate);
            turnAcceleration = Mathf.Max(0f, turnAccelerationRate);
            inputDeadZone = Mathf.Clamp(deadZone, 0f, 0.45f);
        }

        public void QueueDodge()
        {
            queuedDodge = true;
        }

        public float PlayAttack(float duration)
        {
            return PlayAttack(duration, FacingDirection, 1f);
        }

        public float PlayAttack(float duration, Vector2 attackDirection)
        {
            return PlayAttack(duration, attackDirection, 1f);
        }

        public float PlayAttack(float duration, Vector2 attackDirection, float attackSpeedMultiplier)
        {
            int attackSetIndex;
            Sprite[] frames = GetNextAttackFrames(out attackSetIndex);
            LastAttackSequenceIndex = attackSetIndex;
            LastAttackPartIndex = GetAttackPartIndex(attackSetIndex);
            float frameSpeedMultiplier = Mathf.Max(0.05f, attackSpeedMultiplier) * GetAttackFrameSpeedMultiplier(attackSetIndex);
            float framesPerSecond = attackFramesPerSecond * frameSpeedMultiplier;
            float minimumDuration = duration / Mathf.Max(0.05f, frameSpeedMultiplier);
            if (UseAnimatorController())
            {
                return PlayAnimatorState(frames, framesPerSecond, minimumDuration, attackDirection, GetAttackStateHash(attackSetIndex), frameSpeedMultiplier);
            }

            if (frames == null || frames.Length == 0)
            {
                return Mathf.Max(0.01f, minimumDuration);
            }

            return PlayActionFrames(frames, framesPerSecond, minimumDuration, attackDirection);
        }

        public float PlaySpecialAttack(float duration, Vector2 attackDirection)
        {
            return PlaySpecialAttack(duration, attackDirection, 1f);
        }

        public float PlaySpecialAttack(float duration, Vector2 attackDirection, float attackSpeedMultiplier)
        {
            float frameSpeedMultiplier = Mathf.Max(0.05f, attackSpeedMultiplier);
            float framesPerSecond = specialAttackFramesPerSecond * frameSpeedMultiplier;
            float minimumDuration = duration / frameSpeedMultiplier;
            if (UseAnimatorController())
            {
                return PlayAnimatorState(specialAttackFrames, framesPerSecond, minimumDuration, attackDirection, AnimatorSuperSmashStateHash, frameSpeedMultiplier);
            }

            if (specialAttackFrames == null || specialAttackFrames.Length == 0)
            {
                return PlayAttack(duration, attackDirection, attackSpeedMultiplier);
            }

            return PlayActionFrames(specialAttackFrames, framesPerSecond, minimumDuration, attackDirection);
        }

        public void ResetAttackCombo()
        {
            nextAttackFrameSetIndex = 0;
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
            acceleration = Mathf.Max(0f, acceleration);
            deceleration = Mathf.Max(0f, deceleration);
            turnAcceleration = Mathf.Max(0f, turnAcceleration);
            inputDeadZone = Mathf.Clamp(inputDeadZone, 0f, 0.45f);
            sprintSpeedMultiplier = Mathf.Max(1f, sprintSpeedMultiplier);
            sprintStaminaPerSecond = Mathf.Max(0f, sprintStaminaPerSecond);
            sprintStartStamina = Mathf.Max(0f, sprintStartStamina);
            dodgeSpeed = Mathf.Max(0f, dodgeSpeed);
            dodgeDuration = Mathf.Max(0f, dodgeDuration);
            dodgeCooldown = Mathf.Max(0f, dodgeCooldown);
            dodgeStaminaCost = Mathf.Max(0f, dodgeStaminaCost);
            attackFramesPerSecond = Mathf.Max(1f, attackFramesPerSecond);
            dodgeFramesPerSecond = Mathf.Max(1f, dodgeFramesPerSecond);
            hurtFramesPerSecond = Mathf.Max(1f, hurtFramesPerSecond);
            specialAttackFramesPerSecond = Mathf.Max(1f, specialAttackFramesPerSecond);
            attackMoveSpeedMultiplier = Mathf.Clamp01(attackMoveSpeedMultiplier);
        }

        private Vector2 ReadMovementInput()
        {
            Vector2 input = joystick != null ? joystick.Direction : Vector2.zero;

            if (input.sqrMagnitude < 0.01f)
            {
                input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            }

            input = Vector2.ClampMagnitude(input, 1f);
            float magnitude = input.magnitude;
            if (magnitude <= inputDeadZone)
            {
                return Vector2.zero;
            }

            float scaledMagnitude = Mathf.InverseLerp(inputDeadZone, 1f, magnitude);
            return input / magnitude * scaledMagnitude;
        }

        private bool TryStartDodge()
        {
            if (Time.time < nextDodgeTime || characterStats == null || !characterStats.IsAlive || characterStats.IsStunned)
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
            dodgeStartedAt = Time.time;
            nextDodgeTime = Time.time + dodgeCooldown;
            if (UseAnimatorController())
            {
                SetAnimatorTrigger(AnimatorDashHash);
            }

            currentVelocity = dodgeDirection * dodgeSpeed;
            return true;
        }

        private bool IsDodging()
        {
            return Time.time < dodgeEndsAt;
        }

        private void UpdateSprintState(Vector2 input, float deltaTime)
        {
            bool wasSprinting = isSprinting;
            isSprinting = false;

            if (characterStats == null || !characterStats.IsAlive || characterStats.IsStunned || input.sqrMagnitude < 0.01f || !WantsSprint())
            {
                return;
            }

            if (!wasSprinting && characterStats.CurrentStamina < sprintStartStamina)
            {
                return;
            }

            isSprinting = characterStats.TrySpendStamina(sprintStaminaPerSecond * deltaTime);
        }

        private static bool WantsSprint()
        {
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }

        private void UpdateVelocity(Vector2 input, float deltaTime)
        {
            float targetSpeed = isSprinting ? moveSpeed * sprintSpeedMultiplier : moveSpeed;
            Vector2 desiredVelocity = input * targetSpeed;
            float response = desiredVelocity.sqrMagnitude > 0f ? acceleration : deceleration;

            if (desiredVelocity.sqrMagnitude > 0f && currentVelocity.sqrMagnitude > 0f)
            {
                float directionDot = Vector2.Dot(currentVelocity.normalized, desiredVelocity.normalized);
                if (directionDot < 0.45f)
                {
                    response = turnAcceleration;
                }
            }

            currentVelocity = Vector2.MoveTowards(currentVelocity, desiredVelocity, response * deltaTime);
            if (currentVelocity.magnitude < StopSpeedThreshold)
            {
                currentVelocity = Vector2.zero;
            }
        }

        private void Move(Vector2 velocity, float deltaTime)
        {
            if (rigidbody2d == null && velocity.sqrMagnitude < 0.0001f)
            {
                return;
            }

            if (velocity.sqrMagnitude < 0.0001f)
            {
                DreamyCharacterCollisionUtility.StopBodyDrift(rigidbody2d);
                return;
            }

            Vector2 current = rigidbody2d != null ? rigidbody2d.position : (Vector2)transform.position;
            Vector2 unclampedNext = current + velocity * deltaTime;
            Vector2 next = ClampToBounds(unclampedNext);

            if (!Mathf.Approximately(next.x, unclampedNext.x))
            {
                currentVelocity.x = 0f;
            }

            if (!Mathf.Approximately(next.y, unclampedNext.y))
            {
                currentVelocity.y = 0f;
            }

            if (rigidbody2d != null)
            {
                rigidbody2d.MovePosition(next);
            }
            else
            {
                transform.position = new Vector3(next.x, next.y, transform.position.z);
            }

            Vector2 facing = velocity.sqrMagnitude > 0.0001f ? velocity : movementInput;
            if (spriteRenderer != null && Mathf.Abs(facing.x) > 0.01f)
            {
                ApplyFacingFlip();
            }
        }

        private Vector2 ClampToBounds(Vector2 position)
        {
            position.x = Mathf.Clamp(position.x, minBounds.x, maxBounds.x);
            position.y = Mathf.Clamp(position.y, minBounds.y, maxBounds.y);
            return position;
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

        private void Animate(Vector2 velocity, Vector2 input)
        {
            if (UseAnimatorController())
            {
                AnimateWithAnimator(input.sqrMagnitude >= 0.0025f ? input : velocity);
                return;
            }

            if (Time.time < attackEndsAt && activeAttackFrames != null && activeAttackFrames.Length > 0)
            {
                float attackElapsed = Mathf.Max(0f, Time.time - attackStartedAt);
                int attackFrameIndex = Mathf.Min(activeAttackFrames.Length - 1, Mathf.FloorToInt(attackElapsed * Mathf.Max(1f, activeAttackFramesPerSecond)));
                ApplyFrame(activeAttackFrames, attackFrameIndex);
                return;
            }

            if (IsDodging() && dodgeFrames != null && dodgeFrames.Length > 0)
            {
                float dodgeProgress = dodgeDuration > 0f ? Mathf.Clamp01((Time.time - dodgeStartedAt) / dodgeDuration) : 1f;
                int dodgeFrameIndex = Mathf.Min(dodgeFrames.Length - 1, Mathf.FloorToInt(dodgeProgress * dodgeFrames.Length));
                ApplyFrame(dodgeFrames, dodgeFrameIndex);
                return;
            }

            if (Time.time < hurtEndsAt && hurtFrames != null && hurtFrames.Length > 0)
            {
                float hurtElapsed = Mathf.Max(0f, Time.time - hurtStartedAt);
                int hurtFrameIndex = Mathf.Min(hurtFrames.Length - 1, Mathf.FloorToInt(hurtElapsed * hurtFramesPerSecond));
                ApplyFrame(hurtFrames, hurtFrameIndex);
                return;
            }

            bool isMoving = velocity.sqrMagnitude >= 0.01f || input.sqrMagnitude >= 0.01f;
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

            float maxAnimatedSpeed = Mathf.Max(moveSpeed * sprintSpeedMultiplier, dodgeSpeed, 0.01f);
            float speedRatio = Mathf.Clamp01(velocity.magnitude / maxAnimatedSpeed);
            float framesPerSecond = isMoving ? Mathf.Lerp(walkFramesPerSecond * 0.7f, walkFramesPerSecond, speedRatio) : idleFramesPerSecond;
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

        private float PlayActionFrames(Sprite[] frames, float framesPerSecond, float minimumDuration, Vector2 actionDirection)
        {
            if (frames == null || frames.Length == 0)
            {
                return Mathf.Max(0.01f, minimumDuration);
            }

            if (actionDirection.sqrMagnitude >= 0.01f)
            {
                lastMoveDirection = actionDirection.normalized;
            }

            activeAttackFrames = frames;
            activeAttackFramesPerSecond = Mathf.Max(1f, framesPerSecond);
            float frameDuration = activeAttackFrames.Length / activeAttackFramesPerSecond;
            float duration = Mathf.Max(Mathf.Max(0.01f, minimumDuration), frameDuration);
            attackStartedAt = Time.time;
            attackEndsAt = Time.time + duration;
            hurtEndsAt = 0f;
            animationTime = 0f;
            ApplyFacingFlip();
            ApplyFrame(activeAttackFrames, 0);
            return duration;
        }

        private float PlayAnimatorState(Sprite[] frames, float framesPerSecond, float minimumDuration, Vector2 actionDirection, int stateHash, float animatorSpeedMultiplier)
        {
            if (actionDirection.sqrMagnitude >= 0.01f)
            {
                lastMoveDirection = actionDirection.normalized;
            }

            activeAttackFrames = frames;
            activeAttackFramesPerSecond = Mathf.Max(1f, framesPerSecond);
            float duration = ResolveActionDuration(frames, activeAttackFramesPerSecond, minimumDuration);
            attackStartedAt = Time.time;
            attackEndsAt = Time.time + duration;
            hurtEndsAt = 0f;
            animationTime = 0f;
            ApplyFacingFlip();
            ResetAnimatorActionTriggers();
            wasMoving = false;
            animator.SetBool(AnimatorIsMovingHash, false);
            animator.speed = Mathf.Max(0.05f, animatorSpeedMultiplier);
            animator.Play(stateHash, 0, 0f);
            return duration;
        }

        private void HandleDamaged(float amount)
        {
            if (amount <= 0f || IsAttacking || IsDodging())
            {
                return;
            }

            hurtStartedAt = Time.time;
            hurtEndsAt = Time.time + ResolveActionDuration(hurtFrames, hurtFramesPerSecond, 0.05f);
            animationTime = 0f;
            if (UseAnimatorController())
            {
                SetAnimatorTrigger(AnimatorHurtHash);
                return;
            }

            if (hurtFrames == null || hurtFrames.Length == 0)
            {
                return;
            }

            ApplyFrame(hurtFrames, 0);
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

        private static Sprite[][] BuildAttackFrameSets(Texture2D[] textures, int[] columns, int[] rows, int[] startFrames, int[] frameCounts, float pixelsPerUnit)
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
                frames = SliceFrameRange(frames, GetNonNegativeArrayValue(startFrames, i, 0), GetNonNegativeArrayValue(frameCounts, i, 0));
                if (frames.Length > 0)
                {
                    sets.Add(frames);
                }
            }

            return sets.Count > 0 ? sets.ToArray() : System.Array.Empty<Sprite[]>();
        }

        private static Sprite[] SliceFrameRange(Sprite[] frames, int startFrame, int frameCount)
        {
            if (frames == null || frames.Length == 0)
            {
                return System.Array.Empty<Sprite>();
            }

            if (startFrame <= 0)
            {
                startFrame = 0;
            }

            if (startFrame >= frames.Length)
            {
                return System.Array.Empty<Sprite>();
            }

            int count = frameCount > 0 ? Mathf.Min(frameCount, frames.Length - startFrame) : frames.Length - startFrame;
            Sprite[] slicedFrames = new Sprite[count];
            System.Array.Copy(frames, startFrame, slicedFrames, 0, slicedFrames.Length);
            return slicedFrames;
        }

        private static int GetArrayValue(int[] values, int index, int fallback)
        {
            if (values == null || index < 0 || index >= values.Length || values[index] <= 0)
            {
                return Mathf.Max(1, fallback);
            }

            return values[index];
        }

        private static int[] BuildAttackIntMetadata(int count, int[] values, int fallback, bool cycleFallbackByThree)
        {
            if (count <= 0)
            {
                return System.Array.Empty<int>();
            }

            int[] metadata = new int[count];
            for (int i = 0; i < count; i++)
            {
                if (values != null && i >= 0 && i < values.Length)
                {
                    metadata[i] = Mathf.Max(0, values[i]);
                }
                else
                {
                    metadata[i] = cycleFallbackByThree ? i % 3 : Mathf.Max(0, fallback);
                }
            }

            return metadata;
        }

        private static float[] BuildAttackFloatMetadata(int count, float[] values, float fallback)
        {
            if (count <= 0)
            {
                return System.Array.Empty<float>();
            }

            float[] metadata = new float[count];
            for (int i = 0; i < count; i++)
            {
                metadata[i] = values != null && i >= 0 && i < values.Length
                    ? Mathf.Max(0.05f, values[i])
                    : Mathf.Max(0.05f, fallback);
            }

            return metadata;
        }

        private static int GetNonNegativeArrayValue(int[] values, int index, int fallback)
        {
            if (values == null || index < 0 || index >= values.Length || values[index] < 0)
            {
                return Mathf.Max(0, fallback);
            }

            return values[index];
        }

        private Sprite[] GetNextAttackFrames()
        {
            int unusedIndex;
            return GetNextAttackFrames(out unusedIndex);
        }

        private Sprite[] GetNextAttackFrames(out int attackSetIndex)
        {
            if (attackFrameSets != null && attackFrameSets.Length > 0)
            {
                attackSetIndex = nextAttackFrameSetIndex % attackFrameSets.Length;
                Sprite[] frames = attackFrameSets[attackSetIndex];
                nextAttackFrameSetIndex = (nextAttackFrameSetIndex + 1) % attackFrameSets.Length;
                return frames;
            }

            attackSetIndex = 0;
            return attackFrames != null ? attackFrames : System.Array.Empty<Sprite>();
        }

        private void AnimateWithAnimator(Vector2 input)
        {
            if (!IsAttacking && !IsDodging() && Time.time >= hurtEndsAt)
            {
                animator.speed = 1f;
            }

            bool isMoving = !IsAttacking
                && !IsDodging()
                && Time.time >= hurtEndsAt
                && (characterStats == null || !characterStats.IsStunned)
                && input.sqrMagnitude >= 0.0025f;
            if (isMoving != wasMoving)
            {
                wasMoving = isMoving;
                animator.SetBool(AnimatorIsMovingHash, isMoving);
            }

            ApplyFacingFlip();
        }

        private bool UseAnimatorController()
        {
            return useAnimatorController
                && animator != null
                && animator.enabled
                && animator.runtimeAnimatorController != null;
        }

        private void DisableAnimatorController()
        {
            useAnimatorController = false;
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (animator == null)
            {
                return;
            }

            if (animator.runtimeAnimatorController != null)
            {
                ResetAnimatorActionTriggers();
                animator.SetBool(AnimatorIsMovingHash, false);
            }

            animator.runtimeAnimatorController = null;
            animator.enabled = false;
        }

        private void SetAnimatorTrigger(int triggerHash)
        {
            if (!UseAnimatorController())
            {
                return;
            }

            ResetAnimatorActionTriggers();
            animator.SetTrigger(triggerHash);
        }

        private void ResetAnimatorActionTriggers()
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            animator.ResetTrigger(AnimatorAttack1Hash);
            animator.ResetTrigger(AnimatorAttack2Hash);
            animator.ResetTrigger(AnimatorAttack3Hash);
            animator.ResetTrigger(AnimatorSuperSmashHash);
            animator.ResetTrigger(AnimatorDashHash);
            animator.ResetTrigger(AnimatorHurtHash);
        }

        private int GetAttackStateHash(int attackSetIndex)
        {
            switch (GetAttackAnimatorStateIndex(attackSetIndex) % 3)
            {
                case 1:
                    return AnimatorAttack2StateHash;
                case 2:
                    return AnimatorAttack3StateHash;
                default:
                    return AnimatorAttack1StateHash;
            }
        }

        private int GetAttackAnimatorStateIndex(int attackSetIndex)
        {
            if (attackAnimatorStateIndices == null || attackSetIndex < 0 || attackSetIndex >= attackAnimatorStateIndices.Length)
            {
                return attackSetIndex;
            }

            return attackAnimatorStateIndices[attackSetIndex];
        }

        private int GetAttackPartIndex(int attackSetIndex)
        {
            if (attackPartIndices == null || attackSetIndex < 0 || attackSetIndex >= attackPartIndices.Length)
            {
                return attackSetIndex % 3;
            }

            return attackPartIndices[attackSetIndex];
        }

        private float GetAttackFrameSpeedMultiplier(int attackSetIndex)
        {
            if (attackFrameSpeedMultipliers == null || attackSetIndex < 0 || attackSetIndex >= attackFrameSpeedMultipliers.Length)
            {
                return 1f;
            }

            return Mathf.Max(0.05f, attackFrameSpeedMultipliers[attackSetIndex]);
        }

        private float GetMovementSpeedMultiplier()
        {
            return characterStats != null ? characterStats.MovementSpeedMultiplier : 1f;
        }

        private static float ResolveActionDuration(Sprite[] frames, float framesPerSecond, float minimumDuration)
        {
            float duration = Mathf.Max(0.01f, minimumDuration);
            if (frames != null && frames.Length > 0)
            {
                duration = Mathf.Max(duration, frames.Length / Mathf.Max(1f, framesPerSecond));
            }

            return duration;
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
