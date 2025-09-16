using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

public class BagelTestEditorWindow : EditorWindow
{
    public VisualTreeAsset mainMenuUxml;
    public VisualTreeAsset bagelTrackerUxml;

    public VisualElement mainMenuRoot;
    public VisualElement bagelTrackerRoot;

    [MenuItem("Bagel/Test Editor Window")]
    public static void ShowExample()
    {
        BagelTestEditorWindow wnd = GetWindow<BagelTestEditorWindow>();
        wnd.titleContent = new GUIContent("Bagel Test Editor Window");
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;
        
        mainMenuRoot = mainMenuUxml.Instantiate();
        root.Add(mainMenuRoot);

        bagelTrackerRoot = bagelTrackerUxml.Instantiate();
        root.Add(bagelTrackerRoot);
    }
}
