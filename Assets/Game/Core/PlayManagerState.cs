using System;
using System.Collections.Generic;
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
            LevelSelection,
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
        public static readonly BindingId selectedLevelIndexPath = nameof(selectedLevelIndex);
        // The levels to choose from: one added, one gone, one of the player's own changed.
        public static readonly BindingId levelsPath = nameof(levels);
        public static readonly BindingId isEditingLevelPath = nameof(isEditingLevel);
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
        [SerializeField] BagelLevelCollection m_Levels;
        [SerializeField] int m_SelectedLevelIndex;

        [Space]
        [SerializeField] BagelTrackerData m_Tracker = new BagelTrackerData();
        [SerializeField] RunResult m_LastRun = new RunResult();
        [SerializeField] SettingsData m_Settings = new SettingsData();
        [SerializeField] LeaderboardData m_Leaderboard = new LeaderboardData();

        long m_Version;

        // The player's own levels, after the game's, and all of them in that order.
        readonly List<BagelLevel> m_CustomLevels = new();
        readonly List<BagelLevel> m_AllLevels = new();
        bool m_IsEditingLevel;

        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        public event EventHandler<State> onStateChange;
        public event EventHandler<bool> onPauseStateChanged;
        public event EventHandler<BagelType> onSetBagelType;
        public event EventHandler<MainMenuPaneMode> onMainMenuPaneModeChange;
        public event EventHandler<BagelLevel> onSetLevel;

        [CreateProperty] public State currentState => m_State;
        [CreateProperty] public bool isMainMenu => m_State == State.MainMenu;
        [CreateProperty] public bool isBagelSelection => m_State == State.BagelSelection;
        [CreateProperty] public bool isLevelSelection => m_State == State.LevelSelection;
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

        // The bagels go round: before the first is the last.
        [CreateProperty] public bool canSelectPreviousBagel => bagelTypeCount > 1;
        [CreateProperty] public bool canSelectNextBagel => bagelTypeCount > 1;

        // The game's own levels, then the player's.
        [CreateProperty] public IReadOnlyList<BagelLevel> levels => m_AllLevels;
        [CreateProperty] public int levelCount => m_AllLevels.Count;
        public BagelLevelCollection levelCollection => m_Levels;
        public BagelLevelLibrary levelLibrary => m_Levels != null ? m_Levels.library : null;

        [CreateProperty]
        public int selectedLevelIndex
        {
            get => m_SelectedLevelIndex;
            set => SelectLevel(value);
        }

        [CreateProperty]
        public BagelLevel selectedLevel =>
            levelCount > 0 ? m_AllLevels[Mathf.Clamp(m_SelectedLevelIndex, 0, levelCount - 1)] : null;

        // Levels go up and down a list: no going round.
        [CreateProperty] public bool canSelectPreviousLevel => m_SelectedLevelIndex > 0;
        [CreateProperty] public bool canSelectNextLevel => m_SelectedLevelIndex < levelCount - 1;

        // Only the player's own levels are changed in the game.
        [CreateProperty] public bool canEditLevel => selectedLevel != null && selectedLevel.custom;
        [CreateProperty] public bool isEditingLevel => m_IsEditingLevel;

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

        public void GoToLevelSelection()
        {
            Resume();
            SetState(State.LevelSelection);
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
                m_Tracker.toppingsCount, m_Tracker.toppingsMaxCount, m_Tracker.elapsed, selectedLevel);
            // The television shows this level's board.
            if (selectedLevel != null)
                m_Leaderboard.shownLevel = selectedLevel.id;
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

            // From the main menu, the leaderboard opens on the chosen level's board.
            if (pane == MainMenuPane.Leaderboard && selectedLevel != null)
                m_Leaderboard.shownLevel = selectedLevel.id;

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

        public void SelectPreviousBagel() => SelectBagel(bagelTypeCount > 0 ? (m_SelectedBagelIndex - 1 + bagelTypeCount) % bagelTypeCount : 0);

        public void SelectNextBagel() => SelectBagel(bagelTypeCount > 0 ? (m_SelectedBagelIndex + 1) % bagelTypeCount : 0);

        public void SelectLevel(int index)
        {
            index = Mathf.Clamp(index, 0, Mathf.Max(0, levelCount - 1));
            if (index == m_SelectedLevelIndex)
                return;

            m_SelectedLevelIndex = index;
            if (m_IsEditingLevel && !canEditLevel)
                EditLevel(false);
            NotifyLevelSelection();
        }

        public void SelectPreviousLevel() => SelectLevel(m_SelectedLevelIndex - 1);

        public void SelectNextLevel() => SelectLevel(m_SelectedLevelIndex + 1);

        public const string customLevelPrefix = "custom-";

        // A new level of the player's own, after the others, chosen: empty but for the toaster at the end.
        public BagelLevel AddLevel(string name)
        {
            var number = 1;
            foreach (var level in m_CustomLevels)
            {
                if (level.id.StartsWith(customLevelPrefix, StringComparison.Ordinal) &&
                    int.TryParse(level.id.Substring(customLevelPrefix.Length), out var taken))
                    number = Mathf.Max(number, taken + 1);
            }
            var added = BagelLevel.CreateCustom(customLevelPrefix + number, name);
            AddCustomLevel(added);
            m_SelectedLevelIndex = m_AllLevels.IndexOf(added);
            NotifyLevels();
            NotifyLevelSelection();
            return added;
        }

        // Only the player's own go; the one before it is chosen.
        public void DeleteLevel(BagelLevel level)
        {
            if (level == null || !level.custom || !m_CustomLevels.Contains(level))
                return;

            var index = m_AllLevels.IndexOf(level);
            var wasSelected = level == selectedLevel;
            if (wasSelected)
                EditLevel(false);
            m_CustomLevels.Remove(level);
            level.changed -= CustomLevel_OnChanged;
            m_Leaderboard.RemoveLevel(level.id);
            if (m_Leaderboard.shownLevel == level.id)
                m_Leaderboard.shownLevel = null;
            RefreshLevels();
            var selectionMoved = index <= m_SelectedLevelIndex && m_SelectedLevelIndex > 0;
            if (selectionMoved)
                m_SelectedLevelIndex--;
            NotifyLevels();
            if (wasSelected || selectionMoved)
                NotifyLevelSelection();
            DestroyLevel(level);
        }

        // The tablet's editor, open on the chosen level: only for the player's own, while levels are chosen.
        public void EditLevel(bool editing)
        {
            editing &= isLevelSelection && canEditLevel;
            if (m_IsEditingLevel == editing)
                return;
            m_IsEditingLevel = editing;
            Notify(nameof(isEditingLevel));
        }

        void AddCustomLevel(BagelLevel level)
        {
            m_CustomLevels.Add(level);
            level.changed += CustomLevel_OnChanged;
            RefreshLevels();
        }

        void CustomLevel_OnChanged(BagelLevel level) => Notify(nameof(levels));

        void RefreshLevels()
        {
            m_AllLevels.Clear();
            if (m_Levels != null)
            {
                foreach (var level in m_Levels.collection)
                {
                    if (level != null)
                        m_AllLevels.Add(level);
                }
            }
            m_AllLevels.AddRange(m_CustomLevels);
            if (m_Leaderboard != null)
                m_Leaderboard.defaultLevel = m_AllLevels.Count > 0 ? m_AllLevels[0].id : null;
        }

        void NotifyLevels()
        {
            Notify(nameof(levels));
            Notify(nameof(levelCount));
            Notify(nameof(canSelectNextLevel));
        }

        void NotifyLevelSelection()
        {
            Notify(nameof(selectedLevelIndex));
            Notify(nameof(selectedLevel));
            Notify(nameof(canSelectPreviousLevel));
            Notify(nameof(canSelectNextLevel));
            Notify(nameof(canEditLevel));
            onSetLevel?.Invoke(this, selectedLevel);
        }

        void ClearCustomLevels()
        {
            foreach (var level in m_CustomLevels)
            {
                if (level == null)
                    continue;
                level.changed -= CustomLevel_OnChanged;
                DestroyLevel(level);
            }
            m_CustomLevels.Clear();
            RefreshLevels();
        }

        static void DestroyLevel(BagelLevel level)
        {
            if (Application.isPlaying)
                Destroy(level);
            else
                DestroyImmediate(level);
        }

        public void SubmitToLeaderboard(string playerName)
        {
            if (!m_LastRun.canSubmit)
                return;

            m_Leaderboard.Add(playerName, m_LastRun.toppings, m_LastRun.time, m_LastRun.levelId);
            m_LastRun.submitted = true;
        }

        // The level whose board the leaderboard shows, and the one before or after it.
        public BagelLevel leaderboardLevel => m_AllLevels.Find(level => level.id == m_Leaderboard.shownLevel) ?? (levelCount > 0 ? m_AllLevels[0] : null);

        public bool canShowPreviousLeaderboardLevel => m_AllLevels.IndexOf(leaderboardLevel) > 0;
        public bool canShowNextLeaderboardLevel => m_AllLevels.IndexOf(leaderboardLevel) < levelCount - 1;

        public void ShowLeaderboardLevel(int step)
        {
            var index = Mathf.Clamp(m_AllLevels.IndexOf(leaderboardLevel) + step, 0, Mathf.Max(0, levelCount - 1));
            if (index < levelCount)
                m_Leaderboard.shownLevel = m_AllLevels[index].id;
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
            if (state != State.LevelSelection)
                EditLevel(false);
            Notify(nameof(currentState));
            Notify(nameof(isMainMenu));
            Notify(nameof(isBagelSelection));
            Notify(nameof(isLevelSelection));
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
            public bool postProcessing = true;
            public int antiAliasing;
            public bool depthOfField = true;
            public bool shadows = true;
            public int frameRate;
            public System.Collections.Generic.List<LeaderboardData.LeaderboardEntry> leaderboard = new();
            public RunResult lastRun = new();
            public string selectedLevel;
            public System.Collections.Generic.List<BagelLevel.Saved> customLevels = new();
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
                soundVolume = m_Settings.soundVolume,
                postProcessing = m_Settings.postProcessing,
                antiAliasing = m_Settings.antiAliasing,
                depthOfField = m_Settings.depthOfField,
                shadows = m_Settings.shadows,
                frameRate = m_Settings.frameRate
            };
            saved.leaderboard.AddRange(m_Leaderboard.entries);
            saved.lastRun = m_LastRun;
            saved.selectedLevel = selectedLevel != null ? selectedLevel.id : null;
            foreach (var level in m_CustomLevels)
                saved.customLevels.Add(level.ToSaved());
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
            m_Settings.postProcessing = saved.postProcessing;
            m_Settings.antiAliasing = saved.antiAliasing;
            m_Settings.depthOfField = saved.depthOfField;
            m_Settings.shadows = saved.shadows;
            m_Settings.frameRate = saved.frameRate;
            m_Leaderboard.Load(saved.leaderboard);
            m_LastRun.Load(saved.lastRun);

            // The player's own levels, each once, and the level they chose last.
            ClearCustomLevels();
            if (saved.customLevels != null)
            {
                foreach (var savedLevel in saved.customLevels)
                {
                    var level = BagelLevel.FromSaved(savedLevel);
                    if (level == null)
                        continue;
                    if (m_AllLevels.Find(other => other.id == level.id) == null)
                        AddCustomLevel(level);
                    else
                        DestroyLevel(level);
                }
            }
            m_SelectedLevelIndex = Mathf.Max(0, m_AllLevels.FindIndex(level => level.id == saved.selectedLevel));
            NotifyLevels();
            NotifyLevelSelection();
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

            copy.ClearCustomLevels();
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
            RefreshLevels();

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
            nameof(isGameOver), nameof(isLevelSelection), nameof(mainMenuPane), nameof(currentMainMenuPaneMode), nameof(pausePane),
            nameof(bagelTypes), nameof(bagelTypeCount), nameof(selectedBagelIndex), nameof(selectedBagelType),
            nameof(canSelectPreviousBagel), nameof(canSelectNextBagel), nameof(levels), nameof(levelCount),
            nameof(selectedLevelIndex), nameof(selectedLevel), nameof(canSelectPreviousLevel), nameof(canSelectNextLevel),
            nameof(canEditLevel), nameof(isEditingLevel), nameof(tracker), nameof(lastRun),
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
