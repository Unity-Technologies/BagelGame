using System;
using UnityEngine.UIElements;

namespace Bagel
{
    [UxmlElement]
    public partial class SettingsPaneManagerForUI : VisualElement
    {
        DropdownField m_ThemeDropdown;

        public DropdownField themeDropdown => m_ThemeDropdown ??= this.Q<DropdownField>("theme-dropdown");

        [UxmlAttribute]
        public PanelSettingsCollection panelSettingsCollection { get; set; }

        [UxmlAttribute]
        public BagelThemeCollection bagelThemeCollection { get; set; }

        public event EventHandler<ThemeStyleSheet> OnThemeChange;

        public SettingsPaneManagerForUI()
        {
            RegisterCallbackOnce<GeometryChangedEvent>(FirstInit);
        }

        public void BindSettingsCallbacks(SettingsRefsForUI settingsRefs)
        {
            if (settingsRefs == null)
                return;

            OnThemeChange += ChangePanelSettingsTheme;
        }

        void FirstInit(GeometryChangedEvent evt)
        {
            var themes = bagelThemeCollection?.collection;

            if (themes == null || themes.Count == 0)
                return;

            themeDropdown.choices.Clear();
            foreach (var theme in themes)
                themeDropdown.choices.Add(theme.themeName);

            themeDropdown.RegisterValueChangedCallback(OnThemeChanged);
        }

        void OnThemeChanged(ChangeEvent<string> evt)
        {
            var index = themeDropdown.index;
            ChangeTheme(index);
        }

        void ChangeTheme(int index)
        {
            var themes = bagelThemeCollection?.collection;

            if (OnThemeChange == null || themes == null || themes.Count == 0)
                return;

            var theme = themes[index].theme;

            OnThemeChange.Invoke(this, theme);
        }

        void ChangePanelSettingsTheme(object sender, ThemeStyleSheet theme)
        {
            var panelSettings = panelSettingsCollection?.collection;
            if (panelSettings == null || panelSettings.Count == 0)
                return;

            foreach (var settings in panelSettings)
                settings.themeStyleSheet = theme;
        }
    }
}
