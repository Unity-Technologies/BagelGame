using System;
using System.Collections.Generic;
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
            if (themes == null || themes.Count == 0)
                return;

            UpdateThemeNames();
        }

        protected override void OnLocaleChanged() => UpdateThemeNames();

        // Theme names are choices in a dropdown, not elements, so they're set for each language.
        void UpdateThemeNames()
        {
            var themes = bagelThemeCollection?.collection;
            if (themes == null || themes.Count == 0 || themeDropdown == null)
                return;

            var choices = new List<string>(themes.Count);
            foreach (var theme in themes)
                choices.Add(string.IsNullOrEmpty(theme.nameKey) ? theme.themeName : BagelLocalization.Get(theme.nameKey, theme.themeName));
            themeDropdown.choices = choices;

            // The index is two-way bound to settings.themeIndex; show the current one right away.
            var index = state != null ? state.settings.themeIndex : themeDropdown.index;
            themeDropdown.SetValueWithoutNotify(choices[Mathf.Clamp(index, 0, choices.Count - 1)]);
        }

        protected override void OnStateChanged(PlayManagerState oldState, PlayManagerState newState)
        {
            if (newState != null && m_AppliesThemeToPanelSettings)
                ChangeTheme(newState.settings.themeIndex);
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

            if (m_AppliesThemeToPanelSettings && Application.isPlaying)
                BagelThemes.ApplyToPanelSettings(panelSettingsCollection, theme);
        }
    }
}
