using UnityEngine;

namespace Dreamy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class DreamyYSortSprite : MonoBehaviour
    {
        private const int MinSortingOrder = -32768;
        private const int MaxSortingOrder = 32767;

        [SerializeField] private int baseSortingOrder = 100;
        [SerializeField] private float unitsPerWorldUnit = 10f;
        [SerializeField] private float yOffset;

        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            Refresh();
        }

        private void LateUpdate()
        {
            Refresh();
        }

        public void Configure(int sortingBase, float sortingUnitsPerWorldUnit = 10f, float sortingYOffset = 0f)
        {
            baseSortingOrder = sortingBase;
            unitsPerWorldUnit = Mathf.Max(0.01f, sortingUnitsPerWorldUnit);
            yOffset = sortingYOffset;
            Refresh();
        }

        private void Refresh()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer == null)
            {
                return;
            }

            int order = baseSortingOrder - Mathf.RoundToInt((transform.position.y + yOffset) * unitsPerWorldUnit);
            spriteRenderer.sortingOrder = Mathf.Clamp(order, MinSortingOrder, MaxSortingOrder);
        }

        private void OnValidate()
        {
            unitsPerWorldUnit = Mathf.Max(0.01f, unitsPerWorldUnit);
        }
    }
}
