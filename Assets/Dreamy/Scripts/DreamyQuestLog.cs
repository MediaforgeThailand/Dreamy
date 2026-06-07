using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Dreamy
{
    public sealed class DreamyQuestLog : MonoBehaviour
    {
        [SerializeField] private List<DreamyQuestDefinition> activeQuests = new List<DreamyQuestDefinition>();
        [SerializeField] private List<DreamyQuestDefinition> completedQuests = new List<DreamyQuestDefinition>();

        private readonly Dictionary<string, int[]> progressByQuestId = new Dictionary<string, int[]>();
        private DreamyMobilePlayer player;
        private DreamyInventory inventory;
        private DreamyExperience experience;
        private DreamyPlayerProgression progression;

        public event Action QuestsChanged;

        public IReadOnlyList<DreamyQuestDefinition> ActiveQuests => activeQuests;
        public IReadOnlyList<DreamyQuestDefinition> CompletedQuests => completedQuests;

        private void OnEnable()
        {
            DreamyResourcePickup.PickedUp += HandlePickup;
            DreamyMonsterController.AnyDied += HandleMonsterDied;
        }

        private void OnDisable()
        {
            DreamyResourcePickup.PickedUp -= HandlePickup;
            DreamyMonsterController.AnyDied -= HandleMonsterDied;
            UnbindInventory();
            UnbindExperience();
            UnbindProgression();
        }

        public void Configure(DreamyMobilePlayer targetPlayer, IEnumerable<DreamyQuestDefinition> startingQuests)
        {
            player = targetPlayer;
            BindRuntimeSources();

            if (startingQuests != null)
            {
                foreach (DreamyQuestDefinition quest in startingQuests)
                {
                    TryAcceptQuest(quest);
                }
            }

            RecalculatePassiveObjectives();
        }

        public bool TryAcceptQuest(DreamyQuestDefinition quest)
        {
            if (quest == null || ContainsQuest(activeQuests, quest) || ContainsQuest(completedQuests, quest))
            {
                return false;
            }

            activeQuests.Add(quest);
            EnsureProgress(quest);
            RecalculateQuestPassiveObjectives(quest);
            QuestsChanged?.Invoke();
            return true;
        }

        public string BuildHudSummary(int maxObjectives)
        {
            if (activeQuests.Count == 0)
            {
                return "Quest: Complete";
            }

            DreamyQuestDefinition quest = activeQuests[0];
            StringBuilder builder = new StringBuilder();
            builder.Append(quest.DisplayName);

            int[] progress = EnsureProgress(quest);
            int objectiveCount = Mathf.Min(maxObjectives, quest.Objectives.Count);
            for (int i = 0; i < objectiveCount; i++)
            {
                DreamyQuestObjectiveDefinition objective = quest.Objectives[i];
                builder.AppendLine();
                builder.Append(objective.Label);
                builder.Append(" ");
                builder.Append(Mathf.Min(progress[i], objective.RequiredAmount));
                builder.Append("/");
                builder.Append(objective.RequiredAmount);
            }

            return builder.ToString();
        }

        private void BindRuntimeSources()
        {
            UnbindInventory();
            UnbindExperience();
            UnbindProgression();

            inventory = player != null ? player.Inventory : GetComponent<DreamyInventory>();
            experience = player != null ? player.Experience : GetComponent<DreamyExperience>();
            progression = player != null ? player.Progression : GetComponent<DreamyPlayerProgression>();

            if (inventory != null)
            {
                inventory.InventoryChanged += HandleInventoryChanged;
            }

            if (experience != null)
            {
                experience.ExperienceChanged += HandleExperienceChanged;
            }

            if (progression != null)
            {
                progression.ProgressionChanged += HandleProgressionChanged;
            }
        }

        private void UnbindInventory()
        {
            if (inventory != null)
            {
                inventory.InventoryChanged -= HandleInventoryChanged;
                inventory = null;
            }
        }

        private void UnbindExperience()
        {
            if (experience != null)
            {
                experience.ExperienceChanged -= HandleExperienceChanged;
                experience = null;
            }
        }

        private void UnbindProgression()
        {
            if (progression != null)
            {
                progression.ProgressionChanged -= HandleProgressionChanged;
                progression = null;
            }
        }

        private void HandlePickup(DreamyResourcePickup pickup, Transform collector)
        {
            if (pickup == null || collector == null || player == null || collector.GetComponentInParent<DreamyMobilePlayer>() != player)
            {
                return;
            }

            RecalculatePassiveObjectives();
        }

        private void HandleMonsterDied(DreamyMonsterController monster)
        {
            if (monster == null || activeQuests.Count == 0)
            {
                return;
            }

            bool changed = false;
            for (int questIndex = 0; questIndex < activeQuests.Count; questIndex++)
            {
                DreamyQuestDefinition quest = activeQuests[questIndex];
                int[] progress = EnsureProgress(quest);
                for (int objectiveIndex = 0; objectiveIndex < quest.Objectives.Count; objectiveIndex++)
                {
                    DreamyQuestObjectiveDefinition objective = quest.Objectives[objectiveIndex];
                    if (objective.Kind != DreamyQuestObjectiveKind.DefeatMonster || !MatchesMonster(objective, monster))
                    {
                        continue;
                    }

                    int previous = progress[objectiveIndex];
                    progress[objectiveIndex] = Mathf.Min(objective.RequiredAmount, progress[objectiveIndex] + 1);
                    changed |= progress[objectiveIndex] != previous;
                }
            }

            if (changed)
            {
                CompleteReadyQuests();
                QuestsChanged?.Invoke();
            }
        }

        private void HandleInventoryChanged()
        {
            RecalculatePassiveObjectives();
        }

        private void HandleExperienceChanged()
        {
            RecalculatePassiveObjectives();
        }

        private void HandleProgressionChanged()
        {
            RecalculatePassiveObjectives();
        }

        private void RecalculatePassiveObjectives()
        {
            bool changed = false;
            for (int i = 0; i < activeQuests.Count; i++)
            {
                changed |= RecalculateQuestPassiveObjectives(activeQuests[i]);
            }

            if (CompleteReadyQuests())
            {
                changed = true;
            }

            if (changed)
            {
                QuestsChanged?.Invoke();
            }
        }

        private bool RecalculateQuestPassiveObjectives(DreamyQuestDefinition quest)
        {
            if (quest == null)
            {
                return false;
            }

            bool changed = false;
            int[] progress = EnsureProgress(quest);
            for (int i = 0; i < quest.Objectives.Count; i++)
            {
                DreamyQuestObjectiveDefinition objective = quest.Objectives[i];
                int value = progress[i];
                switch (objective.Kind)
                {
                    case DreamyQuestObjectiveKind.CollectItem:
                        value = inventory != null ? inventory.GetQuantity(objective.ItemId) : 0;
                        break;
                    case DreamyQuestObjectiveKind.ReachLevel:
                        value = experience != null ? experience.Level : 1;
                        break;
                    case DreamyQuestObjectiveKind.EarnCurrency:
                        value = progression != null ? progression.Coins : 0;
                        break;
                    case DreamyQuestObjectiveKind.OwnUnlockToken:
                        value = progression != null ? progression.UnlockTokens : 0;
                        break;
                }

                value = Mathf.Clamp(value, 0, objective.RequiredAmount);
                changed |= progress[i] != value;
                progress[i] = value;
            }

            return changed;
        }

        private bool CompleteReadyQuests()
        {
            bool changed = false;
            for (int i = activeQuests.Count - 1; i >= 0; i--)
            {
                DreamyQuestDefinition quest = activeQuests[i];
                if (!IsQuestComplete(quest))
                {
                    continue;
                }

                activeQuests.RemoveAt(i);
                completedQuests.Add(quest);
                ApplyRewards(quest);
                changed = true;
            }

            return changed;
        }

        private bool IsQuestComplete(DreamyQuestDefinition quest)
        {
            if (quest == null || quest.Objectives.Count == 0)
            {
                return false;
            }

            int[] progress = EnsureProgress(quest);
            for (int i = 0; i < quest.Objectives.Count; i++)
            {
                if (progress[i] < quest.Objectives[i].RequiredAmount)
                {
                    return false;
                }
            }

            return true;
        }

        private void ApplyRewards(DreamyQuestDefinition quest)
        {
            if (quest == null || quest.Rewards == null)
            {
                return;
            }

            DreamyQuestRewardDefinition rewards = quest.Rewards;
            if (experience != null)
            {
                experience.AddExperience(rewards.Experience);
            }

            if (progression != null)
            {
                progression.AddCoins(rewards.Coins);
                progression.AddPremiumCurrency(rewards.PremiumCurrency);
                progression.AddSkillPoints(rewards.SkillPoints);
                progression.AddUnlockTokens(rewards.UnlockTokens);
                progression.Unlock(rewards.UnlockId);
            }

            if (inventory != null)
            {
                for (int i = 0; i < rewards.Items.Count; i++)
                {
                    DreamyItemStack item = rewards.Items[i];
                    inventory.AddItem(item.ItemId, item.Quantity, item.DisplayName);
                }
            }
        }

        private int[] EnsureProgress(DreamyQuestDefinition quest)
        {
            string id = quest != null ? quest.QuestId : string.Empty;
            if (string.IsNullOrWhiteSpace(id))
            {
                id = "runtime-quest";
            }

            int objectiveCount = quest != null ? quest.Objectives.Count : 0;
            if (!progressByQuestId.TryGetValue(id, out int[] progress) || progress.Length != objectiveCount)
            {
                progress = new int[objectiveCount];
                progressByQuestId[id] = progress;
            }

            return progress;
        }

        private static bool MatchesMonster(DreamyQuestObjectiveDefinition objective, DreamyMonsterController monster)
        {
            if (objective == null || monster == null || string.IsNullOrWhiteSpace(objective.TargetId))
            {
                return true;
            }

            return string.Equals(objective.TargetId, monster.MonsterDisplayName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(objective.TargetId, monster.name, StringComparison.OrdinalIgnoreCase);
        }

        private static bool ContainsQuest(List<DreamyQuestDefinition> quests, DreamyQuestDefinition quest)
        {
            if (quest == null)
            {
                return false;
            }

            for (int i = 0; i < quests.Count; i++)
            {
                if (quests[i] == quest || (quests[i] != null && quests[i].QuestId == quest.QuestId))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
