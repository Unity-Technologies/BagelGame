using UnityEngine.UIElements;

namespace Bagel
{
    // The < Select Back > bar under the podiums. Buttons and gamepad/keyboard navigation
    // both go straight to the state's bagel selection.
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class BagelSelectionControlsPaneManager : BagelPane
    {
        Button m_SelectButton;
        Button m_BackButton;
        Button m_LeftButton;
        Button m_RightButton;

        public Button selectButton => m_SelectButton ??= this.Q<Button>("select-button");
        public Button backButton => m_BackButton ??= this.Q<Button>("back-button");
        public Button leftButton => m_LeftButton ??= this.Q<Button>("left-button");
        public Button rightButton => m_RightButton ??= this.Q<Button>("right-button");

        bool isSelecting => state != null && state.isBagelSelection;

        bool m_Bound;

        protected override void OnAttached()
        {
            if (m_Bound)
                return;
            m_Bound = true;

            selectButton.clicked += Select;
            backButton.clicked += Back;
            leftButton.clicked += Previous;
            rightButton.clicked += Next;

            RegisterCallback<NavigationMoveEvent>(OnNavigationMoveEvent);
            RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmitEvent);
            RegisterCallback<NavigationCancelEvent>(OnNavigationCancelEvent);
        }

        protected override void OnStateChanged(PlayManagerState oldState, PlayManagerState newState) => UpdateButtons();

        protected override void OnStatePropertyChanged(in BindingId property)
        {
            if (property == PlayManagerState.selectedBagelIndexPath)
                UpdateButtons();
        }

        public void Select() => state?.GoToPlay();

        public void Back() => state?.GoToMainMenu();

        public void Previous() => state?.SelectPreviousBagel();

        public void Next() => state?.SelectNextBagel();

        void UpdateButtons()
        {
            leftButton?.SetEnabled(state != null && state.canSelectPreviousBagel);
            rightButton?.SetEnabled(state != null && state.canSelectNextBagel);
        }

        void OnNavigationMoveEvent(NavigationMoveEvent evt)
        {
            // Only the pane itself turns left/right into a new bagel; focused buttons navigate as usual.
            if (evt.target != this || !isSelecting)
                return;

            if (evt.direction == NavigationMoveEvent.Direction.Left)
                Previous();
            else if (evt.direction == NavigationMoveEvent.Direction.Right)
                Next();

            Focus();
            evt.StopPropagation();
        }

        void OnNavigationSubmitEvent(NavigationSubmitEvent evt)
        {
            if (evt.target != this || !isSelecting)
                return;

            Select();
            evt.StopPropagation();
        }

        void OnNavigationCancelEvent(NavigationCancelEvent evt)
        {
            if (!isSelecting)
                return;

            Back();
            evt.StopPropagation();
        }
    }
}
