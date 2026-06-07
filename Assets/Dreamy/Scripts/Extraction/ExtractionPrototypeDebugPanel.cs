using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Dreamy.Extraction
{
    public sealed class ExtractionPrototypeDebugPanel : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private ExtractionHealth playerHealth;
        [SerializeField] private ExtractionWeaponController weapon;
        [SerializeField] private ExtractionRunInventory runInventory;
        [SerializeField] private ExtractionRoomFlowController roomFlow;

        private readonly StringBuilder builder = new StringBuilder(256);

        public void Bind(
            Text textLabel,
            ExtractionHealth health,
            ExtractionWeaponController weaponController,
            ExtractionRunInventory inventory,
            ExtractionRoomFlowController flow)
        {
            label = textLabel;
            playerHealth = health;
            weapon = weaponController;
            runInventory = inventory;
            roomFlow = flow;
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (label == null)
            {
                return;
            }

            builder.Clear();
            if (playerHealth != null)
            {
                builder.Append("HP: ");
                builder.Append(Mathf.CeilToInt(playerHealth.CurrentHealth));
                builder.Append("/");
                builder.Append(Mathf.CeilToInt(playerHealth.MaxHealth));
                builder.AppendLine();
            }

            if (weapon != null)
            {
                builder.Append("Weapon Durability: ");
                builder.Append(weapon.CurrentDurability);
                builder.Append("/");
                builder.Append(weapon.MaxDurability);
                builder.AppendLine();
            }

            if (roomFlow != null && roomFlow.CurrentRoom != null)
            {
                builder.Append("Room: ");
                builder.Append(roomFlow.CurrentRoom.DisplayName);
                builder.AppendLine();
            }

            builder.Append("Run Inventory Slots: ");
            builder.Append(runInventory != null ? runInventory.Items.Count : 0);
            builder.AppendLine();

            if (ExtractionGameSession.Instance != null && ExtractionGameSession.Instance.BaseStorage != null)
            {
                builder.Append("Base Storage Slots: ");
                builder.Append(ExtractionGameSession.Instance.BaseStorage.Items.Count);
                builder.AppendLine();
            }

            label.text = builder.ToString();
        }
    }
}
