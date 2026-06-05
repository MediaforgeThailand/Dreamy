using UnityEngine;

namespace Dreamy
{
    public sealed class DreamyMobileOrientation : MonoBehaviour
    {
        [SerializeField] private bool allowBothLandscapeDirections = true;

        private void Awake()
        {
            ApplyLandscapeOrientation();
        }

        private void OnEnable()
        {
            ApplyLandscapeOrientation();
        }

        private void ApplyLandscapeOrientation()
        {
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = allowBothLandscapeDirections;
            Screen.orientation = allowBothLandscapeDirections
                ? ScreenOrientation.AutoRotation
                : ScreenOrientation.LandscapeLeft;
        }
    }
}
