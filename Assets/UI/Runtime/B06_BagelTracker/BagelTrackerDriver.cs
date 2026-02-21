using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [RequireComponent(typeof(UIDocument))]
    public class BagelTrackerDriver : MonoBehaviour
    {
        [SerializeField] BagelTracker m_BagelTracker;

        void OnEnable()
        {
            OnUIReload(GetComponent<UIDocument>().rootVisualElement);
        }

        void OnUIReload(VisualElement rootElement)
        {
            var root = rootElement.Q("pane");
            if (root == null)
                return;
            root.dataSource = m_BagelTracker.bagelTrackerData;
        }
    }
}
