using System;
using System.Globalization;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditorInternal;

namespace Bagel
{
    // Before a player build, writes the Editor's full version (with its revision) and the time into BagelBuildInfo,
    // for Settings › About. Both are cleared once the build is done (or, after a failed build, the next time scripts
    // load), so builds don't change the committed asset.
    sealed class BagelBuildInfoStamp : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) =>
            Stamp(InternalEditorUtility.GetFullUnityVersion(), DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture));

        public void OnPostprocessBuild(BuildReport report) => Stamp(string.Empty, string.Empty);

        // A failed build never gets to OnPostprocessBuild.
        [InitializeOnLoadMethod]
        static void ClearLeftovers() => EditorApplication.delayCall += () =>
        {
            if (!BuildPipeline.isBuildingPlayer)
                Stamp(string.Empty, string.Empty);
        };

        static void Stamp(string version, string date)
        {
            Write(info => info.fullUnityVersion, (info, value) => info.fullUnityVersion = value, version);
            Write(info => info.buildDate, (info, value) => info.buildDate = value, date);
        }

        static void Write(Func<BagelBuildInfo, string> get, Action<BagelBuildInfo, string> set, string value)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(BagelBuildInfo)))
            {
                var info = AssetDatabase.LoadAssetAtPath<BagelBuildInfo>(AssetDatabase.GUIDToAssetPath(guid));
                if (info == null || (get(info) ?? string.Empty) == value)
                    continue;
                set(info, value);
                EditorUtility.SetDirty(info);
                AssetDatabase.SaveAssetIfDirty(info);
            }
        }
    }
}
