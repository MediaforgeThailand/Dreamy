using UnityEngine;
using UnityEngine.EventSystems;

namespace Dreamy.Extraction
{
    public enum ExtractionActionButtonType
    {
        Attack,
        Dodge,
        Skill,
        Interact
    }

    public sealed class ExtractionActionButton : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private ExtractionPlayerInput input;
        [SerializeField] private ExtractionActionButtonType actionType;

        public void Bind(ExtractionPlayerInput playerInput, ExtractionActionButtonType type)
        {
            input = playerInput;
            actionType = type;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (input == null)
            {
                return;
            }

            switch (actionType)
            {
                case ExtractionActionButtonType.Attack:
                    input.QueueAttack();
                    break;
                case ExtractionActionButtonType.Dodge:
                    input.QueueDodge();
                    break;
                case ExtractionActionButtonType.Skill:
                    input.QueueSkill();
                    break;
                case ExtractionActionButtonType.Interact:
                    input.QueueInteract();
                    break;
            }
        }
    }
}
