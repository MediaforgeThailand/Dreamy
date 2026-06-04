using UnityEngine;

namespace Dreamy
{
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

            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 1f, 1f, 0.35f);
            }

            return true;
        }
    }
}
