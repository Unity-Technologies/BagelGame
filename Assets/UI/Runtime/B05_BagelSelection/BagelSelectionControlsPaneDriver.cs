using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // The cash register's face (BagelSelectionControlsPaneManager), on the register by the lazy Susan.
    [RequireComponent(typeof(PanelRenderer))]
    public class BagelSelectionControlsPaneDriver : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;

        PlayManagerState m_State;
        BagelSelectionControlsPaneManager m_Pane;

        int m_UIVersion;

        void OnEnable()
        {
            m_State = m_PlayManager.state;
            m_State.onStateChange += State_OnStateChange;
            m_State.onSetBagelType += State_OnSetBagelType;

            GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);
        }

        void OnDisable()
        {
            m_State.onStateChange -= State_OnStateChange;
            m_State.onSetBagelType -= State_OnSetBagelType;

            GetComponent<PanelRenderer>().UnregisterUIReloadCallback(OnUIReload);

            // Play mode can start without a domain reload, and the panel counts versions again from
            // the start: forget the last one so the next UI is always set up.
            m_UIVersion = 0;
        }

        void OnUIReload(PanelRenderer panelRenderer, VisualElement rootElement, int version)
        {
            if (m_UIVersion == version)
                return;

            var pane = rootElement.Q<BagelSelectionControlsPaneManager>();
            if (pane == null)
                return; // The UXML is still importing; the next reload brings it.
            m_UIVersion = version;

            m_Pane = pane;
            m_Pane.SetState(m_State);

            // On a fresh scene load the UI arrives after the first state change, so catch up here.
            State_OnStateChange(this, m_State.currentState);
        }

        void State_OnStateChange(object sender, PlayManagerState.State state)
        {
            if (m_Pane == null)
                return;

            // World-space panels only get gamepad and keyboard navigation once something in them has focus.
            if (state == PlayManagerState.State.BagelSelection)
            {
                m_Pane.schedule.Execute(() => m_Pane.Focus());
                BagelAccessibility.ShowScreen(m_Pane);
            }
            else if (state == PlayManagerState.State.Playing)
            {
                BagelAccessibility.ShowScreen(null);
            }
            // On to the payment tablet, which takes the screen reader (LevelSelectionScreenDriver).
        }

        void State_OnSetBagelType(object sender, BagelType bagelType)
        {
            if (m_State.isBagelSelection && bagelType != null)
                BagelAccessibility.Announce(BagelLocalization.Get(bagelType.nameKey, bagelType.displayName));
        }
    }
}
