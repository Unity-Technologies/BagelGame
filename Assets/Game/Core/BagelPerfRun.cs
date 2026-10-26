using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Bagel
{
    // A scripted tour of the game for measuring its frame times in a player. Start the player with
    // -bagel-perf: it goes through the main menu, Settings, the Leaderboard, the bagels, a run (a virtual
    // gamepad rolls the bagel), Pause and Game Over, a few seconds each, logs each one's frame times (CPU,
    // and GPU where the platform measures it) and quits. -bagel-perf-no-effects turns Visual Effects off
    // first, to see what the blurs, shadows and the chalk cost, and -bagel-perf-shots <folder> saves a
    // screenshot of each part there. Nothing happens without the arguments.
    [NoAutoStaticsCleanup] // read-only
    public class BagelPerfRun : MonoBehaviour
    {
        const string k_Argument = "-bagel-perf";
        const string k_NoEffectsArgument = "-bagel-perf-no-effects";
        const string k_ShotsArgument = "-bagel-perf-shots";
        const float k_Settle = 1.5f;
        const float k_Measure = 5f;

        readonly List<float> m_Cpu = new();
        readonly List<float> m_Gpu = new();
        readonly StringBuilder m_Report = new();
        readonly FrameTiming[] m_Timings = new FrameTiming[1];
        string m_ShotsFolder;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartIfAsked()
        {
            var arguments = Environment.GetCommandLineArgs();
            if (Array.IndexOf(arguments, k_Argument) < 0 && Array.IndexOf(arguments, k_NoEffectsArgument) < 0)
                return;
            DontDestroyOnLoad(new GameObject(nameof(BagelPerfRun)).AddComponent<BagelPerfRun>());
        }

        IEnumerator Start()
        {
            // As fast as it goes: no vertical sync, no frame rate cap.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            BagelSave.enabled = false;
            var arguments = Environment.GetCommandLineArgs();
            var shots = Array.IndexOf(arguments, k_ShotsArgument);
            if (shots >= 0 && shots + 1 < arguments.Length)
                m_ShotsFolder = arguments[shots + 1];

            var playManager = FindAnyObjectByType<PlayManager>();
            var state = playManager.state;
            state.settings.visualEffects = Array.IndexOf(Environment.GetCommandLineArgs(), k_NoEffectsArgument) < 0;
            m_Report.AppendLine($"[BagelPerf] {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsDeviceType}), {SystemInfo.processorType}, {Screen.width}x{Screen.height}, visual effects {(state.settings.visualEffects ? "on" : "off")}");

            yield return new WaitForSecondsRealtime(3f);
            yield return Measure("Main Menu");
            state.SetMainMenuPane(PlayManagerState.MainMenuPane.Settings);
            yield return Measure("Settings");
            state.SetMainMenuPane(PlayManagerState.MainMenuPane.Leaderboard);
            yield return Measure("Leaderboard");
            state.GoToBagelSelection();
            yield return Measure("Bagel Selection");
            state.GoToLevelSelection();
            yield return Measure("Level Selection");
            state.SelectLevel(state.levelCount - 1);
            yield return Measure("Longest Level");

            var gamepad = InputSystem.AddDevice<Gamepad>("Bagel Perf Gamepad");
            state.GoToPlay();
            InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.up });
            yield return Measure("Playing");
            state.Pause();
            yield return Measure("Pause");
            state.Resume();
            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            InputSystem.RemoveDevice(gamepad);
            if (!state.isGameOver)
                state.FinishRun();
            yield return Measure("Game Over");

            Debug.Log(m_Report.ToString());
            Application.Quit();
        }

        IEnumerator Measure(string part)
        {
            yield return new WaitForSecondsRealtime(k_Settle);
            m_Cpu.Clear();
            m_Gpu.Clear();
            var end = Time.realtimeSinceStartup + k_Measure;
            if (m_ShotsFolder != null)
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(m_ShotsFolder, part.Replace(' ', '-') + ".png"));
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
                m_Cpu.Add(Time.unscaledDeltaTime * 1000f);
                FrameTimingManager.CaptureFrameTimings();
                if (FrameTimingManager.GetLatestTimings(1, m_Timings) > 0 && m_Timings[0].gpuFrameTime > 0)
                    m_Gpu.Add((float)m_Timings[0].gpuFrameTime);
            }
            m_Report.AppendLine($"[BagelPerf] {part,-16} frame {Stats(m_Cpu)}" + (m_Gpu.Count > 0 ? $" | gpu {Stats(m_Gpu)}" : ""));
        }

        // Average, 95th and 99th percentiles and the worst, in milliseconds.
        static string Stats(List<float> times)
        {
            if (times.Count == 0)
                return "-";
            times.Sort();
            var sum = 0f;
            foreach (var t in times)
                sum += t;
            float At(float p) => times[Mathf.Clamp(Mathf.CeilToInt(p * times.Count) - 1, 0, times.Count - 1)];
            return $"avg {sum / times.Count:0.00} p95 {At(0.95f):0.00} p99 {At(0.99f):0.00} max {times[^1]:0.00} ms ({times.Count} frames)";
        }
    }
}
