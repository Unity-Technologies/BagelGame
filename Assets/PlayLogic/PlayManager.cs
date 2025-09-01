using System;
using Unity.Cinemachine;
using UnityEngine;

namespace Bagel
{
    public class PlayManager : MonoBehaviour
    {
        [SerializeField] PlayInputBindings m_PlayInputBindings;

        PlayManagerState m_PlayManagerState = new PlayManagerState();

        public PlayManagerState State => m_PlayManagerState;
        public PlayInputBindings PlayInputBindings => m_PlayInputBindings;

        void OnEnable()
        {
            State.GoToMainMenu();
        }
    }
}
