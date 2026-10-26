using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // The payment tablet by the cash register, after a bagel's ordered: the levels to play, each a strip of the counter
    // from above (a longer level, a longer strip), the game's own first, then the player's own. Choosing one sets the
    // counter for it, room and all. Pay & Play starts the game; Back goes back to the bagels; New Table makes a level of
    // the player's own, and Edit (only theirs) opens it in the level editor on the tablet, the counter changing as it's
    // edited. Up and down go through the levels; the tablet keeps the focus while levels are chosen, as the register does.
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class LevelSelectionScreenManager : BagelPane
    {
        public static readonly string ussClassName = "b-tablet";
        public static readonly string editingUssClassName = ussClassName + "--editing";
        public static readonly string rowUssClassName = "b-level-row";
        public static readonly string selectedRowUssClassName = rowUssClassName + "--selected";
        public static readonly string rowTextUssClassName = rowUssClassName + "__text";
        public static readonly string rowNameUssClassName = rowUssClassName + "__name";
        public static readonly string rowDetailsUssClassName = rowUssClassName + "__details";
        public static readonly string rowBadgeUssClassName = rowUssClassName + "__badge";
        public static readonly string rowMapUssClassName = rowUssClassName + "__map";

        ScrollView m_LevelList;
        Button m_BackButton;
        Button m_NewButton;
        Button m_EditButton;
        Button m_PlayButton;
        Label m_OrderLabel;
        BagelLevelEditor m_Editor;
        readonly List<VisualElement> m_Rows = new();
        Focusable m_LastFocused;
        VisualElement m_PanelRoot;
        bool m_Bound;

        public ScrollView levelList => m_LevelList ??= this.Q<ScrollView>("level-list");
        public Button backButton => m_BackButton ??= this.Q<Button>("back-button");
        public Button newButton => m_NewButton ??= this.Q<Button>("new-button");
        public Button editButton => m_EditButton ??= this.Q<Button>("edit-button");
        public Button playButton => m_PlayButton ??= this.Q<Button>("play-button");
        public Label orderLabel => m_OrderLabel ??= this.Q<Label>("order");
        public BagelLevelEditor editor => m_Editor ??= this.Q<BagelLevelEditor>();
        public IReadOnlyList<VisualElement> rows => m_Rows;

        bool isChoosing => state != null && state.isLevelSelection;
        bool isEditing => state != null && state.isEditingLevel;

        protected override void OnAttached()
        {
            ListenToThePanel(panel?.visualTree);

            if (m_Bound)
                return;
            m_Bound = true;

            backButton.clicked += Back;
            newButton.clicked += NewLevel;
            editButton.clicked += Edit;
            playButton.clicked += Play;
            if (editor != null)
            {
                editor.done += () => state?.EditLevel(false);
                editor.deleted += DeleteLevel;
                editor.showDelete = true;
            }

            RegisterCallback<FocusInEvent>(OnFocusIn);
            RegisterCallback<FocusOutEvent>(OnFocusOut);
            RegisterCallback<NavigationCancelEvent>(OnNavigationCancelEvent);
        }

        protected override void OnDetached() => ListenToThePanel(null);

        // A click on nothing leaves no panel in focus, and the next key goes to the panel itself: it's for the tablet,
        // while levels are chosen.
        void ListenToThePanel(VisualElement root)
        {
            if (m_PanelRoot != null)
            {
                m_PanelRoot.UnregisterCallback<NavigationMoveEvent>(OnPanelNavigation);
                m_PanelRoot.UnregisterCallback<NavigationSubmitEvent>(OnPanelNavigation);
                m_PanelRoot.UnregisterCallback<NavigationCancelEvent>(OnPanelNavigation);
            }
            m_PanelRoot = root;
            if (m_PanelRoot != null)
            {
                m_PanelRoot.RegisterCallback<NavigationMoveEvent>(OnPanelNavigation);
                m_PanelRoot.RegisterCallback<NavigationSubmitEvent>(OnPanelNavigation);
                m_PanelRoot.RegisterCallback<NavigationCancelEvent>(OnPanelNavigation);
            }
        }

        void OnPanelNavigation(EventBase evt)
        {
            if (evt.target != m_PanelRoot || !isChoosing)
                return;
            evt.StopPropagation();
            FocusIn();
        }

        protected override void OnStateChanged(PlayManagerState oldState, PlayManagerState newState)
        {
            RebuildRows();
            UpdateMode();
        }

        protected override void OnStatePropertyChanged(in BindingId property)
        {
            if (property == PlayManagerState.levelsPath)
                RebuildRows();
            else if (property == PlayManagerState.selectedLevelIndexPath)
                UpdateSelection();
            else if (property == PlayManagerState.isEditingLevelPath || property == PlayManagerState.currentStatePath)
                UpdateMode();
            else if (property == PlayManagerState.selectedBagelIndexPath)
                UpdateOrder();
        }

        protected override void OnLocaleChanged()
        {
            RebuildRows();
            UpdateOrder();
        }

        public void Play() => state?.GoToPlay();

        public void Back() => state?.GoToBagelSelection();

        public void Edit() => state?.EditLevel(true);

        public void NewLevel()
        {
            if (state == null)
                return;
            var number = 1;
            foreach (var level in state.levels)
            {
                if (level.custom)
                    number++;
            }
            state.AddLevel(string.Format(BagelLocalization.culture, BagelLocalization.Get("levels.new-name", "My Table {0}"), BagelLocalization.LocalizeDigits(number.ToString(BagelLocalization.culture))));
            state.EditLevel(true);
        }

        void DeleteLevel()
        {
            var level = state?.selectedLevel;
            if (level == null || !level.custom)
                return;
            state.DeleteLevel(level);
            FocusIn();
        }

        // The tablet's focus, back where it was: the chosen level's row, or the editor's counter.
        public void FocusIn()
        {
            if (!isChoosing || panel == null)
                return;
            if (isEditing)
            {
                editor?.map.Focus();
                return;
            }
            // The list's buttons keep it; anywhere else (the editor's gone), the chosen level's row.
            var index = state.selectedLevelIndex;
            var actions = this.Q("actions");
            if (m_LastFocused is VisualElement last && last.panel != null && actions != null && actions.Contains(last) && last.enabledInHierarchy)
                last.Focus();
            else if (index >= 0 && index < m_Rows.Count)
                m_Rows[index].Focus();
        }

        void OnFocusIn(FocusInEvent evt) => m_LastFocused = evt.target as Focusable;

        void OnFocusOut(FocusOutEvent evt)
        {
            if (!isChoosing)
                return;
            schedule.Execute(() =>
            {
                var focused = focusController?.focusedElement as VisualElement;
                if (isChoosing && panel != null && (focused == null || !Contains(focused)))
                    FocusIn();
            });
        }

        void OnNavigationCancelEvent(NavigationCancelEvent evt)
        {
            if (!isChoosing)
                return;
            if (isEditing)
            {
                state.EditLevel(false);
                FocusIn();
            }
            else
            {
                Back();
            }
            evt.StopPropagation();
        }

        void RebuildRows()
        {
            if (levelList == null)
                return;
            // The same levels: only what they say may have changed (the editor's changing one).
            if (state != null && m_Rows.Count == state.levelCount && m_Rows.Count > 0)
            {
                var same = true;
                for (int i = 0; i < m_Rows.Count && same; i++)
                    same = m_Rows[i].name == "level-" + state.levels[i].id;
                if (same)
                {
                    for (int i = 0; i < m_Rows.Count; i++)
                        UpdateRow(m_Rows[i], state.levels[i]);
                    UpdateSelection();
                    return;
                }
            }
            levelList.Clear();
            m_Rows.Clear();
            if (state == null)
                return;
            for (int i = 0; i < state.levelCount; i++)
                m_Rows.Add(MakeRow(state.levels[i], i));
            UpdateSelection();
            if (isChoosing && !isEditing && focusController?.focusedElement is VisualElement focused && !Contains(focused))
                FocusIn();
        }

        VisualElement MakeRow(BagelLevel level, int index)
        {
            var row = new VisualElement { name = "level-" + level.id, focusable = true, tabIndex = 0, userData = index };
            row.AddToClassList(rowUssClassName);
            row.AddToClassList(BagelAccessibility.itemUssClassName);
            var text = new VisualElement { pickingMode = PickingMode.Ignore };
            text.AddToClassList(rowTextUssClassName);
            var name = new Label { name = "name", pickingMode = PickingMode.Ignore };
            name.AddToClassList(rowNameUssClassName);
            text.Add(name);
            var details = new Label { name = "details", pickingMode = PickingMode.Ignore };
            details.AddToClassList(rowDetailsUssClassName);
            text.Add(details);
            if (level.custom)
            {
                var badge = new Label(BagelLocalization.Get("levels.yours", "Yours")) { name = "badge", pickingMode = PickingMode.Ignore };
                badge.AddToClassList(rowBadgeUssClassName);
                text.Add(badge);
            }
            row.Add(text);
            UpdateRow(row, level);
            var map = new BagelLevelMap { name = "map", pickingMode = PickingMode.Ignore, library = state.levelLibrary, level = level };
            map.AddToClassList(rowMapUssClassName);
            row.Add(map);

            // Focused (by the pointer, the keyboard or the gamepad), it's the chosen level; Submit plays it.
            row.RegisterCallback<FocusInEvent>(_ =>
            {
                if (isChoosing && !isEditing)
                    state.SelectLevel((int)row.userData);
                levelList.ScrollTo(row);
            });
            row.RegisterCallback<NavigationSubmitEvent>(evt =>
            {
                Play();
                evt.StopPropagation();
            });
            row.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.clickCount == 2)
                    Play();
            });
            levelList.Add(row);
            return row;
        }

        // Its name, how long it is, and how much is on it.
        static void UpdateRow(VisualElement row, BagelLevel level)
        {
            row.Q<Label>("name").text = LevelName(level);
            var meters = BagelLocalization.LocalizeDigits(level.length.ToString("0.#", BagelLocalization.culture));
            var count = BagelLocalization.LocalizeDigits(level.pieceCount.ToString(BagelLocalization.culture));
            row.Q<Label>("details").text = string.Format(BagelLocalization.culture, BagelLocalization.Get("levels.length", "{0} m"), meters) + " · " +
                string.Format(BagelLocalization.culture, BagelLocalization.Get("levels.pieces", "Pieces: {0}"), count);
            row.tooltip = LevelName(level) + ", " + row.Q<Label>("details").text;
            if (row.Q<Label>("badge") is { } badge)
                badge.text = BagelLocalization.Get("levels.yours", "Yours");
        }

        public static string LevelName(BagelLevel level) =>
            level == null ? "" : string.IsNullOrEmpty(level.nameKey) ? level.displayName : BagelLocalization.Get(level.nameKey, level.displayName);

        void UpdateSelection()
        {
            var selected = state != null ? state.selectedLevelIndex : -1;
            for (int i = 0; i < m_Rows.Count; i++)
                m_Rows[i].EnableInClassList(selectedRowUssClassName, i == selected);
            // Chosen some other way (the keyboard's on another row, a new level): the focus goes with it.
            if (selected >= 0 && selected < m_Rows.Count && focusController?.focusedElement is VisualElement focused && m_Rows.Contains(focused) && focused != m_Rows[selected])
                m_Rows[selected].Focus();
            editButton?.SetEnabled(state != null && state.canEditLevel);
            if (editor != null && isEditing)
                editor.level = state.selectedLevel;
        }

        void UpdateMode()
        {
            var editing = isEditing;
            EnableInClassList(editingUssClassName, editing);
            if (editor != null)
            {
                editor.library = state?.levelLibrary;
                editor.level = editing ? state.selectedLevel : null;
            }
            UpdateSelection();
            UpdateOrder();
            if (isChoosing)
                schedule.Execute(FocusIn);
        }

        void UpdateOrder()
        {
            if (orderLabel == null)
                return;
            var bagelType = state?.selectedBagelType;
            orderLabel.text = bagelType != null ? BagelLocalization.Get(bagelType.nameKey, bagelType.displayName) : "";
        }
    }
}
