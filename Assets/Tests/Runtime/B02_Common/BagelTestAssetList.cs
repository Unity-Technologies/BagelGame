using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [CreateAssetMenu(fileName = "BagelTestAssetList", menuName = "Bagel/Bagel Test Asset List")]
    public class BagelTestAssetList : ScriptableObject
    {
        public string mainSceneName = "Main";

        [Header("3 - Main Menu")]
        public VisualTreeAsset mainMenuUxml;

        [Header("4 - Settings")]
        public VisualTreeAsset settingsPaneUxml;
        public VisualTreeAsset settingsPaneForGameUxml;
        public VisualTreeAsset settingsPaneForUIUxml;

        [Header("6 - Bagel Tracker")]
        public VisualTreeAsset bagelTrackerLeftDisplayUxml;
        public VisualTreeAsset bagelTrackerRightDisplayUxml;

        [Header("7 - Pause")]
        public VisualTreeAsset pauseScreenUxml;

        [Header("8 - Game Over")]
        public VisualTreeAsset gameOverScreenUxml;
    }
}
