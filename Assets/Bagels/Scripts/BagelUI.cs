using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    public class BagelUI : MonoBehaviour
    {
        public BagelTracker tracker;
        private UIDocument uiDocument;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            var root = uiDocument.rootVisualElement;
            var progressBar = root.Q<ProgressBar>();
            progressBar.value = progressBar.highValue;
            progressBar.SetBinding("value", new DataBinding
            {
                dataSourcePath = new PropertyPath(nameof(tracker.bagelController.transform.position.x))
            });
        }
    }
}
