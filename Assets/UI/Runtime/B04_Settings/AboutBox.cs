using System;
using System.Globalization;
using System.IO;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Bagel
{
    // Settings › About: the game and Unity versions, the kind of build and when it was built, what the game weighs on
    // disk, and the machine it runs on, all from what the engine says without asking for any permission (SystemInfo,
    // Screen, Application). Two columns of short rows, so it fits without scrolling; a value too long for its row ends
    // in an ellipsis.
    [UxmlElement(libraryPath = "Bagel/Settings")]
    [NoAutoStaticsCleanup] // the size is measured once for the session, and it doesn't change
    public partial class AboutBox : VisualElement
    {
        public static readonly string ussClassName = "b-about";
        public static readonly string rowUssClassName = ussClassName + "__row";
        public static readonly string labelUssClassName = ussClassName + "__label";
        public static readonly string valueUssClassName = ussClassName + "__value";

        static long s_Size = -1;

        [UxmlAttribute]
        public BagelBuildInfo buildInfo { get; set; }

        public AboutBox()
        {
            AddToClassList(ussClassName);
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                Build();
                BagelLocalization.localeChanged += Build;
            });
            RegisterCallback<DetachFromPanelEvent>(_ => BagelLocalization.localeChanged -= Build);
        }

        // The rows again, in the new language.
        void Build()
        {
            Discard.Children(this);
            Row("about.version", Application.version);
            Row("about.unity", fullUnityVersion);
            Row("about.build", BuildKind());
            Row("about.built", buildDate);
            var size = MeasureSize();
            Row("about.size", size >= 0 ? FormatBytes(size) : Application.isEditor ? BagelLocalization.Get("about.editor", "Editor") : "—");
            Row("about.device", SystemInfo.deviceModel);
            Row("about.os", SystemInfo.operatingSystem);
            Row("about.cpu", string.Format(CultureInfo.InvariantCulture, "{0} · {1} × {2} MHz", SystemInfo.processorType.Trim(), SystemInfo.processorCount, SystemInfo.processorFrequency));
            Row("about.gpu", SystemInfo.graphicsDeviceName);
            Row("about.memory", string.Format(CultureInfo.InvariantCulture, "{0:0.#} GB · GPU {1:0.#} GB", SystemInfo.systemMemorySize / 1024f, SystemInfo.graphicsMemorySize / 1024f));
            var resolution = Screen.currentResolution;
            Row("about.screen", string.Format(CultureInfo.InvariantCulture, "{0}×{1} · {2:0} Hz", Screen.width, Screen.height, resolution.refreshRateRatio.value));
            Row("about.graphics-api", SystemInfo.graphicsDeviceVersion);
        }

        // The Editor's own in the Editor; in a player, the one the build wrote down, with its revision.
        string fullUnityVersion
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditorInternal.InternalEditorUtility.GetFullUnityVersion();
#else
                var stamped = buildInfo != null ? buildInfo.fullUnityVersion : null;
                return string.IsNullOrEmpty(stamped) ? Application.unityVersion : stamped;
#endif
            }
        }

        // When the build wrote it down; the Editor isn't a build.
        string buildDate
        {
            get
            {
#if UNITY_EDITOR
                return BagelLocalization.Get("about.editor", "Editor");
#else
                var stamped = buildInfo != null ? buildInfo.buildDate : null;
                return string.IsNullOrEmpty(stamped) ? "—" : stamped;
#endif
            }
        }

        static string BuildKind()
        {
            var kind = BagelLocalization.Get(Debug.isDebugBuild ? "about.development" : "about.release");
#if ENABLE_IL2CPP
            const string backend = "IL2CPP";
#else
            const string backend = "Mono";
#endif
            var pipeline = GraphicsSettings.currentRenderPipeline != null ? " · URP" : "";
            return $"{kind} · {backend} · {SystemInfo.graphicsDeviceType}{pipeline}";
        }

        void Row(string labelKey, string value)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList(rowUssClassName);
            var label = new Label(BagelLocalization.Get(labelKey)) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(labelUssClassName);
            var text = new Label(value) { pickingMode = PickingMode.Ignore };
            text.AddToClassList(valueUssClassName);
            row.Add(label);
            row.Add(text);
            Add(row);
        }

        public static string FormatBytes(long bytes) => (bytes / (1024f * 1024f)).ToString("0.0", CultureInfo.InvariantCulture) + " MB";

        // The installed game's files, measured once a session: the folder with the executable in it (one up from its
        // data folder). The Editor and the Web player have no game to measure: -1.
        static long MeasureSize()
        {
            if (s_Size != -1 || Application.isEditor || Application.platform == RuntimePlatform.WebGLPlayer)
                return s_Size;
            try
            {
                var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                long total = 0;
                foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                    total += new FileInfo(file).Length;
                s_Size = total;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Bagel Game: couldn't measure the game's size. " + e.Message);
                s_Size = -2;
            }
            return s_Size;
        }
    }
}
