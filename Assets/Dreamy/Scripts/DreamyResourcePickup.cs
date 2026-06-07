using System;
using UnityEngine;

namespace Dreamy
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class DreamyResourcePickup : MonoBehaviour
    {
        [SerializeField] private DreamyItemId itemId = DreamyItemId.Wood;
        [SerializeField] private string displayName;
        [SerializeField] private int amount = 1;
        [SerializeField] private int expReward;
        [SerializeField] private float pickupRadius = 0.75f;
        [SerializeField] private bool destroyOnPickup = true;

        public static event Action<DreamyResourcePickup, Transform> PickedUp;
        public static event Action<DreamyResourcePickup, Transform> PickupRejected;

        private bool pickedUp;

        public DreamyItemId ItemId => itemId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? itemId.ToString() : displayName;
        public int Amount => amount;
        public int ExpReward => expReward;
        public float PickupRadius => pickupRadius;

        public void Configure(DreamyItemId id, string itemDisplayName, int itemAmount, int rewardExp, float radius)
        {
            itemId = id;
            displayName = itemDisplayName;
            amount = Mathf.Max(1, itemAmount);
            expReward = Mathf.Max(0, rewardExp);
            pickupRadius = Mathf.Max(0.05f, radius);
        }

        public bool TryPickup(Transform collector)
        {
            if (pickedUp || collector == null)
            {
                return false;
            }

            if (Vector2.Distance(transform.position, collector.position) > pickupRadius)
            {
                return false;
            }

            DreamyInventory inventory = collector.GetComponentInParent<DreamyInventory>();
            if (inventory != null && !inventory.AddItem(itemId, amount, displayName))
            {
                PickupRejected?.Invoke(this, collector);
                return false;
            }

            if (TryConvertToResourceType(itemId, out DreamyResourceType resourceType))
            {
                DreamyGameState.Instance?.AddResource(resourceType, amount);
            }

            DreamyExperience experience = collector.GetComponentInParent<DreamyExperience>();
            if (experience != null)
            {
                experience.AddExperience(expReward);
            }

            pickedUp = true;
            PickedUp?.Invoke(this, collector);
            if (destroyOnPickup)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }

            return true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryPickup(other.transform);
        }

        private static bool TryConvertToResourceType(DreamyItemId itemId, out DreamyResourceType resourceType)
        {
            switch (itemId)
            {
                case DreamyItemId.Wood:
                    resourceType = DreamyResourceType.Wood;
                    return true;
                case DreamyItemId.Gold:
                    resourceType = DreamyResourceType.Gold;
                    return true;
                case DreamyItemId.Food:
                    resourceType = DreamyResourceType.Food;
                    return true;
                default:
                    resourceType = default;
                    return false;
            }
        }

        private void OnValidate()
        {
            amount = Mathf.Max(1, amount);
            expReward = Mathf.Max(0, expReward);
            pickupRadius = Mathf.Max(0.05f, pickupRadius);
        }
    }
}
