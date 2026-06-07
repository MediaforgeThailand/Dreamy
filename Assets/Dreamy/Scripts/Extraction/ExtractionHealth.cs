using System;
using UnityEngine;

namespace Dreamy.Extraction
{
    public readonly struct ExtractionDamage
    {
        public ExtractionDamage(float amount, GameObject source, Vector2 hitPoint)
        {
            Amount = Mathf.Max(0f, amount);
            Source = source;
            HitPoint = hitPoint;
        }

        public float Amount { get; }
        public GameObject Source { get; }
        public Vector2 HitPoint { get; }
    }

    public sealed class ExtractionHealth : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private float invulnerableSecondsAfterHit = 0.12f;

        public event Action<ExtractionDamage> Damaged;
        public event Action Died;
        public event Action Changed;

        private float invulnerableUntil;
        private bool hasDied;

        public float MaxHealth
        {
            get => maxHealth;
            set
            {
                maxHealth = Mathf.Max(1f, value);
                currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
                Changed?.Invoke();
            }
        }

        public float CurrentHealth => currentHealth;
        public bool IsAlive => currentHealth > 0f;
        public float NormalizedHealth => maxHealth > 0f ? currentHealth / maxHealth : 0f;

        private void Awake()
        {
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            hasDied = currentHealth <= 0f;
        }

        public void SetMaxHealth(float value, bool fillHealth)
        {
            maxHealth = Mathf.Max(1f, value);
            currentHealth = fillHealth ? maxHealth : Mathf.Clamp(currentHealth, 0f, maxHealth);
            hasDied = currentHealth <= 0f;
            Changed?.Invoke();
        }

        public bool ApplyDamage(ExtractionDamage damage)
        {
            if (!IsAlive || damage.Amount <= 0f || Time.time < invulnerableUntil)
            {
                return false;
            }

            currentHealth = Mathf.Max(0f, currentHealth - damage.Amount);
            invulnerableUntil = Time.time + invulnerableSecondsAfterHit;
            Damaged?.Invoke(damage);
            Changed?.Invoke();

            if (currentHealth <= 0f && !hasDied)
            {
                hasDied = true;
                Died?.Invoke();
            }

            return true;
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || !IsAlive)
            {
                return;
            }

            currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
            Changed?.Invoke();
        }

        public void Revive(bool fullHealth)
        {
            currentHealth = fullHealth ? maxHealth : Mathf.Max(1f, currentHealth);
            hasDied = false;
            Changed?.Invoke();
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            invulnerableSecondsAfterHit = Mathf.Max(0f, invulnerableSecondsAfterHit);
        }
    }
}
