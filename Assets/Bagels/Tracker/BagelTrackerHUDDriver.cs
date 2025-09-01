using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    public class BagelTrackerDriver : MonoBehaviour
    {
        public BagelTracker tracker;

        UIDocument m_UIDocument;

        private void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            var root = m_UIDocument.rootVisualElement.Q("root");
            if (root == null)
                return;

            root.dataSource = tracker.bagelController.BagelTrackerData;

            //var progressBar = root.Q<ProgressBar>();
            //progressBar.value = progressBar.highValue;
            //progressBar.SetBinding("value", new DataBinding
            //{
            //    dataSourcePath = new PropertyPath(nameof(tracker.bagelController.transform.position.x))
            //});
        }
    }
}
