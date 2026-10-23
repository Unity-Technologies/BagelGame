using Unity.Localization;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Bagel
{
    // Theme and language dropdowns in the Editor's main toolbar (add them from the toolbar's
    // menu). In Play mode they change the game's own settings, like the in-game Settings do;
    // in Edit mode they preview the screens that are open, without changing any asset.
    public static class BagelToolbar
    {
        const string k_ThemePath = "Bagel/Theme";
        const string k_LanguagePath = "Bagel/Language";
        const string k_PreviewThemeKey = "Bagel.PreviewThemeIndex";

        [MainToolbarElement(k_ThemePath, defaultDockPosition = MainToolbarDockPosition.Middle)]
        static MainToolbarElement CreateThemeDropdown()
        {
            var name = themes != null ? themes.collection[currentThemeIndex].themeName : "-";
            return new MainToolbarDropdown(new MainToolbarContent($"Theme: {name}", "Bagel UI theme"), ShowThemeMenu);
        }

        [MainToolbarElement(k_LanguagePath, defaultDockPosition = MainToolbarDockPosition.Middle)]
        static MainToolbarElement CreateLanguageDropdown()
        {
            var locale = LocalizationSettings.HasSettings ? LocalizationSettings.SelectedLocale : null;
            var name = locale != null ? locale.LocaleName : "-";
            return new MainToolbarDropdown(new MainToolbarContent($"Language: {name}", "Bagel UI language"), ShowLanguageMenu);
        }

        static BagelThemeCollection themes
        {
            get
            {
                var guids = AssetDatabase.FindAssets($"t:{nameof(BagelThemeCollection)}");
                return guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<BagelThemeCollection>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
            }
        }

        static PlayManagerState gameState
        {
            get
            {
                if (!EditorApplication.isPlaying)
                    return null;
                var playManager = Object.FindAnyObjectByType<PlayManager>();
                return playManager != null ? playManager.state : null;
            }
        }

        static int currentThemeIndex
        {
            get
            {
                var index = gameState != null ? gameState.settings.themeIndex : SessionState.GetInt(k_PreviewThemeKey, 0);
                return Mathf.Clamp(index, 0, Mathf.Max(0, (themes?.collection.Count ?? 1) - 1));
            }
        }

        static void ShowThemeMenu(Rect rect)
        {
            var collection = themes;
            if (collection == null)
                return;

            var menu = new GenericMenu();
            for (int i = 0; i < collection.collection.Count; i++)
            {
                var index = i;
                menu.AddItem(new GUIContent(collection.collection[i].themeName), i == currentThemeIndex, () => SelectTheme(index));
            }
            menu.DropDown(rect);
        }

        public static void SelectTheme(int index)
        {
            if (gameState != null)
            {
                gameState.settings.themeIndex = index;
            }
            else
            {
                SessionState.SetInt(k_PreviewThemeKey, index);
                BagelThemes.SetPreviewTheme(index == 0 ? null : themes.collection[index].theme);
            }

            MainToolbar.Refresh(k_ThemePath);
        }

        static void ShowLanguageMenu(Rect rect)
        {
            if (!LocalizationSettings.HasSettings)
                return;

            var menu = new GenericMenu();
            foreach (var locale in LocalizationSettings.Instance.AvailableLocales)
            {
                var chosen = locale;
                menu.AddItem(new GUIContent(locale.LocaleName), locale == LocalizationSettings.SelectedLocale, () => SelectLanguage(chosen.Code));
            }
            menu.DropDown(rect);
        }

        public static void SelectLanguage(string localeCode)
        {
            BagelLocalization.SetLocale(localeCode);
            MainToolbar.Refresh(k_LanguagePath);
        }

        [InitializeOnLoadMethod]
        static void RefreshWhenThingsChange()
        {
            BagelLocalization.localeChanged += () => MainToolbar.Refresh(k_LanguagePath);
            BagelThemes.panelSettingsThemeChanged += _ => MainToolbar.Refresh(k_ThemePath);
            BagelThemes.previewThemeChanged += _ => MainToolbar.Refresh(k_ThemePath);
            EditorApplication.playModeStateChanged += change =>
            {
                // Play mode starts on the game's saved theme, and Edit mode goes back to no preview.
                if (change == PlayModeStateChange.EnteredEditMode)
                    SessionState.SetInt(k_PreviewThemeKey, 0);
                MainToolbar.Refresh(k_ThemePath);
                MainToolbar.Refresh(k_LanguagePath);
            };
        }
    }
}
