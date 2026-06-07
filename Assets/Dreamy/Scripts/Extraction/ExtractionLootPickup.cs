using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamy.Extraction
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class ExtractionLootPickup : MonoBehaviour
    {
        [SerializeField] private List<ExtractionItemStack> items = new List<ExtractionItemStack>();
        [SerializeField] private float pickupRadius = 0.8f;
        [SerializeField] private bool destroyOnPickup = true;

        public static event Action<ExtractionRunInventory, IReadOnlyList<ExtractionItemStack>> PickedUp;
        public static event Action<ExtractionRunInventory, IReadOnlyList<ExtractionItemStack>> PickupRejected;

        private bool pickedUp;

        public IReadOnlyList<ExtractionItemStack> Items => items;

        private void Awake()
        {
            Collider2D hitbox = GetComponent<Collider2D>();
            hitbox.isTrigger = true;
        }

        public void SetItems(IEnumerable<ExtractionItemStack> newItems)
        {
            items.Clear();
            if (newItems == null)
            {
                return;
            }

            foreach (ExtractionItemStack item in newItems)
            {
                if (item != null && item.IsValid)
                {
                    items.Add(item.Clone());
                }
            }
        }

        public bool TryPickup(ExtractionRunInventory runInventory)
        {
            if (pickedUp || runInventory == null || !IsCollectorInRange(runInventory.transform))
            {
                return false;
            }

            if (!runInventory.AddItems(items))
            {
                PickupRejected?.Invoke(runInventory, items);
                return false;
            }

            pickedUp = true;
            PickedUp?.Invoke(runInventory, items);
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
            ExtractionRunInventory runInventory = other.GetComponentInParent<ExtractionRunInventory>();
            TryPickup(runInventory);
        }

        private bool IsCollectorInRange(Transform collector)
        {
            return collector != null && Vector2.Distance(transform.position, collector.position) <= pickupRadius;
        }

        private void OnValidate()
        {
            pickupRadius = Mathf.Max(0.05f, pickupRadius);
            items.RemoveAll(item => item == null || !item.IsValid);
        }
    }
}
