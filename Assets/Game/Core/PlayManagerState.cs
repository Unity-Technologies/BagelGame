using System;
using System.Runtime.CompilerServices;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // The whole game state: plain data and rules, no UI. It's the data source for every
    // panel's bindings, and it tells the binding system exactly what changed and when.
    [CreateAssetMenu(fileName = "PlayManagerState", menuName = "Bagel/Play Manager State")]
    public class PlayManagerState : ScriptableObject, INotifyBindablePropertyChanged, IDataSourceViewHashProvider
    {
        public enum State {
            MainMenu,
            BagelSelection,
            Playing,
            GameOver
        }

        public enum MainMenuPane {
            Main,
            Settings,
            Leaderboard
        }

        public enum MainMenuPaneMode {
            Primary,
            Secondary
        }

        public enum PausePane {
            Pause,
            Settings
        }

        // Paths that code listens for in propertyChanged, made once.
        public static readonly BindingId currentStatePath = nameof(currentState);
        public static readonly BindingId mainMenuPanePath = nameof(mainMenuPane);
        public static readonly BindingId pausePanePath = nameof(pausePane);
        public static readonly BindingId isPausedPath = nameof(isPaused);
        public static readonly BindingId selectedBagelIndexPath = nameof(selectedBagelIndex);
        public static readonly BindingId lastRunPath = nameof(lastRun);
        public static readonly BindingId leaderboardPath = nameof(leaderboard);

        [SerializeField] State m_State;
        [SerializeField] MainMenuPane m_MainMenuPane;
        [SerializeField] PausePane m_PausePane;
        [SerializeField] bool m_IsPaused;

        [Space]
        [SerializeField] BagelTypeCollection m_BagelTypes;
        [SerializeField] int m_SelectedBagelIndex;

        [Space]
        [SerializeField] BagelTrackerData m_Tracker = new BagelTrackerData();
        [SerializeField] RunResult m_LastRun = new RunResult();
        [SerializeField] SettingsData m_Settings = new SettingsData();
        [SerializeField] LeaderboardData m_Leaderboard = new LeaderboardData();

        long m_Version;

        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        public event EventHandler<State> onStateChange;
        public event EventHandler<bool> onPauseStateChanged;
        public event EventHandler<BagelType> onSetBagelType;
        public event EventHandler<MainMenuPaneMode> onMainMenuPaneModeChange;

        [CreateProperty] public State currentState => m_State;
        [CreateProperty] public bool isMainMenu => m_State == State.MainMenu;
        [CreateProperty] public bool isBagelSelection => m_State == State.BagelSelection;
        [CreateProperty] public bool isPlaying => m_State == State.Playing;
        [CreateProperty] public bool isPaused => m_IsPaused;
        [CreateProperty] public bool isGameOver => m_State == State.GameOver;

        [CreateProperty]
        public MainMenuPane mainMenuPane
        {
            get => m_MainMenuPane;
            set => SetMainMenuPane(value);
        }

        [CreateProperty]
        public MainMenuPaneMode currentMainMenuPaneMode =>
            m_MainMenuPane == MainMenuPane.Main ? MainMenuPaneMode.Primary : MainMenuPaneMode.Secondary;

        [CreateProperty]
        public PausePane pausePane
        {
            get => m_PausePane;
            set
            {
                if (m_PausePane == value)
                    return;
                m_PausePane = value;
                Notify();
            }
        }

        [CreateProperty] public BagelTypeCollection bagelTypes => m_BagelTypes;
        [CreateProperty] public int bagelTypeCount => m_BagelTypes != null ? m_BagelTypes.collection.Count : 0;

        [CreateProperty]
        public int selectedBagelIndex
        {
            get => m_SelectedBagelIndex;
            set => SelectBagel(value);
        }

        [CreateProperty]
        public BagelType selectedBagelType =>
            bagelTypeCount > 0 ? m_BagelTypes.collection[Mathf.Clamp(m_SelectedBagelIndex, 0, bagelTypeCount - 1)] : null;

        [CreateProperty] public bool canSelectPreviousBagel => m_SelectedBagelIndex > 0;
        [CreateProperty] public bool canSelectNextBagel => m_SelectedBagelIndex < bagelTypeCount - 1;

        [CreateProperty] public BagelTrackerData tracker => m_Tracker;
        [CreateProperty] public RunResult lastRun => m_LastRun;
        [CreateProperty] public SettingsData settings => m_Settings;
        [CreateProperty] public LeaderboardData leaderboard => m_Leaderboard;

        public void GoToMainMenu()
        {
            Resume();
            SetState(State.MainMenu);
            SetMainMenuPane(MainMenuPane.Main);
        }

        public void GoToBagelSelection()
        {
            Resume();
            SetState(State.BagelSelection);
        }

        public void GoToPlay()
        {
            Resume();
            var bagelType = selectedBagelType;
            m_Tracker.Reset(bagelType != null ? bagelType.maxToppingCount : 0);
            SetState(State.Playing);
            onSetBagelType?.Invoke(this, bagelType);
        }

        public void FinishRun()
        {
            var bagelType = selectedBagelType;
            m_LastRun.Set(bagelType != null ? bagelType.displayName : "", bagelType != null ? bagelType.nameKey : "",
                m_Tracker.toppingsCount, m_Tracker.toppingsMaxCount, m_Tracker.elapsed, m_Tracker.freshness);
            GoToGameOver();
        }

        public void GoToGameOver()
        {
            Resume();
            SetState(State.GameOver);
        }

        public void SetMainMenuPane(MainMenuPane pane)
        {
            if (!isMainMenu && pane != MainMenuPane.Main)
                return;
            if (pane == m_MainMenuPane)
                return;

            var oldMode = currentMainMenuPaneMode;
            m_MainMenuPane = pane;
            Notify(nameof(mainMenuPane));

            if (oldMode == currentMainMenuPaneMode)
                return;
            Notify(nameof(currentMainMenuPaneMode));
            onMainMenuPaneModeChange?.Invoke(this, currentMainMenuPaneMode);
        }

        public void SelectBagel(int index)
        {
            index = Mathf.Clamp(index, 0, Mathf.Max(0, bagelTypeCount - 1));
            if (index == m_SelectedBagelIndex)
                return;

            m_SelectedBagelIndex = index;
            Notify(nameof(selectedBagelIndex));
            Notify(nameof(selectedBagelType));
            Notify(nameof(canSelectPreviousBagel));
            Notify(nameof(canSelectNextBagel));
            onSetBagelType?.Invoke(this, selectedBagelType);
        }

        public void SelectPreviousBagel() => SelectBagel(m_SelectedBagelIndex - 1);

        public void SelectNextBagel() => SelectBagel(m_SelectedBagelIndex + 1);

        public void SubmitToLeaderboard(string playerName)
        {
            if (!m_LastRun.canSubmit)
                return;

            m_Leaderboard.Add(playerName, m_LastRun.toppings);
            m_LastRun.submitted = true;
        }

        public void Pause()
        {
            if (m_IsPaused)
                return;

            if (!isPlaying)
                return;

            m_IsPaused = true;
            pausePane = PausePane.Pause;
            Notify(nameof(isPaused));
            onPauseStateChanged?.Invoke(this, true);
        }

        public void Resume()
        {
            if (!m_IsPaused)
                return;

            m_IsPaused = false;
            Notify(nameof(isPaused));
            onPauseStateChanged?.Invoke(this, false);
        }

        public void TogglePause()
        {
            if (m_IsPaused)
                Resume();
            else
                Pause();
        }

        void SetState(State state)
        {
            m_State = state;
            Notify(nameof(currentState));
            Notify(nameof(isMainMenu));
            Notify(nameof(isBagelSelection));
            Notify(nameof(isPlaying));
            Notify(nameof(isGameOver));
            onStateChange?.Invoke(this, state);
        }

        // What a save keeps of the state, for JsonUtility.
        [Serializable]
        public class Saved
        {
            public int version = 1;
            public float fieldOfView;
            public int themeIndex;
            public bool reduceMotion;
            public bool visualEffects = true;
            public bool sound = true;
            public float soundVolume = 0.8f;
            public System.Collections.Generic.List<LeaderboardData.LeaderboardEntry> leaderboard = new();
        }

        public Saved ToSaved()
        {
            var saved = new Saved
            {
                fieldOfView = m_Settings.fieldOfView,
                themeIndex = m_Settings.themeIndex,
                reduceMotion = m_Settings.reduceMotion,
                visualEffects = m_Settings.visualEffects,
                sound = m_Settings.sound,
                soundVolume = m_Settings.soundVolume
            };
            saved.leaderboard.AddRange(m_Leaderboard.entries);
            return saved;
        }

        public void Load(Saved saved)
        {
            if (saved == null)
                return;

            m_Settings.fieldOfView = saved.fieldOfView;
            m_Settings.themeIndex = saved.themeIndex;
            m_Settings.reduceMotion = saved.reduceMotion;
            m_Settings.visualEffects = saved.visualEffects;
            m_Settings.sound = saved.sound;
            m_Settings.soundVolume = saved.soundVolume;
            m_Leaderboard.Load(saved.leaderboard);
        }

        // Clone a template (the start state, or a preview state) into a throwaway runtime instance.
        public static PlayManagerState CreateRuntimeCopy(PlayManagerState template)
        {
            var copy = template != null ? Instantiate(template) : CreateInstance<PlayManagerState>();
            copy.name = (template != null ? template.name : nameof(PlayManagerState)) + " (Runtime)";
            copy.hideFlags = HideFlags.DontSave;
            copy.Initialize();
            return copy;
        }

        public static void DestroyRuntimeCopy(PlayManagerState copy)
        {
            if (copy == null)
                return;

            if (Application.isPlaying)
                Destroy(copy);
            else
                DestroyImmediate(copy);
        }

        void OnEnable() => Initialize();

        void OnValidate()
        {
            // Inspector edits (e.g. in the Bagel All UI window) refresh every bound UI.
            Initialize();
            NotifyAll();
        }

        void Initialize()
        {
            m_Tracker ??= new BagelTrackerData();
            m_LastRun ??= new RunResult();
            m_Settings ??= new SettingsData();
            m_Leaderboard ??= new LeaderboardData();

            m_Tracker.Bind(this);
            m_LastRun.Bind(this);
            m_Settings.Bind(this);
            m_Leaderboard.Bind(this);
        }

        public long GetViewHashCode() => m_Version;

        public void NotifyAll()
        {
            foreach (var property in s_AllProperties)
                Notify(property);
        }

        static readonly string[] s_AllProperties =
        {
            nameof(currentState), nameof(isMainMenu), nameof(isBagelSelection), nameof(isPlaying), nameof(isPaused),
            nameof(isGameOver), nameof(mainMenuPane), nameof(currentMainMenuPaneMode), nameof(pausePane),
            nameof(bagelTypes), nameof(bagelTypeCount), nameof(selectedBagelIndex), nameof(selectedBagelType),
            nameof(canSelectPreviousBagel), nameof(canSelectNextBagel), nameof(tracker), nameof(lastRun),
            nameof(settings), nameof(leaderboard)
        };

        internal void Notify([CallerMemberName] string property = "") => Notify(new BindingId(property));

        internal void Notify(in BindingId property)
        {
            m_Version++;
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));
        }
    }
}
