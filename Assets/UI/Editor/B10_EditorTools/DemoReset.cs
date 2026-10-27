using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Unity.Localization;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Bagel
{
    // The demo's resets, in the Bagel > Demo menu: the project's files back to the last commit, the
    // Editor's windows and main toolbar back to the demo's layout (both from files in the repository),
    // and the Bagel windows and toolbar previews back to their start. Reset All does the three.
    public static class DemoReset
    {
        public const string layoutPath = "Assets/Settings/Layouts/BagelGameDemo.wlt";
        public const string toolbarPresetPath = "Assets/Settings/Layouts/BagelGameToolbar.preset";
        public const string resetAllMenuPath = "Bagel/Demo/Reset All";
        public const string discardMenuPath = "Bagel/Demo/Discard Project Changes";
        public const string resetLayoutMenuPath = "Bagel/Demo/Reset Layout and Toolbar";
        public const string resetStatesMenuPath = "Bagel/Demo/Reset Bagel Project States";

        // Reset All's next step, kept through the script reload that discarding changed scripts starts.
        const string k_ResetAllKey = "Bagel.Demo.ResetAll";
        const string k_LayoutStep = "layout";

        [InitializeOnLoadMethod]
        static void ResumeResetAll()
        {
            if (SessionState.GetString(k_ResetAllKey, string.Empty) == k_LayoutStep)
                OnNextUpdate(ResetAllAfterDiscard);
        }

        // The three resets, safest first: the files (so the layout and toolbar preset are the committed
        // ones), then the layout (which rebuilds every window), then the Bagel windows it opened and the
        // previews. A discard that changes scripts reloads them: the rest carries on after.
        [MenuItem(resetAllMenuPath, priority = 30, editorModes = new[] { BagelMenu.defaultMode, BagelMenu.uiToolkitAuthoringMode })]
        public static void ResetAll()
        {
            SessionState.SetString(k_ResetAllKey, k_LayoutStep);
            DiscardChanges(() => OnNextUpdate(ResetAllAfterDiscard));
        }

        static void ResetAllAfterDiscard()
        {
            if (SessionState.GetString(k_ResetAllKey, string.Empty) != k_LayoutStep)
                return;
            SessionState.EraseString(k_ResetAllKey);
            ResetLayout(ResetProjectStates);
        }

        // The Bagel All UI and Bagel Features windows' Resets (a fresh state, the zoom at 100%, no
        // Inspector, the first feature), no theme preview and the project's language. No window opens
        // or closes.
        [MenuItem(resetStatesMenuPath, priority = 43, editorModes = new[] { BagelMenu.defaultMode, BagelMenu.uiToolkitAuthoringMode })]
        public static void ResetProjectStates()
        {
            BagelToolbar.SelectTheme(0);
            var locale = LocalizationSettings.HasSettings ? LocalizationSettings.Instance.ProjectLocale : null;
            if (locale != null && BagelLocalization.localeCode != locale.Identifier.Code)
                BagelToolbar.SelectLanguage(locale.Identifier.Code);
            foreach (var window in Resources.FindObjectsOfTypeAll<BagelAllUIWindow>())
                window.ResetWindow();
            foreach (var window in Resources.FindObjectsOfTypeAll<BagelFeaturesWindow>())
                window.ResetWindow();
        }

        // Every change in the project goes, new files included: they're put aside in git's stash rather
        // than deleted, so `git stash pop` brings a demo's edits back. Then the assets are reimported and
        // the open scene is reloaded from disk. Leaves Play mode first. No confirmation: with -automated
        // the Editor can't show one.
        [MenuItem(discardMenuPath, priority = 41, editorModes = new[] { BagelMenu.defaultMode, BagelMenu.uiToolkitAuthoringMode })]
        public static void DiscardChanges() => DiscardChanges(null);

        static void DiscardChanges(Action done)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Action<PlayModeStateChange> afterPlayMode = null;
                afterPlayMode = change =>
                {
                    if (change != PlayModeStateChange.EnteredEditMode)
                        return;
                    EditorApplication.playModeStateChanged -= afterPlayMode;
                    Discard();
                    done?.Invoke();
                };
                EditorApplication.playModeStateChanged += afterPlayMode;
                EditorApplication.ExitPlaymode();
                return;
            }
            Discard();
            done?.Invoke();
        }

        static void Discard()
        {
            var project = Path.GetDirectoryName(Application.dataPath);
            var (ok, changes) = Git(project, "status --porcelain -- .");
            if (!ok)
            {
                Debug.LogError("[Bagel] Demo reset: git couldn't list the changes. " + changes);
                return;
            }

            var count = changes.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
            if (count > 0)
            {
                var message = $"Bagel demo reset {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                var (stashed, output) = Git(project, $"stash push --include-untracked --message \"{message}\" -- .");
                if (!stashed)
                {
                    Debug.LogError("[Bagel] Demo reset: git couldn't put the changes aside. " + output);
                    return;
                }
                Debug.Log($"[Bagel] Demo reset: {count} changed file(s) put aside in git's stash as \"{message}\" (git stash pop brings them back).\n{changes}");
            }
            else
            {
                Debug.Log("[Bagel] Demo reset: no changes in the project.");
            }

            AssetDatabase.Refresh();
            // Edits in memory go too.
            var scene = EditorSceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(scene.path) && File.Exists(Path.Combine(project, scene.path)))
                EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
        }

        static (bool ok, string output) Git(string directory, string arguments)
        {
            try
            {
                var info = new ProcessStartInfo("git", arguments)
                {
                    WorkingDirectory = directory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(info);
                var output = process.StandardOutput.ReadToEndAsync();
                var error = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit(30000))
                {
                    process.Kill();
                    return (false, "git took longer than 30 seconds.");
                }
                return (process.ExitCode == 0, process.ExitCode == 0 ? output.Result : error.Result + output.Result);
            }
            catch (Exception e)
            {
                return (false, e.Message);
            }
        }

        // The windows from the demo's layout, then the main toolbar from its preset, with the Bagel strips
        // shown. Loading a layout closes and rebuilds every window, so it runs after the menu's click is done.
        [MenuItem(resetLayoutMenuPath, priority = 42, editorModes = new[] { BagelMenu.defaultMode, BagelMenu.uiToolkitAuthoringMode })]
        public static void ResetLayout() => ResetLayout(null);

        static void ResetLayout(Action done)
        {
            OnNextUpdate(() =>
            {
                if (!EditorUtility.LoadWindowLayout(layoutPath))
                {
                    Debug.LogWarning($"[Bagel] Layout reset: {layoutPath} didn't load (a window with unsaved changes can stop it).");
                    done?.Invoke();
                    return;
                }
                // The toolbar's window is a new one now, and so are the Bagel windows: they're built on the next update.
                OnNextUpdate(() =>
                {
                    ApplyToolbarPreset();
                    if (done != null)
                        OnNextUpdate(done);
                });
            });
        }

        // The main toolbar's preset, through the Editor's own preset loading: it has no public API, so this
        // reaches its internal types, and says so if a newer Editor moved them.
        public static bool ApplyToolbarPreset()
        {
            try
            {
                var editor = typeof(EditorWindow).Assembly;
                var window = editor.GetType("UnityEditor.MainToolbarWindow")?
                    .GetField("instance", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as EditorWindow;
                var load = editor.GetType("UnityEditor.Overlays.OverlayPresetManager")?
                    .GetMethod("LoadFromFile", BindingFlags.Static | BindingFlags.NonPublic);
                var apply = typeof(OverlayCanvas).GetMethod("ApplyPreset", BindingFlags.Instance | BindingFlags.NonPublic);
                if (window == null || load == null || apply == null)
                {
                    Debug.LogWarning("[Bagel] Layout reset: this Editor's main toolbar can't take a preset from code.");
                    return false;
                }
                var preset = load.Invoke(null, new object[] { toolbarPresetPath, "preset" });
                if (preset == null)
                {
                    Debug.LogWarning($"[Bagel] Layout reset: {toolbarPresetPath} didn't load.");
                    return false;
                }
                apply.Invoke(window.overlayCanvas, new[] { preset });
                // The demo's preset shows the strips, but one saved without them would hide them.
                BagelToolbar.Show();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Bagel] Layout reset: the toolbar preset couldn't be applied. " + (e.InnerException ?? e).Message);
                return false;
            }
        }

        // EditorApplication.delayCall only runs while the Editor is the active application; update always does.
        static void OnNextUpdate(Action action)
        {
            EditorApplication.CallbackFunction once = null;
            once = () =>
            {
                EditorApplication.update -= once;
                action();
            };
            EditorApplication.update += once;
        }
    }
}
