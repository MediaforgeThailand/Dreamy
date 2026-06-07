using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamy
{
    public sealed class DreamyPlayerProgression : MonoBehaviour
    {
        [SerializeField] private int coins;
        [SerializeField] private int premiumCurrency;
        [SerializeField] private int skillPoints;
        [SerializeField] private int spentSkillPoints;
        [SerializeField] private int unlockTokens;
        [SerializeField] private int skillPointsPerLevel = 1;
        [SerializeField] private List<string> unlockedIds = new List<string>();

        private DreamyExperience experience;
        private bool subscribedToExperience;

        public event Action ProgressionChanged;

        public int Coins => coins;
        public int PremiumCurrency => premiumCurrency;
        public int SkillPoints => skillPoints;
        public int SpentSkillPoints => spentSkillPoints;
        public int UnlockTokens => unlockTokens;
        public int UnlockCount => unlockedIds.Count;
        public IReadOnlyList<string> UnlockedIds => unlockedIds;

        private void Awake()
        {
            Bind(GetComponent<DreamyExperience>());
        }

        private void OnEnable()
        {
            Bind(experience != null ? experience : GetComponent<DreamyExperience>());
        }

        private void OnDisable()
        {
            UnsubscribeExperience();
        }

        public void Bind(DreamyExperience targetExperience)
        {
            if (experience == targetExperience)
            {
                SubscribeExperienceIfReady();
                return;
            }

            UnsubscribeExperience();

            experience = targetExperience;
            SubscribeExperienceIfReady();
        }

        private void SubscribeExperienceIfReady()
        {
            if (experience == null || subscribedToExperience || !isActiveAndEnabled)
            {
                return;
            }

            experience.LeveledUp += HandleLeveledUp;
            subscribedToExperience = true;
        }

        private void UnsubscribeExperience()
        {
            if (experience == null || !subscribedToExperience)
            {
                return;
            }

            experience.LeveledUp -= HandleLeveledUp;
            subscribedToExperience = false;
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            coins += amount;
            ProgressionChanged?.Invoke();
        }

        public bool SpendCoins(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (coins < amount)
            {
                return false;
            }

            coins -= amount;
            ProgressionChanged?.Invoke();
            return true;
        }

        public void AddPremiumCurrency(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            premiumCurrency += amount;
            ProgressionChanged?.Invoke();
        }

        public void AddSkillPoints(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            skillPoints += amount;
            ProgressionChanged?.Invoke();
        }

        public bool SpendSkillPoint()
        {
            if (skillPoints <= 0)
            {
                return false;
            }

            skillPoints--;
            spentSkillPoints++;
            ProgressionChanged?.Invoke();
            return true;
        }

        public void AddUnlockTokens(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            unlockTokens += amount;
            ProgressionChanged?.Invoke();
        }

        public bool SpendUnlockTokens(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (unlockTokens < amount)
            {
                return false;
            }

            unlockTokens -= amount;
            ProgressionChanged?.Invoke();
            return true;
        }

        public bool Unlock(string unlockId)
        {
            if (string.IsNullOrWhiteSpace(unlockId) || unlockedIds.Contains(unlockId))
            {
                return false;
            }

            unlockedIds.Add(unlockId);
            ProgressionChanged?.Invoke();
            return true;
        }

        public bool IsUnlocked(string unlockId)
        {
            return !string.IsNullOrWhiteSpace(unlockId) && unlockedIds.Contains(unlockId);
        }

        private void HandleLeveledUp(int newLevel)
        {
            AddSkillPoints(skillPointsPerLevel);
        }

        private void OnValidate()
        {
            coins = Mathf.Max(0, coins);
            premiumCurrency = Mathf.Max(0, premiumCurrency);
            skillPoints = Mathf.Max(0, skillPoints);
            spentSkillPoints = Mathf.Max(0, spentSkillPoints);
            unlockTokens = Mathf.Max(0, unlockTokens);
            skillPointsPerLevel = Mathf.Max(0, skillPointsPerLevel);
            unlockedIds.RemoveAll(string.IsNullOrWhiteSpace);
        }
    }
}
