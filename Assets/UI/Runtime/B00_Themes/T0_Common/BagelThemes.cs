using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // Switches the UI theme. While playing, it swaps the theme of every PanelSettings (dropdown
    // menus open at a panel's root, outside the screens) and puts the original themes back when
    // play stops. Outside Play mode it only restyles the screens that are open, so a preview
    // never changes an asset.
    [NoAutoStaticsCleanup] // ResetStatics and RestoreAfterPlayMode clear them
    public static class BagelThemes
    {
        static readonly Dictionary<PanelSettings, ThemeStyleSheet> s_OriginalThemes = new();
        static readonly List<VisualElement> s_PreviewRoots = new();
        static ThemeStyleSheet s_PreviewTheme;

        public static ThemeStyleSheet previewTheme => s_PreviewTheme;
        public static IReadOnlyList<VisualElement> previewRoots => s_PreviewRoots;

        public static event Action<ThemeStyleSheet> previewThemeChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            RestorePanelSettings();
            SetPreviewTheme(null);
            s_PreviewRoots.Clear();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void RestoreAfterPlayMode() => UnityEditor.EditorApplication.playModeStateChanged += change =>
        {
            if (change == UnityEditor.PlayModeStateChange.EnteredEditMode)
                RestorePanelSettings();
        };
#endif

        public static void ApplyToPanelSettings(PanelSettingsCollection panelSettingsCollection, ThemeStyleSheet theme)
        {
            if (theme == null || panelSettingsCollection == null || panelSettingsCollection.collection == null)
                return;

            foreach (var settings in panelSettingsCollection.collection)
            {
                if (settings == null)
                    continue;

                s_OriginalThemes.TryAdd(settings, settings.themeStyleSheet);
                settings.themeStyleSheet = theme;
            }
        }

        public static void RestorePanelSettings()
        {
            foreach (var (settings, theme) in s_OriginalThemes)
            {
                if (settings != null)
                    settings.themeStyleSheet = theme;
            }
            s_OriginalThemes.Clear();
        }

        // Screens shown outside Play mode (in the Scene view, UI Builder or an editor window) report
        // themselves, so a theme can be previewed on them without touching the PanelSettings.
        internal static void AddPreviewRoot(VisualElement root)
        {
            if (s_PreviewRoots.Contains(root))
                return;

            s_PreviewRoots.Add(root);
            if (s_PreviewTheme != null)
                root.styleSheets.Add(s_PreviewTheme);
        }

        internal static void RemovePreviewRoot(VisualElement root)
        {
            if (!s_PreviewRoots.Remove(root))
                return;

            if (s_PreviewTheme != null)
                root.styleSheets.Remove(s_PreviewTheme);
        }

        public static void SetPreviewTheme(ThemeStyleSheet theme)
        {
            if (theme == s_PreviewTheme)
                return;

            foreach (var root in s_PreviewRoots)
            {
                if (s_PreviewTheme != null)
                    root.styleSheets.Remove(s_PreviewTheme);
                if (theme != null)
                    root.styleSheets.Add(theme);
            }

            s_PreviewTheme = theme;
            previewThemeChanged?.Invoke(theme);
        }
    }
}
