using UnityEngine.UIElements;

namespace Bagel
{
    // The cash register's face by the lazy Susan: its display shows the chosen bagel, < and > turn the lazy Susan
    // to the bagel before or after (round and round), Back goes to the menu, and Order takes the bagel. Buttons and
    // gamepad/keyboard navigation both go straight to the state's bagel selection; a key pressed with the gamepad or
    // the keyboard goes down too.
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class BagelSelectionControlsPaneManager : BagelPane
    {
        public static readonly string keyPressedUssClassName = "b-register__key--pressed";

        const long k_KeyPressMs = 120;

        Button m_SelectButton;
        Button m_BackButton;
        Button m_LeftButton;
        Button m_RightButton;
        Label m_DisplayName;

        public Button selectButton => m_SelectButton ??= this.Q<Button>("select-button");
        public Button backButton => m_BackButton ??= this.Q<Button>("back-button");
        public Button leftButton => m_LeftButton ??= this.Q<Button>("left-button");
        public Button rightButton => m_RightButton ??= this.Q<Button>("right-button");
        public Label displayName => m_DisplayName ??= this.Q<Label>("display-name");

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

        protected override void OnStateChanged(PlayManagerState oldState, PlayManagerState newState) => UpdateRegister();

        protected override void OnStatePropertyChanged(in BindingId property)
        {
            if (property == PlayManagerState.selectedBagelIndexPath)
                UpdateRegister();
        }

        protected override void OnLocaleChanged() => UpdateRegister();

        public void Select() => state?.GoToPlay();

        public void Back() => state?.GoToMainMenu();

        public void Previous() => state?.SelectPreviousBagel();

        public void Next() => state?.SelectNextBagel();

        void UpdateRegister()
        {
            leftButton?.SetEnabled(state != null && state.canSelectPreviousBagel);
            rightButton?.SetEnabled(state != null && state.canSelectNextBagel);

            var bagelType = state?.selectedBagelType;
            if (displayName != null && bagelType != null)
                displayName.text = BagelLocalization.Get(bagelType.nameKey, bagelType.displayName);
        }

        // The key goes down and comes back up, as if pressed.
        void Press(Button key)
        {
            key.AddToClassList(keyPressedUssClassName);
            key.schedule.Execute(() => key.RemoveFromClassList(keyPressedUssClassName)).StartingIn(k_KeyPressMs);
        }

        void OnNavigationMoveEvent(NavigationMoveEvent evt)
        {
            // Only the pane itself turns left/right into a new bagel; focused buttons navigate as usual.
            if (evt.target != this || !isSelecting)
                return;

            if (evt.direction == NavigationMoveEvent.Direction.Left)
            {
                Press(leftButton);
                Previous();
            }
            else if (evt.direction == NavigationMoveEvent.Direction.Right)
            {
                Press(rightButton);
                Next();
            }

            Focus();
            evt.StopPropagation();
        }

        void OnNavigationSubmitEvent(NavigationSubmitEvent evt)
        {
            if (evt.target != this || !isSelecting)
                return;

            Press(selectButton);
            Select();
            evt.StopPropagation();
        }

        void OnNavigationCancelEvent(NavigationCancelEvent evt)
        {
            if (!isSelecting)
                return;

            Press(backButton);
            Back();
            evt.StopPropagation();
        }
    }
}
