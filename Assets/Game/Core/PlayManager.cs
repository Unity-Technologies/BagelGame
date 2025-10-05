using System;
using Unity.Cinemachine;
using UnityEngine;

namespace Bagel
{
    public class PlayManager : MonoBehaviour
    {
        [SerializeField] GameObject m_PlaySettingsObject;
        [SerializeField] PlayInputBindings m_PlayInputBindings;

        PlayManagerState m_PlayManagerState = new PlayManagerState();

        public PlayManagerState State => m_PlayManagerState;
        public GameObject playSettingsObject => m_PlaySettingsObject;
        public PlayInputBindings PlayInputBindings => m_PlayInputBindings;

        void OnEnable()
        {
            State.GoToMainMenu();
        }
    }
}
