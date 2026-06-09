using System;
using UnityEngine;

namespace Dreamy
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(DreamyCharacterStats))]
    public sealed class DreamyMonsterController : MonoBehaviour, IDreamyCombatTarget
    {
        private const float DefaultSheetPixelsPerUnit = 128f;
        private const float DefaultVisualScale = 1.45f;
        private const int RuntimeSortingOrder = 100;
        private const float RuntimeSortingUnitsPerWorldUnit = 10f;
        private static readonly Color EnemyDamagePopupColor = new Color(1f, 0.72f, 0.18f, 1f);
        private static readonly Color EnemyHitFlashColor = new Color(1f, 0.46f, 0.46f, 1f);

        [SerializeField] private DreamyMonsterDefinition monsterDefinition;
        [SerializeField] private DreamyPrototypeVisualCatalog visualCatalog;
        [SerializeField] private Transform target;
        [SerializeField] private string monsterDisplayName = "Knight";
        [SerializeField] private int monsterLevel = 1;
        [SerializeField] private float chaseSpeed = 2.15f;
        [SerializeField] private float detectionRange = 7.5f;
        [SerializeField] private float attackRange = 0.92f;
        [SerializeField] private float attackDamage = 9f;
        [SerializeField] private float attackCooldown = 1.25f;
        [SerializeField] private float attackHitMarkerNormalizedTime = 0.52f;
        [SerializeField] private float attackConeDegrees = 100f;
        [SerializeField] private float attackOriginDistance = 0.16f;
        [SerializeField] private float attackRangeGrace = 0.24f;
        [SerializeField] private float knockbackResistance = 2.5f;
        [SerializeField] private float visualScale = DefaultVisualScale;
        [SerializeField] private float knockbackDuration = 0.18f;
        [SerializeField] private float hitFlashDuration = 0.12f;
        [SerializeField] private int idleFrameCount = 8;
        [SerializeField] private int runFrameCount = 6;
        [SerializeField] private int attackFrameCount = 4;
        [SerializeField] private int hitFrameCount = 1;
        [SerializeField] private int deathFrameCount = 1;
        [SerializeField] private float idleFramesPerSecond = 6f;
        [SerializeField] private float runFramesPerSecond = 8f;
        [SerializeField] private float attackFramesPerSecond = 10f;

        public event Action<DreamyMonsterController> Died;
        public static event Action<DreamyMonsterController> AnyDied;

        private SpriteRenderer spriteRenderer;
        private Rigidbody2D body;
        private CircleCollider2D hitbox;
        private DreamyCharacterStats stats;
        private Sprite[] idleFrames;
        private Sprite[] runFrames;
        private Sprite[] attackFrames;
        private Sprite[] hitFrames;
        private Sprite[] deathFrames;
        private float animationTime;
        private float nextAttackTime;
        private float attackEndsAt;
        private float attackHitsAt;
        private float hitAnimationEndsAt;
        private float knockbackEndsAt;
        private float hitFlashEndsAt;
        private Vector2 knockbackVelocity;
        private bool attackDamageApplied;
        private bool hasDied;
        private float nextCollisionRefresh;
        private Vector2 attackDirection = Vector2.left;
        private AnimationState animationState = AnimationState.Idle;

        public bool IsAlive => stats == null || stats.IsAlive;
        public DreamyCharacterStats Stats => stats;
        public string MonsterDisplayName => string.IsNullOrEmpty(monsterDisplayName) ? gameObject.name : monsterDisplayName;
        public int MonsterLevel => Mathf.Max(1, monsterLevel);
        public bool IsTargetAlive => IsAlive;
        public Transform TargetTransform => transform;
        public Collider2D TargetCollider => hitbox != null ? hitbox : GetComponent<Collider2D>();
        public string TargetDisplayName => MonsterDisplayName;

        private enum AnimationState
        {
            Idle,
            Run,
            Attack,
            Hit,
            Dead
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            body = GetComponent<Rigidbody2D>();
            hitbox = GetComponent<CircleCollider2D>();
            stats = GetComponent<DreamyCharacterStats>();

            DreamyCharacterCollisionUtility.NormalizeTopDownBody(body);
            hitbox.radius = 0.36f;
            hitbox.isTrigger = false;
            EnsureYSort();

            if (visualCatalog == null)
            {
                visualCatalog = Resources.Load<DreamyPrototypeVisualCatalog>("DreamyPrototypeVisualCatalog");
            }

            ApplyDefinition();
            ApplyVisualPresentation();
            BuildFrames();
            ApplyFrame(idleFrames, 0);
        }

        public void ConfigureIdentity(string displayName, int level)
        {
            if (!string.IsNullOrEmpty(displayName))
            {
                monsterDisplayName = displayName;
            }

            monsterLevel = Mathf.Max(1, level);
            EnsureFloatingHealthBar();
        }

        private void OnEnable()
        {
            if (stats != null)
            {
                stats.Died += HandleDied;
            }
        }

        private void OnDisable()
        {
            if (stats != null)
            {
                stats.Died -= HandleDied;
            }
        }

        public void Configure(DreamyPrototypeVisualCatalog catalog, Transform chaseTarget)
        {
            visualCatalog = catalog != null ? catalog : visualCatalog;
            target = chaseTarget;
            RefreshCharacterCollisionIgnores(true);
            EnsureFloatingHealthBar();
            BuildFrames();
            ApplyFrame(idleFrames, 0);
        }

        public void Configure(DreamyMonsterDefinition definition, Transform chaseTarget)
        {
            monsterDefinition = definition != null ? definition : monsterDefinition;
            target = chaseTarget;
            RefreshCharacterCollisionIgnores(true);
            ApplyDefinition();
            ApplyVisualPresentation();
            BuildFrames();
            ApplyFrame(idleFrames, 0);
        }

        private void Update()
        {
            if (hasDied)
            {
                if (deathFrames != null && deathFrames.Length > 0)
                {
                    Animate(AnimationState.Dead, deathFrames, attackFramesPerSecond);
                }

                return;
            }

            if (!IsAlive)
            {
                return;
            }

            if (target == null)
            {
                DreamyMobilePlayer player = FindAnyObjectByType<DreamyMobilePlayer>();
                target = player != null ? player.transform : null;
            }

            RefreshCharacterCollisionIgnores();

            if (stats != null && stats.IsStunned)
            {
                DreamyCharacterCollisionUtility.StopBodyDrift(body);
                Animate(AnimationState.Idle, idleFrames, idleFramesPerSecond);
                RefreshHitFlash();
                return;
            }

            if (IsAttacking())
            {
                Animate(AnimationState.Attack, attackFrames, attackFramesPerSecond);
                TryApplyAttackDamage();
                RefreshHitFlash();
                return;
            }

            if (Time.time < hitAnimationEndsAt && hitFrames != null && hitFrames.Length > 0)
            {
                Animate(AnimationState.Hit, hitFrames, attackFramesPerSecond);
                RefreshHitFlash();
                return;
            }

            Vector2 toTarget = GetVectorToTarget();
            float distance = toTarget.magnitude;
            if (target != null && distance <= attackRange && Time.time >= nextAttackTime)
            {
                StartAttack();
                return;
            }

            bool shouldRun = target != null && distance <= detectionRange && distance > attackRange;
            Animate(
                shouldRun ? AnimationState.Run : AnimationState.Idle,
                shouldRun ? runFrames : idleFrames,
                shouldRun ? runFramesPerSecond : idleFramesPerSecond);
            if (spriteRenderer != null && Mathf.Abs(toTarget.x) > 0.02f)
            {
                spriteRenderer.flipX = toTarget.x < 0f;
            }

            RefreshHitFlash();
        }

        private void FixedUpdate()
        {
            if (!IsAlive)
            {
                return;
            }

            if (stats != null && stats.IsStunned)
            {
                DreamyCharacterCollisionUtility.StopBodyDrift(body);
                return;
            }

            if (IsKnockedBack())
            {
                body.MovePosition(body.position + knockbackVelocity * Time.fixedDeltaTime);
                knockbackVelocity = Vector2.Lerp(knockbackVelocity, Vector2.zero, 10f * Time.fixedDeltaTime);
                return;
            }

            if (IsAttacking() || target == null)
            {
                DreamyCharacterCollisionUtility.StopBodyDrift(body);
                return;
            }

            Vector2 toTarget = GetVectorToTarget();
            float distance = toTarget.magnitude;
            if (distance > detectionRange || distance <= attackRange)
            {
                DreamyCharacterCollisionUtility.StopBodyDrift(body);
                return;
            }

            float movementMultiplier = stats != null ? stats.MovementSpeedMultiplier : 1f;
            body.MovePosition(body.position + toTarget.normalized * (chaseSpeed * movementMultiplier * Time.fixedDeltaTime));
        }

        private void RefreshCharacterCollisionIgnores(bool force = false)
        {
            if (!force && Time.time < nextCollisionRefresh)
            {
                return;
            }

            nextCollisionRefresh = Time.time + 0.45f;
            if (target != null)
            {
                DreamyCharacterCollisionUtility.IgnoreCollisionBetween(this, target);
            }

            DreamyCharacterCollisionUtility.IgnoreCollisionWithAll<DreamyMonsterController>(this);
        }

        public void TakeDamage(float amount)
        {
            ApplyHit(amount, Vector2.zero, 0f);
        }

        public void ApplyHit(float amount, Vector2 hitSourcePosition, float knockbackForce)
        {
            ApplyHit(amount, hitSourcePosition, knockbackForce, 1f, 0f, 0f);
        }

        public void ApplyHit(
            float amount,
            Vector2 hitSourcePosition,
            float knockbackForce,
            float slowMultiplier,
            float slowDuration,
            float stunDuration)
        {
            if (stats != null)
            {
                stats.TakeDamage(amount);
            }

            if (!IsAlive)
            {
                return;
            }

            if (stats != null)
            {
                stats.ApplySlow(slowMultiplier, slowDuration);
                stats.ApplyStun(stunDuration);
            }

            ApplyKnockback(hitSourcePosition, knockbackForce);
            hitFlashEndsAt = Time.time + hitFlashDuration;
            if (hitFrames != null && hitFrames.Length > 0)
            {
                hitAnimationEndsAt = Time.time + Mathf.Max(0.1f, hitFrameCount / Mathf.Max(1f, attackFramesPerSecond));
            }
        }

        public void ReceiveCombatHit(
            float amount,
            Vector2 hitSourcePosition,
            float knockbackForce,
            float slowMultiplier,
            float slowDuration,
            float stunDuration)
        {
            ApplyHit(amount, hitSourcePosition, knockbackForce, slowMultiplier, slowDuration, stunDuration);
        }

        private void ApplyKnockback(Vector2 hitSourcePosition, float force)
        {
            if (force <= 0f || body == null)
            {
                return;
            }

            Vector2 direction = (Vector2)transform.position - hitSourcePosition;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = spriteRenderer != null && spriteRenderer.flipX ? Vector2.right : Vector2.left;
            }

            float resistanceMultiplier = 1f / (1f + Mathf.Max(0f, knockbackResistance));
            knockbackVelocity = direction.normalized * (force * resistanceMultiplier);
            knockbackEndsAt = Time.time + knockbackDuration;
            attackEndsAt = Mathf.Min(attackEndsAt, Time.time + 0.04f);
        }

        private void StartAttack()
        {
            attackDamageApplied = false;
            attackDirection = GetVectorToTarget();
            if (attackDirection.sqrMagnitude < 0.01f)
            {
                attackDirection = spriteRenderer != null && spriteRenderer.flipX ? Vector2.left : Vector2.right;
            }

            attackDirection.Normalize();
            if (spriteRenderer != null && Mathf.Abs(attackDirection.x) > 0.02f)
            {
                spriteRenderer.flipX = attackDirection.x < 0f;
            }

            float attackSpeedMultiplier = stats != null ? stats.AttackSpeedMultiplier : 1f;
            float duration = Mathf.Max(0.2f / attackSpeedMultiplier, attackFrameCount / Mathf.Max(1f, attackFramesPerSecond * attackSpeedMultiplier));
            attackEndsAt = Time.time + duration;
            attackHitsAt = Time.time + duration * Mathf.Clamp01(attackHitMarkerNormalizedTime);
            nextAttackTime = Time.time + attackCooldown / attackSpeedMultiplier;
            animationTime = 0f;
        }

        private void TryApplyAttackDamage()
        {
            if (attackDamageApplied || Time.time < attackHitsAt || target == null)
            {
                return;
            }

            attackDamageApplied = true;
            if (!IsTargetInsideAttackArc())
            {
                return;
            }

            DreamyCharacterStats targetStats = target.GetComponentInParent<DreamyCharacterStats>();
            if (targetStats != null)
            {
                float resolvedDamage = stats != null
                    ? stats.ResolveOutgoingDamage(attackDamage, 1f, out _)
                    : attackDamage;
                targetStats.TakeDamage(resolvedDamage);
            }
        }

        public void DreamyAnimationHitMarker()
        {
            TryApplyAttackDamage();
        }

        private bool IsAttacking()
        {
            return Time.time < attackEndsAt;
        }

        private bool IsTargetInsideAttackArc()
        {
            if (target == null)
            {
                return false;
            }

            Vector2 normalizedDirection = attackDirection.sqrMagnitude >= 0.01f
                ? attackDirection.normalized
                : GetVectorToTarget().normalized;
            if (normalizedDirection.sqrMagnitude < 0.01f)
            {
                normalizedDirection = spriteRenderer != null && spriteRenderer.flipX ? Vector2.left : Vector2.right;
            }

            Vector2 origin = (Vector2)transform.position + normalizedDirection * attackOriginDistance;
            Vector2 toTarget = (Vector2)target.position - origin;
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                return true;
            }

            float range = attackRange + attackRangeGrace;
            if (toTarget.sqrMagnitude > range * range)
            {
                return false;
            }

            float halfCone = Mathf.Clamp(attackConeDegrees, 1f, 360f) * 0.5f;
            float minimumDot = Mathf.Cos(halfCone * Mathf.Deg2Rad);
            return Vector2.Dot(normalizedDirection, toTarget.normalized) >= minimumDot;
        }

        private Vector2 GetVectorToTarget()
        {
            return target != null ? (Vector2)(target.position - transform.position) : Vector2.zero;
        }

        private bool IsKnockedBack()
        {
            return Time.time < knockbackEndsAt && knockbackVelocity.sqrMagnitude > 0.0001f;
        }

        private void Animate(AnimationState state, Sprite[] frames, float framesPerSecond)
        {
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            if (animationState != state)
            {
                animationState = state;
                animationTime = 0f;
            }

            animationTime += Time.deltaTime * framesPerSecond;
            int frameIndex = Mathf.FloorToInt(animationTime) % frames.Length;
            ApplyFrame(frames, frameIndex);
        }

        private void RefreshHitFlash()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.color = Time.time <= hitFlashEndsAt
                ? EnemyHitFlashColor
                : Color.white;
        }

        private void EnsureDamageFeedback()
        {
            DreamyCharacterHitFeedback feedback = GetComponent<DreamyCharacterHitFeedback>();
            if (feedback == null)
            {
                feedback = gameObject.AddComponent<DreamyCharacterHitFeedback>();
            }

            feedback.Configure(
                true,
                false,
                EnemyDamagePopupColor,
                EnemyHitFlashColor,
                GetDamagePopupOffset(),
                0f,
                0f);
        }

        private void EnsureFloatingHealthBar()
        {
            DreamyFloatingHealthBar healthBar = GetComponent<DreamyFloatingHealthBar>();
            if (healthBar == null)
            {
                healthBar = gameObject.AddComponent<DreamyFloatingHealthBar>();
            }

            healthBar.Configure(
                MonsterDisplayName,
                MonsterLevel,
                GetHealthBarOffset(),
                visualCatalog != null ? visualCatalog.UiBarBaseSprite : null,
                visualCatalog != null ? visualCatalog.UiBarFillSprite : null);
        }

        private void ApplyFrame(Sprite[] frames, int index)
        {
            if (spriteRenderer == null || frames == null || frames.Length == 0)
            {
                return;
            }

            spriteRenderer.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
        }

        private void BuildFrames()
        {
            if (monsterDefinition != null && monsterDefinition.HasIdleAnimation)
            {
                idleFrames = BuildFrames(monsterDefinition.IdleSheet, monsterDefinition.IdleFrameCount, monsterDefinition.PixelsPerUnit);
                runFrames = BuildFrames(monsterDefinition.MoveSheet, monsterDefinition.MoveFrameCount, monsterDefinition.PixelsPerUnit);
                attackFrames = BuildFrames(monsterDefinition.AttackSheet, monsterDefinition.AttackFrameCount, monsterDefinition.PixelsPerUnit);
                hitFrames = BuildOptionalFrames(monsterDefinition.HitSheet, monsterDefinition.HitFrameCount, monsterDefinition.PixelsPerUnit);
                deathFrames = BuildOptionalFrames(monsterDefinition.DeathSheet, monsterDefinition.DeathFrameCount, monsterDefinition.PixelsPerUnit);
                return;
            }

            idleFrames = BuildFrames(visualCatalog != null ? visualCatalog.EnemyIdleSheet : null, idleFrameCount, DefaultSheetPixelsPerUnit);
            runFrames = BuildFrames(visualCatalog != null ? visualCatalog.EnemyRunSheet : null, runFrameCount, DefaultSheetPixelsPerUnit);
            attackFrames = BuildFrames(visualCatalog != null ? visualCatalog.EnemyAttackSheet : null, attackFrameCount, DefaultSheetPixelsPerUnit);
            hitFrames = Array.Empty<Sprite>();
            deathFrames = Array.Empty<Sprite>();
        }

        private Sprite[] BuildOptionalFrames(Texture2D texture, int frameCount, float pixelsPerUnit)
        {
            return texture == null ? Array.Empty<Sprite>() : BuildFrames(texture, frameCount, pixelsPerUnit);
        }

        private Sprite[] BuildFrames(Texture2D texture, int frameCount, float pixelsPerUnit)
        {
            if (texture == null || frameCount <= 0)
            {
                return new[] { CreateFallbackSprite() };
            }

            PreparePixelArtTexture(texture);
            int width = Mathf.Max(1, texture.width / frameCount);
            int height = texture.height;
            Sprite[] frames = new Sprite[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                Rect rect = new Rect(i * width, 0f, width, height);
                frames[i] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.28f), Mathf.Max(1f, pixelsPerUnit), 0, SpriteMeshType.FullRect);
            }

            return frames;
        }

        private void ApplyDefinition()
        {
            if (monsterDefinition == null)
            {
                return;
            }

            monsterDisplayName = monsterDefinition.DisplayName;
            monsterLevel = monsterDefinition.Level;
            chaseSpeed = monsterDefinition.ChaseSpeed;
            detectionRange = monsterDefinition.DetectionRange;
            attackRange = monsterDefinition.AttackRange;
            attackDamage = monsterDefinition.Damage;
            knockbackResistance = monsterDefinition.KnockbackResistance;
            visualScale = monsterDefinition.VisualScale;
            idleFrameCount = monsterDefinition.IdleFrameCount;
            runFrameCount = monsterDefinition.MoveFrameCount;
            attackFrameCount = monsterDefinition.AttackFrameCount;
            hitFrameCount = monsterDefinition.HitFrameCount;
            deathFrameCount = monsterDefinition.DeathFrameCount;
            idleFramesPerSecond = monsterDefinition.IdleFramesPerSecond;
            runFramesPerSecond = monsterDefinition.MoveFramesPerSecond;
            attackFramesPerSecond = monsterDefinition.AttackFramesPerSecond;

            if (stats != null)
            {
                stats.MaxHealth = monsterDefinition.MaxHealth;
                stats.CurrentHealth = monsterDefinition.MaxHealth;
                stats.Damage = monsterDefinition.Damage;
            }

            EnsureFloatingHealthBar();
        }

        private void ApplyVisualPresentation()
        {
            visualScale = Mathf.Clamp(visualScale <= 0f ? DefaultVisualScale : visualScale, 0.35f, 3.5f);
            transform.localScale = Vector3.one * visualScale;
            EnsureDamageFeedback();
            EnsureFloatingHealthBar();
            EnsureYSort();
        }

        private void EnsureYSort()
        {
            DreamyYSortSprite ySort = GetComponent<DreamyYSortSprite>();
            if (ySort == null)
            {
                ySort = gameObject.AddComponent<DreamyYSortSprite>();
            }

            ySort.Configure(RuntimeSortingOrder, RuntimeSortingUnitsPerWorldUnit);
        }

        private Vector2 GetDamagePopupOffset()
        {
            return new Vector2(0f, 0.92f * Mathf.Max(0.35f, visualScale) + 0.28f);
        }

        private Vector2 GetHealthBarOffset()
        {
            float scale = Mathf.Max(0.35f, visualScale);
            return new Vector2(0f, 0.82f + 0.32f / scale);
        }

        private static void PreparePixelArtTexture(Texture2D texture)
        {
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.anisoLevel = 0;
        }

        private static Sprite CreateFallbackSprite()
        {
            Texture2D texture = new Texture2D(12, 12);
            Color[] pixels = new Color[144];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(0.85f, 0.18f, 0.18f, 1f);
            }

            texture.SetPixels(pixels);
            texture.filterMode = FilterMode.Point;
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 12f, 12f), new Vector2(0.5f, 0.5f), 24f);
        }

        private void HandleDied()
        {
            if (hasDied)
            {
                return;
            }

            hasDied = true;
            animationState = AnimationState.Idle;
            animationTime = 0f;
            if (hitbox != null)
            {
                hitbox.enabled = false;
            }

            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }

            Died?.Invoke(this);
            AnyDied?.Invoke(this);
            float destroyDelay = deathFrames != null && deathFrames.Length > 0
                ? Mathf.Max(0.35f, deathFrames.Length / Mathf.Max(1f, attackFramesPerSecond))
                : 0.35f;
            Destroy(gameObject, destroyDelay);
        }

        private void OnValidate()
        {
            chaseSpeed = Mathf.Max(0f, chaseSpeed);
            detectionRange = Mathf.Max(0.1f, detectionRange);
            attackRange = Mathf.Max(0.1f, attackRange);
            attackDamage = Mathf.Max(0f, attackDamage);
            attackCooldown = Mathf.Max(0.05f, attackCooldown);
            attackHitMarkerNormalizedTime = Mathf.Clamp01(attackHitMarkerNormalizedTime);
            attackConeDegrees = Mathf.Clamp(attackConeDegrees, 1f, 360f);
            attackOriginDistance = Mathf.Max(0f, attackOriginDistance);
            attackRangeGrace = Mathf.Max(0f, attackRangeGrace);
            knockbackResistance = Mathf.Max(0f, knockbackResistance);
            visualScale = Mathf.Clamp(visualScale <= 0f ? DefaultVisualScale : visualScale, 0.35f, 3.5f);
            knockbackDuration = Mathf.Max(0.02f, knockbackDuration);
            hitFlashDuration = Mathf.Max(0f, hitFlashDuration);
            monsterLevel = Mathf.Max(1, monsterLevel);
            idleFrameCount = Mathf.Max(1, idleFrameCount);
            runFrameCount = Mathf.Max(1, runFrameCount);
            attackFrameCount = Mathf.Max(1, attackFrameCount);
            hitFrameCount = Mathf.Max(1, hitFrameCount);
            deathFrameCount = Mathf.Max(1, deathFrameCount);
            idleFramesPerSecond = Mathf.Max(0.1f, idleFramesPerSecond);
            runFramesPerSecond = Mathf.Max(0.1f, runFramesPerSecond);
            attackFramesPerSecond = Mathf.Max(0.1f, attackFramesPerSecond);
        }

        private void OnDrawGizmosSelected()
        {
            Vector2 direction = Application.isPlaying && attackDirection.sqrMagnitude >= 0.01f
                ? attackDirection
                : ResolveGizmoDirection();
            DrawAttackArc(direction, attackRange + attackRangeGrace, attackConeDegrees, new Color(1f, 0.35f, 0.24f, 0.62f));
        }

        private Vector2 ResolveGizmoDirection()
        {
            if (target != null)
            {
                Vector2 toTarget = (Vector2)(target.position - transform.position);
                if (toTarget.sqrMagnitude >= 0.01f)
                {
                    return toTarget.normalized;
                }
            }

            return spriteRenderer != null && spriteRenderer.flipX ? Vector2.left : Vector2.right;
        }

        private void DrawAttackArc(Vector2 direction, float range, float degrees, Color color)
        {
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = Vector2.right;
            }

            Vector3 origin = transform.position + (Vector3)(direction.normalized * attackOriginDistance);
            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float halfCone = Mathf.Clamp(degrees, 1f, 360f) * 0.5f;
            int segments = 18;
            Gizmos.color = color;
            Vector3 previousPoint = origin;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (baseAngle - halfCone + degrees * i / segments) * Mathf.Deg2Rad;
                Vector3 point = origin + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * range;
                if (i > 0)
                {
                    Gizmos.DrawLine(previousPoint, point);
                }

                previousPoint = point;
            }

            float leftAngle = (baseAngle - halfCone) * Mathf.Deg2Rad;
            float rightAngle = (baseAngle + halfCone) * Mathf.Deg2Rad;
            Gizmos.DrawLine(origin, origin + new Vector3(Mathf.Cos(leftAngle), Mathf.Sin(leftAngle), 0f) * range);
            Gizmos.DrawLine(origin, origin + new Vector3(Mathf.Cos(rightAngle), Mathf.Sin(rightAngle), 0f) * range);
        }
    }
}
