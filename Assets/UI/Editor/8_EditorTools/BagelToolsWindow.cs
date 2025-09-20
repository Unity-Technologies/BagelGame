using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class BagelToolsWindow : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    [MenuItem("Bagel/Bagel Tools")]
    public static void ShowExample()
    {
        BagelToolsWindow wnd = GetWindow<BagelToolsWindow>();
        wnd.titleContent = new GUIContent("Bagel Tools");
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // VisualElements objects can contain other VisualElement following a tree hierarchy.
        VisualElement label = new Label("Hello World! From C#");
        root.Add(label);

        // Instantiate UXML
        VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
        root.Add(labelFromUXML);
    }
}
