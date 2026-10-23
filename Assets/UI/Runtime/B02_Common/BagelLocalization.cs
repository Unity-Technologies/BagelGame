using System;
using System.Globalization;
using Unity.Localization;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // Helpers around the game's string table. Most text is localized in UXML with
    // <loc:LocalizedString property="text" table="BagelStrings" entry="..."/>; these cover the
    // text that code sets (page titles, list columns, names from data).
    [NoAutoStaticsCleanup] // ResetStatics resubscribes for every Play session
    public static class BagelLocalization
    {
        public const string table = "BagelStrings";

        static bool s_Subscribed;

        public static event Action localeChanged;

        public static string localeCode => LocalizationSettings.SelectedLocale?.Code ?? "en";

        // The selected language's number formats, for counts shown in the UI.
        public static CultureInfo culture
        {
            get
            {
                try
                {
                    return LocalizationSettings.SelectedLocale?.CultureInfo ?? CultureInfo.InvariantCulture;
                }
                catch (CultureNotFoundException)
                {
                    return CultureInfo.InvariantCulture;
                }
            }
        }

        // A binding that keeps an element's property in the selected language.
        public static LocalizedString String(string key)
        {
            var localizedString = new LocalizedString();
            localizedString.SetReference(table, key);
            return localizedString;
        }

        public static void Bind(VisualElement element, string property, string key)
        {
            element?.SetBinding(property, String(key));
        }

        // The text right now, for code that can't bind (it doesn't follow language changes by itself).
        public static string Get(string key, string fallback = null)
        {
            if (!LocalizationSettings.HasSettings)
                return fallback ?? key;

            var value = String(key).GetLocalizedString();
            return string.IsNullOrEmpty(value) ? fallback ?? key : value;
        }

        public static void SetLocale(string code)
        {
            if (!LocalizationSettings.HasSettings)
                return;

            var locale = LocalizationSettings.Instance.GetLocale(new LocaleIdentifier(code));
            if (locale != null)
                LocalizationSettings.SelectedLocale = locale;
        }

        // Play mode clears the localization events, so subscribe again for every session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Subscribed = false;
            Subscribe();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void SubscribeInEditMode()
        {
            Subscribe();
            UnityEditor.EditorApplication.playModeStateChanged += change =>
            {
                if (change != UnityEditor.PlayModeStateChange.EnteredEditMode)
                    return;
                s_Subscribed = false;
                Subscribe();
            };
        }
#endif

        static void Subscribe()
        {
            if (s_Subscribed)
                return;
            s_Subscribed = true;

            LocalizationSettings.SelectedLocaleChanged += _ => localeChanged?.Invoke();
        }
    }
}
