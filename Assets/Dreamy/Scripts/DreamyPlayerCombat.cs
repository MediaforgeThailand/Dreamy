using System;
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
        [SerializeField] private float attackAnimationDuration = 0.38f;
        [SerializeField] private float staminaCost = 8f;
        [SerializeField] private float knockbackForce = 5.2f;
        [SerializeField] private float slashEffectDistance = 0.58f;
        [SerializeField] private float comboResetDelay = 0.72f;

        public event Action<DreamyMonsterController> AttackHit;
        public event Action AttackMissed;

        private DreamyCharacterStats stats;
        private DreamyMobilePlayer player;
        private float nextAttackTime;
        private float pendingAttackHitsAt;
        private float currentAttackEndsAt;
        private float lastAttackEndedAt = -999f;
        private Vector2 pendingAttackDirection = Vector2.down;
        private bool bufferedAttack;
        private bool pendingAttackHit;

        private void Awake()
        {
            stats = GetComponent<DreamyCharacterStats>();
            player = GetComponent<DreamyMobilePlayer>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.J))
            {
                QueueAttack();
            }

            if (pendingAttackHit && Time.time >= pendingAttackHitsAt)
            {
                ResolvePendingAttack();
            }

            if (bufferedAttack && CanStartBufferedAttack())
            {
                bufferedAttack = false;
                TryAttack();
            }
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

            float animationDuration = Mathf.Max(0.05f, attackAnimationDuration);
            nextAttackTime = Time.time + Mathf.Max(attackCooldown, animationDuration);
            currentAttackEndsAt = Time.time + animationDuration;
            pendingAttackHitsAt = Time.time + attackWindup;
            pendingAttackDirection = ResolveAttackDirection();
            pendingAttackHit = true;
            if (player != null)
            {
                player.PlayAttack(animationDuration, pendingAttackDirection);
            }

            return true;
        }

        private void ResolvePendingAttack()
        {
            pendingAttackHit = false;
            lastAttackEndedAt = Mathf.Max(lastAttackEndedAt, currentAttackEndsAt);
            SpawnSlashEffect(pendingAttackDirection);
            DreamyMonsterController nearest = FindNearestMonster();
            if (nearest == null)
            {
                AttackMissed?.Invoke();
                return;
            }

            nearest.ApplyHit(attackDamage, transform.position, knockbackForce);
            AttackHit?.Invoke(nearest);
        }

        private bool CanStartAttackNow()
        {
            return !pendingAttackHit
                && Time.time >= currentAttackEndsAt
                && Time.time >= nextAttackTime
                && stats != null
                && stats.IsAlive;
        }

        private bool CanBufferAttack()
        {
            return stats != null
                && stats.IsAlive
                && (pendingAttackHit || Time.time < currentAttackEndsAt || Time.time < nextAttackTime);
        }

        private bool CanStartBufferedAttack()
        {
            return stats != null
                && stats.IsAlive
                && !pendingAttackHit
                && Time.time >= currentAttackEndsAt
                && Time.time >= nextAttackTime;
        }

        private DreamyMonsterController FindNearestMonster()
        {
            DreamyMonsterController[] monsters = FindObjectsByType<DreamyMonsterController>(FindObjectsInactive.Exclude);
            DreamyMonsterController nearest = null;
            float nearestDistance = attackRange * attackRange;
            Vector2 origin = transform.position;
            for (int i = 0; i < monsters.Length; i++)
            {
                if (monsters[i] == null || !monsters[i].IsAlive)
                {
                    continue;
                }

                float distance = Vector2.SqrMagnitude((Vector2)monsters[i].transform.position - origin);
                if (distance <= nearestDistance)
                {
                    nearest = monsters[i];
                    nearestDistance = distance;
                }
            }

            return nearest;
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

        private void SpawnSlashEffect(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = Vector2.down;
            }

            Vector2 normalized = direction.normalized;
            GameObject slash = new GameObject("Player Knight Slash Effect");
            slash.transform.position = transform.position + (Vector3)(normalized * slashEffectDistance) + new Vector3(0f, 0.18f, 0f);
            slash.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(normalized.y, normalized.x) * Mathf.Rad2Deg);

            SpriteRenderer renderer = slash.AddComponent<SpriteRenderer>();
            renderer.sprite = DreamyAttackSlashEffect.SlashSprite;
            renderer.color = new Color(1f, 0.92f, 0.48f, 0.92f);
            renderer.sortingOrder = 260;

            DreamyAttackSlashEffect effect = slash.AddComponent<DreamyAttackSlashEffect>();
            effect.Configure(0.15f);
        }

        private void OnValidate()
        {
            attackDamage = Mathf.Max(0f, attackDamage);
            attackRange = Mathf.Max(0.05f, attackRange);
            attackCooldown = Mathf.Max(0f, attackCooldown);
            attackWindup = Mathf.Max(0f, attackWindup);
            attackAnimationDuration = Mathf.Max(0.05f, attackAnimationDuration);
            staminaCost = Mathf.Max(0f, staminaCost);
            knockbackForce = Mathf.Max(0f, knockbackForce);
            slashEffectDistance = Mathf.Max(0.05f, slashEffectDistance);
            comboResetDelay = Mathf.Max(0.05f, comboResetDelay);
        }
    }
}
