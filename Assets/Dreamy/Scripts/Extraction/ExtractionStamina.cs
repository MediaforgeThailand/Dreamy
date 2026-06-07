using System;
using UnityEngine;

namespace Dreamy.Extraction
{
    public sealed class ExtractionStamina : MonoBehaviour
    {
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float currentStamina = 100f;
        [SerializeField] private float regenPerSecond = 24f;
        [SerializeField] private float regenDelayAfterSpend = 0.35f;

        public event Action Changed;

        private float canRegenAt;

        public float MaxStamina
        {
            get => maxStamina;
            set
            {
                maxStamina = Mathf.Max(1f, value);
                currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
                Changed?.Invoke();
            }
        }

        public float CurrentStamina => currentStamina;
        public float NormalizedStamina => maxStamina > 0f ? currentStamina / maxStamina : 0f;

        private void Awake()
        {
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        }

        private void Update()
        {
            if (Time.time < canRegenAt || regenPerSecond <= 0f || currentStamina >= maxStamina)
            {
                return;
            }

            currentStamina = Mathf.Min(maxStamina, currentStamina + regenPerSecond * Time.deltaTime);
            Changed?.Invoke();
        }

        public bool TrySpend(float amount)
        {
            if (amount <= 0f)
            {
                return true;
            }

            if (currentStamina < amount)
            {
                return false;
            }

            currentStamina = Mathf.Max(0f, currentStamina - amount);
            canRegenAt = Time.time + regenDelayAfterSpend;
            Changed?.Invoke();
            return true;
        }

        public void Restore(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            currentStamina = Mathf.Clamp(currentStamina + amount, 0f, maxStamina);
            Changed?.Invoke();
        }

        public void Fill()
        {
            currentStamina = maxStamina;
            Changed?.Invoke();
        }

        private void OnValidate()
        {
            maxStamina = Mathf.Max(1f, maxStamina);
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
            regenPerSecond = Mathf.Max(0f, regenPerSecond);
            regenDelayAfterSpend = Mathf.Max(0f, regenDelayAfterSpend);
        }
    }
}
