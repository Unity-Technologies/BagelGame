using Bagel;
using Unity.Properties;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UIElements;

public class BagelAllUIWindow : EditorWindow
{
    [SerializeField] VisualTreeAsset m_VisualTreeAsset;
    [SerializeField] ThemeStyleSheet m_DefaultTheme;
    [SerializeField] BagelType m_BagelType;
    [SerializeField] BagelTrackerData m_BagelTrackerData;

    ThemeStyleSheet m_CurrentTheme;

    [MenuItem("Bagel/Bagel All UI")]
    public static void ShowExample()
    {
        var wnd = GetWindow<BagelAllUIWindow>();
        wnd.titleContent = new GUIContent("Bagel All UI");
    }

    public void CreateGUI()
    {
        // Remove the Editor theme.
        var editorTheme = rootVisualElement.styleSheets[0];
        rootVisualElement.styleSheets.Clear();

        // Create UI.
        m_VisualTreeAsset.CloneTree(rootVisualElement);

        m_CurrentTheme = m_DefaultTheme;
        ApplyRuntimeTheme(m_DefaultTheme);

        SetupScroller(editorTheme);

        BindMainMenuRow();
        BindBagelTrackerRow(editorTheme);
        BindBagelTypeRow(editorTheme);
        BindPauseRow();
        BindGameOverRow(editorTheme);
        BindLeaderboardRow(editorTheme);
    }

    void ApplyRuntimeTheme(ThemeStyleSheet theme)
    {
        rootVisualElement.Query<TemplateContainer>().ForEach(t =>
        {
            t.styleSheets.Remove(m_CurrentTheme);
            t.styleSheets.Add(theme);
        });
        m_CurrentTheme = theme;
    }

    void SetupScroller(StyleSheet editorTheme)
    {
        var rootScrollView = rootVisualElement.Q<ScrollView>("root-scrollview");
        var rootScroller = rootVisualElement.Q<Scroller>("root-scroller");
        rootScroller.styleSheets.Add(editorTheme);

        var rootScrollViewVerticalScroller = rootScrollView.hierarchy.ElementAt(0).hierarchy.ElementAt(1) as Scroller;

        rootScroller.dataSource = rootScrollViewVerticalScroller;
        rootScroller.SetBinding("value", new DataBinding() { dataSourcePath = new PropertyPath("value"), bindingMode = BindingMode.TwoWay });
        rootScroller.SetBinding("lowValue", new DataBinding() { dataSourcePath = new PropertyPath("lowValue") });
        rootScroller.SetBinding("highValue", new DataBinding() { dataSourcePath = new PropertyPath("highValue") });
    }

    void BindMainMenuRow()
    {
        var manager = rootVisualElement.Q<MainMenuScreenManager>();
        var uiSettings = manager.Q<SettingsPaneManagerForUI>();
        uiSettings.OnThemeChange += (_, theme) => ApplyRuntimeTheme(theme);
    }

    void BindBagelTrackerRow(StyleSheet editorTheme)
    {
        var bagelTrackerDataInspector = new InspectorElement(m_BagelTrackerData);
        bagelTrackerDataInspector.styleSheets.Add(editorTheme);
        var bagelTrackerDataRow = rootVisualElement.Q<VisualElement>("bagel-tracker-row");
        bagelTrackerDataRow.Add(bagelTrackerDataInspector);
    }

    void BindBagelTypeRow(StyleSheet editorTheme)
    {
        var bagelTypeInfoInspector = new InspectorElement(m_BagelType);
        bagelTypeInfoInspector.styleSheets.Add(editorTheme);
        var bagelTypeInfoContainer = rootVisualElement.Q<VisualElement>("bagel-selection-type-info");
        bagelTypeInfoContainer.Add(bagelTypeInfoInspector);
    }

    void BindPauseRow()
    {
        var manager = rootVisualElement.Q<PauseScreenManager>();
        var uiSettings = manager.Q<SettingsPaneManagerForUI>();
        uiSettings.OnThemeChange += (_, theme) => ApplyRuntimeTheme(theme);
    }

    void BindGameOverRow(StyleSheet editorTheme)
    {
        var gameOverScreen = rootVisualElement.Q<VisualElement>("game-over");
        var gameOverScreenManager = gameOverScreen.Q<GameOverScreenManager>();
        var leaderboardManager = gameOverScreen.Q<LeaderboardManager>();
        var toppingsLeftField = gameOverScreen.Q<IntegerField>("toppings-number-field");
        var leaderboardForm = gameOverScreen.Q<VisualElement>("leaderboard-form");

        var gameOverControls = rootVisualElement.Q<VisualElement>("game-over-controls");
        gameOverControls.styleSheets.Add(editorTheme);
        var toppingsLeftResetField = gameOverControls.Q<IntegerField>("toppings-left-reset-field");
        var gameOverResetButton = gameOverControls.Q<Button>("game-over-reset-button");

        gameOverResetButton.clicked += () =>
        {
            toppingsLeftField.value = toppingsLeftResetField.value;
            leaderboardForm.SetEnabled(true);
        };
    }

    void BindLeaderboardRow(StyleSheet editorTheme)
    {
        var leaderboardRow = rootVisualElement.Q<VisualElement>("leaderboard-row");
        var leaderboardManager = leaderboardRow.Q<LeaderboardManager>();
        var leaderboardDataInspector = new InspectorElement(leaderboardManager.leaderboardData);
        leaderboardDataInspector.styleSheets.Add(editorTheme);
        leaderboardRow.Add(leaderboardDataInspector);
    }
}
