using UnityEngine;

namespace Dreamy
{
    public abstract class DreamyPrototypeInteractable : MonoBehaviour
    {
        [SerializeField] private float interactionRadius = 1.25f;
        [SerializeField] private string interactionLabel = "Use";

        public virtual float InteractionRadius => Mathf.Max(0.1f, interactionRadius);
        public virtual string InteractionLabel => string.IsNullOrWhiteSpace(interactionLabel) ? "Use" : interactionLabel;
        public virtual Vector3 InteractionPosition => transform.position;

        public abstract bool Interact(DreamyMobilePlayer player);

        protected void SetInteractionLabel(string value)
        {
            interactionLabel = value;
        }

        protected virtual void OnValidate()
        {
            interactionRadius = Mathf.Max(0.1f, interactionRadius);
        }
    }
}
