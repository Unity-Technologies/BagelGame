using Bagel;
using Unity.Properties;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UIElements;

public class BagelAllUIWindow : EditorWindow
{
    [SerializeField] private VisualTreeAsset m_VisualTreeAsset = default;
    [SerializeField] private StyleSheet m_Theme = default;
    [SerializeField] private BagelType m_BagelType = default;
    [SerializeField] private BagelTrackerData m_BagelTrackerData = default;

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

        // Add runtime theme to all game screens.
        rootVisualElement.Query<TemplateContainer>().ForEach(t =>
        {
           t.styleSheets.Add(m_Theme);
        });

        SetUpScroller(editorTheme);

        SetUpBagelTrackerInspector(editorTheme);
        SetUpBagelTypeInspector(editorTheme);
        SetUpGameOverControls(editorTheme);
        SetUpLeaderboardInspector(editorTheme);
    }

    void SetUpScroller(StyleSheet editorTheme)
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

    void SetUpBagelTrackerInspector(StyleSheet editorTheme)
    {
        var bagelTrackerDataInspector = new InspectorElement(m_BagelTrackerData);
        bagelTrackerDataInspector.styleSheets.Add(editorTheme);
        var bagelTrackerDataRow = rootVisualElement.Q<VisualElement>("bagel-tracker-row");
        bagelTrackerDataRow.Add(bagelTrackerDataInspector);
    }

    void SetUpBagelTypeInspector(StyleSheet editorTheme)
    {
        var bagelTypeInfoInspector = new InspectorElement(m_BagelType);
        bagelTypeInfoInspector.styleSheets.Add(editorTheme);
        var bagelTypeInfoContainer = rootVisualElement.Q<VisualElement>("bagel-selection-type-info");
        bagelTypeInfoContainer.Add(bagelTypeInfoInspector);
    }

    void SetUpGameOverControls(StyleSheet editorTheme)
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

    void SetUpLeaderboardInspector(StyleSheet editorTheme)
    {
        var leaderboardRow = rootVisualElement.Q<VisualElement>("leaderboard-row");
        var leaderboardManager = leaderboardRow.Q<LeaderboardManager>();
        var leaderboardDataInspector = new InspectorElement(leaderboardManager.leaderboardData);
        leaderboardDataInspector.styleSheets.Add(editorTheme);
        leaderboardRow.Add(leaderboardDataInspector);
    }
}
