using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [RequireComponent(typeof(UIDocument))]
    public class GameOverScreenDriver : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;

        UIDocument m_UIDocument;
        VisualElement m_Root;
        Label m_Title;

        void Awake()
        {
            m_PlayManager.State.OnStateChange += State_OnStateChange;
            m_PlayManager.State.OnSetBagelTrackerData += State_OnSetBagelTrackerData;
        }

        void OnEnable()
        {
            m_UIDocument = GetComponent<UIDocument>();
            m_Root = m_UIDocument.rootVisualElement;

            Button button;

            button = m_Root.Q<Button>("main-menu-button");
            if (button != null)
                button.clicked += m_PlayManager.State.GoToMainMenu;

            m_Title = m_Root.Q<Label>("title");
        }

        void State_OnStateChange(object sender, PlayManagerState.State state)
        {
            if (state == PlayManagerState.State.GameOver)
                m_Root.style.display = DisplayStyle.Flex;
            else
                m_Root.style.display = DisplayStyle.None;
        }

        void State_OnSetBagelTrackerData(object sender, BagelTrackerData bagelTrackerData)
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
