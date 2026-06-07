using UnityEngine;

namespace Dreamy
{
    using System;

    public enum DreamyResourceType
    {
        Wood,
        Gold,
        Food
    }

    public sealed class DreamyResourceNode : MonoBehaviour
    {
        [SerializeField] private DreamyResourceType resourceType;
        [SerializeField] private int amount = 1;
        [SerializeField] private float collectRadius = 0.9f;

        public static event Action<DreamyResourceType, int> ResourceCollected;

        private bool collected;
        private SpriteRenderer spriteRenderer;

        public DreamyResourceType ResourceType => resourceType;
        public float CollectRadius => collectRadius;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public bool TryCollect(Transform collector)
        {
            if (collected || collector == null)
            {
                return false;
            }

            float distance = Vector2.Distance(transform.position, collector.position);
            if (distance > collectRadius)
            {
                return false;
            }

            collected = true;
            DreamyGameState.Instance?.AddResource(resourceType, amount);
            ResourceCollected?.Invoke(resourceType, amount);
            DreamyInventory inventory = collector.GetComponentInParent<DreamyInventory>();
            if (inventory != null)
            {
                inventory.AddItem(ToItemId(resourceType), amount, resourceType.ToString());
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 1f, 1f, 0.35f);
            }

            return true;
        }

        private static DreamyItemId ToItemId(DreamyResourceType type)
        {
            switch (type)
            {
                case DreamyResourceType.Wood:
                    return DreamyItemId.Wood;
                case DreamyResourceType.Gold:
                    return DreamyItemId.Gold;
                case DreamyResourceType.Food:
                    return DreamyItemId.Food;
                default:
                    return DreamyItemId.Custom;
            }
        }
    }
}
