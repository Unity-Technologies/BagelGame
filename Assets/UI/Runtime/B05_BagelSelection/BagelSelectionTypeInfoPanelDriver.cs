using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(UIDocument))]
    public class BagelSelectionTypeInfoPanelDriver : MonoBehaviour
    {
        [SerializeField] BagelSelectionPodium m_BagelSelectionPodium;

        UIDocument m_UIDocument;
        BagelType m_BagelType;

        void OnEnable()
        {
            m_UIDocument = GetComponent<UIDocument>();
            m_BagelSelectionPodium.OnBagelTypeChange += BagelSelectionPodium_OnBagelTypeChange;
            BindUI(m_BagelType);
        }

        void BagelSelectionPodium_OnBagelTypeChange(object sender, BagelType bagelType)
        {
            BindUI(bagelType);
        }

        public void BindUI(BagelType bagelType)
        {
            m_BagelType = bagelType;

            if (m_UIDocument == null)
                return;

            var root = m_UIDocument.rootVisualElement;
            var panel = root.Q("panel");
            if (panel == null)
                return;

            panel.dataSource = bagelType;
        }
    }
}
