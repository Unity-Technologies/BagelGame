using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Localization;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Bagel
{
    // Theme and language strips in the Editor's main toolbar, on the right: an icon per theme and per
    // language, like the Bagel All UI window's (BagelOptionStrip). In Play mode they change the game's
    // own settings, like the in-game Settings do; in Edit mode they preview the screens that are open,
    // without changing any asset. They're shown once per user, and by the demo's layout reset
    // (Bagel > Demo > Reset Layout and Toolbar).
    public static class BagelToolbar
    {
        public const string themePath = "Bagel/Theme";
        public const string languagePath = "Bagel/Language";
        public static readonly string[] paths = { themePath, languagePath };

        const string k_PreviewThemeKey = "Bagel.PreviewThemeIndex";
        const string k_ShownKey = "Bagel.Toolbar.StripsShown";
        const string k_Where = "in the open screens, or the game in Play mode";

        // The right zone runs from right to left: the higher index, the further left.
        [MainToolbarElement(themePath, defaultDockPosition = MainToolbarDockPosition.Right, defaultDockIndex = 1001)]
        static IEnumerable<MainToolbarElement> CreateThemeStrip()
        {
            var collection = themes;
            var current = currentThemeIndex;
            for (int i = 0; collection != null && i < collection.collection.Count; i++)
            {
                var index = i;
                var entry = collection.collection[i];
                yield return Option(() => BagelEditorIcons.Theme(entry), $"Theme {k_Where}: {entry.themeName}",
                    i == current, () => SelectTheme(index));
            }
        }

        [MainToolbarElement(languagePath, defaultDockPosition = MainToolbarDockPosition.Right, defaultDockIndex = 1000)]
        static IEnumerable<MainToolbarElement> CreateLanguageStrip()
        {
            if (!LocalizationSettings.HasSettings)
                yield break;

            var current = BagelLocalization.localeCode;
            foreach (var locale in LocalizationSettings.Instance.AvailableLocales)
            {
                var code = locale.Code;
                yield return Option(() => BagelEditorIcons.Language(code), $"Language {k_Where}: {locale.LocaleName}",
                    code == current, () => SelectLanguage(code));
            }
        }

        // An option of a strip is a toolbar element of its own: the main toolbar draws the group around them.
        static MainToolbarElement Option(Func<VisualElement> icon, string tooltip, bool selected, Action choose) =>
            new MainToolbarCustomElement(() =>
            {
                var option = BagelOptionStrip.CreateOption(icon(), tooltip, selected, () =>
                {
                    if (!selected)
                        choose();
                });
                BagelOptionStrip.AddStyleSheet(option);
                return option;
            });

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

        public static int currentThemeIndex
        {
            get
            {
                var index = gameState != null ? gameState.settings.themeIndex : SessionState.GetInt(k_PreviewThemeKey, 0);
                return Mathf.Clamp(index, 0, Mathf.Max(0, (themes?.collection.Count ?? 1) - 1));
            }
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

            MainToolbar.Refresh(themePath);
        }

        public static void SelectLanguage(string localeCode)
        {
            BagelLocalization.SetLocale(localeCode);
            MainToolbar.Refresh(languagePath);
        }

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            BagelLocalization.localeChanged += () => MainToolbar.Refresh(languagePath);
            BagelThemes.panelSettingsThemeChanged += _ => MainToolbar.Refresh(themePath);
            BagelThemes.previewThemeChanged += _ => MainToolbar.Refresh(themePath);
            EditorApplication.playModeStateChanged += change =>
            {
                // Play mode starts on the game's saved theme, and Edit mode goes back to no preview.
                if (change == PlayModeStateChange.EnteredEditMode)
                    SessionState.SetInt(k_PreviewThemeKey, 0);
                MainToolbar.Refresh(themePath);
                MainToolbar.Refresh(languagePath);
            };

            if (EditorUserSettings.GetConfigValue(k_ShownKey) != bool.TrueString)
                EditorApplication.update += ShowOnce;
        }

        static readonly double s_LoadTime = EditorApplication.timeSinceStartup;

        // The toolbar builds its elements a little after scripts load: try until they're there (10 seconds at most).
        static void ShowOnce()
        {
            if (Show())
                EditorUserSettings.SetConfigValue(k_ShownKey, bool.TrueString);
            else if (EditorApplication.timeSinceStartup - s_LoadTime < 10.0)
                return;
            EditorApplication.update -= ShowOnce;
        }

        // Shows the strips in the main toolbar; false while the toolbar isn't there yet. The toolbar has no
        // public way to show an element, so this reaches its window, which is internal.
        public static bool Show()
        {
            var window = typeof(EditorWindow).Assembly.GetType("UnityEditor.MainToolbarWindow")?
                .GetField("instance", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as EditorWindow;
            if (window == null)
                return false;
            foreach (var path in paths)
            {
                if (!window.TryGetOverlay(path, out var overlay))
                    return false;
                overlay.displayed = true;
            }
            return true;
        }
    }
}
