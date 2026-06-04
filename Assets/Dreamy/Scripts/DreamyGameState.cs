using System;
using UnityEngine;

namespace Dreamy
{
    public sealed class DreamyGameState : MonoBehaviour
    {
        public static DreamyGameState Instance { get; private set; }

        public int Wood { get; private set; }
        public int Gold { get; private set; }
        public int Food { get; private set; }

        public event Action ResourcesChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void AddResource(DreamyResourceType type, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            switch (type)
            {
                case DreamyResourceType.Wood:
                    Wood += amount;
                    break;
                case DreamyResourceType.Gold:
                    Gold += amount;
                    break;
                case DreamyResourceType.Food:
                    Food += amount;
                    break;
            }

            ResourcesChanged?.Invoke();
        }
    }
}
