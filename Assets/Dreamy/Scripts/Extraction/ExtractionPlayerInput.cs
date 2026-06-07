using UnityEngine;

namespace Dreamy.Extraction
{
    public sealed class ExtractionPlayerInput : MonoBehaviour, IExtractionInputSource
    {
        [SerializeField] private DreamyVirtualJoystick joystick;
        [SerializeField] private bool readKeyboard = true;

        private bool queuedAttack;
        private bool queuedDodge;
        private bool queuedSkill;
        private bool queuedInteract;

        public void BindJoystick(DreamyVirtualJoystick movementJoystick)
        {
            joystick = movementJoystick;
        }

        public Vector2 ReadMovement()
        {
            Vector2 input = joystick != null ? joystick.Direction : Vector2.zero;
            if (readKeyboard && input.sqrMagnitude < 0.01f)
            {
                input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            }

            return Vector2.ClampMagnitude(input, 1f);
        }

        public void QueueAttack()
        {
            queuedAttack = true;
        }

        public void QueueDodge()
        {
            queuedDodge = true;
        }

        public void QueueSkill()
        {
            queuedSkill = true;
        }

        public void QueueInteract()
        {
            queuedInteract = true;
        }

        public bool ConsumeAttackPressed()
        {
            bool value = queuedAttack || (readKeyboard && Input.GetKeyDown(KeyCode.J));
            queuedAttack = false;
            return value;
        }

        public bool ConsumeDodgePressed()
        {
            bool value = queuedDodge || (readKeyboard && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.LeftShift)));
            queuedDodge = false;
            return value;
        }

        public bool ConsumeSkillPressed()
        {
            bool value = queuedSkill || (readKeyboard && Input.GetKeyDown(KeyCode.E));
            queuedSkill = false;
            return value;
        }

        public bool ConsumeInteractPressed()
        {
            bool value = queuedInteract || (readKeyboard && Input.GetKeyDown(KeyCode.F));
            queuedInteract = false;
            return value;
        }
    }
}
