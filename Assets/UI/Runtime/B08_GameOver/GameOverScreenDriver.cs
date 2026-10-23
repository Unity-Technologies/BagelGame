using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [RequireComponent(typeof(PanelRenderer))]
    public class GameOverScreenDriver : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;

        [Space]
        [SerializeField] float m_GameOverOrbitSpeed = 0.02f;
        [SerializeField] Transform m_GameOverRoot;
        [SerializeField] Transform m_BagelTarget;

        PlayManagerState m_State;
        VisualElement m_Root;
        GameOverScreenManager m_GameOverScreenManager;

        int m_UIVersion;

        void OnEnable()
        {
            m_State = m_PlayManager.state;
            m_State.onStateChange += State_OnStateChange;

            GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);
        }

        void OnDisable()
        {
            m_State.onStateChange -= State_OnStateChange;

            GetComponent<PanelRenderer>().UnregisterUIReloadCallback(OnUIReload);

            // Play mode can start without a domain reload, and the panel counts versions again from
            // the start: forget the last one so the next UI is always set up.
            m_UIVersion = 0;
        }

        void OnUIReload(PanelRenderer panelRenderer, VisualElement rootElement, int version)
        {
            if (m_UIVersion == version)
                return;

            var gameOverScreenManager = rootElement.Q<GameOverScreenManager>();
            if (gameOverScreenManager == null)
                return; // The UXML is still importing; the next reload brings it.
            m_UIVersion = version;

            m_Root = rootElement;
            m_GameOverScreenManager = gameOverScreenManager;
            m_GameOverScreenManager.SetState(m_State);

            // On a fresh scene load the UI arrives after the first state change, so catch up here.
            m_Root.style.display = m_State.isGameOver ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void Update()
        {
            if (!m_State.isGameOver)
                return;

            m_GameOverRoot.Rotate(Vector3.up, m_GameOverOrbitSpeed);
        }

        void State_OnStateChange(object sender, PlayManagerState.State state)
        {
            if (state == PlayManagerState.State.GameOver)
            {
                m_GameOverRoot.rotation = Quaternion.identity;

                var gameOverPosition = m_GameOverRoot.position;
                var bagelPosition = m_BagelTarget.position;
                m_GameOverRoot.position = new Vector3(
                    bagelPosition.x,
                    gameOverPosition.y,
                    bagelPosition.z
                );
            }

            if (m_Root == null)
                return;

            m_Root.style.display = state == PlayManagerState.State.GameOver ? DisplayStyle.Flex : DisplayStyle.None;

            if (state == PlayManagerState.State.GameOver)
            {
                m_GameOverScreenManager.schedule.Execute(() => m_GameOverScreenManager.gameOverPaneManager.mainMenuButton.Focus());
                BagelAccessibility.ShowScreen(m_GameOverScreenManager);
            }
        }
    }
}
