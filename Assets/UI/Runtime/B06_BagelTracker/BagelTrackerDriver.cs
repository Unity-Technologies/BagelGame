using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [RequireComponent(typeof(PanelRenderer))]
    public class BagelTrackerDriver : MonoBehaviour
    {
        [SerializeField] BagelTracker m_BagelTracker;

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
            var root = rootElement.Q("pane");
            if (root == null)
                return;
            root.dataSource = m_BagelTracker.bagelTrackerData;
        }
    }
}
