using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [RequireComponent(typeof(UIDocument))]
    public class BagelSelectionControlsPanelDriver : MonoBehaviour
    {
        [SerializeField] BagelSelectionRoom m_BagelSelectionRoom;

        UIDocument m_UIDocument;

        Button m_LeftButton;
        Button m_RightButton;

        void Start()
        {
            m_BagelSelectionRoom.OnBagelTypeChange += BagelSelectionRoom_OnBagelTypeChange;
        }

        void OnEnable()
        {
            m_UIDocument = GetComponent<UIDocument>();
            var root = m_UIDocument.rootVisualElement;

            Button button;

            button = root.Q<Button>("select-button");
            if (button != null)
                button.clicked += m_BagelSelectionRoom.PlayManager.State.GoToPlay;

            m_LeftButton = root.Q<Button>("left-button");
            if (m_LeftButton != null)
                m_LeftButton.clicked += m_BagelSelectionRoom.PreviousBagel;
            m_RightButton = root.Q<Button>("right-button");
            if (m_RightButton != null)
                m_RightButton.clicked += m_BagelSelectionRoom.NextBagel;

            button = root.Q<Button>("back-button");
            if (button != null)
                button.clicked += m_BagelSelectionRoom.PlayManager.State.GoToMainMenu;

            SetDisableStates(m_BagelSelectionRoom.SelectedBagelIndex);
        }

        void BagelSelectionRoom_OnBagelTypeChange(object sender, int index)
        {
            SetDisableStates(index);
        }

        void SetDisableStates(int index)
        {
            if (m_LeftButton != null)
                m_LeftButton.SetEnabled(index > 0);
            if (m_RightButton != null)
                m_RightButton.SetEnabled(index < m_BagelSelectionRoom.BagelTypeCount - 1);
        }
    }
}
