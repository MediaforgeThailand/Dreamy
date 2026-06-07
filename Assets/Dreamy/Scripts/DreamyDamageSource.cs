using UnityEngine;

namespace Dreamy
{
    public sealed class DreamyDamageSource : MonoBehaviour
    {
        [SerializeField] private float damage = 10f;
        [SerializeField] private float staminaCost;
        [SerializeField] private bool applyOnTriggerEnter = true;

        public float Damage
        {
            get => damage;
            set => damage = Mathf.Max(0f, value);
        }

        public float StaminaCost
        {
            get => staminaCost;
            set => staminaCost = Mathf.Max(0f, value);
        }

        public bool TryApplyTo(GameObject target)
        {
            if (target == null)
            {
                return false;
            }

            DreamyCharacterStats targetStats = target.GetComponentInParent<DreamyCharacterStats>();
            if (targetStats == null)
            {
                return false;
            }

            DreamyCharacterStats sourceStats = GetComponentInParent<DreamyCharacterStats>();
            if (sourceStats != null && sourceStats == targetStats)
            {
                return false;
            }

            if (sourceStats != null && !sourceStats.TrySpendStamina(staminaCost))
            {
                return false;
            }

            targetStats.TakeDamage(damage);
            return true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (applyOnTriggerEnter)
            {
                TryApplyTo(other.gameObject);
            }
        }

        private void OnValidate()
        {
            damage = Mathf.Max(0f, damage);
            staminaCost = Mathf.Max(0f, staminaCost);
        }
    }
}
