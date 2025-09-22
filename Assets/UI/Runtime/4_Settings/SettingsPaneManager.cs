using System;
using UnityEngine.UIElements;

namespace Bagel
{
    [UxmlElement]
    public partial class SettingsPaneManager : VisualElement
    {
        public struct Elements
        {
            public SettingsPaneManager manager;
            public DropdownField themeDropdown;
        }

        public static Elements BindUI(VisualElement root)
        {
            var elements = new Elements
            {
                manager = root.Q<SettingsPaneManager>(),
                themeDropdown = root.Q<DropdownField>("theme-dropdown")
            };

            return elements;
        }
    }
}
