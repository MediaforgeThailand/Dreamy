using System.Collections.Generic;
using UnityEngine;

namespace Dreamy.Extraction
{
    [CreateAssetMenu(menuName = "Dreamy/Extraction/Quest Data", fileName = "QuestData")]
    public sealed class ExtractionQuestData : ScriptableObject
    {
        [SerializeField] private string questId;
        [SerializeField] private string displayName;
        [SerializeField] private List<ExtractionItemStack> requiredItems = new List<ExtractionItemStack>();
        [SerializeField] private List<ExtractionItemStack> rewards = new List<ExtractionItemStack>();

        public string QuestId => string.IsNullOrWhiteSpace(questId) ? name : questId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public IReadOnlyList<ExtractionItemStack> RequiredItems => requiredItems;
        public IReadOnlyList<ExtractionItemStack> Rewards => rewards;

        private void OnValidate()
        {
            requiredItems.RemoveAll(item => item == null || !item.IsValid);
            rewards.RemoveAll(item => item == null || !item.IsValid);
        }
    }
}
