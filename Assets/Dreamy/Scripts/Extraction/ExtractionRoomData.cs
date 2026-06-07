using System.Collections.Generic;
using UnityEngine;

namespace Dreamy.Extraction
{
    public enum ExtractionRoomType
    {
        Combat,
        Treasure,
        Rest,
        Event,
        Extract,
        Boss
    }

    [CreateAssetMenu(menuName = "Dreamy/Extraction/Room Data", fileName = "RoomData")]
    public sealed class ExtractionRoomData : ScriptableObject
    {
        [SerializeField] private string roomId;
        [SerializeField] private string displayName;
        [SerializeField] private ExtractionRoomType roomType = ExtractionRoomType.Combat;
        [SerializeField, Range(0, 5)] private int riskLevel = 1;
        [SerializeField] private string rewardPreview;
        [SerializeField] private string riskPreview;
        [SerializeField] private List<ExtractionEnemyData> enemyPool = new List<ExtractionEnemyData>();
        [SerializeField] private ExtractionLootTableData roomRewardTable;
        [SerializeField] private ExtractionItemData mapUnlockItem;

        public string RoomId => string.IsNullOrWhiteSpace(roomId) ? name : roomId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public ExtractionRoomType RoomType => roomType;
        public int RiskLevel => riskLevel;
        public string RewardPreview => rewardPreview;
        public string RiskPreview => riskPreview;
        public IReadOnlyList<ExtractionEnemyData> EnemyPool => enemyPool;
        public ExtractionLootTableData RoomRewardTable => roomRewardTable;
        public ExtractionItemData MapUnlockItem => mapUnlockItem;
        public bool IsBossRoom => roomType == ExtractionRoomType.Boss;
    }
}
