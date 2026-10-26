using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Bagel
{
    // The game's own levels, in the same level editor the payment tablet has (BagelLevelEditor), with the same live 3D
    // pictures of the library's things: the list of levels on the left (add one, delete one but the tutorial), the
    // chosen one's editor on the right, in the game's look. Every change is an undo step, and saved to the level's asset
    // at once. The game itself only edits the player's own levels.
    public class BagelLevelsWindow : EditorWindow
    {
        const string k_CollectionPath = "Assets/Game/Levels/BagelLevelCollection.asset";
        const string k_LevelsFolder = "Assets/Game/Levels/Tables";
        const string k_ThemeCollectionPath = "Assets/UI/Runtime/B00_Themes/T0_Common/BagelThemeCollection.asset";
        const string k_LevelStylesPath = "Assets/UI/Runtime/B14_LevelSelection/LevelSelectionStyles.uss";
        const string k_WindowStylesPath = "Assets/UI/Editor/B14_LevelSelection/BagelLevelsWindowStyles.uss";
        const double k_FrameTime = 1.0 / 30.0;

        [SerializeField] int m_SelectedIndex;

        BagelLevelCollection m_Collection;
        BagelLevelEditor m_Editor;
        ListView m_List;
        Button m_DeleteButton;
        BagelLevelStudio m_Studio;
        Scene m_StudioScene;
        double m_LastFrame;

        // The editor theme stays on the toolbar and the list; the level editor gets the game's.
        readonly List<StyleSheet> m_EditorSheets = new();
        readonly List<(VisualElement owner, StyleSheet sheet)> m_BorrowedSheets = new();
        readonly List<VisualElement> m_EditorStyledParts = new();

        public BagelLevelCollection collection => m_Collection;
        public BagelLevelEditor levelEditor => m_Editor;
        public ListView list => m_List;
        public BagelLevelStudio studio => m_Studio;
        public BagelLevel selectedLevel => m_Collection != null && m_SelectedIndex >= 0 && m_SelectedIndex < m_Collection.collection.Count ? m_Collection.collection[m_SelectedIndex] : null;

        [MenuItem("Bagel/Bagel Levels", priority = 11, editorModes = new[] { BagelMenu.defaultMode, BagelMenu.uiToolkitAuthoringMode })]
        public static BagelLevelsWindow Open()
        {
            var window = GetWindow<BagelLevelsWindow>();
            window.titleContent = new GUIContent("Bagel Levels");
            window.minSize = new Vector2(820f, 560f);
            return window;
        }

        public void CreateGUI()
        {
            titleContent = new GUIContent("Bagel Levels");
            m_Collection = AssetDatabase.LoadAssetAtPath<BagelLevelCollection>(k_CollectionPath);
            if (m_Collection == null)
            {
                rootVisualElement.Add(new Label($"Missing {k_CollectionPath}"));
                return;
            }

            // Take the editor theme off; it would leak editor looks into the game's editor.
            m_EditorSheets.Clear();
            for (int i = 0; i < rootVisualElement.styleSheets.count; i++)
                m_EditorSheets.Add(rootVisualElement.styleSheets[i]);
            rootVisualElement.styleSheets.Clear();
            var windowStyles = AssetDatabase.LoadAssetAtPath<StyleSheet>(k_WindowStylesPath);
            if (windowStyles != null)
                rootVisualElement.styleSheets.Add(windowStyles);
            rootVisualElement.AddToClassList("b-levels-window");

            var toolbar = new VisualElement { name = "toolbar" };
            toolbar.AddToClassList("b-levels-window__toolbar");
            var title = new Label("Bagel Levels") { name = "title" };
            title.AddToClassList("b-levels-window__title");
            toolbar.Add(title);
            toolbar.Add(new Button(() => AddLevel()) { name = "add-button", text = "New Level" });
            m_DeleteButton = new Button(() => DeleteLevel(selectedLevel, true)) { name = "delete-button", text = "Delete Level" };
            toolbar.Add(m_DeleteButton);
            rootVisualElement.Add(toolbar);

            var body = new VisualElement { name = "body" };
            body.AddToClassList("b-levels-window__body");
            m_List = new ListView
            {
                name = "levels",
                makeItem = () => new Label(),
                bindItem = (element, index) =>
                {
                    var level = m_Collection.collection[index];
                    ((Label)element).text = level != null ? $"{LevelSelectionScreenManager.LevelName(level)}  ({level.length:0.#} m)" : "(missing)";
                },
                selectionType = SelectionType.Single,
                fixedItemHeight = 24,
                itemsSource = m_Collection.collection
            };
            m_List.AddToClassList("b-levels-window__list");
            m_List.selectedIndicesChanged += indices =>
            {
                foreach (var index in indices)
                {
                    Select(index);
                    break;
                }
            };
            body.Add(m_List);

            var stage = new VisualElement { name = "stage" };
            stage.AddToClassList("b-levels-window__stage");
            var themes = AssetDatabase.LoadAssetAtPath<BagelThemeCollection>(k_ThemeCollectionPath);
            if (themes != null && themes.collection.Count > 0 && themes.collection[0].theme != null)
                stage.styleSheets.Add(themes.collection[0].theme);
            var levelStyles = AssetDatabase.LoadAssetAtPath<StyleSheet>(k_LevelStylesPath);
            if (levelStyles != null)
                stage.styleSheets.Add(levelStyles);
            var screen = new VisualElement { name = "screen" };
            screen.AddToClassList("b-glass");
            screen.AddToClassList("b-levels-window__screen");
            m_Editor = new BagelLevelEditor { name = "editor", showDone = false, showDelete = false };
            m_Editor.library = m_Collection.library;
            m_Editor.changing += level => Undo.RecordObject(level, "Change Bagel Level");
            m_Editor.changed += level =>
            {
                EditorUtility.SetDirty(level);
                AssetDatabase.SaveAssetIfDirty(level);
                m_List.RefreshItems();
            };
            screen.Add(m_Editor);
            stage.Add(screen);
            body.Add(stage);
            rootVisualElement.Add(body);

            m_EditorStyledParts.Clear();
            m_EditorStyledParts.Add(toolbar);
            m_EditorStyledParts.Add(m_List);
            rootVisualElement.RegisterCallback<AttachToPanelEvent>(_ => LiftEditorTheme());
            rootVisualElement.RegisterCallback<DetachFromPanelEvent>(_ => RestoreEditorTheme());
            if (rootVisualElement.panel != null)
                LiftEditorTheme();
            else
                ShareEditorTheme();

            OpenStudio();
            Select(Mathf.Clamp(m_SelectedIndex, 0, Mathf.Max(0, m_Collection.collection.Count - 1)));
            m_List.SetSelectionWithoutNotify(new[] { m_SelectedIndex });
        }

        void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
            EditorApplication.update += Tick;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.update -= Tick;
            RestoreEditorTheme();
            CloseStudio();
        }

        void OnDestroy() => rootVisualElement.Clear(VisualElementClearOptions.RecursiveReleaseResources);

        public void Select(int index)
        {
            if (m_Collection == null)
                return;
            m_SelectedIndex = Mathf.Clamp(index, 0, Mathf.Max(0, m_Collection.collection.Count - 1));
            if (m_Editor != null)
                m_Editor.level = selectedLevel;
            // The tutorial stays: it's the game's first and default level.
            m_DeleteButton?.SetEnabled(m_SelectedIndex > 0 && selectedLevel != null);
        }

        // Another collection's levels (a test's, made in memory: its levels aren't assets).
        public void SetCollection(BagelLevelCollection levels)
        {
            m_Collection = levels;
            if (m_List == null || m_Collection == null)
                return;
            m_List.itemsSource = m_Collection.collection;
            m_List.RefreshItems();
            m_Editor.library = m_Collection.library;
            OpenStudio();
            Select(0);
            m_List.SetSelectionWithoutNotify(new[] { m_SelectedIndex });
        }

        bool levelsAreAssets => m_Collection != null && AssetDatabase.Contains(m_Collection);

        // A new level of the game's own, after the others: an asset in the levels' folder, empty but for the toaster.
        public BagelLevel AddLevel()
        {
            if (m_Collection == null)
                return null;
            var path = AssetDatabase.GenerateUniqueAssetPath($"{k_LevelsFolder}/New Table.asset");
            var level = CreateInstance<BagelLevel>();
            var serialized = new SerializedObject(level);
            var fileName = Path.GetFileNameWithoutExtension(path);
            serialized.FindProperty("m_Id").stringValue = UniqueId(Regex.Replace(fileName.ToLowerInvariant(), "[^a-z0-9]", ""));
            serialized.FindProperty("m_DisplayName").stringValue = fileName;
            serialized.FindProperty("m_Length").floatValue = BagelLevel.newLength;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (levelsAreAssets)
                AssetDatabase.CreateAsset(level, path);
            else
                level.hideFlags = HideFlags.DontSave;

            Undo.RecordObject(m_Collection, "Add Bagel Level");
            m_Collection.collection.Add(level);
            EditorUtility.SetDirty(m_Collection);
            AssetDatabase.SaveAssetIfDirty(m_Collection);
            m_List.RefreshItems();
            Select(m_Collection.collection.Count - 1);
            m_List.SetSelectionWithoutNotify(new[] { m_SelectedIndex });
            return level;
        }

        string UniqueId(string id)
        {
            if (string.IsNullOrEmpty(id))
                id = "table";
            var unique = id;
            for (int i = 2; m_Collection.collection.Exists(level => level != null && level.id == unique); i++)
                unique = id + i;
            return unique;
        }

        // Out of the game, and its asset to the trash (asked first, from the toolbar).
        public void DeleteLevel(BagelLevel level, bool ask)
        {
            if (m_Collection == null || level == null || m_Collection.collection.IndexOf(level) <= 0)
                return;
            if (ask && !EditorUtility.DisplayDialog("Delete Bagel Level", $"Delete the level \"{LevelSelectionScreenManager.LevelName(level)}\"? Its asset goes to the trash.", "Delete", "Cancel"))
                return;
            m_Collection.collection.Remove(level);
            EditorUtility.SetDirty(m_Collection);
            AssetDatabase.SaveAssetIfDirty(m_Collection);
            if (AssetDatabase.Contains(level))
                AssetDatabase.MoveAssetToTrash(AssetDatabase.GetAssetPath(level));
            else
                DestroyImmediate(level);
            m_List.RefreshItems();
            Select(m_SelectedIndex - 1);
            m_List.SetSelectionWithoutNotify(new[] { m_SelectedIndex });
        }

        void OnUndoRedo()
        {
            if (m_Collection == null)
                return;
            m_List?.RefreshItems();
            Select(m_SelectedIndex);
            m_Editor?.Refresh();
        }

        // The library's pictures, in a scene of their own, turned and taken again as the window updates.
        void OpenStudio()
        {
            CloseStudio();
            if (m_Collection == null || m_Collection.library == null)
                return;
            m_StudioScene = EditorSceneManager.NewPreviewScene();
            m_Studio = BagelLevelStudio.Create(m_Collection.library, 0, m_StudioScene);
            m_Studio.Render();
            m_Editor.thumbnails = m_Studio;
            m_LastFrame = EditorApplication.timeSinceStartup;
        }

        void CloseStudio()
        {
            if (m_Editor != null)
                m_Editor.thumbnails = null;
            m_Studio?.Dispose();
            m_Studio = null;
            if (m_StudioScene.IsValid())
                EditorSceneManager.ClosePreviewScene(m_StudioScene);
            m_StudioScene = default;
        }

        void Tick()
        {
            if (m_Studio == null)
                return;
            var now = EditorApplication.timeSinceStartup;
            if (now - m_LastFrame < k_FrameTime)
                return;
            m_Studio.Update((float)(now - m_LastFrame));
            m_LastFrame = now;
            m_Studio.Render();
            Repaint();
        }

        // Newer editors keep their theme on an ancestor of the window's root: borrow it while the window is attached,
        // and give it back when it detaches.
        void LiftEditorTheme()
        {
            if (m_BorrowedSheets.Count == 0)
            {
                for (var ancestor = rootVisualElement.hierarchy.parent; ancestor != null; ancestor = ancestor.hierarchy.parent)
                {
                    for (int i = 0; i < ancestor.styleSheets.count; i++)
                    {
                        var sheet = ancestor.styleSheets[i];
                        m_BorrowedSheets.Add((ancestor, sheet));
                        if (!m_EditorSheets.Contains(sheet))
                            m_EditorSheets.Add(sheet);
                    }
                    ancestor.styleSheets.Clear();
                }
            }
            ShareEditorTheme();
        }

        void ShareEditorTheme()
        {
            foreach (var part in m_EditorStyledParts)
            {
                foreach (var sheet in m_EditorSheets)
                {
                    if (!part.styleSheets.Contains(sheet))
                        part.styleSheets.Add(sheet);
                }
            }
        }

        void RestoreEditorTheme()
        {
            foreach (var (owner, sheet) in m_BorrowedSheets)
            {
                if (owner != null && !owner.styleSheets.Contains(sheet))
                    owner.styleSheets.Add(sheet);
            }
            m_BorrowedSheets.Clear();
        }
    }
}
