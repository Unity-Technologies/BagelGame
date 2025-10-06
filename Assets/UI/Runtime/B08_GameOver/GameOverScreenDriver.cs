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

        VisualElement m_Root;
        GameOverScreenManager m_GameOverScreenManager;

        void Awake()
        {
            m_PlayManager.State.OnStateChange += State_OnStateChange;
            m_PlayManager.State.OnSetBagelTrackerData += State_OnSetBagelTrackerData;
        }

        void OnEnable()
        {
            m_Root = GetComponent<UIDocument>().rootVisualElement;
            m_GameOverScreenManager = m_Root.Q<GameOverScreenManager>();
            m_GameOverScreenManager.gameOverPaneManager.BindUI(new GameOverPaneManager.Callbacks
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
                m_GameOverScreenManager.gameOverPaneManager.mainMenuButton.Focus();
                m_Root.style.display = DisplayStyle.Flex;

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
                m_Root.style.display = DisplayStyle.None;
            }
        }

        void State_OnSetBagelTrackerData(object sender, BagelTrackerData bagelTrackerData)
        {
            if (m_GameOverScreenManager.gameOverPaneManager.title == null)
                return;

            if (bagelTrackerData.toppingsCount > 0)
            {
                m_GameOverScreenManager.gameOverPaneManager.title.text = "You Win!";
                m_GameOverScreenManager.gameOverPaneManager.toppingsNumberField.value = bagelTrackerData.toppingsCount;
            }
            else
            {
                m_GameOverScreenManager.gameOverPaneManager.title.text = "You Lose!";
            }
        }
    }
}
