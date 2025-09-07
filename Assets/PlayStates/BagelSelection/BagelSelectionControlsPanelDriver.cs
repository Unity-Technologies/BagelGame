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

        Button m_SelectButton;
        Button m_BackButton;
        Button m_LeftButton;
        Button m_RightButton;

        void OnEnable()
        {
            m_PlayManager.State.OnStateChange += State_OnStateChange;
            m_BagelSelectionRoom.OnBagelTypeChange += BagelSelectionRoom_OnBagelTypeChange;

            m_UIDocument = GetComponent<UIDocument>();
            var root = m_UIDocument.rootVisualElement;

            m_SelectButton = root.Q<Button>("select-button");
            if (m_SelectButton != null)
                m_SelectButton.clicked += m_BagelSelectionRoom.SelectBagelAndGoToPlay;

            m_LeftButton = root.Q<Button>("left-button");
            if (m_LeftButton != null)
                m_LeftButton.clicked += m_BagelSelectionRoom.PreviousBagel;
            m_RightButton = root.Q<Button>("right-button");
            if (m_RightButton != null)
                m_RightButton.clicked += m_BagelSelectionRoom.NextBagel;

            m_BackButton = root.Q<Button>("back-button");
            if (m_BackButton != null)
                m_BackButton.clicked += m_BagelSelectionRoom.PlayManager.State.GoToMainMenu;

            SetValuesFromIndex(m_BagelSelectionRoom.SelectedBagelIndex);
        }

        void State_OnStateChange(object sender, PlayManagerState.State state)
        {
            if (state == PlayManagerState.State.BagelSelection)
            {
                m_SelectButton.Focus();

                m_PlayManager.PlayInputBindings.OnConfirmAction += PlayInputBindings_OnConfirmAction;
                m_PlayManager.PlayInputBindings.OnBackAction += PlayInputBindings_OnBackAction;
                m_PlayManager.PlayInputBindings.OnLeftAction += PlayInputBindings_OnLeftAction;
                m_PlayManager.PlayInputBindings.OnRightAction += PlayInputBindings_OnRightAction;
            }
            else
            {
                m_PlayManager.PlayInputBindings.OnConfirmAction -= PlayInputBindings_OnConfirmAction;
                m_PlayManager.PlayInputBindings.OnBackAction -= PlayInputBindings_OnBackAction;
                m_PlayManager.PlayInputBindings.OnLeftAction -= PlayInputBindings_OnLeftAction;
                m_PlayManager.PlayInputBindings.OnRightAction -= PlayInputBindings_OnRightAction;
            }
        }

        void PlayInputBindings_OnConfirmAction(object sender, EventArgs e)
        {
            m_BagelSelectionRoom.SelectBagelAndGoToPlay();
            m_SelectButton.Focus();
        }

        void PlayInputBindings_OnBackAction(object sender, EventArgs e)
        {
            m_BagelSelectionRoom.PlayManager.State.GoToMainMenu();
            m_BackButton.Focus();
        }

        void PlayInputBindings_OnLeftAction(object sender, EventArgs e)
        {
            m_BagelSelectionRoom.PreviousBagel();
            m_LeftButton.Focus();
        }

        void PlayInputBindings_OnRightAction(object sender, EventArgs e)
        {
            m_BagelSelectionRoom.NextBagel();
            m_RightButton.Focus();
        }

        void OnDisable()
        {
            m_PlayManager.State.OnStateChange -= State_OnStateChange;
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
            SetValuesFromIndex(index);
        }

        void SetValuesFromIndex(int index)
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
