using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Dreamy.Extraction
{
    public sealed class ExtractionRoomChoicePanel : MonoBehaviour
    {
        [SerializeField] private ExtractionRoomFlowController roomFlow;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button[] choiceButtons;
        [SerializeField] private Text[] choiceLabels;

        private bool subscribed;

        private void Awake()
        {
            if (roomFlow == null)
            {
                roomFlow = UnityEngine.Object.FindAnyObjectByType<ExtractionRoomFlowController>();
            }

            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            EnsureCanvasGroup();
        }

        public void Bind(ExtractionRoomFlowController flow, GameObject root, Button[] buttons, Text[] labels)
        {
            Unsubscribe();
            roomFlow = flow;
            panelRoot = root != null ? root : gameObject;
            choiceButtons = buttons;
            choiceLabels = labels;
            EnsureCanvasGroup();
            Subscribe();
            RefreshChoices(roomFlow != null ? roomFlow.CurrentChoices : null);
        }

        private void OnEnable()
        {
            Subscribe();
            RefreshChoices(roomFlow != null ? roomFlow.CurrentChoices : null);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void RefreshChoices(IReadOnlyList<ExtractionRoomChoice> choices)
        {
            bool hasChoices = choices != null && choices.Count > 0;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = hasChoices ? 1f : 0f;
                canvasGroup.blocksRaycasts = hasChoices;
                canvasGroup.interactable = hasChoices;
            }

            int buttonCount = choiceButtons != null ? choiceButtons.Length : 0;
            for (int i = 0; i < buttonCount; i++)
            {
                bool active = hasChoices && i < choices.Count && choices[i].Room != null;
                if (choiceButtons[i] != null)
                {
                    choiceButtons[i].gameObject.SetActive(active);
                    choiceButtons[i].onClick.RemoveAllListeners();
                    int choiceIndex = i;
                    choiceButtons[i].onClick.AddListener(() => roomFlow.ChooseRoom(choiceIndex));
                }

                if (choiceLabels != null && i < choiceLabels.Length && choiceLabels[i] != null)
                {
                    choiceLabels[i].text = active ? BuildChoiceText(choices[i]) : string.Empty;
                }
            }
        }

        private static string BuildChoiceText(ExtractionRoomChoice choice)
        {
            ExtractionRoomData room = choice.Room;
            string reward = string.IsNullOrWhiteSpace(choice.RewardPreview) ? "-" : choice.RewardPreview;
            string risk = string.IsNullOrWhiteSpace(choice.RiskPreview) ? "-" : choice.RiskPreview;
            return $"{room.DisplayName}\nReward: {reward}\nRisk: {risk}";
        }

        private void Subscribe()
        {
            if (subscribed || roomFlow == null)
            {
                return;
            }

            roomFlow.ChoicesChanged += RefreshChoices;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || roomFlow == null)
            {
                subscribed = false;
                return;
            }

            roomFlow.ChoicesChanged -= RefreshChoices;
            subscribed = false;
        }

        private void EnsureCanvasGroup()
        {
            if (panelRoot == null)
            {
                return;
            }

            canvasGroup = panelRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panelRoot.AddComponent<CanvasGroup>();
            }
        }
    }
}
