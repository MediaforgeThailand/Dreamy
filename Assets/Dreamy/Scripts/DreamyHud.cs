using UnityEngine;
using UnityEngine.UI;

namespace Dreamy
{
    public sealed class DreamyHud : MonoBehaviour
    {
        [SerializeField] private Text woodText;
        [SerializeField] private Text goldText;
        [SerializeField] private Text foodText;

        private void OnEnable()
        {
            if (DreamyGameState.Instance != null)
            {
                DreamyGameState.Instance.ResourcesChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (DreamyGameState.Instance != null)
            {
                DreamyGameState.Instance.ResourcesChanged -= Refresh;
            }
        }

        public void Bind(Text wood, Text gold, Text food)
        {
            woodText = wood;
            goldText = gold;
            foodText = food;
            Refresh();
        }

        private void Refresh()
        {
            DreamyGameState state = DreamyGameState.Instance;
            if (state == null)
            {
                return;
            }

            if (woodText != null)
            {
                woodText.text = $"Wood {state.Wood}";
            }

            if (goldText != null)
            {
                goldText.text = $"Gold {state.Gold}";
            }

            if (foodText != null)
            {
                foodText.text = $"Food {state.Food}";
            }
        }
    }
}
