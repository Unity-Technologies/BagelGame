using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [UxmlElement]
    public partial class SettingsPaneManager : VisualElement
    {
        SettingsPaneManagerForGame m_SettingsPaneManagerForGame;
        SettingsPaneManagerForUI m_SettingsPaneManagerForUI;
        Button m_BackButton;

        public SettingsPaneManagerForGame forGame => m_SettingsPaneManagerForGame ??= this.Q<SettingsPaneManagerForGame>();
        public SettingsPaneManagerForUI forUI => m_SettingsPaneManagerForUI ??= this.Q<SettingsPaneManagerForUI>();
        public Button backButton => m_BackButton ??= this.Q<Button>("back-button");

        public struct Callbacks
        {
            public Action onSettingsBack;
        }

        public void BindUI(Callbacks callbacks)
        {
            if (backButton != null && callbacks.onSettingsBack != null)
                backButton.clicked += callbacks.onSettingsBack;
        }

        public void BindSettingsCallbacks(GameObject playSettingsObject)
        {
            forGame.BindSettingsCallbacks(playSettingsObject.GetComponent<SettingsRefsForGame>());
            forUI.BindSettingsCallbacks(playSettingsObject.GetComponent<SettingsRefsForUI>());
        }
    }
}
