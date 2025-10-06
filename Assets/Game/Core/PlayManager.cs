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

        public PlayManagerState state => m_PlayManagerState;
        public GameObject playSettingsObject => m_PlaySettingsObject;
        public PlayInputBindings playInputBindings => m_PlayInputBindings;

        void OnEnable()
        {
            state.GoToMainMenu();
        }
    }
}
