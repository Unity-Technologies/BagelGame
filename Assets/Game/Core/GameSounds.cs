using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // Plugs the synth this AudioSource plays in as the game's sound (BagelAudio), and plays what happens
    // to the game: a run starting and ending, pausing, the camera moving between rooms, picking another
    // bagel, a new theme, a name on the leaderboard. Settings: Sound mutes it all, Volume sets the source's.
    [RequireComponent(typeof(AudioSource), typeof(PlayManager))]
    public class GameSounds : MonoBehaviour
    {
        AudioSource m_Source;
        PlayManagerState m_State;
        PlayManagerState.State m_LastState;

        void OnEnable()
        {
            m_Source = GetComponent<AudioSource>();
            m_State = GetComponent<PlayManager>().state;
            m_LastState = m_State.currentState;
            BagelAudio.source = m_Source;
            ApplySettings();

            m_State.onStateChange += State_OnStateChange;
            m_State.onPauseStateChanged += State_OnPauseStateChanged;
            m_State.onMainMenuPaneModeChange += State_OnMainMenuPaneModeChange;
            m_State.propertyChanged += State_OnPropertyChanged;
            m_State.leaderboard.onEntryAdded += Leaderboard_OnEntryAdded;
        }

        void OnDisable()
        {
            m_State.onStateChange -= State_OnStateChange;
            m_State.onPauseStateChanged -= State_OnPauseStateChanged;
            m_State.onMainMenuPaneModeChange -= State_OnMainMenuPaneModeChange;
            m_State.propertyChanged -= State_OnPropertyChanged;
            m_State.leaderboard.onEntryAdded -= Leaderboard_OnEntryAdded;

            if (BagelAudio.source == m_Source)
                BagelAudio.source = null;
        }

        void ApplySettings()
        {
            BagelAudio.enabled = m_State.settings.sound;
            m_Source.volume = m_State.settings.soundVolume;
        }

        void State_OnStateChange(object sender, PlayManagerState.State state)
        {
            var from = m_LastState;
            m_LastState = state;
            if (state == from)
                return;

            switch (state)
            {
                case PlayManagerState.State.Playing:
                    BagelAudio.Play("go");
                    break;
                case PlayManagerState.State.GameOver:
                    BagelAudio.Play(m_State.lastRun.won ? "win" : "lose");
                    break;
                default:
                    // The camera glides to the menu or to the bagels.
                    BagelAudio.Play("swish");
                    break;
            }
        }

        void State_OnPauseStateChanged(object sender, bool paused) => BagelAudio.Play(paused ? "open" : "close");

        void State_OnMainMenuPaneModeChange(object sender, PlayManagerState.MainMenuPaneMode mode) => BagelAudio.Play("swish");

        void Leaderboard_OnEntryAdded(int index) => BagelAudio.Play("submit");

        void State_OnPropertyChanged(object sender, BindablePropertyChangedEventArgs args)
        {
            var property = args.propertyName;
            if (property == SettingsData.soundPath || property == SettingsData.soundVolumePath)
                ApplySettings();
            else if (property == SettingsData.themeIndexPath)
                BagelAudio.Play("chime");
            else if (property == PlayManagerState.selectedBagelIndexPath)
                BagelAudio.Play("swish", semitones: 3f);
        }
    }
}
