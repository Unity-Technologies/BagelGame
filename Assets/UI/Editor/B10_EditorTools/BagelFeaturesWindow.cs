using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Localization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Bagel
{
    // Bagel > Bagel Features: every UI Toolkit feature the game uses, one at a time, with the game's own UI
    // that uses it, live. Pick a feature on the left. On the right is what it is and how the game uses it,
    // then one of the Bagel All UI window's cards at a moment of the game, with the parts that use the
    // feature outlined, then things to try on it, the lines of the project it's in and where to see it in
    // the Editor. The examples play on a throwaway copy of the start state: nothing here changes an asset.
    // The features are in BagelFeatureCatalog.
    public class BagelFeaturesWindow : EditorWindow
    {
        public const string windowTitle = "Bagel Features";
        const string k_WindowUxmlPath = "Assets/UI/Editor/B10_EditorTools/BagelFeaturesWindow.uxml";
        const double k_StudioFrameTime = 1.0 / 30.0;
        const long k_OutlinesEveryMs = 100;
        const float k_StageMargin = 16f;
        // Small screens (the gauges over the bagel) grow, up to this.
        public const float maxScale = 2f;

        public static readonly string rowUssClassName = "b-features__row";
        public static readonly UniqueStyleString selectedRowUssClassName = new(rowUssClassName + "--selected");
        public static readonly string groupUssClassName = "b-features__group";
        public static readonly string outlineUssClassName = "b-features__outline";
        public static readonly string outlineLabelUssClassName = outlineUssClassName + "-label";
        public static readonly UniqueStyleString outlineLabelInsideUssClassName = new(outlineLabelUssClassName + "--inside");
        public static readonly UniqueStyleString noStageUssClassName = new("b-features__stage--none");
        public static readonly UniqueStyleString inspectorStageUssClassName = new("b-features__stage--inspector");
        public static readonly UniqueStyleString noteShownUssClassName = new("b-features__note--shown");
        public static readonly UniqueStyleString fullDetailsUssClassName = new("b-features__details--full");

        [SerializeField] string m_Feature;
        [SerializeField] string m_Search = "";

        PlayManagerState m_State;
        BagelThemeCollection m_Themes;
        ThemeStyleSheet m_Theme;
        BagelEditorTheme m_EditorTheme;
        readonly List<string> m_LocaleCodes = new();

        ToolbarSearchField m_SearchField;
        BagelOptionStrip m_ThemeStrip;
        BagelOptionStrip m_LanguageStrip;
        ScrollView m_List;
        Label m_Nothing;
        Label m_Title;
        Label m_Category;
        Label m_About;
        Label m_Usage;
        VisualElement m_Stage;
        VisualElement m_Example;
        VisualElement m_Outlines;
        ScrollView m_Details;
        VisualElement m_Tries;
        Label m_Note;
        VisualElement m_Sources;
        VisualElement m_Links;
        VisualElement m_LinksSection;

        readonly Dictionary<string, VisualElement> m_Rows = new();
        readonly Dictionary<BagelFeatureCatalog.Category, VisualElement> m_Groups = new();
        readonly List<string> m_Shown = new();
        readonly List<TemplateContainer> m_Screens = new();
        readonly List<VisualElement> m_Parts = new();
        BagelFeatureCatalog.Example m_ShownExample;
        IVisualElementScheduledItem m_OutlineUpdates;

        // The level editor's live pictures, for an example that has it open.
        BagelLevelStudio m_Studio;
        Scene m_StudioScene;
        BagelLevelEditor m_StudioEditor;
        double m_LastFrame;

        public PlayManagerState state => m_State;
        public BagelFeatureCatalog.Feature feature => BagelFeatureCatalog.Get(m_Feature);
        // The example's root: the card's screens, or the state's Inspector.
        public VisualElement example => m_Example;
        public IReadOnlyList<TemplateContainer> screens => m_Screens;
        // The elements of the example that use the feature (the outlined ones, and any out of sight).
        public IReadOnlyList<VisualElement> parts => m_Parts;
        public VisualElement outlines => m_Outlines;
        public VisualElement stage => m_Stage;
        public IReadOnlyList<string> shownFeatures => m_Shown;
        public IReadOnlyDictionary<string, VisualElement> rows => m_Rows;
        public VisualElement tries => m_Tries;
        public VisualElement sources => m_Sources;
        public Label note => m_Note;
        public ThemeStyleSheet currentTheme => m_Theme;
        public BagelLevelStudio studio => m_Studio;

        [MenuItem("Bagel/Bagel Features", priority = 12, editorModes = new[] { BagelMenu.defaultMode, BagelMenu.uiToolkitAuthoringMode })]
        public static BagelFeaturesWindow Open()
        {
            // A tab next to the Bagel All UI window when it's open: the same room, the same screens.
            var window = GetWindow<BagelFeaturesWindow>(windowTitle, true, typeof(BagelAllUIWindow));
            window.titleContent = new GUIContent(windowTitle);
            window.minSize = new Vector2(820f, 560f);
            return window;
        }

        public void CreateGUI()
        {
            titleContent = new GUIContent(windowTitle);

            DestroyState();
            CreateState();
            m_Themes = AssetDatabase.LoadAssetAtPath<BagelThemeCollection>(BagelAllUIWindow.themeCollectionPath);

            var windowTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_WindowUxmlPath);
            if (windowTree == null)
            {
                rootVisualElement.Add(new Label($"Missing {k_WindowUxmlPath}"));
                return;
            }

            // Take the editor theme off; it would leak editor looks into the game's screens.
            m_EditorTheme?.Restore();
            m_EditorTheme = new BagelEditorTheme(rootVisualElement);
            windowTree.CloneTree(rootVisualElement);

            m_SearchField = rootVisualElement.Q<ToolbarSearchField>("search");
            m_ThemeStrip = rootVisualElement.Q<BagelOptionStrip>("theme-strip");
            m_LanguageStrip = rootVisualElement.Q<BagelOptionStrip>("language-strip");
            m_List = rootVisualElement.Q<ScrollView>("list");
            m_Title = rootVisualElement.Q<Label>("title");
            m_Category = rootVisualElement.Q<Label>("category");
            m_About = rootVisualElement.Q<Label>("about");
            m_Usage = rootVisualElement.Q<Label>("usage");
            m_Stage = rootVisualElement.Q("stage");
            m_Example = rootVisualElement.Q("example");
            m_Outlines = rootVisualElement.Q("outlines");
            m_Details = rootVisualElement.Q<ScrollView>("details");
            m_Tries = rootVisualElement.Q("tries");
            m_Note = rootVisualElement.Q<Label>("note");
            m_Sources = rootVisualElement.Q("sources");
            m_Links = rootVisualElement.Q("links");
            m_LinksSection = rootVisualElement.Q("links-section");

            m_EditorTheme.Share(rootVisualElement.Q("toolbar"));
            m_EditorTheme.Share(m_List);
            m_EditorTheme.Share(rootVisualElement.Q("header"));
            m_EditorTheme.Share(m_Details);

            rootVisualElement.RegisterCallback<AttachToPanelEvent>(e => BagelAllUIWindow.AddSteamChannels(e.destinationPanel));
            BagelAllUIWindow.AddSteamChannels(rootVisualElement.panel);

            m_Stage.RegisterCallback<GeometryChangedEvent>(_ => FitExample());
            m_Example.RegisterCallback<GeometryChangedEvent>(_ => FitExample());
            m_List.RegisterCallback<KeyDownEvent>(OnListKeyDown);

            m_Theme = ThemeAt(m_State.settings.themeIndex);
            BuildList();
            BindToolbar();
            m_SearchField.SetValueWithoutNotify(m_Search);
            ApplySearch();
            Select(BagelFeatureCatalog.Get(m_Feature) != null ? m_Feature : BagelFeatureCatalog.features[0].id);
        }

        void OnEnable()
        {
            BagelLocalization.localeChanged += UpdateLanguageStrip;
            EditorApplication.update += Tick;
        }

        void OnDisable()
        {
            BagelLocalization.localeChanged -= UpdateLanguageStrip;
            EditorApplication.update -= Tick;
            m_EditorTheme?.Restore();
            ClearExample();
            DestroyState();
        }

        // Closed for good: the screens give their resources back to be reused now.
        void OnDestroy() => rootVisualElement.Clear(VisualElementClearOptions.RecursiveReleaseResources);

        void CreateState()
        {
            m_State = PlayManagerState.CreateRuntimeCopy(AssetDatabase.LoadAssetAtPath<PlayManagerState>(BagelAllUIWindow.startStatePath));
            m_State.propertyChanged += State_OnPropertyChanged;
        }

        void DestroyState()
        {
            if (m_State == null)
                return;

            m_State.propertyChanged -= State_OnPropertyChanged;
            PlayManagerState.DestroyRuntimeCopy(m_State);
            m_State = null;
        }

        // The toolbar's Reset, and the demo's (Bagel > Demo > Reset Bagel Project States): a fresh state, the
        // first feature, no search.
        public void ResetWindow()
        {
            // A tab that was never shown has no UI yet: it starts fresh when it's built.
            if (m_List == null)
                return;

            ClearExample();
            DestroyState();
            CreateState();
            ApplyTheme(ThemeAt(m_State.settings.themeIndex));
            UpdateThemeStrip();
            m_Search = "";
            m_SearchField.SetValueWithoutNotify("");
            ApplySearch();
            Select(BagelFeatureCatalog.features[0].id);
            m_List.scrollOffset = Vector2.zero;
        }

        // ---------------------------------------------------------------- The list

        void BuildList()
        {
            m_List.Clear();
            m_Rows.Clear();
            m_Groups.Clear();
            foreach (BagelFeatureCatalog.Category category in Enum.GetValues(typeof(BagelFeatureCatalog.Category)))
            {
                var features = BagelFeatureCatalog.features.Where(f => f.category == category).ToList();
                if (features.Count == 0)
                    continue;

                var group = new Label(BagelFeatureCatalog.CategoryName(category).ToUpperInvariant()) { name = "group-" + category };
                group.AddToClassList(groupUssClassName);
                m_Groups[category] = group;
                m_List.Add(group);
                foreach (var feature in features)
                {
                    var id = feature.id;
                    var row = new Label(feature.title) { name = "row-" + id, tooltip = feature.about };
                    row.AddToClassList(rowUssClassName);
                    row.RegisterCallback<ClickEvent>(_ =>
                    {
                        Select(id);
                        m_List.Focus();
                    });
                    m_Rows[id] = row;
                    m_List.Add(row);
                }
            }
            m_Nothing = new Label { name = "nothing" };
            m_Nothing.AddToClassList("b-features__nothing");
            m_List.Add(m_Nothing);
        }

        // Only the features with every word of the search.
        public void Search(string text)
        {
            m_SearchField?.SetValueWithoutNotify(text);
            m_Search = text ?? "";
            ApplySearch();
        }

        void ApplySearch()
        {
            var words = m_Search.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            m_Shown.Clear();
            foreach (var feature in BagelFeatureCatalog.features)
            {
                var shown = words.All(word => feature.searchText.Contains(word));
                if (shown)
                    m_Shown.Add(feature.id);
                if (m_Rows.TryGetValue(feature.id, out var row))
                    row.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            }
            foreach (var (category, group) in m_Groups)
                group.style.display = m_Shown.Any(id => BagelFeatureCatalog.Get(id).category == category) ? DisplayStyle.Flex : DisplayStyle.None;
            m_Nothing.text = $"No feature has \"{m_Search.Trim()}\".";
            m_Nothing.style.display = m_Shown.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // Up and down the list's shown features.
        void OnListKeyDown(KeyDownEvent evt)
        {
            var step = evt.keyCode == KeyCode.DownArrow ? 1 : evt.keyCode == KeyCode.UpArrow ? -1 : 0;
            if (step == 0 || m_Shown.Count == 0)
                return;
            var index = m_Shown.IndexOf(m_Feature);
            index = index < 0 ? 0 : Mathf.Clamp(index + step, 0, m_Shown.Count - 1);
            Select(m_Shown[index]);
            evt.StopPropagation();
        }

        // ---------------------------------------------------------------- A feature

        public void Select(string featureId)
        {
            var feature = BagelFeatureCatalog.Get(featureId);
            if (feature == null || m_List == null)
                return;

            m_Feature = featureId;
            foreach (var (id, row) in m_Rows)
                row.EnableInClassList(selectedRowUssClassName, id == featureId);
            if (m_Rows.TryGetValue(featureId, out var selected))
                m_List.schedule.Execute(() => m_List.ScrollTo(selected));

            m_Title.text = feature.title;
            m_Category.text = BagelFeatureCatalog.CategoryName(feature.category);
            m_About.text = feature.about;
            m_Usage.text = feature.usage;
            ShowNote(null);
            BuildTries(feature);
            BuildSources(feature);
            BuildLinks(feature);
            m_Details.scrollOffset = Vector2.zero;
            ShowExample(feature.example);
        }

        void BuildTries(BagelFeatureCatalog.Feature feature)
        {
            m_Tries.Clear();
            m_Tries.style.display = feature.tries.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (feature.tries.Length == 0)
                return;

            var label = new Label("Try it:");
            label.AddToClassList("b-features__tries-label");
            m_Tries.Add(label);
            for (int i = 0; i < feature.tries.Length; i++)
            {
                var tryIt = feature.tries[i];
                m_Tries.Add(new Button(() => Try(tryIt)) { name = "try-" + i, text = tryIt.label, tooltip = tryIt.tooltip });
            }
        }

        public void Try(BagelFeatureCatalog.TryIt tryIt)
        {
            tryIt.run(this);
            UpdateOutlines();
        }

        void BuildSources(BagelFeatureCatalog.Feature feature)
        {
            m_Sources.Clear();
            foreach (var source in feature.sources)
            {
                var row = new VisualElement();
                row.AddToClassList("b-features__source");
                var line = source.FindLine();
                var file = new Label(line > 0 ? $"{source.fileName}:{line}" : $"{source.fileName} (not found)")
                {
                    tooltip = source.opensInCodeEditor ? $"Open {source.path} at the line" : $"Show {source.path} in the Project window"
                };
                file.AddToClassList("b-features__file");
                file.EnableInClassList("b-features__file--missing", line == 0);
                file.AddManipulator(new Clickable(source.Open));
                row.Add(file);
                var what = new Label(source.what);
                what.AddToClassList("b-features__what");
                row.Add(what);
                m_Sources.Add(row);
            }
        }

        void BuildLinks(BagelFeatureCatalog.Feature feature)
        {
            m_Links.Clear();
            m_LinksSection.style.display = feature.links.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            foreach (var link in feature.links)
                m_Links.Add(new Button(link.Open) { text = link.label, tooltip = link.tooltip });
        }

        // Text an example's try leaves under its buttons: what a screen reader is told.
        public void ShowNote(string text)
        {
            m_Note.text = text ?? "";
            m_Note.EnableInClassList(noteShownUssClassName, !string.IsNullOrEmpty(text));
        }

        // ---------------------------------------------------------------- The example

        void ShowExample(BagelFeatureCatalog.Example example)
        {
            ClearExample();
            m_ShownExample = example;
            m_Stage.EnableInClassList(noStageUssClassName, example == null);
            m_Details.EnableInClassList(fullDetailsUssClassName, example == null);
            m_Stage.EnableInClassList(inspectorStageUssClassName, example != null && example.inspector);
            if (example == null)
                return;

            // A feature of the Editor's: the Inspector of the state the examples play on, in the Editor's theme.
            if (example.inspector)
            {
                m_EditorTheme.Share(m_Stage);
                var scroll = new ScrollView { name = "inspector" };
                scroll.style.flexGrow = 1f;
                scroll.Add(new InspectorElement(m_State));
                m_Example.Add(scroll);
                return;
            }

            m_EditorTheme.Unshare(m_Stage);
            var spec = example.cardSpec;
            if (spec.uxmlPaths == null)
            {
                Debug.LogWarning($"[Bagel] No Bagel All UI card is called {example.card}");
                return;
            }
            foreach (var screen in BagelAllUIWindow.CreateScreens(spec, m_State, m_Theme))
            {
                m_Screens.Add(screen);
                m_Example.Add(screen);
            }
            // The level editor ends where it is: another example starts back on the tablet's list.
            m_State.EditLevel(false);
            if (example.setup != null)
                BagelAllUIWindow.ApplySetup(m_State, example.setup);
            if (m_State.isEditingLevel && m_Example.Q<BagelLevelEditor>() is { } editor)
                OpenStudio(editor);

            example.prepare?.Invoke(m_Example);
            UpdateOutlines();
            m_OutlineUpdates = m_Stage.schedule.Execute(UpdateOutlines).Every(k_OutlinesEveryMs);
        }

        void ClearExample()
        {
            m_OutlineUpdates?.Pause();
            m_OutlineUpdates = null;
            m_ShownExample = null;
            CloseStudio();
            m_Outlines?.Clear();
            m_Parts.Clear();
            m_Screens.Clear();
            if (m_Example == null)
                return;
            // Made again for the next one: what it showed gives its resources back to be reused now.
            Discard.Children(m_Example);
            m_Example.style.scale = StyleKeyword.Null;
            m_Example.style.left = StyleKeyword.Null;
            m_Example.style.top = StyleKeyword.Null;
        }

        // The screens as big as the stage lets them be, small ones up to twice their size, in its middle.
        void FitExample()
        {
            if (m_ShownExample == null || m_ShownExample.inspector)
                return;
            var size = m_Example.layout.size;
            var room = m_Stage.layout.size;
            if (float.IsNaN(size.x) || float.IsNaN(room.x) || size.x < 1f || size.y < 1f || room.x < 1f || room.y < 1f)
                return;

            var scale = Mathf.Max(0.05f, Mathf.Min(maxScale, Mathf.Min((room.x - 2f * k_StageMargin) / size.x, (room.y - 2f * k_StageMargin) / size.y)));
            m_Example.style.scale = new Scale(new Vector3(scale, scale, 1f));
            m_Example.style.left = Mathf.Round((room.x - size.x * scale) * 0.5f);
            m_Example.style.top = Mathf.Round((room.y - size.y * scale) * 0.5f);
            UpdateOutlines();
        }

        // An outline around every part that uses the feature, and its label on the first of each kind. The
        // parts are looked for again each time: a try or the state can make new ones.
        void UpdateOutlines()
        {
            if (m_ShownExample == null || m_ShownExample.inspector || m_Outlines == null)
                return;

            m_Parts.Clear();
            var used = 0;
            var labeled = new HashSet<BagelFeatureCatalog.Part>();
            foreach (var part in m_ShownExample.parts)
            {
                foreach (var element in m_Example.Query<VisualElement>().Where(part.matches).ToList())
                {
                    m_Parts.Add(element);
                    if (!TryGetShownRect(element, out var rect))
                        continue;

                    var outline = used < m_Outlines.childCount ? m_Outlines[used] : NewOutline();
                    used++;
                    outline.style.left = rect.x;
                    outline.style.top = rect.y;
                    outline.style.width = rect.width;
                    outline.style.height = rect.height;
                    var label = (Label)outline[0];
                    label.text = part.label;
                    label.style.display = labeled.Add(part) ? DisplayStyle.Flex : DisplayStyle.None;
                    label.EnableInClassList(outlineLabelInsideUssClassName, rect.y < 18f);
                }
            }
            while (m_Outlines.childCount > used)
                m_Outlines.RemoveAt(m_Outlines.childCount - 1);
        }

        VisualElement NewOutline()
        {
            var outline = new VisualElement { pickingMode = PickingMode.Ignore };
            outline.AddToClassList(outlineUssClassName);
            var label = new Label { pickingMode = PickingMode.Ignore };
            label.AddToClassList(outlineLabelUssClassName);
            outline.Add(label);
            m_Outlines.Add(outline);
            return outline;
        }

        // The part of an element in sight, in the outlines' space: none when it's hidden (or clear, or not
        // written on the blackboard yet), or scrolled or clipped out of sight by what it's in (a list's thing
        // past its end).
        bool TryGetShownRect(VisualElement element, out Rect rect)
        {
            rect = default;
            if (element.panel == null)
                return false;
            var bounds = element.worldBound;
            for (var e = element; e != null && e != m_Example; e = e.hierarchy.parent)
            {
                if (e.resolvedStyle.display == DisplayStyle.None || e.resolvedStyle.visibility == Visibility.Hidden ||
                    e.resolvedStyle.opacity < 0.01f || e.ClassListContains(ChalkWipe.blankUssClassName))
                    return false;
                if (e != element && e.resolvedStyle.overflow == Overflow.Hidden)
                {
                    var clip = e.worldBound;
                    bounds = Rect.MinMaxRect(Mathf.Max(bounds.xMin, clip.xMin), Mathf.Max(bounds.yMin, clip.yMin),
                        Mathf.Min(bounds.xMax, clip.xMax), Mathf.Min(bounds.yMax, clip.yMax));
                }
            }
            if (float.IsNaN(bounds.width) || bounds.width < 1f || bounds.height < 1f)
                return false;
            rect = m_Outlines.WorldToLocal(bounds);
            return true;
        }

        // ---------------------------------------------------------------- The level editor's pictures

        void OpenStudio(BagelLevelEditor editor)
        {
            CloseStudio();
            if (m_State.levelLibrary == null)
                return;
            m_StudioScene = EditorSceneManager.NewPreviewScene();
            m_Studio = BagelLevelStudio.Create(m_State.levelLibrary, 0, m_StudioScene);
            m_Studio.Render();
            m_StudioEditor = editor;
            editor.thumbnails = m_Studio;
            m_LastFrame = EditorApplication.timeSinceStartup;
        }

        void CloseStudio()
        {
            if (m_StudioEditor != null)
                m_StudioEditor.thumbnails = null;
            m_StudioEditor = null;
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
            if (now - m_LastFrame < k_StudioFrameTime)
                return;
            m_Studio.Update((float)(now - m_LastFrame));
            m_LastFrame = now;
            m_Studio.Render();
            Repaint();
        }

        // ---------------------------------------------------------------- Toolbar: theme and language

        void BindToolbar()
        {
            rootVisualElement.Q<BagelOptionStrip>("reset-strip").AddAction(BagelEditorIcons.Reset(), "Reset", ResetWindow, "reset-button");

            m_SearchField.RegisterValueChangedCallback(evt =>
            {
                m_Search = evt.newValue ?? "";
                ApplySearch();
            });

            for (int i = 0; i < (m_Themes?.collection.Count ?? 0); i++)
            {
                var entry = m_Themes.collection[i];
                m_ThemeStrip.AddOption(BagelEditorIcons.Theme(entry), entry.themeName, "theme-" + entry.nameKey.Replace("theme.", ""));
            }
            m_ThemeStrip.chosen += index => m_State.settings.themeIndex = index;
            UpdateThemeStrip();

            m_LocaleCodes.Clear();
            if (LocalizationSettings.HasSettings)
            {
                foreach (var locale in LocalizationSettings.Instance.AvailableLocales)
                {
                    m_LocaleCodes.Add(locale.Code);
                    m_LanguageStrip.AddOption(BagelEditorIcons.Language(locale.Code), locale.LocaleName, "language-" + locale.Code);
                }
            }
            m_LanguageStrip.chosen += index => SetLanguage(m_LocaleCodes[index]);
            UpdateLanguageStrip();
        }

        public void NextTheme()
        {
            var count = m_Themes != null ? m_Themes.collection.Count : 0;
            if (count > 0)
                m_State.settings.themeIndex = (m_State.settings.themeIndex + 1) % count;
        }

        public void SetLanguage(string localeCode) => BagelToolbar.SelectLanguage(localeCode);

        public void NextLanguage()
        {
            if (m_LocaleCodes.Count == 0)
                return;
            var index = m_LocaleCodes.IndexOf(BagelLocalization.localeCode);
            SetLanguage(m_LocaleCodes[(index + 1) % m_LocaleCodes.Count]);
        }

        void UpdateThemeStrip() => m_ThemeStrip?.SetSelectedWithoutNotify(m_State != null ? m_State.settings.themeIndex : -1);

        void UpdateLanguageStrip() => m_LanguageStrip?.SetSelectedWithoutNotify(m_LocaleCodes.IndexOf(BagelLocalization.localeCode));

        void State_OnPropertyChanged(object sender, BindablePropertyChangedEventArgs args)
        {
            if (args.propertyName == SettingsData.themeIndexPath)
            {
                ApplyTheme(ThemeAt(m_State.settings.themeIndex));
                UpdateThemeStrip();
            }
        }

        ThemeStyleSheet ThemeAt(int index)
        {
            if (m_Themes == null || m_Themes.collection.Count == 0)
                return null;
            return m_Themes.collection[Mathf.Clamp(index, 0, m_Themes.collection.Count - 1)].theme;
        }

        void ApplyTheme(ThemeStyleSheet theme)
        {
            if (theme == m_Theme)
                return;

            foreach (var screen in m_Screens)
            {
                if (m_Theme != null)
                    screen.styleSheets.Remove(m_Theme);
                if (theme != null)
                    screen.styleSheets.Add(theme);
            }
            m_Theme = theme;
        }
    }
}
