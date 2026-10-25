using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // A bagel's card on the lazy Susan: shows the stats of its station's bagel (BagelCarouselStation), and whether it's
    // the chosen one.
    [ExecuteAlways]
    [RequireComponent(typeof(PanelRenderer))]
    public class BagelSelectionTypeInfoPaneDriver : MonoBehaviour
    {
        public static readonly string chosenUssClassName = "b-tag--chosen";

        BagelCarouselStation m_Station;
        VisualElement m_Pane;
        BagelType m_ShownType;

        int m_UIVersion;

        void OnEnable()
        {
            m_Station = GetComponentInParent<BagelCarouselStation>();
            GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);
            if (m_Station != null)
            {
                m_Station.onBagelTypeChange += Station_OnBagelTypeChange;
                m_Station.onSelectedChange += Station_OnSelectedChange;
            }
        }

        void OnDisable()
        {
            GetComponent<PanelRenderer>().UnregisterUIReloadCallback(OnUIReload);
            if (m_Station != null)
            {
                m_Station.onBagelTypeChange -= Station_OnBagelTypeChange;
                m_Station.onSelectedChange -= Station_OnSelectedChange;
            }

            // Play mode can start without a domain reload, and the panel counts versions again from
            // the start: forget the last one so the next UI is always set up.
            m_UIVersion = 0;
        }

        void OnUIReload(PanelRenderer panelRenderer, VisualElement rootElement, int version)
        {
            if (m_UIVersion == version)
                return;
            var pane = rootElement.Q<VisualElement>("pane");
            if (pane == null)
                return; // The UXML is still importing; the next reload brings it.
            m_UIVersion = version;

            m_Pane = pane;
            m_ShownType = null;
            // Nothing on the card is interactive: never pick it, which saves raycasts against world-space panels.
            rootElement.Query().ForEach(element => element.pickingMode = PickingMode.Ignore);
            Show();
        }

        void Station_OnBagelTypeChange(object sender, BagelType bagelType) => Show();

        void Station_OnSelectedChange(bool selected) => m_Pane?.EnableInClassList(chosenUssClassName, selected);

        void Show()
        {
            if (m_Pane == null || m_Station == null)
                return;
            m_Pane.EnableInClassList(chosenUssClassName, m_Station.selected);

            var bagelType = m_Station.bagelType;
            if (bagelType == null || bagelType == m_ShownType)
                return;
            m_ShownType = bagelType;
            m_Pane.dataSource = bagelType;

            // The name comes from the string table, so it follows the language.
            if (!string.IsNullOrEmpty(bagelType.nameKey))
                BagelLocalization.Bind(m_Pane.Q<Label>("name"), "text", bagelType.nameKey);
        }
    }
}
