using UnityEngine.UIElements;

namespace Bagel
{
    // Root of the tracker displays that ride along with the bagel. Everything inside binds to tracker.*.
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class BagelTrackerPaneManager : BagelPane
    {
        ProgressBar m_ToppingsCountBar;
        ProgressBar m_SpinBar;
        ProgressBar m_PainBar;

        public ProgressBar toppingsCountBar => m_ToppingsCountBar ??= this.Q<ProgressBar>("toppings-count-bar");
        public ProgressBar spinBar => m_SpinBar ??= this.Q<ProgressBar>("spin-bar");
        public ProgressBar painBar => m_PainBar ??= this.Q<ProgressBar>("pain-bar");
    }
}
