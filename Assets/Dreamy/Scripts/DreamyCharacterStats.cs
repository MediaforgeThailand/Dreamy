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

        public event Action StatsChanged;
        public event Action<float> Damaged;
        public event Action Died;
        private bool hasDied;

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

        public bool IsAlive => currentHealth > 0f;

        private void Update()
        {
            if (currentStamina < maxStamina && staminaRegenPerSecond > 0f)
            {
                CurrentStamina = Mathf.MoveTowards(currentStamina, maxStamina, staminaRegenPerSecond * Time.deltaTime);
            }
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
        }
    }
}
