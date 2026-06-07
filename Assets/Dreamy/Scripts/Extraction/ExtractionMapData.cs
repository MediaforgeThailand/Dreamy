using System.Collections.Generic;
using UnityEngine;

namespace Dreamy.Extraction
{
    [CreateAssetMenu(menuName = "Dreamy/Extraction/Map Data", fileName = "MapData")]
    public sealed class ExtractionMapData : ScriptableObject
    {
        [SerializeField] private string mapId;
        [SerializeField] private string displayName;
        [SerializeField] private ExtractionRoomData startRoom;
        [SerializeField] private List<ExtractionRoomData> roomPool = new List<ExtractionRoomData>();
        [SerializeField] private ExtractionRoomData bossRoom;
        [SerializeField] private ExtractionItemData unlockRequirementItem;
        [SerializeField, Range(2, 3)] private int choicesPerRoom = 3;
        [SerializeField] private int roomsBeforeBoss = 4;

        public string MapId => string.IsNullOrWhiteSpace(mapId) ? name : mapId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public ExtractionRoomData StartRoom => startRoom;
        public IReadOnlyList<ExtractionRoomData> RoomPool => roomPool;
        public ExtractionRoomData BossRoom => bossRoom;
        public ExtractionItemData UnlockRequirementItem => unlockRequirementItem;
        public int ChoicesPerRoom => choicesPerRoom;
        public int RoomsBeforeBoss => roomsBeforeBoss;

        private void OnValidate()
        {
            choicesPerRoom = Mathf.Clamp(choicesPerRoom, 2, 3);
            roomsBeforeBoss = Mathf.Max(0, roomsBeforeBoss);
        }
    }
}
