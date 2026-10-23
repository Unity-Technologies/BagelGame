using UnityEngine;

namespace Bagel
{
    [ExecuteInEditMode]
    public class PlayManager : MonoBehaviour
    {
        [SerializeField] PlayManagerState m_StartState;
        [SerializeField] PlayInputBindings m_PlayInputBindings;

        PlayManagerState m_State;

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

            state.GoToMainMenu();
        }

        void OnDisable()
        {
            PlayManagerState.DestroyRuntimeCopy(m_State);
            m_State = null;
        }
    }
}
