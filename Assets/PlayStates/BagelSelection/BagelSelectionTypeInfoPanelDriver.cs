using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(UIDocument))]
    public class BagelSelectionTypeInfoPanelDriver : MonoBehaviour
    {
        UIDocument m_UIDocument;
        BagelType m_BagelType;

        void OnEnable()
        {
            m_UIDocument = GetComponent<UIDocument>();
            BindUI(m_BagelType);
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
