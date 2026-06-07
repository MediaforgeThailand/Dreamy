using System.Collections.Generic;
using UnityEngine;

namespace Dreamy.Extraction
{
    public sealed class ExtractionQuestLog : MonoBehaviour
    {
        [SerializeField] private List<ExtractionQuestData> activeQuests = new List<ExtractionQuestData>();
        [SerializeField] private List<ExtractionQuestData> completedQuests = new List<ExtractionQuestData>();

        public IReadOnlyList<ExtractionQuestData> ActiveQuests => activeQuests;
        public IReadOnlyList<ExtractionQuestData> CompletedQuests => completedQuests;

        public bool TryAcceptQuest(ExtractionQuestData quest)
        {
            if (quest == null || activeQuests.Contains(quest) || completedQuests.Contains(quest))
            {
                return false;
            }

            activeQuests.Add(quest);
            return true;
        }

        public bool TryCompleteQuest(ExtractionQuestData quest, ExtractionBaseStorage storage)
        {
            if (quest == null || storage == null || !activeQuests.Contains(quest))
            {
                return false;
            }

            if (!storage.Container.ConsumeItems(quest.RequiredItems))
            {
                return false;
            }

            storage.Container.AddItems(quest.Rewards);
            activeQuests.Remove(quest);
            completedQuests.Add(quest);
            return true;
        }
    }
}
