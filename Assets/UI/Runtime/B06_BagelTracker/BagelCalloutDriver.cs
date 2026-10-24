using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // The callout's screen-space panel, riding along with the bagel's tracker: its PanelRenderer tracks the
    // tracker's position on screen (trackScreenSpacePosition), and this hands the callout the game's state.
    [RequireComponent(typeof(PanelRenderer))]
    public class BagelCalloutDriver : MonoBehaviour
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

            // Play mode can start without a domain reload, and the panel counts versions again from
            // the start: forget the last one so the next UI is always set up.
            m_UIVersion = 0;
        }

        void OnUIReload(PanelRenderer panelRenderer, VisualElement rootElement, int version)
        {
            if (m_UIVersion == version)
                return;

            var callout = rootElement.Q<BagelCalloutManager>();
            if (callout == null || m_BagelTracker == null)
                return; // The UXML is still importing; the next reload brings it.
            m_UIVersion = version;

            callout.SetState(m_BagelTracker.state);
        }
    }
}
