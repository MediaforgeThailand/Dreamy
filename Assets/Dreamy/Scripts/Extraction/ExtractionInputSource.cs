using UnityEngine;

namespace Dreamy.Extraction
{
    public interface IExtractionInputSource
    {
        Vector2 ReadMovement();
        bool ConsumeAttackPressed();
        bool ConsumeDodgePressed();
        bool ConsumeSkillPressed();
        bool ConsumeInteractPressed();
    }
}
