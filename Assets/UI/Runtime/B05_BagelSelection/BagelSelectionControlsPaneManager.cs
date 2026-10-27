using UnityEngine.UIElements;

namespace Bagel
{
    // The cash register's face by the lazy Susan: its display shows the chosen bagel, < and > turn the lazy Susan
    // to the bagel before or after (round and round), Back goes to the menu, and Order takes the bagel to the payment
    // tablet, where the level's chosen (LevelSelectionScreenManager). Buttons and
    // gamepad/keyboard navigation both go straight to the state's bagel selection; a key pressed with the gamepad or
    // the keyboard goes down too. The face keeps the focus while the bagels are chosen: its keys don't take it when
    // clicked, and a click anywhere else gives it back, so the keyboard and gamepad always work.
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
        VisualElement m_PanelRoot;

        protected override void OnAttached()
        {
            ListenToThePanel(panel?.visualTree);

            if (m_Bound)
                return;
            m_Bound = true;

            selectButton.clicked += Select;
            backButton.clicked += Back;
            leftButton.clicked += Previous;
            rightButton.clicked += Next;

            RegisterCallback<FocusOutEvent>(OnFocusOut);
            RegisterCallback<NavigationMoveEvent>(OnNavigationMoveEvent);
            RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmitEvent);
            RegisterCallback<NavigationCancelEvent>(OnNavigationCancelEvent);
        }

        protected override void OnDetached() => ListenToThePanel(null);

        // A click on nothing leaves the world's panels with no panel in focus, and the next key goes to the panel
        // itself, not to the focused face: it's for the face too, while the bagels are chosen.
        void ListenToThePanel(VisualElement root)
        {
            if (m_PanelRoot != null)
            {
                m_PanelRoot.UnregisterCallback<NavigationMoveEvent>(OnNavigationMoveEvent);
                m_PanelRoot.UnregisterCallback<NavigationSubmitEvent>(OnNavigationSubmitEvent);
                m_PanelRoot.UnregisterCallback<NavigationCancelEvent>(OnNavigationCancelEvent);
            }
            m_PanelRoot = root;
            if (m_PanelRoot != null)
            {
                m_PanelRoot.RegisterCallback<NavigationMoveEvent>(OnNavigationMoveEvent);
                m_PanelRoot.RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmitEvent);
                m_PanelRoot.RegisterCallback<NavigationCancelEvent>(OnNavigationCancelEvent);
            }
        }

        // The face itself, or the panel when nothing in it is in focus.
        bool IsForTheFace(EventBase evt) => evt.target == this || evt.target == m_PanelRoot;

        protected override void OnStateChanged(PlayManagerState oldState, PlayManagerState newState) => UpdateRegister();

        protected override void OnStatePropertyChanged(in BindingId property)
        {
            if (property == PlayManagerState.selectedBagelIndexPath)
                UpdateRegister();
        }

        protected override void OnLocaleChanged() => UpdateRegister();

        public void Select() => state?.GoToLevelSelection();

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

        void OnFocusOut(FocusOutEvent evt)
        {
            if (!isSelecting)
                return;
            schedule.Execute(() =>
            {
                if (isSelecting && panel != null && focusController.focusedElement != this)
                    Focus();
            });
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
            if (!IsForTheFace(evt) || !isSelecting)
                return;

            // Tab and Shift+Tab turn it too, the bagel after and before: the face is the register's one stop.
            if (evt.direction is NavigationMoveEvent.Direction.Left or NavigationMoveEvent.Direction.Previous)
            {
                Press(leftButton);
                Previous();
            }
            else if (evt.direction is NavigationMoveEvent.Direction.Right or NavigationMoveEvent.Direction.Next)
            {
                Press(rightButton);
                Next();
            }

            Focus();
            evt.StopPropagation();
        }

        void OnNavigationSubmitEvent(NavigationSubmitEvent evt)
        {
            if (!IsForTheFace(evt) || !isSelecting)
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
