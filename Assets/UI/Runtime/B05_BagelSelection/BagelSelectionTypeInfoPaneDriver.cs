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
        VisualElement m_Pane;

        void OnEnable()
        {
            OnUIReload(GetComponent<UIDocument>().rootVisualElement);
            m_BagelSelectionPodium.onBagelTypeChange += BagelSelectionPodium_OnBagelTypeChange;
        }

        void OnDisable()
        {
            m_BagelSelectionPodium.onBagelTypeChange -= BagelSelectionPodium_OnBagelTypeChange;
        }

        void OnUIReload(VisualElement rootElement)
        {
            m_Pane = rootElement.Q<VisualElement>("pane");
            m_Pane.dataSource = null;
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
