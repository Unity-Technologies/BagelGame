using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // The controls on a touch screen while the bagel rolls, in the pause screen's glass: a thumb stick at the bottom
    // left that rolls and turns the bagel like the keys and the gamepad's stick do, and a pause button at the top
    // right. They're only there on a touch screen (TouchControlsDriver), and only take touches, never the focus.
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class TouchControlsManager : BagelPane
    {
        BagelTouchStick m_MoveStick;
        Button m_PauseButton;

        public BagelTouchStick moveStick => m_MoveStick ??= this.Q<BagelTouchStick>("move-stick");
        public Button pauseButton => m_PauseButton ??= this.Q<Button>("pause-button");

        // Where the stick goes: the game's input, handed over by the Driver.
        public PlayInputBindings playInputBindings { get; set; }

        public struct Callbacks
        {
            public Action onPause;
            public Action<Vector2> onMove;
        }

        public void BindUI(Callbacks callbacks)
        {
            if (pauseButton != null && callbacks.onPause != null)
                pauseButton.clicked += callbacks.onPause;
            if (moveStick != null && callbacks.onMove != null)
                moveStick.RegisterValueChangedCallback(evt => callbacks.onMove(evt.newValue));
        }

        bool m_Bound;

        protected override void OnAttached()
        {
            if (m_Bound)
                return;
            m_Bound = true;

            if (pauseButton != null)
                pauseButton.focusable = false;

            BindUI(new Callbacks
            {
                onPause = () => state?.Pause(),
                onMove = move =>
                {
                    if (playInputBindings != null)
                        playInputBindings.touchMove = move;
                }
            });
        }

        // Keeps the controls clear of a phone's notch and rounded corners: the screen's safe area, in this panel's
        // pixels from each edge.
        public void SetSafeArea(float left, float top, float right, float bottom)
        {
            style.paddingLeft = left;
            style.paddingTop = top;
            style.paddingRight = right;
            style.paddingBottom = bottom;
        }
    }
}
