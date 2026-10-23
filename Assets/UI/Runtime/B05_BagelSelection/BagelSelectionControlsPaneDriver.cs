using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [RequireComponent(typeof(PanelRenderer))]
    public class BagelSelectionControlsPaneDriver : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;
        [SerializeField] BagelSelectionRoom m_BagelSelectionRoom;
        [SerializeField] Transform m_Camera;
        [SerializeField] Transform m_BagelSelectionCameraTraget;

        PlayManagerState m_State;
        BagelSelectionControlsPaneManager m_Pane;

        int m_UIVersion;

        void OnEnable()
        {
            m_State = m_PlayManager.state;
            m_State.onStateChange += State_OnStateChange;
            m_State.onSetBagelType += State_OnSetBagelType;

            GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);

            SetXOffset(m_State.selectedBagelIndex);
        }

        void OnDisable()
        {
            m_State.onStateChange -= State_OnStateChange;
            m_State.onSetBagelType -= State_OnSetBagelType;

            GetComponent<PanelRenderer>().UnregisterUIReloadCallback(OnUIReload);
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
                m_Pane.schedule.Execute(() => m_Pane.Focus());
        }

        void State_OnSetBagelType(object sender, BagelType bagelType)
        {
            SetXOffset(m_State.selectedBagelIndex);
        }

        void Update()
        {
            if (!m_State.isBagelSelection)
                return;

            var p = transform.position;
            p.z = m_Camera.position.z;
            transform.position = p;
        }

        void SetXOffset(int index)
        {
            var offset = m_BagelSelectionRoom.GetOffsetFromIndex(index);

            var p = m_BagelSelectionCameraTraget.localPosition;
            p.x = offset;
            m_BagelSelectionCameraTraget.localPosition = p;

            if (!m_State.isBagelSelection)
            {
                p = transform.localPosition;
                p.x = offset;
                transform.localPosition = p;
            }
        }
    }
}
