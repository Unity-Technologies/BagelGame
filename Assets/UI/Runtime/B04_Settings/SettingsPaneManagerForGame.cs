using UnityEngine.UIElements;

namespace Bagel
{
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class SettingsPaneManagerForGame : BagelPane
    {
        Slider m_FieldOfViewSlider;

        // Two-way bound to settings.fieldOfView in the UXML; the camera follows the state.
        public Slider fieldOfViewSlider => m_FieldOfViewSlider ??= this.Q<Slider>("fov-slider");
    }
}
