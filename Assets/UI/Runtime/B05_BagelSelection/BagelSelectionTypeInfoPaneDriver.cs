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

            // Play mode can start without a domain reload, and the panel counts versions again from
            // the start: forget the last one so the next UI is always set up.
            m_UIVersion = 0;
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

            // The name comes from the string table, so it follows the language.
            if (!string.IsNullOrEmpty(m_BagelType.nameKey))
                BagelLocalization.Bind(m_Pane.Q<Label>("name"), "text", m_BagelType.nameKey);
        }
    }
}
