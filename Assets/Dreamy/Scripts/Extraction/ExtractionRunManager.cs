using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dreamy.Extraction
{
    public sealed class ExtractionRunManager : MonoBehaviour
    {
        [SerializeField] private string baseSceneName = "Prototype_Base";
        [SerializeField] private ExtractionHealth playerHealth;
        [SerializeField] private ExtractionExtractPoint extractPoint;
        [SerializeField] private float returnDelaySeconds = 0.45f;

        private bool returningToBase;

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(string baseScene, ExtractionHealth health, ExtractionExtractPoint point)
        {
            Unsubscribe();
            baseSceneName = string.IsNullOrWhiteSpace(baseScene) ? baseSceneName : baseScene;
            playerHealth = health;
            extractPoint = point;
            Subscribe();
        }

        private void Subscribe()
        {
            if (playerHealth != null)
            {
                playerHealth.Died += HandlePlayerDied;
            }

            if (extractPoint != null)
            {
                extractPoint.Extracted += HandleExtracted;
            }
        }

        private void Unsubscribe()
        {
            if (playerHealth != null)
            {
                playerHealth.Died -= HandlePlayerDied;
            }

            if (extractPoint != null)
            {
                extractPoint.Extracted -= HandleExtracted;
            }
        }

        private void HandleExtracted()
        {
            ReturnToBase();
        }

        private void HandlePlayerDied()
        {
            ReturnToBase();
        }

        private void ReturnToBase()
        {
            if (returningToBase)
            {
                return;
            }

            returningToBase = true;
            StartCoroutine(ReturnAfterDelay());
        }

        private IEnumerator ReturnAfterDelay()
        {
            if (returnDelaySeconds > 0f)
            {
                yield return new WaitForSeconds(returnDelaySeconds);
            }

            if (!string.IsNullOrWhiteSpace(baseSceneName))
            {
                SceneManager.LoadScene(baseSceneName);
            }
        }
    }
}
