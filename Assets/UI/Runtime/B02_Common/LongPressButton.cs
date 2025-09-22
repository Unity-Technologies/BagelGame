using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.UIElements;

namespace Bagel
{
    [UxmlElement]
    public partial class LongPressButton : VisualElement
    {
        public static readonly string ussClassName = "b-long-press-button";
        public static readonly string labelUssClassName = "b-long-press-button__label";
        public static readonly string progressUssClassName = "b-long-press-button__progress";

        [UxmlAttribute]
        public string text
        {
            get => m_TextElement.text;
            set => m_TextElement.text = value;
        }

        [UxmlAttribute]
        public float defaultHoldTime { get; set; } = 1f;

        [UxmlAttribute]
        public InputActionReference confirmAction
        {
            get => m_ConfirmAction;
            set
            {
                if (m_ConfirmAction == value)
                    return;
                m_ConfirmAction = value;
            }
        }

        public event Action clicked;

        public float holdTime => m_ConfirmAction
            ? InputSystem.settings?.defaultHoldTime ?? defaultHoldTime
            : defaultHoldTime;
        public bool isHolding => m_HoldScheduledItem.isActive;

        bool m_InputArmed;
        InputActionReference m_ConfirmAction;

        TextElement m_TextElement;
        VisualElement m_TimerProgress;

        float m_HoldMaxTime;
        float m_HoldCurrentTime;
        IVisualElementScheduledItem m_HoldScheduledItem;

        public LongPressButton()
        {
            focusable = true;
            AddToClassList(ussClassName);
            AddToClassList(Button.ussClassName);

            m_TimerProgress = new VisualElement();
            m_TimerProgress.AddToClassList(progressUssClassName);
            m_TimerProgress.style.position = Position.Absolute;
            m_TimerProgress.style.left = 0;
            m_TimerProgress.style.top = 0;
            m_TimerProgress.style.bottom = 0;
            m_TimerProgress.style.width = new StyleLength(Length.Percent(0));
            Add(m_TimerProgress);

            m_TextElement = new TextElement();
            m_TextElement.text = "Long Press Button";
            m_TextElement.AddToClassList(labelUssClassName);
            Add(m_TextElement);

            m_HoldScheduledItem = schedule.Execute(AnimateHoldProgress).Every(16);
            m_HoldScheduledItem.Pause();
        }

        protected override void HandleEventBubbleUp(EventBase evt)
        {
            base.HandleEventBubbleUp(evt);

            if (evt is PointerDownEvent pde)
            {
                if (pde.button != 0)
                    return;
                StartHold();
                evt.StopPropagation();
                PointerCaptureHelper.CapturePointer(this, 0);
            }
            else if ((evt is PointerUpEvent || evt is PointerCancelEvent) && isHolding)
            {
                CancelHold();
                evt.StopPropagation();
            }
            else if (evt is FocusEvent)
            {
                ArmInput();
            }
            else if (evt is FocusOutEvent)
            {
                if (isHolding)
                    CancelHold();

                DisarmInput();
                evt.StopPropagation();
            }
        }

        void StartHold() => StartHold(holdTime);

        void StartHold(float time)
        {
            m_HoldMaxTime = time;
            m_HoldCurrentTime = 0.0f;
            m_HoldScheduledItem.Resume();
        }

        void CancelHold()
        {
            m_HoldScheduledItem.Pause();
            m_TimerProgress.style.width = new StyleLength(Length.Percent(0));

            if (PointerCaptureHelper.HasPointerCapture(this, 0))
                PointerCaptureHelper.ReleasePointer(this, 0);
        }

        void AnimateHoldProgress(TimerState timer)
        {
            m_HoldCurrentTime += timer.deltaTime / 1000.0f;
            var progress = Mathf.Clamp01(m_HoldCurrentTime / defaultHoldTime) * 100.0f;
            m_TimerProgress.style.width = new StyleLength(Length.Percent(progress));

            if (m_HoldCurrentTime < defaultHoldTime)
                return;

            CancelHold();
            clicked?.Invoke();
        }

        void ArmInput()
        {
            if (m_InputArmed || m_ConfirmAction == null || m_ConfirmAction.action == null)
                return;

            var a = m_ConfirmAction.action;
            if (!a.enabled)
                a.Enable();
            a.started += OnActionStarted;
            a.canceled += OnActionCanceled;
            a.performed += OnActionPerformed;
            m_InputArmed = true;
        }

        void DisarmInput()
        {
            if (!m_InputArmed || m_ConfirmAction == null || m_ConfirmAction.action == null)
                return;

            var a = m_ConfirmAction.action;
            a.started -= OnActionStarted;
            a.canceled -= OnActionCanceled;
            a.performed -= OnActionPerformed;
            m_InputArmed = false;
        }

        void OnActionStarted(InputAction.CallbackContext ctx)
        {
            if (ctx.interaction is HoldInteraction holdInteraction)
                StartHold(holdInteraction.duration);
        }

        void OnActionCanceled(InputAction.CallbackContext ctx)
        {
            if (!isHolding)
                return;

            CancelHold();
        }

        void OnActionPerformed(InputAction.CallbackContext ctx)
        {
            if (ctx.interaction is HoldInteraction)
            {
                CancelHold();
                clicked?.Invoke();
            }
        }
    }
}
