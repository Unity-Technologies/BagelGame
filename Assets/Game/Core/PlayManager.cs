using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [ExecuteInEditMode]
    public class PlayManager : MonoBehaviour
    {
        [SerializeField] PlayManagerState m_StartState;
        [SerializeField] PlayInputBindings m_PlayInputBindings;
        [SerializeField] float m_SaveDelay = 1.0f;

        PlayManagerState m_State;
        float m_SaveTime = -1.0f;

        // Everyone shares this runtime copy of the start state, so playing never changes the asset.
        public PlayManagerState state
        {
            get
            {
                if (m_State == null)
                    m_State = PlayManagerState.CreateRuntimeCopy(m_StartState);
                return m_State;
            }
        }

        public PlayManagerState startState => m_StartState;
        public PlayInputBindings playInputBindings => m_PlayInputBindings;

        void OnEnable()
        {
            if (!Application.isPlaying)
                return;

            BagelSave.Load(state);
            state.propertyChanged += State_OnPropertyChanged;
            state.GoToMainMenu();
        }

        void OnDisable()
        {
            if (m_State != null && Application.isPlaying)
            {
                m_State.propertyChanged -= State_OnPropertyChanged;
                BagelSave.Save(m_State);
            }

            PlayManagerState.DestroyRuntimeCopy(m_State);
            m_State = null;
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
                BagelSave.Save(m_State);
        }

        void Update()
        {
            // Save a moment after the last change, so dragging a slider doesn't write every frame.
            if (m_SaveTime < 0.0f || Time.unscaledTime < m_SaveTime)
                return;

            m_SaveTime = -1.0f;
            BagelSave.Save(m_State);
        }

        void State_OnPropertyChanged(object sender, BindablePropertyChangedEventArgs args)
        {
            var path = args.propertyName.ToString();
            if (path.StartsWith(nameof(PlayManagerState.settings), StringComparison.Ordinal) ||
                path.StartsWith(nameof(PlayManagerState.leaderboard), StringComparison.Ordinal) ||
                path.StartsWith(nameof(PlayManagerState.lastRun), StringComparison.Ordinal) ||
                path == nameof(PlayManagerState.levels) || path == nameof(PlayManagerState.selectedLevelIndex))
                m_SaveTime = Time.unscaledTime + m_SaveDelay;
        }
    }
}
