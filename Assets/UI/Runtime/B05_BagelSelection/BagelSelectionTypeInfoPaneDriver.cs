using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(UIDocument))]
    public class BagelSelectionTypeInfoPaneDriver : MonoBehaviour
    {
        [SerializeField] BagelSelectionPodium m_BagelSelectionPodium;

        [SerializeField] BagelType m_BagelType;
        UIDocument m_UIDocument;
        VisualElement m_Pane;

        void OnEnable()
        {
            m_UIDocument = GetComponent<UIDocument>();
            m_Pane = m_UIDocument.rootVisualElement.Q("pane");
            m_Pane.dataSource = null;

            m_BagelSelectionPodium.OnBagelTypeChange += BagelSelectionPodium_OnBagelTypeChange;
        }

        void BagelSelectionPodium_OnBagelTypeChange(object sender, BagelType bagelType)
        {
            m_BagelType = bagelType;
        }

        void Update()
        {
            if (m_BagelType == null)
                return;

            if (m_Pane == null)
                return;

            if (m_Pane.dataSource != null)
                return;

            m_Pane.dataSource = m_BagelType;
        }
    }
}
