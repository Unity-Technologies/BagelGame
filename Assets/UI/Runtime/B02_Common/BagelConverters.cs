using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // Named converter groups for UXML bindings, for example:
    // <ui:DataBinding property="title" data-source-path="toppingsCount" source-to-ui-converters="b-number"/>
    [NoAutoStaticsCleanup] // the converter groups are registered once for the domain
    public static class BagelConverters
    {
        public const string number = "b-number";
        public const string percent = "b-percent";

        static bool s_Registered;

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Register()
        {
            if (s_Registered)
                return;
            s_Registered = true;

            var numberGroup = new ConverterGroup(number, "Bagel", "Thousands separators: 12,345");
            numberGroup.AddConverter((ref int v) => FormatNumber(v));
            numberGroup.AddConverter((ref float v) => FormatNumber(Mathf.CeilToInt(v)));
            ConverterGroups.RegisterConverterGroup(numberGroup);

            var percentGroup = new ConverterGroup(percent, "Bagel", "A 0 to 1 fraction as a percentage: 57%");
            percentGroup.AddConverter((ref float v) => Mathf.RoundToInt(v * 100f).ToString(BagelLocalization.culture) + "%");
            ConverterGroups.RegisterConverterGroup(percentGroup);
        }

        public static string FormatNumber(int value) => value.ToString("N0", BagelLocalization.culture);
    }
}
