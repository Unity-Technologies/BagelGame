using System;
using System.Collections.Generic;
using Unity.Localization;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // Every screen of the game side by side, live, all playing on one throwaway copy of the
    // start state. Click through them, switch the theme and language, stage a setup (a win, a
    // wrecked bagel, the Settings pane...) or edit the state in the inspector. Nothing here
    // changes an asset.
    [NoAutoStaticsCleanup] // read-only tables of screens and setups
    public class BagelAllUIWindow : EditorWindow
    {
        const string k_WindowUxmlPath = "Assets/UI/Editor/B10_EditorTools/BagelAllUIWindow.uxml";
        const string k_StartStatePath = "Assets/Game/Core/PlayManagerState.asset";
        const string k_ThemeCollectionPath = "Assets/UI/Runtime/B00_Themes/T0_Common/BagelThemeCollection.asset";
        const string k_UIPath = "Assets/UI/Runtime/";

        // One card per screen, all of them shown at once.
        public struct CardSpec
        {
            public string title;
            public string[] uxmlPaths;
            public Vector2 size;
        }

        public static readonly CardSpec[] cardSpecs =
        {
            new CardSpec { title = "Main Menu", uxmlPaths = new[] { "B03_MainMenu/MainMenuScreen.uxml" } },
            new CardSpec { title = "Bagel Selection", uxmlPaths = new[] { "B05_BagelSelection/BagelSelectionControlsPane.uxml" } },
            new CardSpec { title = "Bagel Cards", uxmlPaths = new[] { "B05_BagelSelection/BagelSelectionTypeInfoPane.uxml" } },
            new CardSpec { title = "Payment Tablet", uxmlPaths = new[] { "B14_LevelSelection/LevelSelectionScreen.uxml" } },
            new CardSpec { title = "Tracker", uxmlPaths = new[] { "B06_BagelTracker/BagelTrackerLeftDisplay.uxml", "B06_BagelTracker/BagelTrackerRightDisplay.uxml" } },
            new CardSpec { title = "Callout (Screen Space)", uxmlPaths = new[] { "B06_BagelTracker/BagelCallout.uxml" }, size = new Vector2(220f, 90f) },
            new CardSpec { title = "Pause (Screen Space)", uxmlPaths = new[] { "B07_Pause/PauseScreen.uxml" }, size = new Vector2(640f, 400f) },
            new CardSpec { title = "Television", uxmlPaths = new[] { "B08_GameOver/GameOverScreen.uxml" } },
            new CardSpec { title = "Leaderboard", uxmlPaths = new[] { "B09_Leaderboard/LeaderboardPane.uxml" } },
            new CardSpec { title = "Settings", uxmlPaths = new[] { "B04_Settings/SettingsPane.uxml" } },
            new CardSpec { title = "Jam Label (World Space)", uxmlPaths = new[] { "B13_Kitchen/JamLabel.uxml" } },
            new CardSpec { title = "Steam (World Space)", uxmlPaths = new[] { "B13_Kitchen/Steam.uxml" } },
        };

        public class Card
        {
            public CardSpec spec;
            public VisualElement root;
            public VisualElement slot;
            public VisualElement content;
            public readonly List<TemplateContainer> screens = new();
        }

        [SerializeField] float m_Zoom = 1.0f;
        [SerializeField] bool m_ShowInspector;

        PlayManagerState m_State;
        BagelThemeCollection m_Themes;
        ThemeStyleSheet m_Theme;
        readonly List<Card> m_Cards = new();
        IVisualElementScheduledItem m_Roll;

        BagelOptionStrip m_ThemeStrip;
        BagelOptionStrip m_LanguageStrip;
        VisualElement m_RollToggle;
        VisualElement m_InspectorToggle;
        Label m_ZoomLabel;
        readonly List<string> m_LocaleCodes = new();

        // The editor theme stays on the toolbar and the inspector; the screens get the game's.
        readonly List<StyleSheet> m_EditorSheets = new();
        readonly List<(VisualElement owner, StyleSheet sheet)> m_BorrowedSheets = new();
        readonly List<VisualElement> m_EditorStyledParts = new();

        public PlayManagerState state => m_State;
        public IReadOnlyList<Card> cards => m_Cards;
        public ThemeStyleSheet currentTheme => m_Theme;
        public float zoom => m_Zoom;
        public BagelOptionStrip themeStrip => m_ThemeStrip;
        public BagelOptionStrip languageStrip => m_LanguageStrip;

        [MenuItem("Bagel/Bagel All UI", priority = 10, editorModes = new[] { BagelMenu.defaultMode, BagelMenu.uiToolkitAuthoringMode })]
        public static BagelAllUIWindow Open()
        {
            var window = GetWindow<BagelAllUIWindow>();
            window.titleContent = new GUIContent("Bagel All UI");
            window.minSize = new Vector2(720f, 480f);
            return window;
        }

        public void CreateGUI()
        {
            titleContent = new GUIContent("Bagel All UI");

            DestroyState();
            m_State = PlayManagerState.CreateRuntimeCopy(AssetDatabase.LoadAssetAtPath<PlayManagerState>(k_StartStatePath));
            m_State.propertyChanged += State_OnPropertyChanged;
            m_Themes = AssetDatabase.LoadAssetAtPath<BagelThemeCollection>(k_ThemeCollectionPath);

            var windowTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_WindowUxmlPath);
            if (windowTree == null)
            {
                rootVisualElement.Add(new Label($"Missing {k_WindowUxmlPath}"));
                return;
            }

            // Take the editor theme off; it would leak editor looks into the game's screens.
            m_EditorSheets.Clear();
            for (int i = 0; i < rootVisualElement.styleSheets.count; i++)
                m_EditorSheets.Add(rootVisualElement.styleSheets[i]);
            rootVisualElement.styleSheets.Clear();

            windowTree.CloneTree(rootVisualElement);

            var stage = rootVisualElement.Q<ScrollView>("stage");
            m_EditorStyledParts.Clear();
            m_EditorStyledParts.Add(rootVisualElement.Q("toolbar"));
            m_EditorStyledParts.Add(rootVisualElement.Q("inspector-dock"));
            m_EditorStyledParts.Add(stage.horizontalScroller);
            m_EditorStyledParts.Add(stage.verticalScroller);
            rootVisualElement.RegisterCallback<AttachToPanelEvent>(_ => LiftEditorTheme());
            rootVisualElement.RegisterCallback<DetachFromPanelEvent>(_ => RestoreEditorTheme());
            if (rootVisualElement.panel != null)
                LiftEditorTheme();
            else
                ShareEditorTheme();

            rootVisualElement.RegisterCallback<AttachToPanelEvent>(e => AddSteamChannels(e.destinationPanel));
            AddSteamChannels(rootVisualElement.panel);

            m_Theme = ThemeAt(m_State.settings.themeIndex);
            BuildCards();
            BindToolbar();
            BindInspector();
        }

        // Closed for good: the screens give their resources back to be reused now.
        void OnDestroy() => rootVisualElement.Clear(VisualElementClearOptions.RecursiveReleaseResources);

        void OnEnable() => BagelLocalization.localeChanged += UpdateLanguageStrip;

        void OnDisable()
        {
            BagelLocalization.localeChanged -= UpdateLanguageStrip;
            m_Roll?.Pause();
            RestoreEditorTheme();
            m_Cards.Clear();
            DestroyState();
        }

        void DestroyState()
        {
            if (m_State == null)
                return;

            m_State.propertyChanged -= State_OnPropertyChanged;
            PlayManagerState.DestroyRuntimeCopy(m_State);
            m_State = null;
        }

        // The steam's material moves its puffs with extra vertex channels: the window's panel draws them.
        static void AddSteamChannels(IPanel panel)
        {
            if (panel != null)
                panel.extraVertexChannels |= SteamPuffs.vertexChannels;
        }

        // Newer editors keep their theme on an ancestor of the window's root: borrow it while
        // the window is attached and give it back when it detaches (closed, docked elsewhere).
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
                if (part == null)
                    continue;
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

        void BuildCards()
        {
            m_Cards.Clear();
            var container = rootVisualElement.Q("cards");
            foreach (var spec in cardSpecs)
            {
                var card = CreateCard(spec);
                m_Cards.Add(card);

                // Added last: by now every screen has the shared state, so attaching doesn't make a copy per screen.
                container.Add(card.root);
            }
        }

        Card CreateCard(CardSpec spec)
        {
            var card = new Card { spec = spec };
            card.root = new VisualElement { name = "card-" + spec.title.ToLowerInvariant().Replace(' ', '-') };
            card.root.AddToClassList("b-card");
            card.root.Add(new Label(spec.title) { name = "card-title" });
            card.root[0].AddToClassList("b-card__title");

            card.slot = new VisualElement { name = "card-slot" };
            card.slot.AddToClassList("b-card__slot");
            card.content = new VisualElement { name = "card-content" };
            card.content.AddToClassList("b-card__content");
            card.slot.Add(card.content);
            card.root.Add(card.slot);

            // The slot takes the zoomed size of what's in it, so cards don't overlap.
            card.content.RegisterCallback<GeometryChangedEvent>(_ => FitSlot(card));
            card.content.style.scale = new Scale(new Vector3(m_Zoom, m_Zoom, 1f));

            foreach (var path in spec.uxmlPaths)
            {
                var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_UIPath + path);
                if (tree == null)
                {
                    card.content.Add(new Label($"Missing {path}"));
                    continue;
                }

                // The bagel cards show every bagel; the other screens show once.
                var count = path.Contains("TypeInfo") ? m_State.bagelTypeCount : 1;
                for (int i = 0; i < count; i++)
                {
                    var screen = tree.Instantiate();
                    screen.AddToClassList("b-card__screen");
                    if (spec.size != Vector2.zero)
                    {
                        screen.AddToClassList("b-card__screen--fixed");
                        screen.style.width = spec.size.x;
                        screen.style.height = spec.size.y;
                    }
                    if (m_Theme != null)
                        screen.styleSheets.Add(m_Theme);

                    PrepareScreen(screen, path, i);
                    card.screens.Add(screen);
                    card.content.Add(screen);
                }
            }
            return card;
        }

        void PrepareScreen(VisualElement screen, string path, int index)
        {
            // Every pane plays on the shared state.
            screen.Query<BagelPane>().ForEach(pane =>
            {
                if (pane.GetFirstAncestorOfType<BagelPane>() == null)
                    pane.SetState(m_State);
            });

            // The bagel cards show the bagel types, like the podiums do.
            if (path.Contains("TypeInfo"))
            {
                var bagelType = m_State.bagelTypes.collection[index];
                var pane = screen.Q("pane");
                pane.dataSource = bagelType;
                if (!string.IsNullOrEmpty(bagelType.nameKey))
                    BagelLocalization.Bind(pane.Q<Label>("name"), "text", bagelType.nameKey);
            }

            // The callout says something and keeps saying it, in the middle of its card.
            if (screen.Q<BagelCalloutManager>() is { } callout)
            {
                callout.stays = true;
                callout.style.left = 110f;
                callout.style.top = 70f;
                callout.RegisterCallback<AttachToPanelEvent>(_ => callout.Show(BagelLocalization.Get("toast.go", "Go!")));
            }

            // The window themes its own screens, so a toolbar preview doesn't stack on top.
            screen.RegisterCallback<AttachToPanelEvent>(_ =>
                screen.Query<BagelPane>().ForEach(pane => BagelThemes.RemovePreviewRoot(pane)));
        }

        void FitSlot(Card card)
        {
            var size = card.content.layout.size;
            if (float.IsNaN(size.x) || float.IsNaN(size.y))
                return;

            card.slot.style.width = size.x * m_Zoom;
            card.slot.style.height = size.y * m_Zoom;
        }

        public void SetZoom(float zoom)
        {
            m_Zoom = Mathf.Clamp(zoom, 0.5f, 1.5f);
            if (m_ZoomLabel != null)
                m_ZoomLabel.text = $"{Mathf.RoundToInt(m_Zoom * 100f)}%";
            foreach (var card in m_Cards)
            {
                card.content.style.scale = new Scale(new Vector3(m_Zoom, m_Zoom, 1f));
                FitSlot(card);
            }
        }

        // ---------------------------------------------------------------- Toolbar

        void BindToolbar()
        {
            rootVisualElement.Q<BagelOptionStrip>("reset-strip").AddAction(BagelEditorIcons.Reset(), "Reset", ResetWindow, "reset-button");

            VisualElement setups = null;
            setups = rootVisualElement.Q<BagelOptionStrip>("setup-strip").AddTextAction("Setups \u25BE",
                "Stage a moment of the game: a pane, a win or a loss, a tracker full or wrecked", () => ShowSetupMenu(setups), elementName: "setup-menu");

            m_RollToggle = rootVisualElement.Q<BagelOptionStrip>("roll-strip").AddToggle(BagelEditorIcons.Window("tool-simulate-roll"),
                "Simulate Roll", false, SimulateRoll, "roll-toggle");

            var zoomSlider = rootVisualElement.Q<Slider>("zoom-slider");
            m_ZoomLabel = rootVisualElement.Q<Label>("zoom-label");
            zoomSlider.SetValueWithoutNotify(m_Zoom);
            zoomSlider.RegisterValueChangedCallback(evt => SetZoom(evt.newValue));
            SetZoom(m_Zoom);

            m_ThemeStrip = rootVisualElement.Q<BagelOptionStrip>("theme-strip");
            for (int i = 0; i < (m_Themes?.collection.Count ?? 0); i++)
            {
                var entry = m_Themes.collection[i];
                m_ThemeStrip.AddOption(BagelEditorIcons.Theme(entry), entry.themeName, "theme-" + entry.nameKey.Replace("theme.", ""));
            }
            m_ThemeStrip.chosen += index => m_State.settings.themeIndex = index;
            UpdateThemeStrip();

            m_LanguageStrip = rootVisualElement.Q<BagelOptionStrip>("language-strip");
            m_LocaleCodes.Clear();
            if (LocalizationSettings.HasSettings)
            {
                foreach (var locale in LocalizationSettings.Instance.AvailableLocales)
                {
                    m_LocaleCodes.Add(locale.Code);
                    m_LanguageStrip.AddOption(BagelEditorIcons.Language(locale.Code), locale.LocaleName, "language-" + locale.Code);
                }
            }
            m_LanguageStrip.chosen += index => BagelToolbar.SelectLanguage(m_LocaleCodes[index]);
            UpdateLanguageStrip();

            m_InspectorToggle = rootVisualElement.Q<BagelOptionStrip>("inspector-strip").AddToggle(BagelEditorIcons.Inspector(), "State Inspector",
                m_ShowInspector, ShowInspector, "inspector-toggle");
            ShowInspector(m_ShowInspector);
        }

        void ShowInspector(bool show)
        {
            m_ShowInspector = show;
            BagelOptionStrip.SetOnWithoutNotify(m_InspectorToggle, show);
            rootVisualElement.Q("inspector-dock").EnableInClassList("b-inspector-dock--shown", show);
        }

        void ShowSetupMenu(VisualElement button)
        {
            var menu = new GenericMenu();
            foreach (var setup in setupNames)
            {
                var name = setup;
                menu.AddItem(new GUIContent(name), false, () => ApplySetup(name));
            }
            menu.DropDown(button.worldBound);
        }

        void UpdateThemeStrip() => m_ThemeStrip?.SetSelectedWithoutNotify(m_State != null ? m_State.settings.themeIndex : -1);

        void UpdateLanguageStrip() => m_LanguageStrip?.SetSelectedWithoutNotify(m_LocaleCodes.IndexOf(BagelLocalization.localeCode));

        // ---------------------------------------------------------------- Setups

        public static readonly string[] setupNames =
        {
            "Main Menu/Main Pane",
            "Main Menu/Settings",
            "Main Menu/Leaderboard",
            "Payment Tablet/Levels",
            "Payment Tablet/Level Editor",
            "Pause/Pause Pane",
            "Pause/Settings",
            "Game Over/Win",
            "Game Over/Lose",
            "Tracker/Full",
            "Tracker/Halfway",
            "Tracker/Wrecked",
        };

        public void ApplySetup(string setup)
        {
            var tracker = m_State.tracker;
            switch (setup)
            {
                case "Main Menu/Main Pane":
                    m_State.GoToMainMenu();
                    break;
                case "Main Menu/Settings":
                    m_State.GoToMainMenu();
                    m_State.SetMainMenuPane(PlayManagerState.MainMenuPane.Settings);
                    break;
                case "Main Menu/Leaderboard":
                    m_State.GoToMainMenu();
                    m_State.SetMainMenuPane(PlayManagerState.MainMenuPane.Leaderboard);
                    break;
                case "Payment Tablet/Levels":
                    m_State.GoToLevelSelection();
                    break;
                case "Payment Tablet/Level Editor":
                    m_State.GoToLevelSelection();
                    if (!m_State.canEditLevel)
                    {
                        var level = m_State.AddLevel("My Table");
                        foreach (var piece in m_State.levels[0].pieces)
                            level.Add(piece);
                    }
                    m_State.EditLevel(true);
                    break;
                case "Pause/Pause Pane":
                case "Pause/Settings":
                    if (!m_State.isPlaying)
                        m_State.GoToPlay();
                    m_State.Pause();
                    m_State.pausePane = setup == "Pause/Settings" ? PlayManagerState.PausePane.Settings : PlayManagerState.PausePane.Pause;
                    break;
                case "Game Over/Win":
                case "Game Over/Lose":
                    m_State.GoToPlay();
                    tracker.toppingsCount = setup == "Game Over/Win" ? Mathf.RoundToInt(tracker.toppingsMaxCount * 0.42f) : 0;
                    m_State.FinishRun();
                    break;
                case "Tracker/Full":
                case "Tracker/Halfway":
                case "Tracker/Wrecked":
                    m_State.GoToPlay();
                    var fraction = setup == "Tracker/Full" ? 1f : setup == "Tracker/Halfway" ? 0.5f : 0.05f;
                    tracker.toppingsCount = Mathf.RoundToInt(tracker.toppingsMaxCount * fraction);
                    tracker.spin = tracker.constants != null ? tracker.constants.spinMax * (1f - fraction) : 0f;
                    tracker.force = tracker.constants != null ? tracker.constants.forceMax * (1f - fraction) : 0f;
                    break;
            }
        }

        // Moves the tracker as if a bagel were rolling: spinning, bumping and shedding toppings.
        void SimulateRoll(bool on)
        {
            if (!on)
            {
                m_Roll?.Pause();
                return;
            }

            if (!m_State.isPlaying)
                m_State.GoToPlay();

            var time = 0f;
            m_Roll ??= rootVisualElement.schedule.Execute(timer =>
            {
                time += timer.deltaTime / 1000f;
                var tracker = m_State.tracker;
                var constants = tracker.constants;
                var spinMax = constants != null ? constants.spinMax : 15f;
                var forceMax = constants != null ? constants.forceMax : 20f;
                tracker.spin = Mathf.Ceil(spinMax * 0.5f * (1f + Mathf.Sin(time * 1.3f)));
                tracker.force = Mathf.Ceil(forceMax * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(time * 3.1f)), 8f));
                tracker.toppingsCount = tracker.toppingsCount > 0 ? tracker.toppingsCount - 1 : tracker.toppingsMaxCount;
            }).Every(50);
            m_Roll.Resume();
        }

        // ---------------------------------------------------------------- State

        // The toolbar's Reset, and the demo's (Bagel > Demo > Reset Bagel Project States): a fresh state,
        // the cards at 100% from the top, no Inspector.
        public void ResetWindow()
        {
            // A tab that was never shown has no UI yet: it starts fresh when it's built.
            if (rootVisualElement.Q("window") == null)
                return;

            ResetState();
            SetZoom(1f);
            rootVisualElement.Q<Slider>("zoom-slider")?.SetValueWithoutNotify(1f);
            ShowInspector(false);
            if (rootVisualElement.Q<ScrollView>("stage") is { } stage)
                stage.scrollOffset = Vector2.zero;
        }

        // Back to a fresh copy of the start state.
        public void ResetState()
        {
            m_Roll?.Pause();
            BagelOptionStrip.SetOnWithoutNotify(m_RollToggle, false);
            DestroyState();
            m_State = PlayManagerState.CreateRuntimeCopy(AssetDatabase.LoadAssetAtPath<PlayManagerState>(k_StartStatePath));
            m_State.propertyChanged += State_OnPropertyChanged;

            foreach (var card in m_Cards)
            {
                foreach (var screen in card.screens)
                {
                    screen.Query<BagelPane>().ForEach(pane =>
                    {
                        if (pane.parentPane == null)
                            pane.SetState(m_State);
                    });
                }
            }
            ApplyTheme(ThemeAt(m_State.settings.themeIndex));
            BindInspector();
            UpdateThemeStrip();
        }

        void BindInspector()
        {
            var dock = rootVisualElement.Q("inspector-dock");
            dock.Clear();
            dock.Add(new InspectorElement(m_State));
        }

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

            foreach (var card in m_Cards)
            {
                foreach (var screen in card.screens)
                {
                    if (m_Theme != null)
                        screen.styleSheets.Remove(m_Theme);
                    if (theme != null)
                        screen.styleSheets.Add(theme);
                }
            }
            m_Theme = theme;
        }
    }
}
