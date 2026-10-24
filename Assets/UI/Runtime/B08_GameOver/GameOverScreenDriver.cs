using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // Shows the game over screen on the menu that falls onto the table when a run ends (GameOverMenu): the
    // panel lies on the menu's pages, wherever the menu is.
    [RequireComponent(typeof(PanelRenderer))]
    public class GameOverScreenDriver : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;

        [Space]
        [SerializeField] Transform m_GameOverRoot;
        [SerializeField] GameOverMenu m_MenuPrefab;
        [Tooltip("Where the menu lands, from the game over root: on the table, in front of the toaster.")]
        [SerializeField] Vector3 m_MenuRest = new(0f, 0f, 21.8f);

        PlayManagerState m_State;
        VisualElement m_Root;
        GameOverScreenManager m_GameOverScreenManager;
        GameOverMenu m_Menu;

        int m_UIVersion;

        public GameOverMenu menu => m_Menu;

        void Awake()
        {
            if (m_MenuPrefab == null)
                return;
            m_Menu = Instantiate(m_MenuPrefab, m_GameOverRoot);
            m_Menu.name = m_MenuPrefab.name;
            m_Menu.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (m_Menu != null)
                Destroy(m_Menu.gameObject);
        }

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

        // After the menu has moved this frame (falling, or swept off the table): the screen stays on its pages.
        void LateUpdate()
        {
            if (m_Menu != null && m_Menu.gameObject.activeInHierarchy)
                transform.SetPositionAndRotation(m_Menu.pages.position, m_Menu.pages.rotation);
        }

        void State_OnStateChange(object sender, PlayManagerState.State state)
        {
            if (state == PlayManagerState.State.GameOver && m_Menu != null)
            {
                m_Menu.Drop(m_GameOverRoot.TransformPoint(m_MenuRest), m_GameOverRoot.rotation);
                LateUpdate();
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
