using System;
using UnityEngine.UIElements;

namespace Bagel
{
    // Base for every screen and pane manager. A screen's Driver hands it the game's shared
    // state; nested panes find it through their ancestors. Opened on its own (UI Builder,
    // a test, the Bagel All UI window), a pane makes a throwaway copy of its start state,
    // so every UXML file is fully interactive by itself.
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class BagelPane : VisualElement
    {
        // Set on the top pane from the settings; the common styles turn things off under them.
        public static readonly UniqueStyleString reduceMotionUssClassName = new UniqueStyleString("b-reduce-motion");
        public static readonly UniqueStyleString noVisualEffectsUssClassName = new UniqueStyleString("b-no-visual-effects");

        PlayManagerState m_State;
        PlayManagerState m_GivenState;
        PlayManagerState m_OwnedState;
        BagelPane m_ParentPane;

        [UxmlAttribute]
        public PlayManagerState startState { get; set; }

        public PlayManagerState state => m_State;
        public BagelPane parentPane => m_ParentPane;

        public event Action<PlayManagerState> stateChanged;

        public BagelPane()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        // Show this state here and in every pane inside. Null goes back to the parent's state.
        public void SetState(PlayManagerState newState)
        {
            m_GivenState = newState;
            if (panel != null)
                ResolveState();
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            m_ParentPane = GetFirstAncestorOfType<BagelPane>();
            if (m_ParentPane != null)
                m_ParentPane.stateChanged += ParentPane_OnStateChanged;

            ResolveState();
            BagelLocalization.localeChanged += BagelLocalization_OnLocaleChanged;
            OnAttached();

            if (m_ParentPane == null && !UnityEngine.Application.isPlaying)
                BagelThemes.AddPreviewRoot(this);

            // A screen's top pane plays what's pressed in it.
            if (m_ParentPane == null)
                BagelSound.Listen(this);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            BagelThemes.RemovePreviewRoot(this);
            BagelLocalization.localeChanged -= BagelLocalization_OnLocaleChanged;
            OnDetached();

            if (m_ParentPane != null)
                m_ParentPane.stateChanged -= ParentPane_OnStateChanged;
            m_ParentPane = null;

            ApplyState(null);
            ReleaseOwnedState();
        }

        void ParentPane_OnStateChanged(PlayManagerState parentState) => ResolveState();

        void ResolveState()
        {
            var newState = m_GivenState;
            if (newState == null && m_ParentPane != null)
                newState = m_ParentPane.state;
            if (newState == null)
            {
                if (m_OwnedState == null)
                    m_OwnedState = PlayManagerState.CreateRuntimeCopy(startState);
                newState = m_OwnedState;
            }

            ApplyState(newState);

            if (newState != m_OwnedState)
                ReleaseOwnedState();
        }

        void ApplyState(PlayManagerState newState)
        {
            if (m_State == newState)
                return;

            var oldState = m_State;
            if (oldState != null)
                oldState.propertyChanged -= State_OnPropertyChanged;

            m_State = newState;

            if (m_State != null)
                m_State.propertyChanged += State_OnPropertyChanged;

            // The top pane is the data source of everything below it.
            if (m_ParentPane == null && (dataSource == null || dataSource is PlayManagerState))
                dataSource = m_State;

            ApplyDisplaySettings();
            OnStateChanged(oldState, m_State);
            stateChanged?.Invoke(m_State);
        }

        void ReleaseOwnedState()
        {
            if (m_OwnedState == null)
                return;

            PlayManagerState.DestroyRuntimeCopy(m_OwnedState);
            m_OwnedState = null;
        }

        void State_OnPropertyChanged(object sender, BindablePropertyChangedEventArgs args)
        {
            var property = args.propertyName;
            if (property == SettingsData.reduceMotionPath || property == SettingsData.visualEffectsPath)
                ApplyDisplaySettings();

            OnStatePropertyChanged(property);
        }

        void BagelLocalization_OnLocaleChanged()
        {
            ApplyDisplaySettings();
            if (m_ParentPane == null)
                BagelLocalization.RefreshBindings(this);
            OnLocaleChanged();
        }

        void ApplyDisplaySettings()
        {
            var isTopPane = m_ParentPane == null && m_State != null;
            EnableInClassList(reduceMotionUssClassName, isTopPane && m_State.settings.reduceMotion);
            EnableInClassList(noVisualEffectsUssClassName, isTopPane && !m_State.settings.visualEffects);

            // The screen reads in the language's direction; the panes inside follow it.
            if (isTopPane)
                FollowsLanguage.Apply(this, BagelLocalization.isRightToLeft);
            else
            {
                languageDirection = LanguageDirection.Inherit;
                RemoveFromClassList(FollowsLanguage.rtlUssClassName);
            }
        }

        // Called after the pane is attached and has a state.
        protected virtual void OnAttached() {}
        protected virtual void OnDetached() {}
        protected virtual void OnStateChanged(PlayManagerState oldState, PlayManagerState newState) {}
        protected virtual void OnStatePropertyChanged(in BindingId property) {}
        // Called after the language changes. LocalizedString bindings update by themselves.
        protected virtual void OnLocaleChanged() {}
    }
}
