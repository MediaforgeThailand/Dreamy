using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamy.Extraction
{
    [Serializable]
    public sealed class ExtractionRoomChoice
    {
        [SerializeField] private ExtractionRoomData room;

        public ExtractionRoomChoice(ExtractionRoomData room)
        {
            this.room = room;
        }

        public ExtractionRoomData Room => room;
        public string RewardPreview => room != null ? room.RewardPreview : string.Empty;
        public string RiskPreview => room != null ? room.RiskPreview : string.Empty;
    }

    public sealed class ExtractionRoomFlowController : MonoBehaviour
    {
        [SerializeField] private ExtractionMapData mapData;
        [SerializeField] private ExtractionRoomData currentRoom;
        [SerializeField] private Transform rewardDropPoint;
        [SerializeField] private ExtractionLootPickup rewardPickupPrefab;
        [SerializeField] private List<ExtractionRoomChoice> currentChoices = new List<ExtractionRoomChoice>();

        public event Action<ExtractionRoomData> RoomEntered;
        public event Action<ExtractionRoomData> RoomCleared;
        public event Action<IReadOnlyList<ExtractionRoomChoice>> ChoicesChanged;

        private readonly HashSet<ExtractionEnemyController> liveEnemies = new HashSet<ExtractionEnemyController>();
        private int roomsCleared;

        public ExtractionMapData MapData => mapData;
        public ExtractionRoomData CurrentRoom => currentRoom;
        public int CurrentDepth => roomsCleared;
        public IReadOnlyList<ExtractionRoomChoice> CurrentChoices => currentChoices;

        public void Configure(ExtractionMapData data, Transform dropPoint)
        {
            mapData = data;
            rewardDropPoint = dropPoint != null ? dropPoint : transform;
            if (mapData != null && currentRoom == null && mapData.StartRoom != null)
            {
                EnterRoom(mapData.StartRoom);
            }
        }

        private void Start()
        {
            if (currentRoom == null && mapData != null && mapData.StartRoom != null)
            {
                EnterRoom(mapData.StartRoom);
            }
        }

        public void EnterRoom(ExtractionRoomData room)
        {
            currentRoom = room;
            liveEnemies.Clear();
            currentChoices.Clear();
            RoomEntered?.Invoke(currentRoom);
            ChoicesChanged?.Invoke(currentChoices);
        }

        public void RegisterEnemy(ExtractionEnemyController enemy)
        {
            if (enemy != null)
            {
                liveEnemies.Add(enemy);
            }
        }

        public void NotifyEnemyDefeated(ExtractionEnemyController enemy)
        {
            if (enemy != null)
            {
                liveEnemies.Remove(enemy);
            }

            if (liveEnemies.Count == 0)
            {
                ClearCurrentRoom();
            }
        }

        public void ClearCurrentRoom()
        {
            if (currentRoom == null)
            {
                return;
            }

            RoomCleared?.Invoke(currentRoom);
            SpawnRoomReward(currentRoom);
            roomsCleared++;
            BuildRoomChoices();
        }

        public void ChooseRoom(int choiceIndex)
        {
            if (choiceIndex < 0 || choiceIndex >= currentChoices.Count)
            {
                return;
            }

            EnterRoom(currentChoices[choiceIndex].Room);
        }

        private void BuildRoomChoices()
        {
            currentChoices.Clear();
            if (mapData == null)
            {
                ChoicesChanged?.Invoke(currentChoices);
                return;
            }

            if (roomsCleared >= mapData.RoomsBeforeBoss && mapData.BossRoom != null)
            {
                currentChoices.Add(new ExtractionRoomChoice(mapData.BossRoom));
            }

            IReadOnlyList<ExtractionRoomData> pool = mapData.RoomPool;
            int maxChoices = Mathf.Clamp(mapData.ChoicesPerRoom, 2, 3);
            int safety = 0;
            while (currentChoices.Count < maxChoices && pool.Count > 0 && safety < 50)
            {
                safety++;
                ExtractionRoomData candidate = pool[UnityEngine.Random.Range(0, pool.Count)];
                if (candidate != null && candidate != currentRoom && !ContainsChoice(candidate))
                {
                    currentChoices.Add(new ExtractionRoomChoice(candidate));
                }
            }

            ChoicesChanged?.Invoke(currentChoices);
        }

        private bool ContainsChoice(ExtractionRoomData room)
        {
            for (int i = 0; i < currentChoices.Count; i++)
            {
                if (currentChoices[i].Room == room)
                {
                    return true;
                }
            }

            return false;
        }

        private void SpawnRoomReward(ExtractionRoomData room)
        {
            if (room == null)
            {
                return;
            }

            Vector3 dropPosition = rewardDropPoint != null ? rewardDropPoint.position : transform.position;
            ExtractionLootSpawner spawner = GetComponent<ExtractionLootSpawner>();
            if (spawner != null)
            {
                spawner.SpawnLoot(room.RoomRewardTable, dropPosition);
            }

            if (room.IsBossRoom && room.MapUnlockItem != null)
            {
                ExtractionLootPickup pickup = rewardPickupPrefab != null
                    ? Instantiate(rewardPickupPrefab, dropPosition, Quaternion.identity)
                    : CreateDefaultBossUnlockPickup(dropPosition);
                pickup.SetItems(new[] { new ExtractionItemStack(room.MapUnlockItem, 1) });
            }
        }

        private static ExtractionLootPickup CreateDefaultBossUnlockPickup(Vector3 position)
        {
            GameObject drop = new GameObject("Extraction Boss Unlock Drop");
            drop.transform.position = position;
            CircleCollider2D collider = drop.AddComponent<CircleCollider2D>();
            collider.radius = 0.42f;
            collider.isTrigger = true;
            SpriteRenderer renderer = drop.AddComponent<SpriteRenderer>();
            renderer.sprite = ExtractionPlaceholderSprite.Get(new Color(0.6f, 0.35f, 1f, 1f));
            renderer.sortingOrder = 26;
            return drop.AddComponent<ExtractionLootPickup>();
        }
    }
}
