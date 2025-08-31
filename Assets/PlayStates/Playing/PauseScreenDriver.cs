using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(UIDocument))]
    public class PauseScreenDriver : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;

        UIDocument m_UIDocument;
        VisualElement m_Root;

        void Start()
        {
            m_PlayManager.OnPauseStateChanged += PlayerManager_OnPauseStateChanged;
        }

        void OnEnable()
        {
            m_UIDocument = GetComponent<UIDocument>();
            m_Root = m_UIDocument.rootVisualElement;
            SetPauseState(false);

            if (m_PlayManager == null)
                return;

            Button button;

            button = m_Root.Q<Button>("resume-button");
            if (button != null)
                button.clicked += m_PlayManager.Resume;

            button = m_Root.Q<Button>("main-menu-button");
            if (button != null)
                button.clicked += m_PlayManager.GoToMainMenu;
        }

        void PlayerManager_OnPauseStateChanged(object sender, bool paused)
        {
            SetPauseState(paused);
        }

        void SetPauseState(bool paused)
        {
            if (m_Root == null)
                return;

            if (paused)
                m_Root.style.display = DisplayStyle.Flex;
            else
                m_Root.style.display = DisplayStyle.None;
        }
    }
}
