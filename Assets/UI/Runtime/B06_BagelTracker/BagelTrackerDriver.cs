using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [RequireComponent(typeof(PanelRenderer))]
    public class BagelTrackerDriver : MonoBehaviour
    {
        [SerializeField] BagelTracker m_BagelTracker;

        void Awake()
        {
            GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);
        }

        private void OnDestroy()
        {
            GetComponent<PanelRenderer>().UnregisterUIReloadCallback(OnUIReload);
        }

        void OnUIReload(PanelRenderer panelRenderer, VisualElement rootElement)
        {
            var root = rootElement.Q("pane");
            if (root == null)
                return;
            root.dataSource = m_BagelTracker.bagelTrackerData;
        }
    }
}
