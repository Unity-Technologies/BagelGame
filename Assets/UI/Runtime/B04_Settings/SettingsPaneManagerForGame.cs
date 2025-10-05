using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [UxmlElement]
    public partial class SettingsPaneManagerForGame : VisualElement
    {
        Slider m_FieldOfViewSlider;

        public Slider fieldOfViewSlider => m_FieldOfViewSlider ??= this.Q<Slider>("fov-slider");

        public void BindSettingsCallbacks(SettingsRefsForGame settingsRefs)
        {
            if (settingsRefs == null)
                return;

            fieldOfViewSlider.RegisterValueChangedCallback(evt =>
            {
                settingsRefs.playCamera.fieldOfView = evt.newValue;
            });
        }
    }
}
