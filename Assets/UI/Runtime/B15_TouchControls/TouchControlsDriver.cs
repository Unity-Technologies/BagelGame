using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Bagel
{
    // Shows the touch controls while the bagel rolls, on a touch screen: a phone, or a computer with a touchscreen
    // (the Editor's Device Simulator has one). Paused, or with the game over, they're gone, and the stick lets go.
    [ExecuteInEditMode]
    [RequireComponent(typeof(PanelRenderer))]
    public class TouchControlsDriver : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;

        PlayManagerState m_State;
        VisualElement m_Root;
        TouchControlsManager m_TouchControlsManager;
        Rect m_SafeArea;

        int m_UIVersion;

        public static bool hasTouchScreen => Application.isMobilePlatform || Touchscreen.current != null;

        void OnEnable()
        {
            // In Edit mode the controls only hide themselves, so they don't cover the Game view.
            if (Application.isPlaying)
            {
                m_State = m_PlayManager.state;
                m_State.onStateChange += State_OnStateChange;
                m_State.onPauseStateChanged += State_OnPauseStateChanged;
                InputSystem.onDeviceChange += InputSystem_OnDeviceChange;
            }

            // Registering calls OnUIReload right away when the UI is already loaded.
            GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);
        }

        void OnDisable()
        {
            GetComponent<PanelRenderer>().UnregisterUIReloadCallback(OnUIReload);

            // Play mode can start without a domain reload, and the panel counts versions again from
            // the start: forget the last one so the next UI is always set up.
            m_UIVersion = 0;

            if (m_State == null)
                return;

            m_TouchControlsManager?.moveStick?.Release();
            m_State.onStateChange -= State_OnStateChange;
            m_State.onPauseStateChanged -= State_OnPauseStateChanged;
            InputSystem.onDeviceChange -= InputSystem_OnDeviceChange;
            m_State = null;
        }

        void OnUIReload(PanelRenderer panelRenderer, VisualElement rootElement, int version)
        {
            if (m_UIVersion == version)
                return;

            var touchControlsManager = rootElement.Q<TouchControlsManager>();
            if (touchControlsManager == null)
                return; // The UXML is still importing; the next reload brings it.
            m_UIVersion = version;

            m_Root = rootElement;
            m_TouchControlsManager = touchControlsManager;

            if (m_State != null)
            {
                m_TouchControlsManager.SetState(m_State);
                m_TouchControlsManager.playInputBindings = m_PlayManager.playInputBindings;
            }

            m_SafeArea = default;
            UpdateShown();
        }

        void State_OnStateChange(object sender, PlayManagerState.State state) => UpdateShown();

        void State_OnPauseStateChanged(object sender, bool paused) => UpdateShown();

        void InputSystem_OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device is Touchscreen)
                UpdateShown();
        }

        void UpdateShown()
        {
            if (m_Root == null)
                return;

            var shown = m_State != null && m_State.isPlaying && !m_State.isPaused && hasTouchScreen;
            m_Root.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            if (!shown)
                m_TouchControlsManager.moveStick?.Release();
        }

        void Update()
        {
            if (m_TouchControlsManager?.panel == null || Screen.safeArea == m_SafeArea)
                return;

            // The safe area is in screen pixels, from the bottom left; the panel's are from the top left.
            m_SafeArea = Screen.safeArea;
            var panel = m_TouchControlsManager.panel;
            var topLeft = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(m_SafeArea.xMin, Screen.height - m_SafeArea.yMax));
            var bottomRight = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(m_SafeArea.xMax, Screen.height - m_SafeArea.yMin));
            var screen = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(Screen.width, Screen.height));
            m_TouchControlsManager.SetSafeArea(topLeft.x, topLeft.y, screen.x - bottomRight.x, screen.y - bottomRight.y);
        }
    }
}
