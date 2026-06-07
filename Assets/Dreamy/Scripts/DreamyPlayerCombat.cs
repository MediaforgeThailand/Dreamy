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
        [SerializeField] private float staminaCost = 8f;
        [SerializeField] private float knockbackForce = 5.2f;

        public event Action<DreamyMonsterController> AttackHit;
        public event Action AttackMissed;

        private DreamyCharacterStats stats;
        private float nextAttackTime;
        private bool queuedAttack;

        private void Awake()
        {
            stats = GetComponent<DreamyCharacterStats>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.J))
            {
                queuedAttack = true;
            }

            if (queuedAttack)
            {
                queuedAttack = false;
                TryAttack();
            }
        }

        public void QueueAttack()
        {
            queuedAttack = true;
        }

        public bool TryAttack()
        {
            if (Time.time < nextAttackTime || stats == null || !stats.IsAlive || !stats.TrySpendStamina(staminaCost))
            {
                return false;
            }

            nextAttackTime = Time.time + attackCooldown;
            DreamyMonsterController nearest = FindNearestMonster();
            if (nearest == null)
            {
                AttackMissed?.Invoke();
                return false;
            }

            nearest.ApplyHit(attackDamage, transform.position, knockbackForce);
            AttackHit?.Invoke(nearest);
            return true;
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

        private void OnValidate()
        {
            attackDamage = Mathf.Max(0f, attackDamage);
            attackRange = Mathf.Max(0.05f, attackRange);
            attackCooldown = Mathf.Max(0f, attackCooldown);
            staminaCost = Mathf.Max(0f, staminaCost);
            knockbackForce = Mathf.Max(0f, knockbackForce);
        }
    }
}
