using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class SettingsPaneManagerForUI : BagelPane
    {
        DropdownField m_ThemeDropdown;

        public DropdownField themeDropdown => m_ThemeDropdown ??= this.Q<DropdownField>("theme-dropdown");

        [UxmlAttribute]
        public PanelSettingsCollection panelSettingsCollection { get; set; }

        [UxmlAttribute]
        public BagelThemeCollection bagelThemeCollection { get; set; }

        // Set by the in-game Drivers: only the running game swaps the PanelSettings themes.
        public bool appliesThemeToPanelSettings { get; set; }

        public event EventHandler<ThemeStyleSheet> OnThemeChange;

        bool m_Bound;

        protected override void OnAttached()
        {
            if (m_Bound)
                return;
            m_Bound = true;

            var themes = bagelThemeCollection?.collection;
            if (themes == null || themes.Count == 0)
                return;

            themeDropdown.choices.Clear();
            foreach (var theme in themes)
                themeDropdown.choices.Add(theme.themeName);

            // The index is two-way bound to settings.themeIndex; show the current one right away.
            if (state != null)
                themeDropdown.SetValueWithoutNotify(themeDropdown.choices[Mathf.Clamp(state.settings.themeIndex, 0, themes.Count - 1)]);
        }

        protected override void OnStatePropertyChanged(in BindingId property)
        {
            if (property == SettingsData.themeIndexPath)
                ChangeTheme(state.settings.themeIndex);
        }

        void ChangeTheme(int index)
        {
            var themes = bagelThemeCollection?.collection;
            if (themes == null || themes.Count == 0)
                return;

            var theme = themes[Mathf.Clamp(index, 0, themes.Count - 1)].theme;

            OnThemeChange?.Invoke(this, theme);

            if (appliesThemeToPanelSettings)
                ChangePanelSettingsTheme(theme);
        }

        void ChangePanelSettingsTheme(ThemeStyleSheet theme)
        {
            var panelSettings = panelSettingsCollection?.collection;
            if (panelSettings == null || panelSettings.Count == 0)
                return;

            foreach (var settings in panelSettings)
                settings.themeStyleSheet = theme;
        }
    }
}
