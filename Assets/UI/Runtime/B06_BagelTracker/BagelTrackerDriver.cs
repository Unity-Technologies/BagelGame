using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [RequireComponent(typeof(PanelRenderer))]
    public class BagelTrackerDriver : MonoBehaviour
    {
        [SerializeField] BagelTracker m_BagelTracker;

        int m_UIVersion;

        void OnEnable()
        {
            GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);
        }

        void OnDisable()
        {
            GetComponent<PanelRenderer>().UnregisterUIReloadCallback(OnUIReload);
        }

        void OnUIReload(PanelRenderer panelRenderer, VisualElement rootElement, int version)
        {
            if (m_UIVersion == version)
                return;

            var pane = rootElement.Q<BagelTrackerPaneManager>();
            if (pane == null)
                return; // The UXML is still importing; the next reload brings it.
            m_UIVersion = version;

            pane.SetState(m_BagelTracker.state);
        }
    }
}
