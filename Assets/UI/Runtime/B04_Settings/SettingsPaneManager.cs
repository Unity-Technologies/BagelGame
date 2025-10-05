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

        public SettingsPaneManagerForGame forGame => m_SettingsPaneManagerForGame ??= this.Q<SettingsPaneManagerForGame>();
        public SettingsPaneManagerForUI forUI => m_SettingsPaneManagerForUI ??= this.Q<SettingsPaneManagerForUI>();

        public void BindSettingsCallbacks(GameObject playSettingsObject)
        {
            forGame.BindSettingsCallbacks(playSettingsObject.GetComponent<SettingsRefsForGame>());
            forUI.BindSettingsCallbacks(playSettingsObject.GetComponent<SettingsRefsForUI>());
        }
    }
}
