using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    public class BagelTrackerDriver : MonoBehaviour
    {
        [SerializeField] BagelTracker m_BagelTracker;

        UIDocument m_UIDocument;

        void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            var root = m_UIDocument.rootVisualElement.Q("pane");
            if (root == null)
                return;

            root.dataSource = m_BagelTracker.BagelTrackerData;
        }
    }
}
