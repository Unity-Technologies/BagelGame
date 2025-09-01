using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [RequireComponent(typeof(UIDocument))]
    public class GameOverScreenDriver : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;

        UIDocument m_UIDocument;
        Label m_Title;

        void Awake()
        {
            m_PlayManager.OnGameOver += PlayManager_OnGameOver;
        }

        void OnEnable()
        {
            m_UIDocument = GetComponent<UIDocument>();
            var root = m_UIDocument.rootVisualElement;

            Button button;

            button = root.Q<Button>("main-menu-button");
            if (button != null)
                button.clicked += m_PlayManager.GoToMainMenu;

            m_Title = root.Q<Label>("title");
        }

        void PlayManager_OnGameOver(object sender, BagelTrackerData bagelTrackerData)
        {
            if (m_Title == null)
                return;

            if (bagelTrackerData.toppingsCount > 0)
                m_Title.text = "You Win!";
            else
                m_Title.text = "You Lose!";
        }
    }
}
