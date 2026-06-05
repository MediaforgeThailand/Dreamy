using UnityEngine;

namespace Dreamy
{
    public enum DreamyLevelBlockerKind
    {
        Blocker,
        Water,
        Cliff,
        TreeLine,
        Decoration
    }

    [ExecuteAlways]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class DreamyLevelBlocker : MonoBehaviour
    {
        [SerializeField] private DreamyLevelBlockerKind blockerKind = DreamyLevelBlockerKind.Blocker;
        [SerializeField] private Color editorColor = new Color(0.06f, 0.08f, 0.10f, 0.38f);
        [SerializeField] private bool showGizmo = true;

        public DreamyLevelBlockerKind BlockerKind => blockerKind;

        public void Configure(DreamyLevelBlockerKind kind, Color color)
        {
            blockerKind = kind;
            editorColor = color;
        }

        private void Reset()
        {
            BoxCollider2D blockerCollider = GetComponent<BoxCollider2D>();
            blockerCollider.isTrigger = false;
        }

        private void OnValidate()
        {
            BoxCollider2D blockerCollider = GetComponent<BoxCollider2D>();
            if (blockerCollider != null)
            {
                blockerCollider.isTrigger = false;
            }
        }

        private void OnDrawGizmos()
        {
            if (!showGizmo)
            {
                return;
            }

            BoxCollider2D blockerCollider = GetComponent<BoxCollider2D>();
            if (blockerCollider == null)
            {
                return;
            }

            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = editorColor;
            Gizmos.DrawCube(blockerCollider.offset, blockerCollider.size);
            Gizmos.color = new Color(editorColor.r, editorColor.g, editorColor.b, 0.9f);
            Gizmos.DrawWireCube(blockerCollider.offset, blockerCollider.size);
            Gizmos.matrix = oldMatrix;
        }
    }
}
