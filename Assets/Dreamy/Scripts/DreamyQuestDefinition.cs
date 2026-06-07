using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamy
{
    public enum DreamyQuestObjectiveKind
    {
        CollectItem,
        DefeatMonster,
        ReachLevel,
        EarnCurrency,
        OwnUnlockToken
    }

    [Serializable]
    public sealed class DreamyQuestObjectiveDefinition
    {
        [SerializeField] private DreamyQuestObjectiveKind kind;
        [SerializeField] private DreamyItemId itemId = DreamyItemId.Wood;
        [SerializeField] private string targetId;
        [SerializeField] private string label;
        [SerializeField] private int requiredAmount = 1;

        public DreamyQuestObjectiveKind Kind => kind;
        public DreamyItemId ItemId => itemId;
        public string TargetId => targetId;
        public string Label => string.IsNullOrWhiteSpace(label) ? kind.ToString() : label;
        public int RequiredAmount => Mathf.Max(1, requiredAmount);

        public DreamyQuestObjectiveDefinition()
        {
        }

        public DreamyQuestObjectiveDefinition(
            DreamyQuestObjectiveKind kind,
            DreamyItemId itemId,
            string targetId,
            string label,
            int requiredAmount)
        {
            this.kind = kind;
            this.itemId = itemId;
            this.targetId = targetId;
            this.label = label;
            this.requiredAmount = Mathf.Max(1, requiredAmount);
        }

        public void Validate()
        {
            requiredAmount = Mathf.Max(1, requiredAmount);
        }
    }

    [Serializable]
    public sealed class DreamyQuestRewardDefinition
    {
        [SerializeField] private int experience;
        [SerializeField] private int coins;
        [SerializeField] private int premiumCurrency;
        [SerializeField] private int skillPoints;
        [SerializeField] private int unlockTokens;
        [SerializeField] private string unlockId;
        [SerializeField] private List<DreamyItemStack> items = new List<DreamyItemStack>();

        public int Experience => Mathf.Max(0, experience);
        public int Coins => Mathf.Max(0, coins);
        public int PremiumCurrency => Mathf.Max(0, premiumCurrency);
        public int SkillPoints => Mathf.Max(0, skillPoints);
        public int UnlockTokens => Mathf.Max(0, unlockTokens);
        public string UnlockId => unlockId;
        public IReadOnlyList<DreamyItemStack> Items => items;

        public DreamyQuestRewardDefinition()
        {
        }

        public DreamyQuestRewardDefinition(
            int experience,
            int coins,
            int premiumCurrency,
            int skillPoints,
            int unlockTokens,
            string unlockId,
            IEnumerable<DreamyItemStack> items)
        {
            this.experience = Mathf.Max(0, experience);
            this.coins = Mathf.Max(0, coins);
            this.premiumCurrency = Mathf.Max(0, premiumCurrency);
            this.skillPoints = Mathf.Max(0, skillPoints);
            this.unlockTokens = Mathf.Max(0, unlockTokens);
            this.unlockId = unlockId;
            this.items = items != null ? new List<DreamyItemStack>(items) : new List<DreamyItemStack>();
            Validate();
        }

        public void Validate()
        {
            experience = Mathf.Max(0, experience);
            coins = Mathf.Max(0, coins);
            premiumCurrency = Mathf.Max(0, premiumCurrency);
            skillPoints = Mathf.Max(0, skillPoints);
            unlockTokens = Mathf.Max(0, unlockTokens);
            items.RemoveAll(item => item == null || !item.IsValid);
            for (int i = 0; i < items.Count; i++)
            {
                items[i].Validate();
            }
        }
    }

    [CreateAssetMenu(menuName = "Dreamy/Prototype/Quest Definition", fileName = "DreamyQuestDefinition")]
    public sealed class DreamyQuestDefinition : ScriptableObject
    {
        [SerializeField] private string questId;
        [SerializeField] private string displayName;
        [SerializeField] private string description;
        [SerializeField] private List<DreamyQuestObjectiveDefinition> objectives = new List<DreamyQuestObjectiveDefinition>();
        [SerializeField] private DreamyQuestRewardDefinition rewards = new DreamyQuestRewardDefinition();

        public string QuestId => string.IsNullOrWhiteSpace(questId) ? name : questId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? QuestId : displayName;
        public string Description => description;
        public IReadOnlyList<DreamyQuestObjectiveDefinition> Objectives => objectives;
        public DreamyQuestRewardDefinition Rewards => rewards;

        public void ConfigureRuntime(
            string id,
            string title,
            string body,
            IEnumerable<DreamyQuestObjectiveDefinition> objectiveDefinitions,
            DreamyQuestRewardDefinition rewardDefinition)
        {
            questId = id;
            displayName = title;
            description = body;
            objectives = objectiveDefinitions != null
                ? new List<DreamyQuestObjectiveDefinition>(objectiveDefinitions)
                : new List<DreamyQuestObjectiveDefinition>();
            rewards = rewardDefinition ?? new DreamyQuestRewardDefinition();
            ValidateData();
        }

        private void OnValidate()
        {
            ValidateData();
        }

        private void ValidateData()
        {
            objectives.RemoveAll(objective => objective == null);
            for (int i = 0; i < objectives.Count; i++)
            {
                objectives[i].Validate();
            }

            if (rewards == null)
            {
                rewards = new DreamyQuestRewardDefinition();
            }

            rewards.Validate();
        }
    }
}
