using System;
using UnityEngine;

namespace Dreamy
{
    public sealed class DreamyExperience : MonoBehaviour
    {
        [SerializeField] private int level = 1;
        [SerializeField] private int currentExp;
        [SerializeField] private int expToNextLevel = 100;
        [SerializeField] private float expGrowthMultiplier = 1.25f;

        public event Action ExperienceChanged;
        public event Action<int> LeveledUp;

        public int Level
        {
            get => level;
            set
            {
                level = Mathf.Max(1, value);
                ExperienceChanged?.Invoke();
            }
        }

        public int CurrentExp => currentExp;
        public int ExpToNextLevel => expToNextLevel;

        public float ExpGrowthMultiplier
        {
            get => expGrowthMultiplier;
            set => expGrowthMultiplier = Mathf.Max(1f, value);
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            currentExp += amount;
            while (currentExp >= expToNextLevel)
            {
                currentExp -= expToNextLevel;
                level++;
                expToNextLevel = Mathf.Max(expToNextLevel + 1, Mathf.RoundToInt(expToNextLevel * expGrowthMultiplier));
                LeveledUp?.Invoke(level);
            }

            ExperienceChanged?.Invoke();
        }

        public void SetExperience(int levelValue, int expValue, int nextLevelValue)
        {
            level = Mathf.Max(1, levelValue);
            expToNextLevel = Mathf.Max(1, nextLevelValue);
            currentExp = Mathf.Clamp(expValue, 0, expToNextLevel - 1);
            ExperienceChanged?.Invoke();
        }

        private void OnValidate()
        {
            level = Mathf.Max(1, level);
            expToNextLevel = Mathf.Max(1, expToNextLevel);
            currentExp = Mathf.Clamp(currentExp, 0, expToNextLevel - 1);
            expGrowthMultiplier = Mathf.Max(1f, expGrowthMultiplier);
        }
    }
}
