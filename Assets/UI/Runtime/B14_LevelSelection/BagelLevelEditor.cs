using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Bagel
{
    // A level's editor: the counter as a grid (the gingham's half-meter checks), the library of things to put on it as
    // live 3D pictures that turn and can be spun, and the level's name and length. Pick a thing and click the counter
    // (or drag it there) to put it down; drag what's down to move it; turn it, or take it off. The keyboard and the
    // gamepad move a cursor over the counter: Submit puts the chosen thing down, or picks up what's there to move it and
    // puts it down again; Q and E (or the shoulder buttons) turn it, Delete (or the gamepad's left face button) takes it
    // off. The same editor is on the payment tablet in the game, for the player's own levels, and in the Editor's
    // Bagel Levels window, for the game's own.
    [UxmlElement]
    public partial class BagelLevelEditor : VisualElement
    {
        public static readonly string ussClassName = "b-level-editor";
        public static readonly string headerUssClassName = ussClassName + "__header";
        public static readonly string nameUssClassName = ussClassName + "__name";
        public static readonly string lengthUssClassName = ussClassName + "__length";
        public static readonly string lengthValueUssClassName = ussClassName + "__length-value";
        public static readonly string lengthButtonUssClassName = ussClassName + "__length-button";
        public static readonly string boardUssClassName = ussClassName + "__board";
        public static readonly string mapUssClassName = ussClassName + "__map";
        public static readonly string footerUssClassName = ussClassName + "__footer";
        public static readonly string libraryUssClassName = ussClassName + "__library";
        public static readonly string itemUssClassName = ussClassName + "__item";
        public static readonly string chosenItemUssClassName = itemUssClassName + "--chosen";
        public static readonly string thumbnailUssClassName = ussClassName + "__thumbnail";
        public static readonly string itemNameUssClassName = ussClassName + "__item-name";
        public static readonly string toolsUssClassName = ussClassName + "__tools";
        public static readonly string hintUssClassName = ussClassName + "__hint";
        public static readonly string ghostUssClassName = ussClassName + "__ghost";
        public static readonly string cursorUssClassName = ussClassName + "__cursor";
        public static readonly string invalidUssClassName = ussClassName + "--invalid";
        public static readonly string selectedPieceUssClassName = BagelLevelMap.pieceUssClassName + "--selected";
        public static readonly string heldPieceUssClassName = BagelLevelMap.pieceUssClassName + "--held";

        public const float turnStep = 45f;
        public static readonly string removeActionName = "UI/RemovePiece";

        const float k_ClickSlop = 6f;
        const float k_SpinPerPixel = 1.5f;

        BagelLevel m_Level;
        BagelLevelLibrary m_Library;
        IBagelLevelThumbnails m_Thumbnails;

        string m_Brush;
        float m_BrushAngle;
        int m_Selected = -1;
        Vector2 m_Cursor;
        bool m_CursorShown;
        int m_Held = -1;
        BagelLevelPiece m_HeldPiece;

        // A piece dragged with the pointer, and a thing dragged from the library.
        int m_DragPointer = -1;
        int m_Dragged = -1;
        Vector2 m_DragOffset;
        Vector2 m_DragTo;
        Vector2 m_DragStart;
        bool m_DragMoved;
        VisualElement m_SpinItem;
        Vector2 m_SpinStart;
        bool m_SpinMoved;
        bool m_PlacingFromLibrary;

        readonly TextField m_NameField;
        readonly Label m_LengthLabel;
        readonly Button m_ShorterButton;
        readonly Button m_LongerButton;
        readonly LongPressButton m_DeleteButton;
        readonly Button m_DoneButton;
        readonly ScrollView m_Board;
        readonly BagelLevelMap m_Map;
        readonly VisualElement m_Ghost;
        readonly VisualElement m_CursorElement;
        readonly ScrollView m_LibraryView;
        readonly Button m_TurnButton;
        readonly Button m_RemoveButton;
        readonly Label m_Hint;
        readonly List<VisualElement> m_Items = new();

        InputAction m_TurnLeftAction;
        InputAction m_TurnRightAction;
        InputAction m_RemoveAction;

        // Before and after every change to the level: the Editor window records an undo step before.
        public event Action<BagelLevel> changing;
        public event Action<BagelLevel> changed;
        public event Action done;
        public event Action deleted;

        public BagelLevel level
        {
            get => m_Level;
            set
            {
                if (m_Level == value)
                    return;
                if (m_Level != null)
                    m_Level.changed -= Level_OnChanged;
                m_Level = value;
                if (m_Level != null && panel != null)
                    m_Level.changed += Level_OnChanged;
                m_Map.level = value;
                m_Selected = -1;
                m_Held = -1;
                m_Cursor = new Vector2(0f, 2f);
                UpdateFields();
                UpdateSelection();
            }
        }

        public BagelLevelLibrary library
        {
            get => m_Library;
            set
            {
                if (m_Library == value)
                    return;
                m_Library = value;
                m_Map.library = value;
                BuildLibrary();
            }
        }

        public IBagelLevelThumbnails thumbnails
        {
            get => m_Thumbnails;
            set
            {
                m_Thumbnails = value;
                UpdateThumbnails();
            }
        }

        // The tablet deletes the player's level from here; the Editor window has its own.
        public bool showDelete
        {
            get => m_DeleteButton.resolvedStyle.display != DisplayStyle.None;
            set => m_DeleteButton.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public bool showDone
        {
            get => m_DoneButton.resolvedStyle.display != DisplayStyle.None;
            set => m_DoneButton.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public string brush => m_Brush;
        public float brushAngle => m_BrushAngle;
        public int selectedIndex => m_Selected;
        public int heldIndex => m_Held;
        public Vector2 cursor => m_Cursor;
        public BagelLevelMap map => m_Map;
        public ScrollView board => m_Board;
        public TextField nameField => m_NameField;
        public Button shorterButton => m_ShorterButton;
        public Button longerButton => m_LongerButton;
        public LongPressButton deleteButton => m_DeleteButton;
        public Button doneButton => m_DoneButton;
        public Button turnButton => m_TurnButton;
        public Button removeButton => m_RemoveButton;
        public IReadOnlyList<VisualElement> items => m_Items;

        public BagelLevelEditor()
        {
            AddToClassList(ussClassName);

            var header = new VisualElement { name = "header" };
            header.AddToClassList(headerUssClassName);
            m_NameField = new TextField { name = "name-field", isDelayed = true, maxLength = 24 };
            m_NameField.AddToClassList(nameUssClassName);
            m_NameField.RegisterValueChangedCallback(evt => Rename(evt.newValue));
            header.Add(m_NameField);

            var length = new VisualElement { name = "length" };
            length.AddToClassList(lengthUssClassName);
            m_ShorterButton = new Button(() => Lengthen(-1)) { name = "shorter-button", text = "-" };
            m_ShorterButton.AddToClassList(lengthButtonUssClassName);
            m_LengthLabel = new Label { name = "length-value" };
            m_LengthLabel.AddToClassList(lengthValueUssClassName);
            m_LongerButton = new Button(() => Lengthen(1)) { name = "longer-button", text = "+" };
            m_LongerButton.AddToClassList(lengthButtonUssClassName);
            length.Add(m_ShorterButton);
            length.Add(m_LengthLabel);
            length.Add(m_LongerButton);
            header.Add(length);

            m_DeleteButton = new LongPressButton { name = "delete-button", text = "Delete table" };
            m_DeleteButton.clicked += () => deleted?.Invoke();
            header.Add(m_DeleteButton);
            m_DoneButton = new Button(() => done?.Invoke()) { name = "done-button", text = "Done" };
            m_DoneButton.AddToClassList("b-primary");
            header.Add(m_DoneButton);
            Add(header);

            m_Board = new ScrollView(ScrollViewMode.Horizontal) { name = "board" };
            m_Board.AddToClassList(boardUssClassName);
            m_Board.horizontalScrollerVisibility = ScrollerVisibility.Auto;
            m_Board.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            m_Map = new BagelLevelMap { name = "map", showGrid = true, focusable = true, tabIndex = 0, pickingMode = PickingMode.Position };
            m_Map.AddToClassList(mapUssClassName);
            m_Ghost = new VisualElement { name = "ghost", pickingMode = PickingMode.Ignore };
            m_Ghost.AddToClassList(ghostUssClassName);
            m_Ghost.style.display = DisplayStyle.None;
            m_Map.Add(m_Ghost);
            m_CursorElement = new VisualElement { name = "cursor", pickingMode = PickingMode.Ignore };
            m_CursorElement.AddToClassList(cursorUssClassName);
            m_CursorElement.style.display = DisplayStyle.None;
            m_Map.Add(m_CursorElement);
            m_Board.Add(m_Map);
            Add(m_Board);

            var footer = new VisualElement { name = "footer" };
            footer.AddToClassList(footerUssClassName);
            m_LibraryView = new ScrollView(ScrollViewMode.Horizontal) { name = "library", verticalScrollerVisibility = ScrollerVisibility.Hidden };
            m_LibraryView.AddToClassList(libraryUssClassName);
            footer.Add(m_LibraryView);
            ScrollBars.KeepOutOfTheFocus(m_Board);
            ScrollBars.KeepOutOfTheFocus(m_LibraryView);
            var tools = new VisualElement { name = "tools" };
            tools.AddToClassList(toolsUssClassName);
            m_TurnButton = new Button(() => Turn(1)) { name = "turn-button", text = "Turn" };
            m_RemoveButton = new Button(RemoveSelected) { name = "remove-button", text = "Remove" };
            tools.Add(m_TurnButton);
            tools.Add(m_RemoveButton);
            footer.Add(tools);
            Add(footer);

            m_Hint = new Label { name = "hint", pickingMode = PickingMode.Ignore };
            m_Hint.AddToClassList(hintUssClassName);
            Add(m_Hint);

            m_Map.RegisterCallback<PointerDownEvent>(OnMapPointerDown);
            m_Map.RegisterCallback<PointerMoveEvent>(OnMapPointerMove);
            m_Map.RegisterCallback<PointerUpEvent>(OnMapPointerUp);
            m_Map.RegisterCallback<PointerCaptureOutEvent>(_ => CancelDrag());
            m_Map.RegisterCallback<PointerLeaveEvent>(_ => { if (m_Dragged < 0) ShowGhost(false); });
            m_Map.RegisterCallback<NavigationMoveEvent>(OnMapNavigationMove);
            m_Map.RegisterCallback<NavigationSubmitEvent>(OnMapNavigationSubmit);
            m_Map.RegisterCallback<NavigationCancelEvent>(OnMapNavigationCancel);
            m_Map.RegisterCallback<KeyDownEvent>(OnMapKeyDown);
            m_Map.RegisterCallback<FocusInEvent>(_ => ShowCursor(true));
            m_Map.RegisterCallback<FocusOutEvent>(_ => ShowCursor(false));
            m_Map.RegisterCallback<GeometryChangedEvent>(_ => UpdateSelection());

            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
            UpdateFields();
            UpdateSelection();
        }

        void OnAttach(AttachToPanelEvent evt)
        {
            if (m_Level != null)
            {
                m_Level.changed -= Level_OnChanged;
                m_Level.changed += Level_OnChanged;
            }
            BagelLocalization.localeChanged += UpdateFields;
            ListenToActions(evt.destinationPanel.contextType == ContextType.Player);
            UpdateFields();
        }

        void OnDetach(DetachFromPanelEvent evt)
        {
            if (m_Level != null)
                m_Level.changed -= Level_OnChanged;
            BagelLocalization.localeChanged -= UpdateFields;
            ListenToActions(false);
        }

        // The gamepad's buttons for turning and taking off, in the game (they're keys in an Editor window).
        void ListenToActions(bool listen)
        {
            if (listen)
            {
                var actions = InputSystem.actions;
                m_TurnLeftAction = actions?.FindAction(SettingsPaneManager.previousTabActionName);
                m_TurnRightAction = actions?.FindAction(SettingsPaneManager.nextTabActionName);
                m_RemoveAction = actions?.FindAction(removeActionName);
            }
            Listen(m_TurnLeftAction, OnTurnLeftAction, listen);
            Listen(m_TurnRightAction, OnTurnRightAction, listen);
            Listen(m_RemoveAction, OnRemoveAction, listen);
        }

        static void Listen(InputAction action, Action<InputAction.CallbackContext> callback, bool listen)
        {
            if (action == null)
                return;
            action.performed -= callback;
            if (listen)
                action.performed += callback;
        }

        bool mapHasFocus => m_Map.focusController?.focusedElement == m_Map && m_Map.enabledInHierarchy;

        void OnTurnLeftAction(InputAction.CallbackContext _)
        {
            if (mapHasFocus)
                Turn(-1);
        }

        void OnTurnRightAction(InputAction.CallbackContext _)
        {
            if (mapHasFocus)
                Turn(1);
        }

        void OnRemoveAction(InputAction.CallbackContext _)
        {
            if (mapHasFocus)
                RemoveSelected();
        }

        void Level_OnChanged(BagelLevel level)
        {
            if (m_Selected >= level.pieceCount)
                m_Selected = -1;
            UpdateFields();
            m_Map.Refresh();
            UpdateSelection();
        }

        // Shown again from the level as it is (an undo in the Editor changes it underneath).
        public void Refresh()
        {
            if (m_Level != null && m_Selected >= m_Level.pieceCount)
                m_Selected = -1;
            m_Held = -1;
            UpdateFields();
            m_Map.Refresh(true);
            UpdateSelection();
        }

        // ------------------------------------------------------------------ Changing the level

        void Edit(Action edit)
        {
            if (m_Level == null)
                return;
            changing?.Invoke(m_Level);
            edit();
            changed?.Invoke(m_Level);
        }

        public void Rename(string name)
        {
            name = name?.Trim();
            if (m_Level == null || string.IsNullOrEmpty(name) || name == m_Level.displayName)
            {
                UpdateFields();
                return;
            }
            Edit(() => m_Level.displayName = name);
        }

        public void Lengthen(int steps)
        {
            if (m_Level == null)
                return;
            var length = BagelLevel.ClampLength(m_Level.length + steps * BagelLevel.lengthStep);
            if (Mathf.Approximately(length, m_Level.length))
                return;
            Edit(() => m_Level.length = length);
        }

        public void ChooseBrush(string kind)
        {
            m_Brush = kind == m_Brush ? null : kind;
            m_BrushAngle = 0f;
            foreach (var item in m_Items)
                item.EnableInClassList(chosenItemUssClassName, (string)item.userData == m_Brush);
            UpdateHint();
        }

        // The chosen thing down there (snapped to the grid, nudged onto the cloth at its edges), chosen; false if it
        // can't stand there.
        public bool PlaceAt(Vector2 position)
        {
            var entry = m_Library != null ? m_Library.Find(m_Brush) : null;
            if (m_Level == null || entry == null)
                return false;
            position = BagelLevel.SnapInside(position, m_Level.PlaceableArea(entry.HalfExtents(m_BrushAngle)));
            if (!m_Level.CanPlace(position, entry.HalfExtents(m_BrushAngle)))
                return false;
            var index = -1;
            Edit(() => index = m_Level.Add(new BagelLevelPiece(entry.id, position.x, position.y, m_BrushAngle)));
            Select(index);
            return true;
        }

        public void Select(int index)
        {
            m_Selected = m_Level != null && index >= 0 && index < m_Level.pieceCount ? index : -1;
            UpdateSelection();
        }

        // A piece somewhere else; false if it can't stand there.
        public bool Move(int index, Vector2 position)
        {
            if (m_Level == null || index < 0 || index >= m_Level.pieceCount)
                return false;
            var piece = m_Level.pieces[index];
            var entry = m_Library?.Find(piece.kind);
            position = BagelLevel.Snap(position);
            if (entry == null || !m_Level.CanPlace(position, entry.HalfExtents(piece.angle)))
                return false;
            if (piece.position == position)
                return true;
            piece.x = position.x;
            piece.z = position.y;
            Edit(() => m_Level.Set(index, piece));
            return true;
        }

        // The piece in hand or chosen a step round (clockwise for 1), or the thing to put down next.
        public void Turn(int direction)
        {
            var index = m_Held >= 0 ? m_Held : m_Selected;
            if (m_Level == null || index < 0 || index >= m_Level.pieceCount)
            {
                m_BrushAngle = Mathf.Repeat(m_BrushAngle + direction * turnStep, 360f);
                UpdateGhost();
                return;
            }

            var piece = m_Held >= 0 ? m_HeldPiece : m_Level.pieces[index];
            var entry = m_Library?.Find(piece.kind);
            if (entry == null)
                return;
            piece.angle = Mathf.Repeat(piece.angle + direction * turnStep, 360f);
            // Turned at the edge, it's nudged back onto the cloth.
            var position = BagelLevel.SnapInside(piece.position, m_Level.PlaceableArea(entry.HalfExtents(piece.angle)));
            if (!m_Level.CanPlace(position, entry.HalfExtents(piece.angle)))
            {
                Refuse();
                return;
            }
            piece.x = position.x;
            piece.z = position.y;
            if (m_Held >= 0)
            {
                m_HeldPiece = piece;
                m_Cursor = position;
                UpdateSelection();
                return;
            }
            Edit(() => m_Level.Set(index, piece));
        }

        public void RemoveSelected()
        {
            var index = m_Held >= 0 ? m_Held : m_Selected;
            if (m_Level == null || index < 0 || index >= m_Level.pieceCount)
                return;
            m_Held = -1;
            m_Selected = -1;
            Edit(() => m_Level.RemoveAt(index));
        }

        // ------------------------------------------------------------------ Keyboard and gamepad

        void OnMapNavigationMove(NavigationMoveEvent evt)
        {
            if (m_Level == null)
                return;
            var step = BagelLevel.cellSize;
            var move = evt.direction switch
            {
                NavigationMoveEvent.Direction.Right => new Vector2(0f, step),
                NavigationMoveEvent.Direction.Left => new Vector2(0f, -step),
                NavigationMoveEvent.Direction.Up => new Vector2(-step, 0f),
                NavigationMoveEvent.Direction.Down => new Vector2(step, 0f),
                _ => Vector2.zero
            };
            if (move == Vector2.zero)
                return;

            var area = Rect.MinMaxRect(-BagelLevel.halfWidth + step, BagelLevel.clothStart + step, BagelLevel.halfWidth - step, m_Level.length + BagelLevel.clothBeyond - step);
            var next = m_Cursor + move;
            // Off the counter's sides, the focus goes on to the controls above and below.
            if (m_Held < 0 && (next.x < area.xMin - 0.01f || next.x > area.xMax + 0.01f))
                return;
            next = new Vector2(Mathf.Clamp(next.x, area.xMin, area.xMax), Mathf.Clamp(next.y, area.yMin, area.yMax));
            m_Cursor = BagelLevel.Snap(next);
            if (m_Held >= 0)
            {
                m_HeldPiece.x = m_Cursor.x;
                m_HeldPiece.z = m_Cursor.y;
            }
            else
            {
                m_Selected = m_Level.PieceAt(m_Cursor, m_Library);
            }
            UpdateSelection();
            KeepInView(m_CursorElement);
            m_Map.focusController?.IgnoreEvent(evt);
            evt.StopPropagation();
        }

        void OnMapNavigationSubmit(NavigationSubmitEvent evt)
        {
            if (m_Level == null)
                return;
            if (m_Held >= 0)
            {
                PutDown();
            }
            else
            {
                var index = m_Level.PieceAt(m_Cursor, m_Library);
                if (index >= 0)
                    PickUp(index);
                else if (!PlaceAt(m_Cursor))
                    Refuse();
            }
            evt.StopPropagation();
        }

        void OnMapNavigationCancel(NavigationCancelEvent evt)
        {
            if (m_Held < 0)
                return;
            // Put back where it was.
            m_Held = -1;
            m_Cursor = m_Level.pieces[m_Selected].position;
            m_Map.Refresh(true);
            UpdateSelection();
            evt.StopPropagation();
        }

        void OnMapKeyDown(KeyDownEvent evt)
        {
            var editorKeys = panel != null && panel.contextType == ContextType.Editor;
            switch (evt.keyCode)
            {
                case KeyCode.R:
                    Turn(evt.shiftKey ? -1 : 1);
                    break;
                case KeyCode.Q when editorKeys:
                    Turn(-1);
                    break;
                case KeyCode.E when editorKeys:
                    Turn(1);
                    break;
                case KeyCode.Delete:
                case KeyCode.Backspace:
                    RemoveSelected();
                    break;
                default:
                    return;
            }
            evt.StopPropagation();
        }

        void PickUp(int index)
        {
            m_Held = index;
            m_Selected = index;
            m_HeldPiece = m_Level.pieces[index];
            m_Cursor = m_HeldPiece.position;
            UpdateSelection();
        }

        void PutDown()
        {
            var index = m_Held;
            var piece = m_HeldPiece;
            var entry = m_Library?.Find(piece.kind);
            if (entry == null || !m_Level.CanPlace(piece.position, entry.HalfExtents(piece.angle)))
            {
                Refuse();
                return;
            }
            m_Held = -1;
            Edit(() => m_Level.Set(index, piece));
            Select(index);
        }

        // Can't do that: a shake.
        void Refuse()
        {
            AddToClassList(invalidUssClassName);
            schedule.Execute(() => RemoveFromClassList(invalidUssClassName)).StartingIn(250);
        }

        void ShowCursor(bool show)
        {
            m_CursorShown = show;
            if (!show && m_Held >= 0)
            {
                m_Held = -1;
                m_Map.Refresh(true);
            }
            UpdateSelection();
        }

        // ------------------------------------------------------------------ Pointer

        Vector2 CounterAt(Vector3 panelPosition) => m_Map.ToCounter(m_Map.WorldToLocal(panelPosition));

        void OnMapPointerDown(PointerDownEvent evt)
        {
            if (m_Level == null || evt.button != 0)
                return;
            var at = CounterAt(evt.position);
            m_Cursor = BagelLevel.Snap(at);
            var index = m_Level.PieceAt(at, m_Library);
            if (index >= 0)
            {
                Select(index);
                m_Dragged = index;
                m_DragOffset = m_Level.pieces[index].position - at;
                m_DragTo = m_Level.pieces[index].position;
                m_DragStart = evt.position;
                m_DragMoved = false;
                m_DragPointer = evt.pointerId;
                m_Map.CapturePointer(evt.pointerId);
            }
            else if (m_Brush != null)
            {
                if (!PlaceAt(at))
                    Refuse();
            }
            else
            {
                Select(-1);
            }
            m_Map.Focus();
            evt.StopPropagation();
        }

        void OnMapPointerMove(PointerMoveEvent evt)
        {
            if (m_Level == null)
                return;
            var at = CounterAt(evt.position);
            if (m_Dragged >= 0 && evt.pointerId == m_DragPointer)
            {
                if (!m_DragMoved && Vector2.Distance(evt.position, m_DragStart) < k_ClickSlop)
                    return;
                m_DragMoved = true;
                var piece = m_Level.pieces[m_Dragged];
                var entry = m_Library?.Find(piece.kind);
                if (entry == null)
                    return;
                var half = entry.HalfExtents(piece.angle);
                var to = BagelLevel.SnapInside(at + m_DragOffset, m_Level.PlaceableArea(half));
                m_DragTo = to;
                var element = m_Map.pieceElements[m_Dragged];
                m_Map.Place(element, to, entry.size, piece.angle);
                element.EnableInClassList(invalidUssClassName, !m_Level.CanPlace(to, half));
                KeepInView(element);
                return;
            }
            if (m_Brush != null && m_Dragged < 0)
                UpdateGhost(at);
        }

        void OnMapPointerUp(PointerUpEvent evt)
        {
            if (m_Dragged < 0 || evt.pointerId != m_DragPointer)
                return;
            var index = m_Dragged;
            var moved = m_DragMoved;
            var to = m_DragTo;
            EndDrag();
            if (moved && !Move(index, to))
                Refuse();
            m_Map.Refresh(true);
            UpdateSelection();
            evt.StopPropagation();
        }

        void CancelDrag()
        {
            if (m_Dragged < 0)
                return;
            EndDrag();
            m_Map.Refresh(true);
            UpdateSelection();
        }

        void EndDrag()
        {
            if (m_Dragged >= 0 && m_Dragged < m_Map.pieceElements.Count)
                m_Map.pieceElements[m_Dragged].RemoveFromClassList(invalidUssClassName);
            if (m_DragPointer >= 0 && m_Map.HasPointerCapture(m_DragPointer))
                m_Map.ReleasePointer(m_DragPointer);
            m_Dragged = -1;
            m_DragPointer = -1;
        }

        // The library: a click chooses (or unchooses) a thing; dragged sideways it spins; dragged up onto the counter
        // it's put down where it's let go.
        void OnItemPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || evt.currentTarget is not VisualElement item)
                return;
            m_SpinItem = item;
            m_SpinStart = evt.position;
            m_SpinMoved = false;
            m_PlacingFromLibrary = false;
            item.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        void OnItemPointerMove(PointerMoveEvent evt)
        {
            if (m_SpinItem == null || evt.currentTarget != m_SpinItem || !m_SpinItem.HasPointerCapture(evt.pointerId))
                return;
            if (!m_SpinMoved && Vector2.Distance(evt.position, m_SpinStart) < k_ClickSlop)
                return;
            m_SpinMoved = true;
            var kind = (string)m_SpinItem.userData;
            var overBoard = m_Board.worldBound.Contains(evt.position);
            if (overBoard)
            {
                if (!m_PlacingFromLibrary)
                {
                    m_PlacingFromLibrary = true;
                    if (m_Brush != kind)
                        ChooseBrush(kind);
                }
                UpdateGhost(CounterAt(evt.position));
            }
            else if (!m_PlacingFromLibrary)
            {
                m_Thumbnails?.Spin(kind, evt.deltaPosition.x * k_SpinPerPixel);
            }
            else
            {
                ShowGhost(false);
            }
            evt.StopPropagation();
        }

        void OnItemPointerUp(PointerUpEvent evt)
        {
            if (m_SpinItem == null || evt.currentTarget != m_SpinItem)
                return;
            var item = m_SpinItem;
            m_SpinItem = null;
            if (item.HasPointerCapture(evt.pointerId))
                item.ReleasePointer(evt.pointerId);
            var kind = (string)item.userData;
            if (!m_SpinMoved)
                ChooseBrush(kind);
            else if (m_PlacingFromLibrary && m_Board.worldBound.Contains(evt.position) && !PlaceAt(CounterAt(evt.position)))
                Refuse();
            m_PlacingFromLibrary = false;
            ShowGhost(false);
            evt.StopPropagation();
        }

        void OnItemNavigationSubmit(NavigationSubmitEvent evt)
        {
            if (evt.currentTarget is not VisualElement item)
                return;
            ChooseBrush((string)item.userData);
            if (m_Brush != null)
                m_Map.Focus();
            evt.StopPropagation();
        }

        // ------------------------------------------------------------------ Showing it

        void BuildLibrary()
        {
            m_LibraryView.Clear();
            m_Items.Clear();
            if (m_Library == null)
                return;
            foreach (var entry in m_Library.entries)
            {
                var item = new VisualElement { name = entry.id, focusable = true, tabIndex = 0, userData = entry.id };
                item.AddToClassList(itemUssClassName);
                item.AddToClassList(BagelAccessibility.itemUssClassName);
                var picture = new Image { name = "thumbnail", pickingMode = PickingMode.Ignore, scaleMode = ScaleMode.ScaleToFit };
                picture.AddToClassList(thumbnailUssClassName);
                item.Add(picture);
                var label = new Label { name = "item-name", pickingMode = PickingMode.Ignore };
                label.AddToClassList(itemNameUssClassName);
                item.Add(label);
                item.RegisterCallback<PointerDownEvent>(OnItemPointerDown);
                item.RegisterCallback<PointerMoveEvent>(OnItemPointerMove);
                item.RegisterCallback<PointerUpEvent>(OnItemPointerUp);
                item.RegisterCallback<NavigationSubmitEvent>(OnItemNavigationSubmit);
                item.RegisterCallback<FocusInEvent>(_ => KeepInView(item));
                m_LibraryView.Add(item);
                m_Items.Add(item);
            }
            UpdateThumbnails();
            UpdateFields();
        }

        void UpdateThumbnails()
        {
            foreach (var item in m_Items)
            {
                var kind = (string)item.userData;
                var picture = item.Q<Image>("thumbnail");
                var entry = m_Library?.Find(kind);
                if (m_Thumbnails != null && m_Thumbnails.texture != null)
                {
                    picture.image = m_Thumbnails.texture;
                    picture.uv = m_Thumbnails.UV(kind);
                    picture.style.backgroundColor = StyleKeyword.Null;
                }
                else
                {
                    // No pictures (a test, a window without its studio): the map's color.
                    picture.image = null;
                    picture.style.backgroundColor = entry != null ? entry.color : Color.gray;
                }
            }
        }

        void UpdateFields()
        {
            m_NameField.SetValueWithoutNotify(m_Level != null ? m_Level.displayName : "");
            m_NameField.label = BagelLocalization.Get("levels.name", "Name");
            if (m_Level != null)
            {
                var meters = BagelLocalization.LocalizeDigits(m_Level.length.ToString("0.#", BagelLocalization.culture));
                m_LengthLabel.text = string.Format(BagelLocalization.culture, BagelLocalization.Get("levels.length", "{0} m"), meters);
            }
            m_ShorterButton.SetEnabled(m_Level != null && m_Level.length > BagelLevel.minLength);
            m_LongerButton.SetEnabled(m_Level != null && m_Level.length < BagelLevel.maxLength);
            m_DeleteButton.text = BagelLocalization.Get("levels.delete", "Delete table");
            m_DoneButton.text = BagelLocalization.Get("levels.done", "Done");
            m_TurnButton.text = BagelLocalization.Get("levels.turn", "Turn");
            m_RemoveButton.text = BagelLocalization.Get("levels.remove", "Remove");
            foreach (var item in m_Items)
            {
                var entry = m_Library?.Find((string)item.userData);
                if (entry != null)
                    item.tooltip = item.Q<Label>("item-name").text = BagelLocalization.Get(entry.nameKey, entry.displayName);
            }
            UpdateHint();
        }

        void UpdateHint()
        {
            var entry = m_Library?.Find(m_Brush);
            m_Hint.text = entry != null
                ? string.Format(BagelLocalization.culture, BagelLocalization.Get("levels.hint.place", "Click the counter to put down: {0}"), BagelLocalization.Get(entry.nameKey, entry.displayName))
                : BagelLocalization.Get("levels.hint.choose", "Choose a thing below, or drag one onto the counter");
        }

        void UpdateSelection()
        {
            var count = m_Map.pieceElements.Count;
            for (int i = 0; i < count; i++)
            {
                var element = m_Map.pieceElements[i];
                element.EnableInClassList(selectedPieceUssClassName, i == m_Selected);
                element.EnableInClassList(heldPieceUssClassName, i == m_Held);
            }
            if (m_Held >= 0 && m_Held < count)
            {
                var entry = m_Library?.Find(m_HeldPiece.kind);
                if (entry != null)
                {
                    m_Map.Place(m_Map.pieceElements[m_Held], m_HeldPiece.position, entry.size, m_HeldPiece.angle);
                    m_Map.pieceElements[m_Held].EnableInClassList(invalidUssClassName, !m_Level.CanPlace(m_HeldPiece.position, entry.HalfExtents(m_HeldPiece.angle)));
                }
            }

            var canAct = m_Level != null && (m_Held >= 0 || (m_Selected >= 0 && m_Selected < count));
            m_RemoveButton.SetEnabled(canAct);
            m_TurnButton.SetEnabled(canAct || m_Brush != null);

            m_CursorElement.style.display = m_CursorShown && m_Level != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (m_CursorShown)
                m_Map.Place(m_CursorElement, m_Cursor, Vector2.one * BagelLevel.cellSize, 0f);
            UpdateGhost();
        }

        // The chosen thing, where it'd go: under the pointer, or at the keyboard's cursor.
        void UpdateGhost(Vector2? at = null)
        {
            var entry = m_Library?.Find(m_Brush);
            if (entry == null || m_Level == null || (at == null && !m_CursorShown) || m_Held >= 0)
            {
                ShowGhost(false);
                return;
            }
            var position = BagelLevel.Snap(at ?? m_Cursor);
            if (at == null && m_Level.PieceAt(position, m_Library) >= 0)
            {
                ShowGhost(false);
                return;
            }
            ShowGhost(true);
            m_Ghost.EnableInClassList(BagelLevelMap.roundPieceUssClassName, entry.round);
            m_Ghost.EnableInClassList(invalidUssClassName, !m_Level.CanPlace(position, entry.HalfExtents(m_BrushAngle)));
            m_Ghost.style.backgroundColor = entry.color;
            m_Map.Place(m_Ghost, position, entry.size, m_BrushAngle);
        }

        void ShowGhost(bool show) => m_Ghost.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;

        void KeepInView(VisualElement element)
        {
            var scrollView = element.GetFirstAncestorOfType<ScrollView>();
            if (scrollView != null && element.layout.width > 0f)
                scrollView.ScrollTo(element);
        }
    }
}
