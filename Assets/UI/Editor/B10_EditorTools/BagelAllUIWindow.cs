using Bagel;
using Unity.Properties;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
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
        var root = rootVisualElement;

        var editorTheme = root.styleSheets[0];
        root.styleSheets.Clear();

        m_VisualTreeAsset.CloneTree(root);

        var bagelTrackerDataInspector = new InspectorElement(m_BagelTrackerData);
        bagelTrackerDataInspector.styleSheets.Add(editorTheme);
        var bagelTrackerDataRow = root.Q<VisualElement>("bagel-tracker-row");
        bagelTrackerDataRow.Add(bagelTrackerDataInspector);

        var bagelTypeInfoInspector = new InspectorElement(m_BagelType);
        bagelTypeInfoInspector.styleSheets.Add(editorTheme);
        var bagelTypeInfoContainer = root.Q<VisualElement>("bagel-selection-type-info");
        bagelTypeInfoContainer.Add(bagelTypeInfoInspector);

        root.Query<TemplateContainer>().ForEach(t =>
        {
           t.styleSheets.Add(m_Theme);
        });

        var rootScrollView = root.Q<ScrollView>("root-scrollview");
        var rootScroller = root.Q<Scroller>("root-scroller");
        rootScroller.styleSheets.Add(editorTheme);

        var rootScrollViewVerticalScroller = rootScrollView.hierarchy.ElementAt(0).hierarchy.ElementAt(1) as Scroller;

        rootScroller.dataSource = rootScrollViewVerticalScroller;
        rootScroller.SetBinding("value", new DataBinding() { dataSourcePath = new PropertyPath("value"), bindingMode = BindingMode.TwoWay });
        rootScroller.SetBinding("lowValue", new DataBinding() { dataSourcePath = new PropertyPath("lowValue") });
        rootScroller.SetBinding("highValue", new DataBinding() { dataSourcePath = new PropertyPath("highValue") });
    }
}
