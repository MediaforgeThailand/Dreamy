using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamy.Extraction
{
    [Serializable]
    public sealed class ExtractionLostLootRecord
    {
        [SerializeField] private string mapId;
        [SerializeField] private string roomId;
        [SerializeField] private int roomDepth;
        [SerializeField] private List<ExtractionItemStack> items = new List<ExtractionItemStack>();

        public ExtractionLostLootRecord(string mapId, string roomId, int roomDepth, IEnumerable<ExtractionItemStack> lostItems)
        {
            this.mapId = mapId;
            this.roomId = roomId;
            this.roomDepth = roomDepth;
            SetItems(lostItems);
        }

        public string MapId => mapId;
        public string RoomId => roomId;
        public int RoomDepth => roomDepth;
        public IReadOnlyList<ExtractionItemStack> Items => items;

        private void SetItems(IEnumerable<ExtractionItemStack> lostItems)
        {
            items.Clear();
            if (lostItems == null)
            {
                return;
            }

            foreach (ExtractionItemStack item in lostItems)
            {
                if (item != null && item.IsValid)
                {
                    items.Add(item.Clone());
                }
            }
        }
    }

    public sealed class ExtractionGameSession : MonoBehaviour
    {
        [SerializeField] private ExtractionBaseStorage baseStorage;
        [SerializeField] private List<ExtractionLostLootRecord> lostLootRecords = new List<ExtractionLostLootRecord>();

        public static ExtractionGameSession Instance { get; private set; }

        public ExtractionBaseStorage BaseStorage => baseStorage;
        public IReadOnlyList<ExtractionLostLootRecord> LostLootRecords => lostLootRecords;

        public static ExtractionGameSession GetOrCreate()
        {
            if (Instance != null)
            {
                return Instance;
            }

            ExtractionGameSession existing = UnityEngine.Object.FindAnyObjectByType<ExtractionGameSession>();
            if (existing != null)
            {
                existing.InitializeSingleton();
                return existing;
            }

            GameObject sessionObject = new GameObject("Extraction Game Session");
            return sessionObject.AddComponent<ExtractionGameSession>();
        }

        private void Awake()
        {
            InitializeSingleton();
        }

        public void RecordLostLoot(IEnumerable<ExtractionItemStack> items, ExtractionRoomData room, ExtractionMapData map, int roomDepth)
        {
            List<ExtractionItemStack> snapshot = new List<ExtractionItemStack>();
            if (items != null)
            {
                foreach (ExtractionItemStack item in items)
                {
                    if (item != null && item.IsValid)
                    {
                        snapshot.Add(item.Clone());
                    }
                }
            }

            if (snapshot.Count == 0)
            {
                return;
            }

            string mapId = map != null ? map.MapId : string.Empty;
            string roomId = room != null ? room.RoomId : string.Empty;
            lostLootRecords.Add(new ExtractionLostLootRecord(mapId, roomId, roomDepth, snapshot));
        }

        private void InitializeSingleton()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (baseStorage == null)
            {
                baseStorage = GetComponent<ExtractionBaseStorage>();
            }

            if (baseStorage == null)
            {
                baseStorage = gameObject.AddComponent<ExtractionBaseStorage>();
            }
        }
    }
}
