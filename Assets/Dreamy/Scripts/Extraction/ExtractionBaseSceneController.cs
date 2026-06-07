using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Text;

namespace Dreamy.Extraction
{
    public sealed class ExtractionBaseSceneController : MonoBehaviour
    {
        [SerializeField] private string runSceneName = "Prototype_Run";
        [SerializeField] private Text storageText;

        private readonly StringBuilder builder = new StringBuilder(256);

        private void Start()
        {
            ExtractionGameSession.GetOrCreate();
            RefreshStorageText();
        }

        private void Update()
        {
            RefreshStorageText();
        }

        public void StartExpedition()
        {
            if (!string.IsNullOrWhiteSpace(runSceneName))
            {
                SceneManager.LoadScene(runSceneName);
            }
        }

        public void Bind(Text storageLabel, string runScene)
        {
            storageText = storageLabel;
            runSceneName = string.IsNullOrWhiteSpace(runScene) ? runSceneName : runScene;
            RefreshStorageText();
        }

        private void RefreshStorageText()
        {
            if (storageText == null || ExtractionGameSession.Instance == null)
            {
                return;
            }

            ExtractionBaseStorage storage = ExtractionGameSession.Instance.BaseStorage;
            int lostLootCount = ExtractionGameSession.Instance.LostLootRecords.Count;
            builder.Clear();
            builder.AppendLine("Base Storage");
            if (storage == null || storage.Items.Count == 0)
            {
                builder.AppendLine("- Empty");
            }
            else
            {
                int rows = Mathf.Min(storage.Items.Count, 8);
                for (int i = 0; i < rows; i++)
                {
                    ExtractionItemStack stack = storage.Items[i];
                    builder.Append("- ");
                    builder.Append(stack.Item != null ? stack.Item.DisplayName : "Unknown");
                    builder.Append(" x");
                    builder.AppendLine(stack.Quantity.ToString());
                }

                if (storage.Items.Count > rows)
                {
                    builder.Append("+ ");
                    builder.Append(storage.Items.Count - rows);
                    builder.AppendLine(" more");
                }
            }

            builder.AppendLine();
            builder.Append("Lost Loot Records: ");
            builder.Append(lostLootCount);
            storageText.text = builder.ToString();
        }
    }
}
