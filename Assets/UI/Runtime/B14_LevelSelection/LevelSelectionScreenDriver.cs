using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // The payment tablet's screen (LevelSelectionScreenManager), by the cash register. It takes the focus while levels
    // are chosen, says which level is chosen, and gives the level editor its live pictures of the library's things
    // while it's open (BagelLevelStudio, made then, and gone with it).
    [RequireComponent(typeof(PanelRenderer))]
    public class LevelSelectionScreenDriver : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;

        PlayManagerState m_State;
        LevelSelectionScreenManager m_Screen;
        BagelLevelStudio m_Studio;

        int m_UIVersion;

        public LevelSelectionScreenManager screen => m_Screen;
        public BagelLevelStudio studio => m_Studio;

        void OnEnable()
        {
            m_State = m_PlayManager.state;
            m_State.onStateChange += State_OnStateChange;
            m_State.onSetLevel += State_OnSetLevel;
            m_State.propertyChanged += State_OnPropertyChanged;

            GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);
        }

        void OnDisable()
        {
            m_State.onStateChange -= State_OnStateChange;
            m_State.onSetLevel -= State_OnSetLevel;
            m_State.propertyChanged -= State_OnPropertyChanged;

            GetComponent<PanelRenderer>().UnregisterUIReloadCallback(OnUIReload);
            CloseStudio();

            // Play mode can start without a domain reload, and the panel counts versions again from
            // the start: forget the last one so the next UI is always set up.
            m_UIVersion = 0;
        }

        void OnUIReload(PanelRenderer panelRenderer, VisualElement rootElement, int version)
        {
            if (m_UIVersion == version)
                return;

            var screen = rootElement.Q<LevelSelectionScreenManager>();
            if (screen == null)
                return; // The UXML is still importing; the next reload brings it.
            m_UIVersion = version;

            m_Screen = screen;
            m_Screen.SetState(m_State);

            // On a fresh scene load the UI arrives after the first state change, so catch up here.
            State_OnStateChange(this, m_State.currentState);
        }

        void State_OnStateChange(object sender, PlayManagerState.State state)
        {
            UpdateStudio();
            if (m_Screen == null)
                return;

            // World-space panels only get gamepad and keyboard navigation once something in them has focus.
            if (state == PlayManagerState.State.LevelSelection)
            {
                m_Screen.schedule.Execute(m_Screen.FocusIn);
                BagelAccessibility.ShowScreen(m_Screen);
            }
        }

        void State_OnSetLevel(object sender, BagelLevel level)
        {
            if (m_State.isLevelSelection && level != null)
                BagelAccessibility.Announce(LevelSelectionScreenManager.LevelName(level));
        }

        void State_OnPropertyChanged(object sender, BindablePropertyChangedEventArgs args)
        {
            if (args.propertyName == PlayManagerState.isEditingLevelPath)
                UpdateStudio();
        }

        void Update() => m_Studio?.Update(Time.unscaledDeltaTime);

        // The pictures turn only while the editor's open.
        void UpdateStudio()
        {
            if (m_State.isEditingLevel)
            {
                m_Studio ??= BagelLevelStudio.Create(m_State.levelLibrary, BagelLevelStudio.runtimeLayer);
                if (m_Screen?.editor != null)
                    m_Screen.editor.thumbnails = m_Studio;
            }
            else
            {
                CloseStudio();
            }
        }

        void CloseStudio()
        {
            if (m_Screen?.editor != null)
                m_Screen.editor.thumbnails = null;
            m_Studio?.Dispose();
            m_Studio = null;
        }
    }
}
