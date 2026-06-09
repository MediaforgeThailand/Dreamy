using System;
using UnityEngine;

namespace Dreamy
{
    public sealed class DreamyCharacterStats : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float currentStamina = 100f;
        [SerializeField] private float staminaRegenPerSecond = 12f;
        [SerializeField] private float damage = 10f;
        [SerializeField] private float strength;
        [SerializeField] private float agility;
        [SerializeField] private float attackSpeed = 1f;
        [SerializeField, Range(0f, 1f)] private float criticalChance = 0.05f;
        [SerializeField] private float criticalDamageMultiplier = 1.5f;
        [SerializeField, Range(0f, 1f)] private float statusResistance;

        public event Action StatsChanged;
        public event Action<float> Damaged;
        public event Action Died;
        private bool hasDied;
        private float slowEndsAt;
        private float slowMultiplier = 1f;
        private float stunEndsAt;

        public float MaxHealth
        {
            get => maxHealth;
            set
            {
                maxHealth = Mathf.Max(1f, value);
                currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
                StatsChanged?.Invoke();
            }
        }

        public float CurrentHealth
        {
            get => currentHealth;
            set
            {
                currentHealth = Mathf.Clamp(value, 0f, maxHealth);
                StatsChanged?.Invoke();
                if (currentHealth <= 0f && !hasDied)
                {
                    hasDied = true;
                    Died?.Invoke();
                }
                else if (currentHealth > 0f)
                {
                    hasDied = false;
                }
            }
        }

        public float MaxStamina
        {
            get => maxStamina;
            set
            {
                maxStamina = Mathf.Max(1f, value);
                currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
                StatsChanged?.Invoke();
            }
        }

        public float CurrentStamina
        {
            get => currentStamina;
            set
            {
                currentStamina = Mathf.Clamp(value, 0f, maxStamina);
                StatsChanged?.Invoke();
            }
        }

        public float StaminaRegenPerSecond
        {
            get => staminaRegenPerSecond;
            set => staminaRegenPerSecond = Mathf.Max(0f, value);
        }

        public float Damage
        {
            get => damage;
            set => damage = Mathf.Max(0f, value);
        }

        public float Strength
        {
            get => strength;
            set => strength = Mathf.Max(0f, value);
        }

        public float Agility
        {
            get => agility;
            set => agility = Mathf.Max(0f, value);
        }

        public float AttackSpeed
        {
            get => attackSpeed;
            set => attackSpeed = Mathf.Max(0.1f, value);
        }

        public float CriticalChance
        {
            get => criticalChance;
            set => criticalChance = Mathf.Clamp01(value);
        }

        public float CriticalDamageMultiplier
        {
            get => criticalDamageMultiplier;
            set => criticalDamageMultiplier = Mathf.Max(1f, value);
        }

        public float StatusResistance
        {
            get => statusResistance;
            set => statusResistance = Mathf.Clamp01(value);
        }

        public bool IsAlive => currentHealth > 0f;
        public bool IsStunned => Application.isPlaying && Time.time < stunEndsAt;
        public bool IsSlowed => Application.isPlaying && Time.time < slowEndsAt;
        public float DamageMultiplier => 1f + strength * 0.025f;
        public float AttackSpeedMultiplier => Mathf.Max(0.1f, attackSpeed * (1f + agility * 0.02f) * StatusSpeedMultiplier);
        public float MovementSpeedMultiplier => IsStunned ? 0f : StatusSpeedMultiplier;
        private float StatusSpeedMultiplier => IsSlowed ? Mathf.Clamp(slowMultiplier, 0.05f, 1f) : 1f;

        private void Update()
        {
            if (currentStamina < maxStamina && staminaRegenPerSecond > 0f)
            {
                CurrentStamina = Mathf.MoveTowards(currentStamina, maxStamina, staminaRegenPerSecond * Time.deltaTime);
            }
        }

        public float ResolveOutgoingDamage(float baseDamage, float actionMultiplier, out bool critical)
        {
            critical = false;
            float resolvedDamage = Mathf.Max(0f, baseDamage) * Mathf.Max(0f, actionMultiplier) * DamageMultiplier;
            if (resolvedDamage <= 0f)
            {
                return 0f;
            }

            if (UnityEngine.Random.value < criticalChance)
            {
                critical = true;
                resolvedDamage *= criticalDamageMultiplier;
            }

            return resolvedDamage;
        }

        public void ApplySlow(float movementAndActionMultiplier, float duration)
        {
            if (duration <= 0f || !IsAlive)
            {
                return;
            }

            float effectiveDuration = duration * (1f - statusResistance);
            if (effectiveDuration <= 0f)
            {
                return;
            }

            slowMultiplier = Mathf.Min(Mathf.Clamp(movementAndActionMultiplier, 0.05f, 1f), IsSlowed ? slowMultiplier : 1f);
            slowEndsAt = Mathf.Max(slowEndsAt, Time.time + effectiveDuration);
            StatsChanged?.Invoke();
        }

        public void ApplyStun(float duration)
        {
            if (duration <= 0f || !IsAlive)
            {
                return;
            }

            float effectiveDuration = duration * (1f - statusResistance);
            if (effectiveDuration <= 0f)
            {
                return;
            }

            stunEndsAt = Mathf.Max(stunEndsAt, Time.time + effectiveDuration);
            StatsChanged?.Invoke();
        }

        public float TakeDamage(float amount)
        {
            if (amount <= 0f || !IsAlive)
            {
                return 0f;
            }

            float previousHealth = currentHealth;
            CurrentHealth = currentHealth - amount;
            float actualDamage = Mathf.Max(0f, previousHealth - currentHealth);
            if (actualDamage > 0f)
            {
                Damaged?.Invoke(actualDamage);
            }

            return actualDamage;
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || !IsAlive)
            {
                return;
            }

            CurrentHealth = currentHealth + amount;
        }

        public bool TrySpendStamina(float amount)
        {
            if (amount <= 0f)
            {
                return true;
            }

            if (currentStamina < amount)
            {
                return false;
            }

            CurrentStamina = currentStamina - amount;
            return true;
        }

        public void RestoreStamina(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            CurrentStamina = currentStamina + amount;
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            hasDied = currentHealth <= 0f;
            maxStamina = Mathf.Max(1f, maxStamina);
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
            staminaRegenPerSecond = Mathf.Max(0f, staminaRegenPerSecond);
            damage = Mathf.Max(0f, damage);
            strength = Mathf.Max(0f, strength);
            agility = Mathf.Max(0f, agility);
            attackSpeed = Mathf.Max(0.1f, attackSpeed);
            criticalChance = Mathf.Clamp01(criticalChance);
            criticalDamageMultiplier = Mathf.Max(1f, criticalDamageMultiplier);
            statusResistance = Mathf.Clamp01(statusResistance);
        }
    }
}
