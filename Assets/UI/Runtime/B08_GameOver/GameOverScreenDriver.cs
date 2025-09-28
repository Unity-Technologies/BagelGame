using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [RequireComponent(typeof(UIDocument))]
    public class GameOverScreenDriver : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;

        [Space]
        [SerializeField] float m_GameOverOrbitSpeed = 0.02f;
        [SerializeField] Transform m_GameOverRoot;
        [SerializeField] Transform m_BagelTarget;

        UIDocument m_UIDocument;

        GameOverScreenManager.Elements m_Elements;

        void Awake()
        {
            m_PlayManager.State.OnStateChange += State_OnStateChange;
            m_PlayManager.State.OnSetBagelTrackerData += State_OnSetBagelTrackerData;
        }

        void OnEnable()
        {
            m_UIDocument = GetComponent<UIDocument>();
            m_Elements = GameOverScreenManager.BindUI(m_UIDocument.rootVisualElement, new GameOverScreenManager.Callbacks
            {
                onRestart = m_PlayManager.State.GoToPlay,
                onMainMenu = m_PlayManager.State.GoToMainMenu
            });
        }

        void Update()
        {
            if (!m_PlayManager.State.IsGameOver)
                return;

            m_GameOverRoot.Rotate(Vector3.up, m_GameOverOrbitSpeed);
        }

        void State_OnStateChange(object sender, PlayManagerState.State state)
        {
            if (state == PlayManagerState.State.GameOver)
            {
                m_Elements.mainMenuButton.Focus();
                m_Elements.root.style.display = DisplayStyle.Flex;

                m_GameOverRoot.rotation = Quaternion.identity;

                var gameOverPosition = m_GameOverRoot.position;
                var bagelPosition = m_BagelTarget.position;
                m_GameOverRoot.position = new Vector3(
                    bagelPosition.x,
                    gameOverPosition.y,
                    bagelPosition.z
                );
            }
            else
            {
                m_Elements.root.style.display = DisplayStyle.None;
            }
        }

        void State_OnSetBagelTrackerData(object sender, BagelTrackerData bagelTrackerData)
        {
            if (m_Elements.title == null)
                return;

            if (bagelTrackerData.toppingsCount > 0)
            {
                m_Elements.title.text = "You Win!";
                m_Elements.toppingsField.value = bagelTrackerData.toppingsCount;
            }
            else
            {
                m_Elements.title.text = "You Lose!";
            }
        }
    }
}
