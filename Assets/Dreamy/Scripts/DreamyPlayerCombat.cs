using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamy
{
    [RequireComponent(typeof(DreamyCharacterStats))]
    public sealed class DreamyPlayerCombat : MonoBehaviour
    {
        [SerializeField] private float attackDamage = 18f;
        [SerializeField] private float attackRange = 1.15f;
        [SerializeField] private float attackCooldown = 0.48f;
        [SerializeField] private float attackWindup = 0.16f;
        [SerializeField] private float attackHitMarkerNormalizedTime = 0.45f;
        [SerializeField] private float attack2HitMarkerNormalizedTime = 0.22f;
        [SerializeField] private float attack3HitMarkerNormalizedTime = 0.18f;
        [SerializeField] private float attackAnimationDuration = 0.38f;
        [SerializeField] private float attack2DamageMultiplier = 1.15f;
        [SerializeField] private float attack3DamageMultiplier = 2.25f;
        [SerializeField] private float attack3SlowMultiplier = 0.5f;
        [SerializeField] private float attack3SlowDuration = 1.65f;
        [SerializeField] private float attack3StunDuration;
        [SerializeField] private float staminaCost = 8f;
        [SerializeField] private float knockbackForce = 5.2f;
        [SerializeField] private float comboResetDelay = 0.4f;
        [SerializeField] private float comboInputBufferWindow = 0.2f;
        [SerializeField] private float attackOriginDistance = 0.28f;
        [SerializeField] private float attackHitboxWidth = 0.92f;
        [SerializeField] private float attack2HitboxLengthMultiplier = 1.08f;
        [SerializeField] private float attack2HitboxWidthMultiplier = 1.08f;
        [SerializeField] private float attack3HitboxLengthMultiplier = 1.24f;
        [SerializeField] private float attack3HitboxWidthMultiplier = 1.22f;
        [SerializeField] private float specialDamageMultiplier = 2.4f;
        [SerializeField] private float specialAttackRange = 1.75f;
        [SerializeField] private float specialAttackHitboxWidth = 1.35f;
        [SerializeField] private float specialAttackCooldown = 4.5f;
        [SerializeField] private float specialAttackWindup = 0.34f;
        [SerializeField] private float specialHitMarkerNormalizedTime = 0.52f;
        [SerializeField] private float specialAttackAnimationDuration = 0.85f;
        [SerializeField] private float specialStaminaCost = 30f;
        [SerializeField] private float specialKnockbackForce = 8.5f;
        [SerializeField] private DreamyCombatTuningProfile tuningProfile;
        [SerializeField] private bool loadDefaultTuningProfile = true;
        [SerializeField] private bool showCombatTuningGizmos = true;

        public event Action<IDreamyCombatTarget> AttackHit;
        public event Action AttackMissed;

        private DreamyCharacterStats stats;
        private DreamyMobilePlayer player;
        private float nextAttackTime;
        private float nextSpecialAttackTime;
        private float pendingAttackHitsAt;
        private float currentAttackEndsAt;
        private float lastAttackEndedAt = -999f;
        private Vector2 pendingAttackDirection = Vector2.down;
        private float pendingAttackDamage;
        private float pendingAttackRange;
        private float pendingAttackHitboxWidth;
        private float pendingAttackOriginDistance;
        private float pendingAttackKnockbackForce;
        private float pendingAttackSlowMultiplier = 1f;
        private float pendingAttackSlowDuration;
        private float pendingAttackStunDuration;
        private bool pendingAttackIsSpecial;
        private bool bufferedAttack;
        private bool pendingAttackHit;
        private readonly List<PendingCombatVfxEvent> pendingCombatVfxEvents = new List<PendingCombatVfxEvent>();

        private void Awake()
        {
            stats = GetComponent<DreamyCharacterStats>();
            player = GetComponent<DreamyMobilePlayer>();
            if (loadDefaultTuningProfile && tuningProfile == null)
            {
                tuningProfile = DreamyCombatTuningProfile.LoadDefault();
            }

            ApplyTuningProfile(tuningProfile);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.J))
            {
                QueueAttack();
            }

            if (Input.GetKeyDown(KeyCode.K))
            {
                QueueSpecialSkill();
            }

            if (pendingAttackHit && Time.time >= pendingAttackHitsAt)
            {
                ResolvePendingAttack();
            }

            ResolvePendingCombatVfxEvents();

            if (bufferedAttack && CanStartBufferedAttack())
            {
                bufferedAttack = false;
                TryAttack();
            }
        }

        public void ApplyTuningProfile(DreamyCombatTuningProfile profile)
        {
            tuningProfile = profile;
            if (tuningProfile == null)
            {
                return;
            }

            tuningProfile.EnsureDefaults();
            attackDamage = tuningProfile.BaseDamage;
            attackAnimationDuration = tuningProfile.NormalAttackMinimumDuration;
            staminaCost = tuningProfile.NormalStaminaCost;
            comboResetDelay = tuningProfile.ComboResetDelay;
            comboInputBufferWindow = tuningProfile.ComboInputBufferWindow;
            specialAttackAnimationDuration = tuningProfile.SpecialAttackMinimumDuration;
            specialStaminaCost = tuningProfile.SpecialStaminaCost;
            specialAttackCooldown = tuningProfile.SpecialCooldown;

            DreamyCombatActionTuning attack1 = tuningProfile.GetNormalAttack(0);
            DreamyCombatActionTuning attack2 = tuningProfile.GetNormalAttack(1);
            DreamyCombatActionTuning attack3 = tuningProfile.GetNormalAttack(2);
            attackHitMarkerNormalizedTime = attack1.HitMarkerNormalizedTime;
            attack2HitMarkerNormalizedTime = attack2.HitMarkerNormalizedTime;
            attack3HitMarkerNormalizedTime = attack3.HitMarkerNormalizedTime;
            attackRange = attack1.HitboxLength;
            attackHitboxWidth = attack1.HitboxWidth;
            attackOriginDistance = attack1.OriginDistance;
            attack2DamageMultiplier = attack2.DamageMultiplier;
            attack3DamageMultiplier = attack3.DamageMultiplier;
            attack2HitboxLengthMultiplier = attackRange > 0f ? attack2.HitboxLength / attackRange : 1f;
            attack2HitboxWidthMultiplier = attackHitboxWidth > 0f ? attack2.HitboxWidth / attackHitboxWidth : 1f;
            attack3HitboxLengthMultiplier = attackRange > 0f ? attack3.HitboxLength / attackRange : 1f;
            attack3HitboxWidthMultiplier = attackHitboxWidth > 0f ? attack3.HitboxWidth / attackHitboxWidth : 1f;
            knockbackForce = attack1.KnockbackForce;
            attack3SlowMultiplier = attack3.SlowMultiplier;
            attack3SlowDuration = attack3.SlowDuration;
            attack3StunDuration = attack3.StunDuration;

            DreamyCombatActionTuning special = tuningProfile.SpecialAttack;
            specialDamageMultiplier = special.DamageMultiplier;
            specialAttackRange = special.HitboxLength;
            specialAttackHitboxWidth = special.HitboxWidth;
            specialAttackWindup = special.OriginDistance;
            specialHitMarkerNormalizedTime = special.HitMarkerNormalizedTime;
            specialKnockbackForce = special.KnockbackForce;
        }

        public void QueueAttack()
        {
            if (TryAttack())
            {
                return;
            }

            if (CanBufferAttack())
            {
                bufferedAttack = true;
            }
        }

        public bool TryAttack()
        {
            if (!CanStartAttackNow() || stats == null || !stats.TrySpendStamina(staminaCost))
            {
                return false;
            }

            if (Time.time > lastAttackEndedAt + comboResetDelay && player != null)
            {
                player.ResetAttackCombo();
            }

            pendingCombatVfxEvents.Clear();
            float attackSpeedMultiplier = ResolveAttackSpeedMultiplier();
            Vector2 attackDirection = ResolveAttackDirection();
            float animationDuration = Mathf.Max(0.05f, attackAnimationDuration);
            if (player != null)
            {
                animationDuration = player.PlayAttack(animationDuration, attackDirection, attackSpeedMultiplier);
            }

            int attackPartIndex = player != null ? player.LastAttackPartIndex : 0;
            DreamyCombatActionTuning attackTuning = GetNormalAttackTuning(attackPartIndex);
            currentAttackEndsAt = Time.time + animationDuration;
            nextAttackTime = currentAttackEndsAt;
            pendingAttackHitsAt = Time.time + ResolveHitDelay(animationDuration, attackWindup, GetAttackPartHitMarkerNormalizedTime(attackPartIndex));
            pendingAttackDirection = attackDirection;
            pendingAttackDamage = ResolvePlayerDamage(GetAttackPartDamageMultiplier(attackPartIndex));
            pendingAttackRange = GetAttackPartHitboxLength(attackPartIndex);
            pendingAttackHitboxWidth = GetAttackPartHitboxWidth(attackPartIndex);
            pendingAttackOriginDistance = GetAttackPartOriginDistance(attackPartIndex);
            pendingAttackKnockbackForce = GetAttackPartKnockbackForce(attackPartIndex);
            pendingAttackSlowMultiplier = attackTuning != null ? attackTuning.SlowMultiplier : attackPartIndex == 2 ? attack3SlowMultiplier : 1f;
            pendingAttackSlowDuration = attackTuning != null ? attackTuning.SlowDuration : attackPartIndex == 2 ? attack3SlowDuration : 0f;
            pendingAttackStunDuration = attackTuning != null ? attackTuning.StunDuration : attackPartIndex == 2 ? attack3StunDuration : 0f;
            pendingAttackIsSpecial = false;
            pendingAttackHit = true;
            ScheduleCombatVfxEvents(attackTuning, animationDuration, attackDirection);

            return true;
        }

        public void QueueSpecialSkill()
        {
            TrySpecialSkill();
        }

        public bool TrySpecialSkill()
        {
            if (!CanStartSpecialSkillNow() || stats == null || !stats.TrySpendStamina(specialStaminaCost))
            {
                return false;
            }

            pendingCombatVfxEvents.Clear();
            if (player != null)
            {
                player.ResetAttackCombo();
            }

            DreamyCombatActionTuning specialTuning = GetSpecialAttackTuning();
            float attackSpeedMultiplier = ResolveAttackSpeedMultiplier();
            Vector2 attackDirection = ResolveAttackDirection();
            float animationDuration = Mathf.Max(0.05f, specialAttackAnimationDuration);
            if (player != null)
            {
                animationDuration = player.PlaySpecialAttack(animationDuration, attackDirection, attackSpeedMultiplier);
            }

            nextAttackTime = Time.time + animationDuration;
            nextSpecialAttackTime = Time.time + Mathf.Max(specialAttackCooldown, animationDuration);
            currentAttackEndsAt = Time.time + animationDuration;
            pendingAttackHitsAt = Time.time + ResolveHitDelay(animationDuration, specialAttackWindup, specialHitMarkerNormalizedTime);
            pendingAttackDirection = attackDirection;
            pendingAttackDamage = ResolvePlayerDamage(specialDamageMultiplier);
            pendingAttackRange = specialAttackRange;
            pendingAttackHitboxWidth = specialAttackHitboxWidth;
            pendingAttackOriginDistance = specialTuning != null ? specialTuning.OriginDistance : specialAttackWindup;
            pendingAttackKnockbackForce = specialKnockbackForce;
            pendingAttackSlowMultiplier = specialTuning != null ? specialTuning.SlowMultiplier : 1f;
            pendingAttackSlowDuration = specialTuning != null ? specialTuning.SlowDuration : 0f;
            pendingAttackStunDuration = specialTuning != null ? specialTuning.StunDuration : 0f;
            pendingAttackIsSpecial = true;
            pendingAttackHit = true;
            bufferedAttack = false;
            ScheduleCombatVfxEvents(specialTuning, animationDuration, attackDirection);
            return true;
        }

        public void DreamyAnimationHitMarker()
        {
            if (pendingAttackHit)
            {
                ResolvePendingAttack();
            }
        }

        private void ResolvePendingAttack()
        {
            pendingAttackHit = false;
            lastAttackEndedAt = Mathf.Max(lastAttackEndedAt, currentAttackEndsAt);

            List<IDreamyCombatTarget> targets = FindTargetsInSlashHitbox(
                pendingAttackDirection,
                pendingAttackRange,
                pendingAttackHitboxWidth,
                pendingAttackOriginDistance);
            if (targets.Count == 0)
            {
                AttackMissed?.Invoke();
                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                ApplyCombatHit(targets[i]);
                AttackHit?.Invoke(targets[i]);
            }
        }

        private bool CanStartAttackNow()
        {
            return !pendingAttackHit
                && Time.time >= currentAttackEndsAt
                && Time.time >= nextAttackTime
                && stats != null
                && stats.IsAlive
                && !stats.IsStunned;
        }

        private bool CanBufferAttack()
        {
            return IsInsideComboInputWindow();
        }

        private bool IsInsideComboInputWindow()
        {
            return stats != null
                && stats.IsAlive
                && !stats.IsStunned
                && !pendingAttackIsSpecial
                && Time.time < currentAttackEndsAt
                && Time.time >= currentAttackEndsAt - comboInputBufferWindow;
        }

        private bool CanStartBufferedAttack()
        {
            return stats != null
                && stats.IsAlive
                && !stats.IsStunned
                && !pendingAttackHit
                && Time.time >= currentAttackEndsAt
                && Time.time >= nextAttackTime;
        }

        private bool CanStartSpecialSkillNow()
        {
            return !pendingAttackHit
                && Time.time >= currentAttackEndsAt
                && Time.time >= nextAttackTime
                && Time.time >= nextSpecialAttackTime
                && stats != null
                && stats.IsAlive
                && !stats.IsStunned;
        }

        private void ApplyCombatHit(IDreamyCombatTarget target)
        {
            if (target == null)
            {
                return;
            }

            target.ReceiveCombatHit(
                pendingAttackDamage,
                transform.position,
                pendingAttackKnockbackForce,
                pendingAttackSlowMultiplier,
                pendingAttackSlowDuration,
                pendingAttackStunDuration);
        }

        private float ResolveAttackSpeedMultiplier()
        {
            return stats != null ? stats.AttackSpeedMultiplier : 1f;
        }

        private float ResolvePlayerDamage(float actionMultiplier)
        {
            float baseDamage = stats != null ? Mathf.Max(attackDamage, stats.Damage) : attackDamage;
            float resolvedDamage = baseDamage * Mathf.Max(0f, actionMultiplier);
            if (stats != null)
            {
                resolvedDamage *= stats.DamageMultiplier;
                if (resolvedDamage > 0f && UnityEngine.Random.value < stats.CriticalChance)
                {
                    resolvedDamage *= stats.CriticalDamageMultiplier;
                }
            }

            return resolvedDamage;
        }

        private float GetAttackPartDamageMultiplier(int attackPartIndex)
        {
            DreamyCombatActionTuning tuning = GetNormalAttackTuning(attackPartIndex);
            if (tuning != null)
            {
                return tuning.DamageMultiplier;
            }

            switch (attackPartIndex)
            {
                case 1:
                    return attack2DamageMultiplier;
                case 2:
                    return attack3DamageMultiplier;
                default:
                    return 1f;
            }
        }

        private float GetAttackPartLengthMultiplier(int attackPartIndex)
        {
            if (tuningProfile != null)
            {
                return attackRange > 0f ? GetAttackPartHitboxLength(attackPartIndex) / attackRange : 1f;
            }

            switch (attackPartIndex)
            {
                case 1:
                    return attack2HitboxLengthMultiplier;
                case 2:
                    return attack3HitboxLengthMultiplier;
                default:
                    return 1f;
            }
        }

        private float GetAttackPartWidthMultiplier(int attackPartIndex)
        {
            if (tuningProfile != null)
            {
                return attackHitboxWidth > 0f ? GetAttackPartHitboxWidth(attackPartIndex) / attackHitboxWidth : 1f;
            }

            switch (attackPartIndex)
            {
                case 1:
                    return attack2HitboxWidthMultiplier;
                case 2:
                    return attack3HitboxWidthMultiplier;
                default:
                    return 1f;
            }
        }

        private float GetAttackPartHitMarkerNormalizedTime(int attackPartIndex)
        {
            DreamyCombatActionTuning tuning = GetNormalAttackTuning(attackPartIndex);
            if (tuning != null)
            {
                return tuning.HitMarkerNormalizedTime;
            }

            switch (attackPartIndex)
            {
                case 1:
                    return attack2HitMarkerNormalizedTime;
                case 2:
                    return attack3HitMarkerNormalizedTime;
                default:
                    return attackHitMarkerNormalizedTime;
            }
        }

        private float GetAttackPartHitboxLength(int attackPartIndex)
        {
            DreamyCombatActionTuning tuning = GetNormalAttackTuning(attackPartIndex);
            if (tuning != null)
            {
                return tuning.HitboxLength;
            }

            return attackRange * GetAttackPartLengthMultiplier(attackPartIndex);
        }

        private float GetAttackPartHitboxWidth(int attackPartIndex)
        {
            DreamyCombatActionTuning tuning = GetNormalAttackTuning(attackPartIndex);
            if (tuning != null)
            {
                return tuning.HitboxWidth;
            }

            return attackHitboxWidth * GetAttackPartWidthMultiplier(attackPartIndex);
        }

        private float GetAttackPartOriginDistance(int attackPartIndex)
        {
            DreamyCombatActionTuning tuning = GetNormalAttackTuning(attackPartIndex);
            return tuning != null ? tuning.OriginDistance : attackOriginDistance;
        }

        private float GetAttackPartKnockbackForce(int attackPartIndex)
        {
            DreamyCombatActionTuning tuning = GetNormalAttackTuning(attackPartIndex);
            return tuning != null ? tuning.KnockbackForce : knockbackForce;
        }

        private DreamyCombatActionTuning GetNormalAttackTuning(int attackPartIndex)
        {
            if (tuningProfile == null)
            {
                return null;
            }

            tuningProfile.EnsureDefaults();
            return tuningProfile.GetNormalAttack(attackPartIndex);
        }

        private DreamyCombatActionTuning GetSpecialAttackTuning()
        {
            if (tuningProfile == null)
            {
                return null;
            }

            tuningProfile.EnsureDefaults();
            return tuningProfile.SpecialAttack;
        }

        private static float ResolveHitDelay(float animationDuration, float fallbackWindup, float normalizedMarkerTime)
        {
            if (normalizedMarkerTime > 0f)
            {
                return Mathf.Clamp01(normalizedMarkerTime) * Mathf.Max(0.01f, animationDuration);
            }

            return Mathf.Min(Mathf.Max(0f, fallbackWindup), Mathf.Max(0.01f, animationDuration) * 0.95f);
        }

        private List<IDreamyCombatTarget> FindTargetsInSlashHitbox(Vector2 attackDirection, float length, float width, float originDistance)
        {
            if (attackDirection.sqrMagnitude < 0.01f)
            {
                attackDirection = Vector2.down;
            }

            Vector2 normalizedDirection = attackDirection.normalized;
            Vector2 origin = (Vector2)transform.position + normalizedDirection * Mathf.Max(0f, originDistance);
            Vector2 perpendicular = new Vector2(-normalizedDirection.y, normalizedDirection.x);
            float hitboxLength = Mathf.Max(0.05f, length);
            float halfWidth = Mathf.Max(0.05f, width) * 0.5f;
            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude);
            List<IDreamyCombatTarget> targets = new List<IDreamyCombatTarget>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] == null || behaviours[i] is not IDreamyCombatTarget target || targets.Contains(target))
                {
                    continue;
                }

                Transform targetTransform = target.TargetTransform;
                if (targetTransform == null || targetTransform == transform || !target.IsTargetAlive)
                {
                    continue;
                }

                Vector2 toTarget = (Vector2)targetTransform.position - origin;
                float targetRadius = ResolveTargetRadius(target);
                float forwardDistance = Vector2.Dot(toTarget, normalizedDirection);
                float sideDistance = Mathf.Abs(Vector2.Dot(toTarget, perpendicular));
                if (forwardDistance >= -targetRadius
                    && forwardDistance <= hitboxLength + targetRadius
                    && sideDistance <= halfWidth + targetRadius)
                {
                    targets.Add(target);
                }
            }

            return targets;
        }

        private static float ResolveTargetRadius(IDreamyCombatTarget target)
        {
            if (target == null)
            {
                return 0f;
            }

            Collider2D targetCollider = target.TargetCollider;
            if (targetCollider == null)
            {
                return 0.18f;
            }

            Vector3 extents = targetCollider.bounds.extents;
            return Mathf.Max(0.05f, Mathf.Max(extents.x, extents.y));
        }

        private Vector2 ResolveAttackDirection()
        {
            if (player != null)
            {
                return player.FacingDirection;
            }

            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                return renderer.flipX ? Vector2.left : Vector2.right;
            }

            return Vector2.down;
        }

        private void ScheduleCombatVfxEvents(DreamyCombatActionTuning tuning, float animationDuration, Vector2 attackDirection)
        {
            if (tuning == null)
            {
                return;
            }

            DreamyCombatEventTuning[] events = tuning.Events;
            for (int i = 0; i < events.Length; i++)
            {
                DreamyCombatEventTuning combatEvent = events[i];
                if (combatEvent == null || !combatEvent.Enabled || combatEvent.VfxKind == DreamyCombatVfxKind.None)
                {
                    continue;
                }

                pendingCombatVfxEvents.Add(new PendingCombatVfxEvent(
                    Time.time + ResolveHitDelay(animationDuration, 0f, combatEvent.NormalizedTime),
                    combatEvent,
                    attackDirection));
            }
        }

        private void ResolvePendingCombatVfxEvents()
        {
            if (pendingCombatVfxEvents.Count == 0)
            {
                return;
            }

            for (int i = pendingCombatVfxEvents.Count - 1; i >= 0; i--)
            {
                PendingCombatVfxEvent pendingEvent = pendingCombatVfxEvents[i];
                if (Time.time < pendingEvent.TriggerTime)
                {
                    continue;
                }

                SpawnCombatVfx(pendingEvent.Event, pendingEvent.Direction);
                pendingCombatVfxEvents.RemoveAt(i);
            }
        }

        private void SpawnCombatVfx(DreamyCombatEventTuning combatEvent, Vector2 attackDirection)
        {
            if (combatEvent == null)
            {
                return;
            }

            if (attackDirection.sqrMagnitude < 0.01f)
            {
                attackDirection = ResolveAttackDirection();
            }

            Vector2 forward = attackDirection.sqrMagnitude >= 0.01f ? attackDirection.normalized : Vector2.down;
            Vector2 side = new Vector2(-forward.y, forward.x);
            Vector2 offset = combatEvent.Offset;
            Vector3 spawnPosition = transform.position + (Vector3)(forward * offset.x + side * offset.y);
            Quaternion rotation = combatEvent.RotateToDirection
                ? Quaternion.Euler(0f, 0f, Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg)
                : Quaternion.identity;

            if (combatEvent.VfxKind == DreamyCombatVfxKind.Prefab && combatEvent.Prefab != null)
            {
                Instantiate(combatEvent.Prefab, spawnPosition, rotation);
                return;
            }

            if (combatEvent.VfxKind == DreamyCombatVfxKind.BuiltInSlash)
            {
                GameObject slashObject = new GameObject(combatEvent.Label);
                slashObject.transform.position = spawnPosition;
                slashObject.transform.rotation = rotation;
                SpriteRenderer renderer = slashObject.AddComponent<SpriteRenderer>();
                renderer.sprite = DreamyAttackSlashEffect.SlashSprite;
                renderer.color = combatEvent.Color;
                renderer.sortingOrder = 180;
                slashObject.AddComponent<DreamyAttackSlashEffect>().Configure(combatEvent.Duration, combatEvent.StartScale, combatEvent.EndScale);
            }
        }

        private void OnValidate()
        {
            attackDamage = Mathf.Max(0f, attackDamage);
            attackRange = Mathf.Max(0.05f, attackRange);
            attackCooldown = Mathf.Max(0f, attackCooldown);
            attackWindup = Mathf.Max(0f, attackWindup);
            attackAnimationDuration = Mathf.Max(0.05f, attackAnimationDuration);
            attack2DamageMultiplier = Mathf.Max(0f, attack2DamageMultiplier);
            attack3DamageMultiplier = Mathf.Max(0f, attack3DamageMultiplier);
            attack3SlowMultiplier = Mathf.Clamp(attack3SlowMultiplier, 0.05f, 1f);
            attack3SlowDuration = Mathf.Max(0f, attack3SlowDuration);
            attack3StunDuration = Mathf.Max(0f, attack3StunDuration);
            staminaCost = Mathf.Max(0f, staminaCost);
            knockbackForce = Mathf.Max(0f, knockbackForce);
            comboResetDelay = Mathf.Max(0.05f, comboResetDelay);
            comboInputBufferWindow = Mathf.Max(0f, comboInputBufferWindow);
            attackOriginDistance = Mathf.Max(0f, attackOriginDistance);
            attackHitboxWidth = Mathf.Max(0.05f, attackHitboxWidth);
            attack2HitboxLengthMultiplier = Mathf.Max(0.05f, attack2HitboxLengthMultiplier);
            attack2HitboxWidthMultiplier = Mathf.Max(0.05f, attack2HitboxWidthMultiplier);
            attack3HitboxLengthMultiplier = Mathf.Max(0.05f, attack3HitboxLengthMultiplier);
            attack3HitboxWidthMultiplier = Mathf.Max(0.05f, attack3HitboxWidthMultiplier);
            specialDamageMultiplier = Mathf.Max(0f, specialDamageMultiplier);
            specialAttackRange = Mathf.Max(0.05f, specialAttackRange);
            specialAttackHitboxWidth = Mathf.Max(0.05f, specialAttackHitboxWidth);
            specialAttackCooldown = Mathf.Max(0f, specialAttackCooldown);
            specialAttackWindup = Mathf.Max(0f, specialAttackWindup);
            attackHitMarkerNormalizedTime = Mathf.Clamp01(attackHitMarkerNormalizedTime);
            attack2HitMarkerNormalizedTime = Mathf.Clamp01(attack2HitMarkerNormalizedTime);
            attack3HitMarkerNormalizedTime = Mathf.Clamp01(attack3HitMarkerNormalizedTime);
            specialHitMarkerNormalizedTime = Mathf.Clamp01(specialHitMarkerNormalizedTime);
            specialAttackAnimationDuration = Mathf.Max(0.05f, specialAttackAnimationDuration);
            specialStaminaCost = Mathf.Max(0f, specialStaminaCost);
            specialKnockbackForce = Mathf.Max(0f, specialKnockbackForce);
        }

        private void OnDrawGizmosSelected()
        {
            if (!showCombatTuningGizmos)
            {
                return;
            }

            Vector2 direction = ResolveGizmoDirection();
            DrawSlashHitbox(direction, GetAttackPartHitboxLength(0), GetAttackPartHitboxWidth(0), GetAttackPartOriginDistance(0), new Color(1f, 0.82f, 0.18f, 0.72f));
            DrawSlashHitbox(direction, GetAttackPartHitboxLength(1), GetAttackPartHitboxWidth(1), GetAttackPartOriginDistance(1), new Color(1f, 0.64f, 0.18f, 0.64f));
            DrawSlashHitbox(direction, GetAttackPartHitboxLength(2), GetAttackPartHitboxWidth(2), GetAttackPartOriginDistance(2), new Color(1f, 0.48f, 0.18f, 0.58f));
            DreamyCombatActionTuning special = GetSpecialAttackTuning();
            DrawSlashHitbox(
                direction,
                special != null ? special.HitboxLength : specialAttackRange,
                special != null ? special.HitboxWidth : specialAttackHitboxWidth,
                special != null ? special.OriginDistance : specialAttackWindup,
                new Color(0.35f, 0.92f, 1f, 0.58f));
        }

        private Vector2 ResolveGizmoDirection()
        {
            if (Application.isPlaying && player != null)
            {
                return player.FacingDirection;
            }

            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                return renderer.flipX ? Vector2.left : Vector2.right;
            }

            return Vector2.down;
        }

        private void DrawSlashHitbox(Vector2 direction, float length, float width, float originDistance, Color color)
        {
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = Vector2.down;
            }

            Vector2 forward = direction.normalized;
            Vector2 side = new Vector2(-forward.y, forward.x) * (Mathf.Max(0.05f, width) * 0.5f);
            Vector3 origin = transform.position + (Vector3)(forward * Mathf.Max(0f, originDistance));
            Vector3 front = origin + (Vector3)(forward * Mathf.Max(0.05f, length));
            Vector3 backLeft = origin + (Vector3)side;
            Vector3 backRight = origin - (Vector3)side;
            Vector3 frontLeft = front + (Vector3)side;
            Vector3 frontRight = front - (Vector3)side;
            Gizmos.color = color;
            Gizmos.DrawLine(backLeft, frontLeft);
            Gizmos.DrawLine(frontLeft, frontRight);
            Gizmos.DrawLine(frontRight, backRight);
            Gizmos.DrawLine(backRight, backLeft);
        }

        private readonly struct PendingCombatVfxEvent
        {
            public PendingCombatVfxEvent(float triggerTime, DreamyCombatEventTuning combatEvent, Vector2 direction)
            {
                TriggerTime = triggerTime;
                Event = combatEvent;
                Direction = direction;
            }

            public float TriggerTime { get; }
            public DreamyCombatEventTuning Event { get; }
            public Vector2 Direction { get; }
        }
    }
}
