using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(PanelRenderer))]
    public class BagelSelectionTypeInfoPaneDriver : MonoBehaviour
    {
        [SerializeField] BagelSelectionPodium m_BagelSelectionPodium;

        [SerializeField] BagelType m_BagelType;
        VisualElement m_Pane;

        int m_UIVersion;

        void OnEnable()
        {
            GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);
            m_BagelSelectionPodium.onBagelTypeChange += BagelSelectionPodium_OnBagelTypeChange;
        }

        void OnDisable()
        {
            GetComponent<PanelRenderer>().UnregisterUIReloadCallback(OnUIReload);
            m_BagelSelectionPodium.onBagelTypeChange -= BagelSelectionPodium_OnBagelTypeChange;
        }

        void OnUIReload(PanelRenderer panelRenderer, VisualElement rootElement, int version)
        {
            if (m_UIVersion == version)
                return;
            m_UIVersion = version;

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
