using System;
using UnityEngine;

namespace Dreamy
{
    [RequireComponent(typeof(DreamyMobilePlayer))]
    public sealed class DreamyPrototypeInteraction : MonoBehaviour
    {
        [SerializeField] private float scanRadius = 1.65f;

        public static event Action<string> PromptChanged;
        public static event Action<string> InteractionMessage;

        private DreamyMobilePlayer player;
        private DreamyPrototypeInteractable focused;
        private bool queuedInteract;
        private string lastPrompt;

        public DreamyPrototypeInteractable Focused => focused;

        private void Awake()
        {
            player = GetComponent<DreamyMobilePlayer>();
        }

        private void Update()
        {
            focused = FindFocusedInteractable();
            string prompt = focused != null ? "USE: " + focused.InteractionLabel : string.Empty;
            if (!string.Equals(prompt, lastPrompt, StringComparison.Ordinal))
            {
                lastPrompt = prompt;
                PromptChanged?.Invoke(prompt);
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                QueueInteract();
            }

            if (queuedInteract)
            {
                queuedInteract = false;
                TryInteract();
            }
        }

        public void QueueInteract()
        {
            queuedInteract = true;
        }

        public static void PublishMessage(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                InteractionMessage?.Invoke(message);
            }
        }

        private void TryInteract()
        {
            if (focused == null || player == null)
            {
                PublishMessage("Nothing nearby");
                return;
            }

            if (!focused.Interact(player))
            {
                PublishMessage("Cannot use this yet");
            }
        }

        private DreamyPrototypeInteractable FindFocusedInteractable()
        {
            DreamyPrototypeInteractable[] interactables = FindObjectsByType<DreamyPrototypeInteractable>(FindObjectsInactive.Exclude);
            DreamyPrototypeInteractable best = null;
            float bestDistance = float.MaxValue;
            Vector2 currentPosition = transform.position;

            for (int i = 0; i < interactables.Length; i++)
            {
                DreamyPrototypeInteractable candidate = interactables[i];
                if (candidate == null)
                {
                    continue;
                }

                float allowedRadius = Mathf.Max(scanRadius, candidate.InteractionRadius);
                float distance = Vector2.Distance(currentPosition, candidate.InteractionPosition);
                if (distance <= allowedRadius && distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private void OnValidate()
        {
            scanRadius = Mathf.Max(0.1f, scanRadius);
        }
    }
}
