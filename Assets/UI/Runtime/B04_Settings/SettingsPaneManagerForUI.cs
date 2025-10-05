using System;
using UnityEngine.UIElements;

namespace Bagel
{
    [UxmlElement]
    public partial class SettingsPaneManagerForUI : VisualElement
    {
        DropdownField m_ThemeDropdown;

        public DropdownField themeDropdown => m_ThemeDropdown ??= this.Q<DropdownField>("theme-dropdown");

        public void BindSettingsCallbacks(SettingsRefsForUI settingsRefs)
        {
            if (settingsRefs == null)
                return;
        }
    }
}
