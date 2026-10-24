using System;
using System.Collections.Generic;
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

        static Action s_LocaleChanged;

        // Play mode and the localization module clear the module's events, so every new listener
        // subscribes again (it's the same static handler, so it's never there twice).
        public static event Action localeChanged
        {
            add
            {
                s_LocaleChanged += value;
                Subscribe();
            }
            remove => s_LocaleChanged -= value;
        }

        public static string localeCode => LocalizationSettings.SelectedLocale?.Code ?? "en";

        // The selected language, without its script or region: "zh" for "zh-Hans".
        public static string language
        {
            get
            {
                var code = localeCode;
                var dash = code.IndexOf('-');
                return dash < 0 ? code : code.Substring(0, dash);
            }
        }

        // Whether the selected language is written right to left (Arabic). The language code decides when the
        // platform has no data for the culture.
        public static bool isRightToLeft => Array.IndexOf(s_RightToLeftLanguages, language) >= 0 || culture.TextInfo.IsRightToLeft;

        static readonly string[] s_RightToLeftLanguages = { "ar", "fa", "he", "ur" };

        // Arabic writes its numbers with Arabic-Indic digits and separators (١٢٬٣٤٥ ٪٥٧ ٣٫٥); the platform's culture
        // data has Western ones. The text generator lays them out among the words, in either direction.
        public static bool usesArabicDigits => language == "ar";

        // A number formatted with the culture, in the language's digits.
        public static string LocalizeDigits(string number)
        {
            if (string.IsNullOrEmpty(number) || !usesArabicDigits)
                return number;
            return string.Create(number.Length, number, (digits, source) =>
            {
                source.AsSpan().CopyTo(digits);
                ToArabicDigits(digits);
            });
        }

        public static void LocalizeDigits(Span<char> number)
        {
            if (usesArabicDigits)
                ToArabicDigits(number);
        }

        static void ToArabicDigits(Span<char> number)
        {
            for (int i = 0; i < number.Length; i++)
            {
                var c = number[i];
                if (c >= '0' && c <= '9')
                    number[i] = (char)('\u0660' + (c - '0'));
                else if (c == ',')
                    number[i] = '\u066C'; // thousands separator
                else if (c == '.')
                    number[i] = '\u066B'; // decimal separator
                else if (c == '%')
                    number[i] = '\u066A';
            }
        }

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

        // Shows the selected language in every localized binding inside the element. A binding hears the language
        // change by itself, unless it was made before Play mode started or stopped: the localization module drops
        // its listeners then (an Editor window's screens, for one).
        public static void RefreshBindings(VisualElement root)
        {
            // Found first, refreshed after: new text can change what's in the tree.
            var strings = new List<LocalizedString>();
            var bindingInfos = new List<BindingInfo>();
            root.Query<VisualElement>().ForEach(element =>
            {
                bindingInfos.Clear();
                element.GetBindingInfos(bindingInfos);
                foreach (var info in bindingInfos)
                {
                    if (info.binding is LocalizedString localizedString)
                        strings.Add(localizedString);
                }
            });
            foreach (var localizedString in strings)
                localizedString.RefreshString();
        }

        public static void SetLocale(string code)
        {
            if (!LocalizationSettings.HasSettings)
                return;

            // The module may have dropped its listeners since (Play mode started or stopped): listen again first.
            Subscribe();

            // Starting up, the module picks its startup language over this one: let it start first (it usually can
            // right away), and when it can't, pick this one again once it's ready.
            var settings = LocalizationSettings.Instance;
            if (!settings.IsInitialized)
                _ = LocalizationSettings.InitializeAsync();

            var locale = settings.GetLocale(new LocaleIdentifier(code));
            if (locale == null)
                return;
            LocalizationSettings.SelectedLocale = locale;

            if (!settings.IsInitialized)
            {
                s_PendingLocaleCode = code;
                LocalizationSettings.InitializationCompleted -= LocalizationSettings_OnInitializationCompleted;
                LocalizationSettings.InitializationCompleted += LocalizationSettings_OnInitializationCompleted;
            }
        }

        static string s_PendingLocaleCode;

        static void LocalizationSettings_OnInitializationCompleted()
        {
            LocalizationSettings.InitializationCompleted -= LocalizationSettings_OnInitializationCompleted;
            var code = s_PendingLocaleCode;
            s_PendingLocaleCode = null;
            if (code != null)
                SetLocale(code);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Subscribe();

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void SubscribeInEditMode()
        {
            Subscribe();
            // Once Play mode has started or stopped: the module clears its events during the change.
            UnityEditor.EditorApplication.playModeStateChanged += _ => UnityEditor.EditorApplication.delayCall += Subscribe;
        }
#endif

        static void Subscribe()
        {
            LocalizationSettings.SelectedLocaleChanged -= LocalizationSettings_OnSelectedLocaleChanged;
            LocalizationSettings.SelectedLocaleChanged += LocalizationSettings_OnSelectedLocaleChanged;
        }

        static void LocalizationSettings_OnSelectedLocaleChanged(Locale locale) => s_LocaleChanged?.Invoke();
    }
}
