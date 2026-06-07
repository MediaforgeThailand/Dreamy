using System;
using UnityEngine;

namespace Dreamy.Extraction
{
    public sealed class ExtractionWeaponController : MonoBehaviour
    {
        [SerializeField] private ExtractionWeaponData activeWeapon;
        [SerializeField] private Transform attackOrigin;
        [SerializeField] private LayerMask targetLayers;
        [SerializeField] private float fallbackDamage = 6f;
        [SerializeField] private float fallbackRange = 0.65f;
        [SerializeField] private float fallbackRadius = 0.45f;
        [SerializeField] private float fallbackCooldown = 0.55f;

        public event Action WeaponChanged;
        public event Action DurabilityChanged;

        private int currentDurability;
        private float nextAttackTime;
        private float nextSkillTime;
        private ExtractionStamina stamina;

        public ExtractionWeaponData ActiveWeapon => activeWeapon;
        public int CurrentDurability => currentDurability;
        public int MaxDurability => activeWeapon != null ? activeWeapon.MaxDurability : 1;
        public bool HasUsableWeapon => activeWeapon == null || currentDurability > 0;

        private void Awake()
        {
            if (attackOrigin == null)
            {
                attackOrigin = transform;
            }

            stamina = GetComponent<ExtractionStamina>();
            if (activeWeapon != null && currentDurability <= 0)
            {
                currentDurability = activeWeapon.MaxDurability;
            }
        }

        public void SetWeapon(ExtractionWeaponData weapon, bool resetDurability)
        {
            activeWeapon = weapon;
            if (resetDurability)
            {
                currentDurability = weapon != null ? weapon.MaxDurability : 1;
            }

            WeaponChanged?.Invoke();
            DurabilityChanged?.Invoke();
        }

        public bool TryAttack(Vector2 direction)
        {
            if (Time.time < nextAttackTime || !HasUsableWeapon)
            {
                return false;
            }

            float damage = activeWeapon != null ? activeWeapon.Damage : fallbackDamage;
            float range = activeWeapon != null ? activeWeapon.AttackRange : fallbackRange;
            float radius = activeWeapon != null ? activeWeapon.AttackRadius : fallbackRadius;
            float cooldown = activeWeapon != null ? activeWeapon.AttackCooldown : fallbackCooldown;
            int durabilityCost = activeWeapon != null ? activeWeapon.DurabilityLossPerAttack : 0;

            ApplyAreaDamage(direction, range, radius, damage);
            SpendDurability(durabilityCost);
            nextAttackTime = Time.time + cooldown;
            return true;
        }

        public bool TryUseActiveSkill(Vector2 direction)
        {
            ExtractionWeaponSkillData skill = activeWeapon != null ? activeWeapon.ActiveSkill : null;
            if (skill == null || Time.time < nextSkillTime || currentDurability < skill.DurabilityCost)
            {
                return false;
            }

            if (stamina != null && !stamina.TrySpend(skill.StaminaCost))
            {
                return false;
            }

            float baseDamage = activeWeapon != null ? activeWeapon.Damage : fallbackDamage;
            ApplyAreaDamage(direction, activeWeapon.AttackRange, skill.Radius, baseDamage * skill.DamageMultiplier);
            SpendDurability(skill.DurabilityCost);
            nextSkillTime = Time.time + skill.Cooldown;
            return true;
        }

        public void RepairActiveWeapon(int amount)
        {
            if (amount <= 0 || activeWeapon == null)
            {
                return;
            }

            currentDurability = Mathf.Clamp(currentDurability + amount, 0, activeWeapon.MaxDurability);
            DurabilityChanged?.Invoke();
        }

        private void ApplyAreaDamage(Vector2 direction, float range, float radius, float damage)
        {
            Vector2 normalized = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector2.down;
            Vector2 center = (Vector2)attackOrigin.position + normalized * range;
            int mask = targetLayers.value == 0 ? Physics2D.AllLayers : targetLayers.value;
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, mask);
            for (int i = 0; i < hits.Length; i++)
            {
                ExtractionHealth target = hits[i].GetComponentInParent<ExtractionHealth>();
                if (target != null && target.gameObject != gameObject)
                {
                    target.ApplyDamage(new ExtractionDamage(damage, gameObject, center));
                }
            }
        }

        private void SpendDurability(int amount)
        {
            if (activeWeapon == null || amount <= 0)
            {
                return;
            }

            currentDurability = Mathf.Max(0, currentDurability - amount);
            DurabilityChanged?.Invoke();
        }

        private void OnValidate()
        {
            fallbackDamage = Mathf.Max(0f, fallbackDamage);
            fallbackRange = Mathf.Max(0.05f, fallbackRange);
            fallbackRadius = Mathf.Max(0.05f, fallbackRadius);
            fallbackCooldown = Mathf.Max(0f, fallbackCooldown);
        }
    }
}
