using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [CreateAssetMenu(fileName = "BagelTestAssetList", menuName = "Bagel/Bagel Test Asset List")]
    public class BagelTestAssetList : ScriptableObject
    {
        public string mainSceneName = "Main";
        public PlayManagerState startState;

        [Header("0 - Themes")]
        public BagelThemeCollection themes;
        public PanelSettingsCollection panelSettings;

        [Header("3 - Main Menu")]
        public VisualTreeAsset mainMenuUxml;
        public VisualTreeAsset mainMenuScreenUxml;

        [Header("4 - Settings")]
        public VisualTreeAsset settingsPaneUxml;
        public VisualTreeAsset settingsPaneForGameUxml;
        public VisualTreeAsset settingsPaneForUIUxml;
        public VisualTreeAsset settingsPaneForControlsUxml;

        [Header("5 - Bagel Selection")]
        public VisualTreeAsset bagelSelectionControlsUxml;
        public VisualTreeAsset bagelSelectionTypeInfoUxml;

        [Header("6 - Bagel Tracker")]
        public VisualTreeAsset bagelTrackerLeftDisplayUxml;
        public VisualTreeAsset bagelTrackerRightDisplayUxml;

        [Header("7 - Pause")]
        public VisualTreeAsset pauseScreenUxml;
        public VisualTreeAsset pausePaneUxml;

        [Header("8 - Game Over")]
        public VisualTreeAsset gameOverScreenUxml;
        public VisualTreeAsset gameOverPaneUxml;

        [Header("9 - Leaderboard")]
        public VisualTreeAsset leaderboardPaneUxml;
    }
}
