using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [CreateAssetMenu(fileName = "BagelTestAssetList", menuName = "Bagel/Bagel Test Asset List")]
    public class BagelTestAssetList : ScriptableObject
    {
        public string mainSceneName = "Main";

        [Header("Main Menu")]
        public VisualTreeAsset mainMenuUxml;

        [Header("Bagel Tracker")]
        public VisualTreeAsset bagelTrackerLeftDisplayUxml;
        public VisualTreeAsset bagelTrackerRightDisplayUxml;

        [Header("Pause Screen")]
        public VisualTreeAsset pauseScreenUxml;
    }
}
