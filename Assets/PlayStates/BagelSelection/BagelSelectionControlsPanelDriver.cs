using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [RequireComponent(typeof(UIDocument))]
    public class BagelSelectionControlsPanelDriver : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;
        [SerializeField] BagelSelectionRoom m_BagelSelectionRoom;
        [SerializeField] Transform m_Camera;
        [SerializeField] Transform m_BagelSelectionCameraTraget;

        UIDocument m_UIDocument;

        Button m_LeftButton;
        Button m_RightButton;

        void OnEnable()
        {
            m_BagelSelectionRoom.OnBagelTypeChange += BagelSelectionRoom_OnBagelTypeChange;

            m_UIDocument = GetComponent<UIDocument>();
            var root = m_UIDocument.rootVisualElement;

            Button button;

            button = root.Q<Button>("select-button");
            if (button != null)
                button.clicked += m_BagelSelectionRoom.SelectBagelAndGoToPlay;

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

        void OnDisable()
        {
            m_BagelSelectionRoom.OnBagelTypeChange -= BagelSelectionRoom_OnBagelTypeChange;
        }

        void Update()
        {
            if (!m_PlayManager.State.IsBagelSelection)
                return;

            var p = transform.position;
            p.z = m_Camera.position.z;
            transform.position = p;
        }

        void BagelSelectionRoom_OnBagelTypeChange(object sender, int index)
        {
            SetXOffset(index);
            SetDisableStates(index);
        }

        void SetXOffset(int index)
        {
            var offset = m_BagelSelectionRoom.GetOffsetFromIndex(index);

            var p = m_BagelSelectionCameraTraget.localPosition;
            p.x = offset;
            m_BagelSelectionCameraTraget.localPosition = p;

            if (!m_PlayManager.State.IsBagelSelection)
            {
                p = transform.localPosition;
                p.x = offset;
                transform.localPosition = p;
            }
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
