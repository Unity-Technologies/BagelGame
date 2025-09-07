using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuScreenDriver : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;

        UIDocument m_UIDocument;

        Button m_PlayButton;

        void OnEnable()
        {
            m_PlayManager.State.OnStateChange += State_OnStateChange;

            m_UIDocument = GetComponent<UIDocument>();
            var root = m_UIDocument.rootVisualElement;

            m_PlayButton = root.Q<Button>("play-button");
            if (m_PlayButton != null)
                m_PlayButton.clicked += m_PlayManager.State.GoToBagelSelection;

            var exitButton = root.Q<Button>("quit-button");
            if (exitButton != null)
                exitButton.clicked += Application.Quit;
        }

        void OnDisable()
        {
            m_PlayManager.State.OnStateChange -= State_OnStateChange;
        }

        void State_OnStateChange(object sender, PlayManagerState.State state)
        {
            if (m_PlayButton == null)
                return;

            if (state == PlayManagerState.State.MainMenu)
            {
                m_PlayButton.Focus();
                m_PlayManager.PlayInputBindings.OnNavigationAction += PlayInputBindings_OnNavigationAction;
            }
            else
            {
                m_PlayManager.PlayInputBindings.OnNavigationAction -= PlayInputBindings_OnNavigationAction;
            }
        }

        void PlayInputBindings_OnNavigationAction(object sender, System.EventArgs e)
        {
            m_PlayButton.Focus();
        }
    }
}
