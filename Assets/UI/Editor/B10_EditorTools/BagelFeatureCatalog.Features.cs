using System.Collections.Generic;
using System.Text;
using Unity.Localization;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;

namespace Bagel
{
    // The features, by category. An example is a Bagel All UI card at one of its setups; its parts are
    // what it outlines.
    public static partial class BagelFeatureCatalog
    {
        const string k_UI = "Assets/UI/Runtime/";
        const string k_Editor = "Assets/UI/Editor/";
        const string k_Common = k_UI + "B00_Themes/T0_Common/BagelCommonStyles.uss";

        static Feature[] CreateFeatures() => new[]
        {
            // ------------------------------------------------------------------ UXML and elements

            F("uxml-templates", Category.Authoring, "Templates and attribute overrides",
                about: "<ui:Template> and <ui:Instance> put one UXML file inside another. <AttributeOverrides> changes an attribute of one element in that instance only.",
                usage: "The Pause, Game Over and Leaderboard pages are instances of one Page template, each with its own title. Settings is one template, on the blackboard and in the Pause screen.",
                Show("Pause (Screen Space)", "Pause/Pause Pane", Name("pause-pane"), Name("pause-page")),
                Src(k_UI + "B07_Pause/PauseScreen.uxml", "<ui:Template name=\"PausePane\"", "<ui:Template> names a UXML file to use here"),
                Src(k_UI + "B07_Pause/PauseScreen.uxml", "<ui:Instance template=\"SettingsPane\"", "<ui:Instance>: Settings, also on the blackboard"),
                Src(k_UI + "B07_Pause/PausePane.uxml", "<AttributeOverrides element-name=\"page-title\" text=\"Paused\"/>", "The Page template, its title overridden"),
                Src(k_UI + "B08_GameOver/GameOverPane.uxml", "<AttributeOverrides element-name=\"page-title\" text=\"Game Over\"/>", "The same template, another title"),
                Src(k_UI + "B02_Common/Page.uxml", "name=\"page-title\"", "Page.uxml: the title every page overrides"),
                UIBuilder(k_UI + "B07_Pause/PauseScreen.uxml"),
                UIBuilder(k_UI + "B02_Common/Page.uxml")),

            F("custom-elements", Category.Authoring, "Custom elements and UXML attributes",
                about: "[UxmlElement] makes a C# element a UXML tag and a UI Builder Library item. Each [UxmlAttribute] property is an attribute, even one that names an asset.",
                usage: "The blackboard's doodles are <Bagel.ChalkDoodle kind=\"Bagel\" seed=\"11\">, a hand of their own for each seed. Every screen's manager names, in UXML, the state asset it plays on by itself.",
                Show("Main Menu", "Main Menu/Main Pane", Is<ChalkDoodle>()),
                Src(k_UI + "B02_Common/ChalkDoodle.cs", "[UxmlElement(libraryPath = \"Bagel/Common\")]", "[UxmlElement], in the Library's Bagel/Common"),
                Src(k_UI + "B02_Common/ChalkDoodle.cs", "public Kind kind", "[UxmlAttribute] kind: an enum, a dropdown in UI Builder"),
                Src(k_UI + "B03_MainMenu/MainMenuScreen.uxml", "<Bagel.ChalkDoodle kind=\"Bagel\" seed=\"11\"", "The doodles, in UXML"),
                Src(k_UI + "B02_Common/BagelPane.cs", "public PlayManagerState startState { get; set; }", "An attribute that names an asset: a pane's start state"),
                Src(k_UI + "B03_MainMenu/MainMenuScreen.uxml", "start-state=\"project://database/Assets/Game/Core/PlayManagerState.asset", "start-state, in UXML"),
                Try("Draw them again", "Another seed for every doodle: another hand", window =>
                    window.example.Query<ChalkDoodle>().ForEach(doodle => doodle.seed += 7)),
                UIBuilder(k_UI + "B03_MainMenu/MainMenuScreen.uxml")),

            F("ui-components", Category.Authoring, "UI Components",
                about: "A [VisualElementComponent] struct adds behavior to any element without subclassing it: a tag inside the element in UXML, or AddComponent in C#.",
                usage: "PressSquish squishes the big buttons when they're pressed. HandLettered letters the blackboard by hand, and FollowsLanguage turns UI with no pane right to left.",
                Show("Main Menu", "Main Menu/Main Pane", Has<PressSquish>(), Has<HandLettered>()),
                Src(k_UI + "B02_Common/Components/PressSquish.cs", "[VisualElementComponent]", "A [VisualElementComponent] struct"),
                Src(k_UI + "B02_Common/Components/PressSquish.cs", "[UxmlAttribute] public float pressedScale;", "Its attribute, pressed-scale"),
                Src(k_UI + "B03_MainMenu/MainMenuPane.uxml", "<Bagel.PressSquish/>", "Added in UXML, inside the button"),
                Src(k_UI + "B02_Common/Components/HandLettered.cs", "[OnComponentAdded]", "[OnComponentAdded]: set up as it's added"),
                Src(k_UI + "B02_Common/Components/FollowsLanguage.cs", "[RegisterCallback(typeof(AttachToPanelEvent))]", "[RegisterCallback]: its own event callbacks"),
                Src("Assets/Tests/Runtime/B02_Common/PressSquishTests.cs", "button.AddComponent(new PressSquish { pressedScale = 0.8f });", "AddComponent, in C#"),
                UIBuilder(k_UI + "B03_MainMenu/MainMenuPane.uxml")),

            F("mesh-modifiers", Category.Authoring, "Mesh modifiers",
                about: "AddMeshModifier changes an element's meshes after they're made, and those of the elements inside it if you like: here, each letter's quad.",
                usage: "HandLettered turns each letter on the blackboard a little and lifts it off the line, as if chalked by hand. The same text always leans the same way, so nothing shimmers.",
                Show("Main Menu", "Main Menu/Main Pane", Class("b-title")),
                Src(k_UI + "B02_Common/Components/HandLettered.cs", "owner.AddMeshModifier(s_Letter, recursive: true)", "AddMeshModifier, recursive"),
                Src(k_UI + "B02_Common/Components/HandLettered.cs", "static void Letter(MeshModificationContext context)", "Each letter's quad, turned and lifted"),
                Src(k_UI + "B03_MainMenu/MainMenuScreen.uxml", "<Bagel.HandLettered/>", "On the board: everything written on it"),
                Try("Straight letters", "Take HandLettered off the board, or put it back", window =>
                {
                    var board = window.example.Q<MainMenuScreenManager>();
                    if (board == null)
                        return;
                    if (board.HasComponent<HandLettered>())
                        board.RemoveComponent<HandLettered>();
                    else
                        board.AddComponent(new HandLettered { lean = 4f, bounce = 0.06f });
                    board.MarkDirtyRepaint();
                })),

            // ------------------------------------------------------------------ Data and language

            F("data-binding", Category.DataAndLanguage, "Data binding",
                about: "An element's property follows a [CreateProperty] of its data source, by path: <ui:DataBinding> in UXML, or SetBinding in C#. A source that says what changed updates only that.",
                usage: "Each screen's top pane has the game's state as its data source: the gauges over the bagel follow its speed, spin and knocks, and the Settings sliders bind both ways.",
                Show("Tracker", "Tracker/Halfway", Is<BagelGauge>()),
                Src(k_UI + "B06_BagelTracker/BagelTrackerLeftDisplay.uxml", "<ui:DataBinding property=\"value\" data-source-path=\"speed\"", "<ui:DataBinding>: a gauge's value is the bagel's speed"),
                Src("Assets/Game/Core/PlayManagerState.cs", "INotifyBindablePropertyChanged, IDataSourceViewHashProvider", "The state says what changed"),
                Src(k_UI + "B02_Common/BagelPane.cs", "dataSource = m_State;", "The top pane's data source"),
                Src(k_UI + "B04_Settings/SettingsPaneForGame.uxml", "data-source-path=\"settings.fieldOfView\" binding-mode=\"TwoWay\"", "Two way: a slider sets the state"),
                Src(k_UI + "B05_BagelSelection/BagelSelectionTypeInfoPane.uxml", "data-source=\"project://database/Assets/Game/Bagels/BagelTypes/Sesame/BagelTypeSesame.asset", "An asset as a data source, in UXML"),
                Src(k_UI + "AssemblyInfo.cs", "[assembly: GeneratePropertyBagsForAssembly]", "Property bags made at compile time"),
                Try("Knock the bagel", "The state's spin and knock at their most: the gauges follow", window =>
                {
                    var tracker = window.state.tracker;
                    if (tracker.constants == null)
                        return;
                    tracker.spin = tracker.constants.spinMax;
                    tracker.force = tracker.constants.forceMax;
                }),
                Try("Speed up", "The state's speed, higher", window =>
                {
                    var tracker = window.state.tracker;
                    var max = tracker.constants != null ? tracker.constants.speedMax : 10f;
                    tracker.speed = tracker.speed >= max ? 0f : Mathf.Min(max, tracker.speed + max * 0.25f);
                }),
                UIBuilder(k_UI + "B06_BagelTracker/BagelTrackerLeftDisplay.uxml")),

            F("localization", Category.DataAndLanguage, "Localized strings",
                about: "The Localization package's LocalizedString is a binding: <loc:LocalizedString> in UXML, or SetBinding in C#. The text follows the selected language.",
                usage: "Every word the game shows comes from its BagelStrings table, in English, French, Chinese and Arabic. Pick another language and every screen changes at once.",
                Show("Main Menu", "Main Menu/Main Pane", new Part("LocalizedString", element => element.TryGetBinding("text", out var binding) && binding is LocalizedString)),
                Src(k_UI + "B03_MainMenu/MainMenuScreen.uxml", "<loc:LocalizedString property=\"text\" table=\"BagelStrings\" entry=\"specials.title\"/>", "<loc:LocalizedString> in UXML"),
                Src(k_UI + "B02_Common/BagelLocalization.cs", "element?.SetBinding(property, String(key));", "SetBinding in C#"),
                Src(k_UI + "B08_GameOver/GameOverPaneManager.cs", "m_StampString.SetReference(BagelLocalization.table, lastRun.won ? \"stamp.win\" : \"stamp.lose\");", "One binding, another entry: the stamp says won or lost"),
                Try("Next language", "The project's next language: every screen follows", window => window.NextLanguage()),
                Asset("Assets/Localization/BagelStrings/BagelStrings.asset")),

            F("right-to-left", Category.DataAndLanguage, "Right-to-left text and layout",
                about: "languageDirection = RTL shapes and orders an element's text right to left, with the Advanced Text Generator, and the elements inside inherit it.",
                usage: "In Arabic every screen's top pane turns right to left, and a b-rtl class mirrors the layout in USS: rows, toggles and tabs flip. The leaderboard's arrows stay as they are.",
                Show("Bagel Cards", null, Has<FollowsLanguage>()),
                Src(k_UI + "B02_Common/Components/FollowsLanguage.cs", "element.languageDirection = rightToLeft ? LanguageDirection.RTL : LanguageDirection.LTR;", "languageDirection, from the language"),
                Src(k_UI + "B02_Common/BagelPane.cs", "FollowsLanguage.Apply(this, BagelLocalization.isRightToLeft);", "Every screen's top pane follows it"),
                Src(k_UI + "B05_BagelSelection/BagelSelectionTypeInfoPane.uxml", "<Bagel.FollowsLanguage/>", "UI with no pane, in UXML"),
                Src(k_Common, ".b-rtl .unity-toggle,", "The layout mirrored in USS"),
                Src(k_UI + "B09_Leaderboard/LeaderboardPane.uxml", "name=\"previous-level-button\" language-direction=\"LTR\"", "An arrow that stays left to right"),
                Try("Arabic", "Every screen in Arabic", window => window.SetLanguage("ar")),
                Try("English", "Every screen in English", window => window.SetLanguage("en"))),

            // ------------------------------------------------------------------ Styling

            F("themes", Category.Styling, "Theme Style Sheets, swapped live",
                about: "A .tss theme @imports style sheets. Setting a PanelSettings' themeStyleSheet restyles every panel that uses it, at once.",
                usage: "Four themes (Classic chalk, Edgy neon, Daylight and High Contrast) import the common styles, then set their own variables. Settings swaps them while you play.",
                Show("Main Menu", "Main Menu/Settings", OpenTab("ui-settings-tab"), Name("theme-choice")),
                Src(k_UI + "B00_Themes/T1_Classic/BagelClassicTheme.tss", "@import url(\"/Assets/UI/Runtime/B00_Themes/T0_Common/BagelCommonStyles.uss\");", "A theme imports the common styles"),
                Src(k_UI + "B00_Themes/T1_Classic/BagelClassicTheme.tss", "@import url(\"BagelClassicThemeStyles.uss\");", "Then its own"),
                Src(k_UI + "B00_Themes/T0_Common/BagelThemeCollection.cs", "public ThemeStyleSheet theme;", "The themes Settings offers"),
                Src(k_UI + "B00_Themes/T0_Common/BagelThemes.cs", "settings.themeStyleSheet = theme;", "Swapped on the game's PanelSettings"),
                Src(k_UI + "B00_Themes/T1_Classic/BagelClassicThemeEditor.uxml", "<ui:Instance template=\"BagelThemeSampler\"", "A theme's sampler, to edit it in UI Builder"),
                Try("Next theme", "The window's next theme, as Settings picks it", window => window.NextTheme()),
                UIBuilder(k_UI + "B00_Themes/T1_Classic/BagelClassicThemeEditor.uxml"),
                Asset(k_UI + "B00_Themes/T0_Common/BagelThemeCollection.asset")),

            F("uss-variables", Category.Styling, "USS variables",
                about: "Custom properties (--name) inherit down the tree and are read with var(), even inside gradients and filters.",
                usage: "The colors, the blackboard's chalk filter and the TV's picture are --b-* variables on :root. A theme is mostly a block of other values for them.",
                Show("Television", "Game Over/Win", Name("tv-screen")),
                Src(k_Common, ":root {", "The variables, on :root"),
                Src(k_UI + "B08_GameOver/GameOverStyles.uss", "linear-gradient(180deg, var(--b-tv-screen-top) 0%, var(--b-tv-screen-bottom) 100%)", "var() in a gradient: the TV's picture"),
                Src(k_Common, "--b-board-filter: filter(", "A whole filter in a variable"),
                Src(k_UI + "B00_Themes/T2_Edgy/BagelEdgyThemeStyles.uss", "--b-board-filter: none;", "A theme's value: no chalk"),
                Try("Next theme", "The window's next theme: other values for the variables", window => window.NextTheme())),

            F("custom-style-properties", Category.Styling, "Custom style properties, read in C#",
                about: "CustomStyleProperty<T> reads a --property in C# once an element's styles resolve (CustomStyleResolvedEvent), so USS can set what code draws or does.",
                usage: "The gauges and stat meters take their colors from --b-gauge-* and --b-meter-color. Every sound the UI makes is named in USS (--b-sound-*), and made by the game's synth.",
                Show("Tracker", "Tracker/Halfway", Is<BagelGauge>()),
                Src(k_UI + "B06_BagelTracker/BagelGauge.cs", "static readonly CustomStyleProperty<Color> s_AccentProperty = new(\"--b-gauge-accent\");", "CustomStyleProperty<Color>"),
                Src(k_UI + "B06_BagelTracker/BagelGauge.cs", "RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);", "Read once styles resolve"),
                Src(k_UI + "B02_Common/StatMeter.cs", "new(\"--b-meter-color\")", "A stat meter's color"),
                Src(k_UI + "B12_Sound/BagelSounds.uss", "--b-sound-confirm: confirm;", "Sounds, named in USS"),
                Src(k_UI + "B12_Sound/BagelSound.cs", "static readonly CustomStyleProperty<string> s_Press = new(pressProperty);", "Read when something's pressed"),
                Try("Next theme", "The window's next theme: other colors for the gauges", window => window.NextTheme())),

            F("state-classes", Category.Styling, "Pseudo-classes and state classes",
                about: "Pseudo-classes (:hover, :focus, :checked, :disabled) follow an element's state by themselves. Other states are classes, set in C# with EnableInClassList.",
                usage: "Buttons light up on :hover and :focus. The payment tablet marks the chosen level's row with b-level-row--selected, and panes not in use are dimmed with a class.",
                Show("Payment Tablet", "Payment Tablet/Levels", Class(LevelSelectionScreenManager.selectedRowUssClassName)),
                Src(k_Common, ".unity-button:enabled:hover {", ":hover, on enabled buttons"),
                Src(k_Common, ".unity-toggle:checked .unity-toggle__checkmark {", ":checked"),
                Src(k_UI + "B14_LevelSelection/LevelSelectionScreenManager.cs", "m_Rows[i].EnableInClassList(selectedRowUssClassName, i == selected);", "A state class, set in C#"),
                Src(k_UI + "B02_Common/BagelPane.cs", "new UniqueStyleString(\"b-reduce-motion\")", "UniqueStyleString: a class name, made once"),
                Try("Choose the next table", "The state's next level: its row gets the class", window =>
                    window.state.selectedLevelIndex = (window.state.selectedLevelIndex + 1) % Mathf.Max(1, window.state.levelCount))),

            F("transitions", Category.Styling, "Transitions",
                about: "transition-property, -duration and -timing-function animate a style from its old value to its new one, even a filter's parameters.",
                usage: "The blackboard is wiped with an eraser and chalked again: classes change a custom filter's parameters, and transitions animate them. Panes fade, and Reduce Motion turns it all off.",
                Show("Main Menu", "Main Menu/Main Pane", Name("board-specials"), Name("settings-pane")),
                Src(k_UI + "B03_MainMenu/MainMenuStyles.uss", "transition-property: filter;", "A filter, transitioned"),
                Src(k_UI + "B03_MainMenu/MainMenuStyles.uss", ".b-wipe--erasing {", "The eraser's class"),
                Src(k_UI + "B02_Common/ChalkWipe.cs", "public static readonly UniqueStyleString erasingUssClassName = new(\"b-wipe--erasing\");", "Set from C#, step by step"),
                Src(k_Common, "transition-property: background-color, border-color, filter;", "Buttons ease between their states"),
                Src(k_Common, ".b-reduce-motion .unity-button,", "Reduce Motion: no transitions"),
                Try("Wipe to Settings", "The board wiped, and Settings written in", window => window.state.SetMainMenuPane(PlayManagerState.MainMenuPane.Settings)),
                Try("Wipe back", "Settings wiped, and the specials written in", window => window.state.SetMainMenuPane(PlayManagerState.MainMenuPane.Main))),

            F("svg-icons", Category.Styling, "SVG icons",
                about: "An imported .svg is a VectorImage: a background-image, or an Image's, that stays sharp at any size.",
                usage: "The bagel stats, the themes and the languages are SVG icons, one USS class each (b-icon--stat-toppings), so any element can show one. The Editor's Bagel strips use them too.",
                Show("Bagel Cards", null, Class("b-icon")),
                Src(k_UI + "B02_Common/Icons/BagelIcons.uss", ".b-icon--stat-toppings {", "A class per icon"),
                Src(k_UI + "B02_Common/Icons/BagelIcons.uss", "background-image: url(\"stat-toppings.svg\");", "The SVG, as a background"),
                Src(k_UI + "B05_BagelSelection/BagelSelectionTypeInfoPane.uxml", "class=\"b-icon b-icon--stat-toppings b-tag__icon\"", "Shown on the bagel cards"),
                Src(k_Editor + "B10_EditorTools/BagelEditorIcons.cs", "Background.FromVectorImage(image)", "The same icons, in the Editor"),
                Asset(k_UI + "B02_Common/Icons/stat-toppings.svg")),

            // ------------------------------------------------------------------ Drawing and effects

            F("gradients", Category.Effects, "Linear and radial gradients",
                about: "background-image takes linear-gradient() and radial-gradient(), their colors from variables if you like.",
                usage: "The TV's picture fades from top to bottom, the bagel cards and the gauges have a radial shine, and the paused game darkens toward its edges.",
                Show("Television", "Game Over/Win", Name("tv-screen")),
                Src(k_UI + "B08_GameOver/GameOverStyles.uss", "background-image: linear-gradient(180deg, var(--b-tv-screen-top)", "The TV's picture"),
                Src(k_UI + "B05_BagelSelection/BagelSelectionTypeInfoPaneStyles.uss", "radial-gradient(ellipse farthest-corner at 20% 0%", "A bagel card's shine"),
                Src(k_UI + "B06_BagelTracker/BagelTrackerStyles.uss", "radial-gradient(ellipse farthest-side at 50% 25%", "A gauge's glass"),
                Src(k_UI + "B07_Pause/PauseScreenStyles.uss", "radial-gradient(ellipse farthest-corner at center", "The paused game, darker at its edges"),
                Try("Next theme", "The window's next theme: other colors for the TV", window => window.NextTheme())),

            F("filters", Category.Effects, "Filters, built in and custom",
                about: "filter applies effects to an element and everything in it: built-in ones like drop-shadow() and blur(), or your own, a shader in a filter asset.",
                usage: "Everything written on the blackboard goes through a custom Chalk filter that gives it chalk's grain. Turning visual effects off in Settings turns the filters off.",
                Show("Main Menu", "Main Menu/Main Pane", Class("b-chalk")),
                Src(k_Common, "--b-board-filter: filter(\"/Assets/UI/Runtime/B00_Themes/T0_Common/Filters/Chalk.asset\" 0.55 1.6);", "A custom filter, with its parameters"),
                Src(k_UI + "B03_MainMenu/MainMenuStyles.uss", "filter: var(--b-board-filter);", "On what's chalked"),
                Src(k_UI + "B00_Themes/T0_Common/Filters/Chalk.shader", "Shader \"", "Its shader"),
                Src(k_Common, "filter: drop-shadow(0 0 6 var(--b-glow-color));", "drop-shadow(): a glow"),
                Src(k_Common, "filter: none;", "Visual effects off"),
                Try("Visual effects on or off", "The state's Visual Effects setting, as in Settings", window =>
                    window.state.settings.visualEffects = !window.state.settings.visualEffects),
                Asset(k_UI + "B00_Themes/T0_Common/Filters/Chalk.asset")),

            F("backdrop-filter", Category.Effects, "Backdrop filter",
                about: "backdrop-filter filters what's behind an element, a blur for frosted glass. It works on screen-space panels.",
                usage: "The Pause screen, the callouts and the touch controls are frosted glass over the game going on behind them.",
                Show("Pause (Screen Space)", "Pause/Pause Pane", Name("pause-pane")),
                Src(k_UI + "B07_Pause/PauseScreenStyles.uss", "backdrop-filter: blur(18);", "Frosted glass, over the paused game"),
                Src(k_UI + "B06_BagelTracker/BagelCalloutStyles.uss", "backdrop-filter: blur(8);", "A callout's glass"),
                Src(k_UI + "B15_TouchControls/TouchControlsStyles.uss", "backdrop-filter: blur(18);", "The thumb stick's glass"),
                Src(k_Common, "backdrop-filter: none;", "Visual effects off"),
                Try("Visual effects on or off", "The state's Visual Effects setting, as in Settings", window =>
                    window.state.settings.visualEffects = !window.state.settings.visualEffects)),

            F("custom-materials", Category.Effects, "Custom materials and vertex channels",
                about: "-unity-material draws an element with your own shader, such as a Shader Graph made for UI. Extra vertex channels give it more data per vertex.",
                usage: "The steam over the toaster is puffs that its material moves, with extra vertex channels. The Edgy theme's neon buttons are a Shader Graph material: pick Edgy in the toolbar and look at Pause.",
                Show("Steam (World Space)", null, Is<SteamPuffs>()),
                Src(k_UI + "B13_Kitchen/SteamStyles.uss", "-unity-material: url(\"Steam.mat\");", "The steam's material"),
                Src(k_UI + "B13_Kitchen/SteamPuffs.cs", "public const ExtraVertexChannels vertexChannels", "Extra vertex channels"),
                Src(k_Editor + "B10_EditorTools/BagelAllUIWindow.cs", "panel.extraVertexChannels |= SteamPuffs.vertexChannels;", "A panel draws them"),
                Src(k_UI + "B00_Themes/T2_Edgy/BagelEdgyThemeStyles.uss", "-unity-material: url(", "Edgy: neon buttons, a Shader Graph"),
                Src(k_Common, "-unity-material can't come from a variable", "Not from a variable: one rule per theme"),
                Asset(k_UI + "B00_Themes/T2_Edgy/BagelEdgyThemeButtonShader.shadergraph")),

            F("vector-drawing", Category.Effects, "Drawing with Painter2D",
                about: "generateVisualContent and its Painter2D draw lines, arcs and shapes in an element, as vectors, again each time it's marked dirty.",
                usage: "The chalk doodles, the gauges over the bagel, the stat meters and the tablet's maps of the levels are all drawn in code.",
                Show("Payment Tablet", "Payment Tablet/Levels", Is<BagelLevelMap>(), Is<ChalkDoodle>()),
                Src(k_UI + "B02_Common/ChalkDoodle.cs", "generateVisualContent += Draw;", "A doodle draws itself"),
                Src(k_UI + "B02_Common/ChalkDoodle.cs", "var painter = context.painter2D;", "With Painter2D"),
                Src(k_UI + "B14_LevelSelection/BagelLevelMap.cs", "m_Cloth.generateVisualContent += DrawCloth;", "A level's map: its gingham cloth"),
                Src(k_UI + "B06_BagelTracker/BagelGauge.cs", "generateVisualContent += Draw;", "A gauge"),
                Src(k_UI + "B02_Common/StatMeter.cs", "generateVisualContent += Draw;", "A stat meter"),
                Try("Draw them again", "Another seed for every doodle", window =>
                    window.example.Query<ChalkDoodle>().ForEach(doodle => doodle.seed += 7))),

            F("ui-animation-clips", Category.Effects, "UIAnimationClip",
                about: "-unity-animation-clip plays an animation clip on an element's styles. A one-shot clip plays again when its class is added again.",
                usage: "The stamp on the game over receipt slams down each time it shows, and the callouts pop in and fade away.",
                Show("Television", "Game Over/Win", Name("stamp")),
                Src(k_UI + "B08_GameOver/GameOverStyles.uss", "-unity-animation-clip: url(\"/Assets/UI/Runtime/B08_GameOver/GameOverStampAnimation.asset\");", "The stamp's clip, in USS"),
                Src(k_UI + "B08_GameOver/GameOverPaneManager.cs", "stamp.schedule.Execute(() => stamp.AddToClassList(stampSlamUssClassName)).ExecuteLater(16);", "Its class put back, to play it again"),
                Src(k_UI + "B06_BagelTracker/BagelCalloutStyles.uss", "-unity-animation-clip: url(\"/Assets/UI/Runtime/B06_BagelTracker/BagelCalloutAnimation.asset\");", "A callout's clip"),
                Try("Stamp it again", "Its class taken off, and put back a frame later", window =>
                {
                    var stamp = window.example.Q<Label>("stamp");
                    if (stamp == null)
                        return;
                    stamp.RemoveFromClassList(GameOverPaneManager.stampSlamUssClassName);
                    stamp.schedule.Execute(() => stamp.AddToClassList(GameOverPaneManager.stampSlamUssClassName)).ExecuteLater(16);
                }),
                Asset(k_UI + "B08_GameOver/GameOverStampAnimation.asset")),

            // ------------------------------------------------------------------ Controls and input

            F("multi-column-list-view", Category.Controls, "MultiColumnListView",
                about: "A list with columns that only makes the rows in view. Its columns are in UXML, each column's cells made and bound in C#, with sorting and an element for when it's empty.",
                usage: "The leaderboard on the TV: the name, the toppings left and the time, one board per table, the best run first.",
                Show("Television", "Main Menu/Leaderboard", Is<MultiColumnListView>()),
                Src(k_UI + "B09_Leaderboard/LeaderboardPane.uxml", "<ui:MultiColumnListView", "In UXML"),
                Src(k_UI + "B09_Leaderboard/LeaderboardPane.uxml", "<ui:Column name=\"toppings\" title=\"Toppings Left\"", "Its columns, in UXML too"),
                Src(k_UI + "B09_Leaderboard/LeaderboardManager.cs", "leaderboardTable.columns[\"name\"].makeCell = () => new Label();", "Each column's cells, made in C#"),
                Src(k_UI + "B09_Leaderboard/LeaderboardManager.cs", "leaderboardTable.columns[\"toppings\"].comparison = (a, b) =>", "Sorted by a column"),
                Src(k_UI + "B09_Leaderboard/LeaderboardManager.cs", "leaderboardTable.makeNoneElement = () =>", "When it's empty"),
                Try("Add a run", "A run on the shown table, in the state", window =>
                {
                    var leaderboard = window.state.leaderboard;
                    leaderboard.Add("You", Random.Range(5, 60), Random.Range(20f, 90f), leaderboard.shownLevel);
                }),
                Try("Next table", "The next table's board", window => window.state.ShowLeaderboardLevel(1)),
                UIBuilder(k_UI + "B09_Leaderboard/LeaderboardPane.uxml")),

            F("tab-view", Category.Controls, "TabView",
                about: "TabView and Tab: the tabs in UXML, each with what it shows. activeTab says which one is open.",
                usage: "Settings has five tabs (Game, Look, Graphics, Controls and About), each a template instance. Moving the focus onto a tab's header opens it.",
                Show("Main Menu", "Main Menu/Settings", Is<TabView>()),
                Src(k_UI + "B04_Settings/SettingsPane.uxml", "<ui:TabView name=\"settings-tab-view\"", "In UXML"),
                Src(k_UI + "B04_Settings/SettingsPane.uxml", "<ui:Tab label=\"Game\" name=\"game-settings-tab\"", "A tab"),
                Src(k_UI + "B04_Settings/SettingsPaneManager.cs", "header.RegisterCallback<FocusInEvent>(_ => tabView.activeTab = tab);", "The focus opens a tab"),
                Src(k_Common, ".unity-tab__header:hover {", "Its headers, styled"),
                Try("Next tab", "The next tab, open", window =>
                {
                    var tabs = window.example.Q<TabView>();
                    if (tabs != null)
                        tabs.selectedTabIndex = (tabs.selectedTabIndex + 1) % Mathf.Max(1, tabs.Query<Tab>().ToList().Count);
                })),

            F("toggle-button-group", Category.Controls, "ToggleButtonGroup",
                about: "A row of buttons that picks one, or several. Its value is a ToggleButtonGroupState: a bit for each button.",
                usage: "Settings picks the theme and the language with groups of cards, each with its icon.",
                Show("Main Menu", "Main Menu/Settings", OpenTab("ui-settings-tab"), Is<ToggleButtonGroup>()),
                Src(k_UI + "B04_Settings/SettingsPaneForUI.uxml", "<ui:ToggleButtonGroup label=\"Theme\" name=\"theme-choice\"", "In UXML"),
                Src(k_UI + "B02_Common/Choice.cs", "public static ToggleButtonGroupState One(int index, int count) =>", "One button's state"),
                Src(k_UI + "B04_Settings/SettingsPaneManagerForUI.cs", "themeChoice.RegisterValueChangedCallback(evt =>", "Its value, chosen"),
                Try("Next theme", "The state's next theme: the group follows", window => window.NextTheme())),

            F("navigation", Category.Controls, "Focus and navigation events",
                about: "Focusable elements take the keyboard's and the gamepad's focus. NavigationMove, Submit and Cancel events come from either, the same way.",
                usage: "On the payment tablet each level's row takes the focus: moving it chooses the level, Submit plays it, and Cancel goes back to the bagels.",
                Show("Payment Tablet", "Payment Tablet/Levels", Class(LevelSelectionScreenManager.rowUssClassName)),
                Src(k_UI + "B14_LevelSelection/LevelSelectionScreenManager.cs", "focusable = true, tabIndex = 0", "A row takes the focus"),
                Src(k_UI + "B14_LevelSelection/LevelSelectionScreenManager.cs", "m_PanelRoot.RegisterCallback<NavigationSubmitEvent>(OnPanelNavigation);", "Submit, from a key or a button"),
                Src(k_UI + "B14_LevelSelection/LevelSelectionScreenManager.cs", "RegisterCallback<NavigationCancelEvent>(OnNavigationCancelEvent);", "Cancel"),
                Src(k_UI + "B14_LevelSelection/LevelSelectionScreenManager.cs", "m_Rows[index].Focus();", "Focus() from C#"),
                Try("Focus the next row", "The next level's row takes the focus, and chooses it", window =>
                {
                    var rows = window.example.Query(className: LevelSelectionScreenManager.rowUssClassName).ToList();
                    if (rows.Count > 0)
                        rows[(window.state.selectedLevelIndex + 1) % rows.Count].Focus();
                })),

            F("pointer-capture", Category.Controls, "Pointer events and capture",
                about: "Pointer events come from the mouse, touch and pens alike. An element that captures the pointer keeps getting its events after it leaves.",
                usage: "In the level editor a piece follows the pointer across the counter until it's let go. A long-press button keeps its pointer while it fills. On a touch screen the thumb stick keeps the finger that pressed it, while another presses pause.",
                Show("Payment Tablet", "Payment Tablet/Level Editor", Is<BagelLevelMap>(), Is<LongPressButton>()),
                Src(k_UI + "B14_LevelSelection/BagelLevelEditor.cs", "m_Map.RegisterCallback<PointerDownEvent>(OnMapPointerDown);", "A press on the map"),
                Src(k_UI + "B14_LevelSelection/BagelLevelEditor.cs", "m_Map.CapturePointer(evt.pointerId);", "The pointer, captured"),
                Src(k_UI + "B02_Common/LongPressButton.cs", "PointerCaptureHelper.CapturePointer(this, 0);", "A long press, held"),
                Src(k_UI + "B02_Common/LongPressButton.cs", "m_HoldScheduledItem = schedule.Execute(AnimateHoldProgress).Every(16);", "Filling, on a schedule"),
                Src(k_UI + "B15_TouchControls/BagelTouchStick.cs", "this.CapturePointer(m_PointerId);", "One finger of several, kept")),

            // ------------------------------------------------------------------ Panels

            F("world-space-panels", Category.Panels, "World-space panels",
                about: "A PanelRenderer draws a panel in the 3D scene, with a world-space PanelSettings and a size in the world. It takes clicks through the scene's colliders.",
                usage: "Almost all of the game's UI is in the bakery: the blackboard, the cash register, the payment tablet, the TV and the gauges over the bagel. Only Pause, the callouts and the touch controls are on the screen.",
                Show("Payment Tablet", "Payment Tablet/Levels", Is<LevelSelectionScreenManager>()),
                Src(k_UI + "B01_PanelSettings/TabletPanelSettings.asset", "m_RenderMode: 1", "World space"),
                Src("Assets/Scenes/Main.unity", "m_Name: LevelSelectionScreenUI", "The tablet's PanelRenderer"),
                Src("Assets/Scenes/Main.unity", "m_WorldSpaceWidth: 800", "Its size in the world: fixed"),
                Src(k_UI + "B01_PanelSettings/ScreenSpacePanelSettings.asset", "m_RenderMode: 0", "Screen space: Pause"),
                SceneObject("LevelSelectionScreenUI", "The tablet's PanelRenderer"),
                Asset(k_UI + "B01_PanelSettings/TabletPanelSettings.asset")),

            F("render-texture-images", Category.Panels, "Live 3D in an Image",
                about: "An Image shows any texture, a RenderTexture too, and its uv picks a part of it: a camera's picture, live in the UI.",
                usage: "The level editor's library shows each thing for the counter turning in 3D, all from one camera rendering one texture. Drag a picture to spin it.",
                Show("Payment Tablet", "Payment Tablet/Level Editor", Class(BagelLevelEditor.thumbnailUssClassName)),
                Src("Assets/Game/Levels/BagelLevelStudio.cs", "m_Texture = new RenderTexture(", "One texture for every picture"),
                Src("Assets/Game/Levels/BagelLevelStudio.cs", "public Rect UV(string kind)", "Each thing's part of it"),
                Src(k_UI + "B14_LevelSelection/BagelLevelEditor.cs", "picture.image = m_Thumbnails.texture;", "An Image shows it"),
                Src(k_UI + "B14_LevelSelection/BagelLevelEditor.cs", "picture.uv = m_Thumbnails.UV(kind);", "Its part, by uv"),
                Menu("Bagel/Bagel Levels", "Bagel Levels window")),

            F("accessibility", Category.Panels, "Screen readers",
                about: "An AccessibilityHierarchy of nodes, each with a role, a label and a value, tells a screen reader what's on the screen. Unity makes one for screen-space panels.",
                usage: "Most of the game's UI is in world space, so the game makes the hierarchy itself, from the screen in front of the player, each node framed through the camera.",
                Show("Payment Tablet", "Payment Tablet/Levels", Class(BagelAccessibility.itemUssClassName)),
                Src(k_UI + "B11_Accessibility/BagelAccessibility.cs", "public static AccessibilityHierarchy Build(VisualElement screenRoot, PanelRenderer panelRenderer)", "The hierarchy, made from a screen"),
                Src(k_UI + "B11_Accessibility/BagelAccessibility.cs", "AssistiveSupport.activeHierarchy = s_Hierarchy;", "Given to the screen reader"),
                Src(k_UI + "B11_Accessibility/BagelAccessibility.cs", "public static readonly string itemUssClassName = \"b-accessible-item\";", "A row read like a button"),
                Try("Read it", "What a screen reader is told about this screen", window => window.ShowNote(ReadAloud(window.example)))),

            // ------------------------------------------------------------------ Editor and tests

            F("editor-windows", Category.EditorAndTests, "Editor windows",
                about: "An EditorWindow makes its UI in CreateGUI from UXML and USS: the same elements as the game's, the Editor's own controls, and its theme.",
                usage: "Bagel All UI shows every screen at once on a throwaway state, Bagel Levels edits the game's levels in the tablet's own level editor, and this window is one too.",
                null,
                Src(k_Editor + "B10_EditorTools/BagelAllUIWindow.cs", "public void CreateGUI()", "CreateGUI"),
                Src(k_Editor + "B10_EditorTools/BagelAllUIWindow.uxml", "editor-extension-mode=\"True\"", "Its UXML, with the Editor's elements"),
                Src(k_Editor + "B10_EditorTools/BagelEditorTheme.cs", "public void Share(VisualElement part)", "The Editor's theme on its own parts only"),
                Src(k_Editor + "B14_LevelSelection/BagelLevelsWindow.cs", "m_Editor = new BagelLevelEditor", "The game's level editor, in a window"),
                Menu("Bagel/Bagel All UI", "Bagel All UI window"),
                Menu("Bagel/Bagel Levels", "Bagel Levels window")),

            F("property-drawers", Category.EditorAndTests, "Property drawers",
                about: "A PropertyDrawer's CreatePropertyGUI makes a field's Inspector from elements. A UxmlSerializedDataPropertyDrawer does it for an element's attributes in UI Builder.",
                usage: "The state's leaderboard is a MultiColumnListView in the Inspector, below. In UI Builder, a gauge's kind is picked from pictures of the gauges.",
                StateInspector(),
                Src(k_Editor + "B09_Leaderboard/LeaderboardDataDrawer.cs", "[CustomPropertyDrawer(typeof(LeaderboardData))]", "A drawer for the leaderboard"),
                Src(k_Editor + "B09_Leaderboard/LeaderboardDataDrawer.cs", "public override VisualElement CreatePropertyGUI(SerializedProperty property)", "Its UI, made of elements"),
                Src(k_Editor + "B06_BagelTracker/BagelGaugeDrawer.cs", "public class BagelGaugeDrawer : UxmlSerializedDataPropertyDrawer", "A drawer for UI Builder's attributes"),
                Asset("Assets/Game/Core/PlayManagerState.asset"),
                UIBuilder(k_UI + "B06_BagelTracker/BagelTrackerLeftDisplay.uxml")),

            F("main-toolbar", Category.EditorAndTests, "Main toolbar elements",
                about: "[MainToolbarElement] adds buttons, toggles and dropdowns to the Editor's main toolbar, where they can be moved and hidden.",
                usage: "The Bagel strips in the main toolbar preview a theme on every panel and switch the language, without Play mode.",
                null,
                Src(k_Editor + "B10_EditorTools/BagelToolbar.cs", "[MainToolbarElement(themePath", "The theme strip"),
                Src(k_Editor + "B10_EditorTools/BagelToolbar.cs", "[MainToolbarElement(languagePath", "The language strip"),
                Src(k_Editor + "B10_EditorTools/BagelToolbar.cs", "static MainToolbarElement Option(", "An option in a strip"),
                Do("Show the Bagel strips", "Show the theme and language strips in the main toolbar", () => BagelToolbar.Show())),

            F("ui-test-framework", Category.EditorAndTests, "UI Test Framework",
                about: "UITestFixture and EditorWindowUITestFixture give a test a panel to put UI in, and simulate clicks, keys and frames on it.",
                usage: "Every screen and window has tests that click through it like a player, hundreds of them, in Edit and Play mode. This window's tests check every line it links to is still there.",
                null,
                Src("Assets/Tests/Runtime/B14_LevelSelection/LevelSelectionScreenTests.cs", "public class LevelSelectionScreenTests : UITestFixture", "A screen's tests"),
                Src("Assets/Tests/Runtime/B14_LevelSelection/LevelSelectionScreenTests.cs", "simulate.Click(m_Screen.backButton);", "A click, simulated"),
                Src("Assets/Tests/Editor/B14_LevelSelection/BagelLevelsWindowTests.cs", "EditorWindowUITestFixture<BagelLevelsWindow>", "A window's tests"),
                Src("Assets/Tests/Editor/B10_EditorTools/BagelFeaturesWindowTests.cs", "public void EverySourceIsWhereItSays()", "This window's tests"),
                Menu("Window/General/Test Runner", "Test Runner")),
        };

        // What a setup can't do: open a tab of Settings.
        static System.Action<VisualElement> OpenTab(string tabName) => root =>
        {
            var tabs = root.Q<TabView>();
            var tab = root.Q<Tab>(tabName);
            if (tabs != null && tab != null)
                tabs.activeTab = tab;
        };

        // What a screen reader is told about a screen: each node's role, label and value, indented under its parent.
        public static string ReadAloud(VisualElement root)
        {
            var screen = root?.Q<BagelPane>();
            if (screen == null)
                return "";
            if (Application.isPlaying)
                return "Out of Play mode only: the game's own screen reader hierarchy is in use.";

            var text = new StringBuilder();
            void Add(IReadOnlyList<AccessibilityNode> nodes, int depth)
            {
                foreach (var node in nodes)
                {
                    text.Append(' ', depth * 4).Append(node.role).Append(": ").Append(node.label);
                    if (!string.IsNullOrEmpty(node.value))
                        text.Append(" (").Append(node.value).Append(')');
                    text.Append('\n');
                    Add(node.children, depth + 1);
                }
            }
            Add(BagelAccessibility.Build(screen, null).rootNodes, 0);
            return text.ToString().TrimEnd();
        }
    }
}
