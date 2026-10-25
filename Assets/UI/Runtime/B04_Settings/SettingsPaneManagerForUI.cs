using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // Settings › Look: the theme, picked from a card for each, and Reduce Motion and Visual Effects. Each card shows
    // its theme's icon, its name (the button's own text), and a few of its colors, in that theme whichever one is on:
    // the theme's style sheet is on the swatch itself (its :root), so the swatch's colors are that theme's.
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class SettingsPaneManagerForUI : BagelPane
    {
        public static readonly string themeCardUssClassName = "b-theme-card";
        public static readonly string themeCardIconUssClassName = themeCardUssClassName + "__icon";
        public static readonly string themeCardSwatchUssClassName = themeCardUssClassName + "__swatch";
        public static readonly string themeCardDotUssClassName = themeCardUssClassName + "__dot";

        // The colors a card shows, one dot each: the board's chalk, the paper's ink, the window's gold, a gauge's needle.
        static readonly string[] s_Dots = { "board", "paper", "glass", "gauge" };

        ToggleButtonGroup m_ThemeChoice;

        public ToggleButtonGroup themeChoice => m_ThemeChoice ??= this.Q<ToggleButtonGroup>("theme-choice");

        // The theme cards, in the collection's order.
        public Button GetThemeCard(int index) => themeChoice?.Q<Button>("theme-card-" + index);

        [UxmlAttribute]
        public PanelSettingsCollection panelSettingsCollection { get; set; }

        [UxmlAttribute]
        public BagelThemeCollection bagelThemeCollection { get; set; }

        // Set by the in-game Drivers: only the running game swaps the PanelSettings themes.
        public bool appliesThemeToPanelSettings
        {
            get => m_AppliesThemeToPanelSettings;
            set
            {
                m_AppliesThemeToPanelSettings = value;
                if (value && state != null)
                    ChangeTheme(state.settings.themeIndex);
            }
        }

        bool m_AppliesThemeToPanelSettings;

        public event EventHandler<ThemeStyleSheet> OnThemeChange;

        bool m_Bound;

        protected override void OnAttached()
        {
            if (m_Bound)
                return;
            m_Bound = true;

            var themes = bagelThemeCollection?.collection;
            if (themes == null || themes.Count == 0 || themeChoice == null)
                return;

            for (int i = 0; i < themes.Count; i++)
                themeChoice.Add(MakeCard(i, themes[i]));
            UpdateThemeNames();
            UpdateThemeChoice();
            themeChoice.RegisterValueChangedCallback(evt =>
            {
                var index = Choice.Selected(evt.newValue);
                if (index >= 0 && state != null)
                    state.settings.themeIndex = index;
            });
        }

        // The card's size is set in USS: a button with children inside doesn't size itself to its text.
        static Button MakeCard(int index, BagelThemeCollection.ThemeEntry theme)
        {
            var card = new Button { name = "theme-card-" + index, text = theme.themeName };
            card.AddToClassList(themeCardUssClassName);

            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList(themeCardIconUssClassName);
            if (theme.icon != null)
                icon.style.backgroundImage = Background.FromVectorImage(theme.icon);
            card.Add(icon);

            var swatch = new VisualElement { pickingMode = PickingMode.Ignore };
            swatch.AddToClassList(themeCardSwatchUssClassName);
            if (theme.theme != null)
                swatch.styleSheets.Add(theme.theme);
            foreach (var dot in s_Dots)
            {
                var element = new VisualElement { pickingMode = PickingMode.Ignore };
                element.AddToClassList(themeCardDotUssClassName);
                element.AddToClassList(themeCardDotUssClassName + "--" + dot);
                swatch.Add(element);
            }
            card.Add(swatch);
            return card;
        }

        protected override void OnLocaleChanged() => UpdateThemeNames();

        // Theme names come from the collection, so they're set for each language.
        void UpdateThemeNames()
        {
            var themes = bagelThemeCollection?.collection;
            if (themes == null)
                return;

            for (int i = 0; i < themes.Count; i++)
            {
                var card = GetThemeCard(i);
                if (card != null)
                    card.text = string.IsNullOrEmpty(themes[i].nameKey) ? themes[i].themeName : BagelLocalization.Get(themes[i].nameKey, themes[i].themeName);
            }
        }

        void UpdateThemeChoice()
        {
            var count = bagelThemeCollection?.collection?.Count ?? 0;
            if (themeChoice == null || count == 0)
                return;
            var index = state != null ? state.settings.themeIndex : 0;
            themeChoice.SetValueWithoutNotify(Choice.One(Mathf.Clamp(index, 0, count - 1), count));
        }

        protected override void OnStateChanged(PlayManagerState oldState, PlayManagerState newState)
        {
            UpdateThemeChoice();
            if (newState != null && m_AppliesThemeToPanelSettings)
                ChangeTheme(newState.settings.themeIndex);
        }

        protected override void OnStatePropertyChanged(in BindingId property)
        {
            if (property == SettingsData.themeIndexPath)
            {
                UpdateThemeChoice();
                ChangeTheme(state.settings.themeIndex);
            }
        }

        void ChangeTheme(int index)
        {
            var themes = bagelThemeCollection?.collection;
            if (themes == null || themes.Count == 0)
                return;

            var theme = themes[Mathf.Clamp(index, 0, themes.Count - 1)].theme;

            OnThemeChange?.Invoke(this, theme);

            if (m_AppliesThemeToPanelSettings && Application.isPlaying)
                BagelThemes.ApplyToPanelSettings(panelSettingsCollection, theme);
        }
    }
}
