using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Dreamy.Extraction
{
    public sealed class ExtractionPlayerHud : MonoBehaviour
    {
        [SerializeField] private Image healthFill;
        [SerializeField] private Image staminaFill;
        [SerializeField] private Image weaponDurabilityFill;
        [SerializeField] private Text healthLabel;
        [SerializeField] private Text staminaLabel;
        [SerializeField] private Text weaponLabel;
        [SerializeField] private Text roomLabel;
        [SerializeField] private Text runInventoryLabel;
        [SerializeField] private Text baseStorageLabel;
        [SerializeField] private Text messageLabel;
        [SerializeField] private ExtractionHealth playerHealth;
        [SerializeField] private ExtractionStamina playerStamina;
        [SerializeField] private ExtractionWeaponController weapon;
        [SerializeField] private ExtractionRunInventory runInventory;
        [SerializeField] private ExtractionBaseStorage baseStorage;
        [SerializeField] private ExtractionRoomFlowController roomFlow;

        private readonly StringBuilder builder = new StringBuilder(256);
        private string message;
        private float messageUntil;

        private void OnEnable()
        {
            ExtractionLootPickup.PickedUp += HandleLootPickedUp;
            ExtractionLootPickup.PickupRejected += HandleLootRejected;
        }

        private void OnDisable()
        {
            ExtractionLootPickup.PickedUp -= HandleLootPickedUp;
            ExtractionLootPickup.PickupRejected -= HandleLootRejected;
        }

        public void Bind(
            ExtractionHealth health,
            ExtractionStamina stamina,
            ExtractionWeaponController weaponController,
            ExtractionRunInventory inventory,
            ExtractionBaseStorage storage,
            ExtractionRoomFlowController flow)
        {
            playerHealth = health;
            playerStamina = stamina;
            weapon = weaponController;
            runInventory = inventory;
            baseStorage = storage;
            roomFlow = flow;
            Refresh();
        }

        public void BindViews(
            Image healthBar,
            Image staminaBar,
            Image durabilityBar,
            Text healthText,
            Text staminaText,
            Text weaponText,
            Text roomText,
            Text runInventoryText,
            Text baseStorageText,
            Text messageText)
        {
            healthFill = healthBar;
            staminaFill = staminaBar;
            weaponDurabilityFill = durabilityBar;
            healthLabel = healthText;
            staminaLabel = staminaText;
            weaponLabel = weaponText;
            roomLabel = roomText;
            runInventoryLabel = runInventoryText;
            baseStorageLabel = baseStorageText;
            messageLabel = messageText;
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            RefreshVitals();
            RefreshWeapon();
            RefreshRoom();
            RefreshInventory();
            RefreshMessage();
        }

        private void RefreshVitals()
        {
            if (playerHealth != null)
            {
                SetFill(healthFill, playerHealth.NormalizedHealth);
                SetText(healthLabel, "HP " + Mathf.CeilToInt(playerHealth.CurrentHealth) + "/" + Mathf.CeilToInt(playerHealth.MaxHealth));
            }

            if (playerStamina != null)
            {
                SetFill(staminaFill, playerStamina.NormalizedStamina);
                SetText(staminaLabel, "STA " + Mathf.CeilToInt(playerStamina.CurrentStamina) + "/" + Mathf.CeilToInt(playerStamina.MaxStamina));
            }
        }

        private void RefreshWeapon()
        {
            if (weapon == null)
            {
                return;
            }

            SetFill(weaponDurabilityFill, weapon.MaxDurability > 0 ? (float)weapon.CurrentDurability / weapon.MaxDurability : 0f);
            string weaponName = weapon.ActiveWeapon != null && weapon.ActiveWeapon.Item != null
                ? weapon.ActiveWeapon.Item.DisplayName
                : "Unarmed";
            SetText(weaponLabel, weaponName + " DUR " + weapon.CurrentDurability + "/" + weapon.MaxDurability);
        }

        private void RefreshRoom()
        {
            if (roomFlow == null || roomLabel == null)
            {
                return;
            }

            string roomName = roomFlow.CurrentRoom != null ? roomFlow.CurrentRoom.DisplayName : "No Room";
            roomLabel.text = "Room " + (roomFlow.CurrentDepth + 1) + ": " + roomName;
        }

        private void RefreshInventory()
        {
            SetText(runInventoryLabel, BuildInventoryText("Run Inventory", runInventory != null ? runInventory.Items : null, 6));
            SetText(baseStorageLabel, BuildInventoryText("Base Storage", baseStorage != null ? baseStorage.Items : null, 6));
        }

        private void RefreshMessage()
        {
            if (messageLabel == null)
            {
                return;
            }

            if (Time.time > messageUntil)
            {
                messageLabel.text = string.Empty;
                return;
            }

            messageLabel.text = message;
        }

        private string BuildInventoryText(string title, IReadOnlyList<ExtractionItemStack> items, int maxRows)
        {
            builder.Clear();
            int count = items != null ? items.Count : 0;
            builder.Append(title);
            builder.Append(" (");
            builder.Append(count);
            builder.AppendLine(")");

            if (count == 0)
            {
                builder.Append("- Empty");
                return builder.ToString();
            }

            int rows = Mathf.Min(count, maxRows);
            for (int i = 0; i < rows; i++)
            {
                ExtractionItemStack stack = items[i];
                builder.Append("- ");
                builder.Append(stack.Item != null ? stack.Item.DisplayName : "Unknown");
                builder.Append(" x");
                builder.Append(stack.Quantity);
                if (i < rows - 1)
                {
                    builder.AppendLine();
                }
            }

            if (count > rows)
            {
                builder.AppendLine();
                builder.Append("+ ");
                builder.Append(count - rows);
                builder.Append(" more");
            }

            return builder.ToString();
        }

        private void HandleLootPickedUp(ExtractionRunInventory inventory, IReadOnlyList<ExtractionItemStack> items)
        {
            if (inventory != runInventory)
            {
                return;
            }

            ShowMessage("Picked up " + BuildStackSummary(items));
        }

        private void HandleLootRejected(ExtractionRunInventory inventory, IReadOnlyList<ExtractionItemStack> items)
        {
            if (inventory != runInventory)
            {
                return;
            }

            ShowMessage("Inventory full: " + BuildStackSummary(items));
        }

        private void ShowMessage(string text)
        {
            message = text;
            messageUntil = Time.time + 2.4f;
        }

        private string BuildStackSummary(IReadOnlyList<ExtractionItemStack> items)
        {
            if (items == null || items.Count == 0)
            {
                return "loot";
            }

            ExtractionItemStack first = items[0];
            string name = first.Item != null ? first.Item.DisplayName : "loot";
            if (items.Count == 1)
            {
                return name + " x" + first.Quantity;
            }

            return name + " +" + (items.Count - 1) + " more";
        }

        private static void SetFill(Image image, float value)
        {
            if (image != null)
            {
                image.fillAmount = Mathf.Clamp01(value);
            }
        }

        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }
    }
}
