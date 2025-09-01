using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    public class BagelTrackerDriver : MonoBehaviour
    {
        public BagelTracker tracker;

        UIDocument m_UIDocument;

        void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            var root = m_UIDocument.rootVisualElement.Q("root");
            if (root == null)
                return;

            root.dataSource = tracker.BagelTrackerData;
        }
    }
}
