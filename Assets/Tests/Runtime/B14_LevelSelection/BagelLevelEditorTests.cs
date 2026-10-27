using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B14_LevelSelection
{
    // The level editor on the payment tablet, on a level of the player's own: things from the library put down on the
    // counter's grid by a click or a drag, moved, turned and taken off with the pointer, the keyboard or the gamepad;
    // nothing on the bagel's start or off the cloth; the level's name and length.
    public class BagelLevelEditorTests : UITestFixture
    {
        BagelTestAssetList m_BagelTestAssetList;
        LevelSelectionScreenManager m_Screen;
        BagelLevelEditor m_Editor;
        BagelLevel m_Level;
        List<string> m_Changes;

        PlayManagerState state => m_Screen.state;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_BagelTestAssetList = Resources.Load<BagelTestAssetList>("BagelTestAssetList");
        }

        [SetUp]
        public void SetUp()
        {
            panelSize = new Vector2(1000f, 700f);
            var screen = m_BagelTestAssetList.levelSelectionScreenUxml.Instantiate();
            screen.styleSheets.Add(m_BagelTestAssetList.themes.collection[0].theme);
            rootVisualElement.Add(screen);
            m_Screen = screen.Q<LevelSelectionScreenManager>();
            state.GoToBagelSelection();
            state.GoToLevelSelection();
            m_Screen.NewLevel();
            m_Level = state.selectedLevel;
            m_Editor = m_Screen.editor;
            m_Changes = new List<string>();
            m_Editor.changing += _ => m_Changes.Add("changing");
            m_Editor.changed += _ => m_Changes.Add("changed");
            simulate.FrameUpdate();
            simulate.FrameUpdate();
        }

        [TearDown]
        public void TearDown() => rootVisualElement.Clear();

        // Where a point on the counter is in the panel.
        Vector2 At(float x, float z) => m_Editor.map.LocalToWorld(m_Editor.map.ToMap(new Vector2(x, z)));

        // A library thing, scrolled into view.
        VisualElement Item(string kind)
        {
            var item = m_Editor.items[IndexOf(kind)];
            item.GetFirstAncestorOfType<ScrollView>().ScrollTo(item);
            simulate.FrameUpdate();
            return item;
        }

        int IndexOf(string kind)
        {
            for (int i = 0; i < m_Editor.items.Count; i++)
            {
                if ((string)m_Editor.items[i].userData == kind)
                    return i;
            }
            return -1;
        }

        static void Send(VisualElement target, EventBase evt)
        {
            using (evt)
            {
                evt.target = target;
                target.SendEvent(evt);
            }
        }

        [Test]
        public void EveryThingInTheLibraryIsThereWithItsName()
        {
            var library = state.levelLibrary;
            Assert.AreEqual(library.entries.Count, m_Editor.items.Count);
            foreach (var entry in library.entries)
                Assert.AreEqual(entry.displayName, Item(entry.id).Q<Label>().text);
            Assert.AreEqual(0, m_Level.pieceCount);
            Assert.AreEqual(m_Level.displayName, m_Editor.nameField.value);
        }

        [Test]
        public void ClickTheCounterToPutTheChosenThingDown()
        {
            simulate.Click(Item("Orange"));
            Assert.AreEqual("Orange", m_Editor.brush);
            Assert.IsTrue(Item("Orange").ClassListContains(BagelLevelEditor.chosenItemUssClassName));

            simulate.Click(At(2.1f, 6.2f));
            Assert.AreEqual(1, m_Level.pieceCount);
            Assert.AreEqual(new BagelLevelPiece("Orange", 2f, 6f), m_Level.pieces[0], "on the grid");
            Assert.AreEqual(0, m_Editor.selectedIndex, "and chosen");
            CollectionAssert.AreEqual(new[] { "changing", "changed" }, m_Changes, "one change");

            simulate.Click(Item("Orange"));
            Assert.IsNull(m_Editor.brush, "clicked again, nothing's chosen");
            simulate.Click(At(-3f, 10f));
            Assert.AreEqual(1, m_Level.pieceCount);
        }

        [Test]
        public void NothingGoesOnTheStartOrTheToasterAndTheClothsEdgeNudges()
        {
            m_Editor.ChooseBrush("Cookbooks");
            simulate.Click(At(0f, 0.5f));
            Assert.AreEqual(0, m_Level.pieceCount, "the bagel starts there");
            Assert.IsTrue(m_Editor.ClassListContains(BagelLevelEditor.invalidUssClassName), "a shake");
            Assert.IsFalse(m_Editor.PlaceAt(new Vector2(0f, m_Level.length)), "the toaster's there");
            Assert.IsEmpty(m_Changes);

            Assert.IsTrue(m_Editor.PlaceAt(new Vector2(5.4f, 10f)));
            Assert.AreEqual(new Vector2(3.5f, 10f), m_Level.pieces[0].position, "nudged onto the cloth, on the grid");
        }

        [Test]
        public void DragAPieceToMoveIt()
        {
            m_Level.Add(new BagelLevelPiece("Platform", -2f, 10f));
            simulate.FrameUpdate();
            m_Changes.Clear();

            simulate.MouseDown(At(-2f, 10f));
            simulate.MouseMove(At(-2f, 10f), At(0.3f, 13.2f));
            Assert.AreEqual(new Vector2(-2f, 10f), m_Level.pieces[0].position, "it's only moved when it's let go");
            simulate.MouseUp(At(0.3f, 13.2f));
            Assert.AreEqual(new Vector2(0.5f, 13f), m_Level.pieces[0].position, "snapped to the grid");
            CollectionAssert.AreEqual(new[] { "changing", "changed" }, m_Changes, "one change for the drag");
        }

        [Test]
        public void TurnAndTakeOffTheChosenPiece()
        {
            m_Level.Add(new BagelLevelPiece("BaguetteLong", 3f, 15f));
            simulate.FrameUpdate();
            simulate.Click(At(3f, 15f));
            Assert.AreEqual(0, m_Editor.selectedIndex);

            Assert.IsTrue(m_Editor.turnButton.enabledSelf, "something chosen to turn");
            simulate.Click(m_Editor.turnButton);
            Assert.IsFalse(m_Editor.ClassListContains(BagelLevelEditor.invalidUssClassName), "not refused");
            Assert.AreEqual(45f, m_Level.pieces[0].angle);
            simulate.Click(m_Editor.turnButton);
            Assert.AreEqual(90f, m_Level.pieces[0].angle);
            var turned = m_Level.pieces[0];
            Assert.IsTrue(m_Level.CanPlace(turned.position, state.levelLibrary.Find("BaguetteLong").HalfExtents(90f)), "nudged back onto the cloth, across it");

            simulate.Click(m_Editor.removeButton);
            Assert.AreEqual(0, m_Level.pieceCount);
            Assert.AreEqual(-1, m_Editor.selectedIndex);
            Assert.IsFalse(m_Editor.removeButton.enabledSelf, "nothing chosen to take off");
        }

        [Test]
        public void TheKeyboardPutsDownPicksUpAndMoves()
        {
            m_Editor.ChooseBrush("JamJar");
            m_Editor.map.Focus();
            simulate.FrameUpdate();
            var start = m_Editor.cursor;
            Send(m_Editor.map, NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Right));
            Send(m_Editor.map, NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Right));
            Send(m_Editor.map, NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Down));
            Assert.AreEqual(start + new Vector2(0.5f, 1f), m_Editor.cursor, "right is along the counter, down to the bagel's right");
            Assert.AreEqual(m_Editor.map, m_Editor.map.focusController.focusedElement, "the counter keeps the focus");

            Send(m_Editor.map, NavigationSubmitEvent.GetPooled());
            Assert.AreEqual(1, m_Level.pieceCount);
            Assert.AreEqual(m_Editor.cursor, m_Level.pieces[0].position);

            // Picked up, moved two along, turned, put down.
            Send(m_Editor.map, NavigationSubmitEvent.GetPooled());
            Assert.AreEqual(0, m_Editor.heldIndex);
            Send(m_Editor.map, NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Right));
            Send(m_Editor.map, NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Right));
            simulate.KeyDown(KeyCode.R);
            Assert.AreEqual(start + new Vector2(0.5f, 1f), m_Level.pieces[0].position, "not moved until it's put down");
            Send(m_Editor.map, NavigationSubmitEvent.GetPooled());
            Assert.AreEqual(-1, m_Editor.heldIndex);
            Assert.AreEqual(start + new Vector2(0.5f, 2f), m_Level.pieces[0].position);
            Assert.AreEqual(45f, m_Level.pieces[0].angle);

            simulate.KeyDown(KeyCode.Delete);
            Assert.AreEqual(0, m_Level.pieceCount, "Delete takes it off");
        }

        [Test]
        public void CancelPutsAPieceInHandBack()
        {
            m_Level.Add(new BagelLevelPiece("Mug", 2f, 5f));
            m_Editor.map.Focus();
            simulate.FrameUpdate();
            m_Editor.Select(0);
            Send(m_Editor.map, NavigationSubmitEvent.GetPooled());
            Assert.AreEqual(-1, m_Editor.heldIndex, "the cursor isn't on it");

            // Onto it, picked up, moved, and let go of.
            while (m_Editor.cursor.y < 5f)
                Send(m_Editor.map, NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Right));
            while (m_Editor.cursor.x < 2f)
                Send(m_Editor.map, NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Down));
            Send(m_Editor.map, NavigationSubmitEvent.GetPooled());
            Assert.AreEqual(0, m_Editor.heldIndex);
            Send(m_Editor.map, NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Right));
            m_Changes.Clear();
            Send(m_Editor.map, NavigationCancelEvent.GetPooled());
            Assert.AreEqual(-1, m_Editor.heldIndex);
            Assert.AreEqual(new Vector2(2f, 5f), m_Level.pieces[0].position);
            Assert.IsEmpty(m_Changes);
            Assert.IsTrue(state.isEditingLevel, "still editing");
        }

        [Test]
        public void DragAThingFromTheLibraryOntoTheCounter()
        {
            var item = Item("SugarCubes");
            simulate.MouseDown(item);
            simulate.MouseMove(item.worldBound.center, At(-1.9f, 8.1f));
            simulate.MouseUp(At(-1.9f, 8.1f));
            Assert.AreEqual(1, m_Level.pieceCount);
            Assert.AreEqual(new BagelLevelPiece("SugarCubes", -2f, 8f), m_Level.pieces[0]);
        }

        [Test]
        public void TheNameAndTheLengthChange()
        {
            m_Editor.nameField.value = "  Long Table ";
            Assert.AreEqual("Long Table", m_Level.displayName);
            var length = m_Level.length;
            simulate.Click(m_Editor.longerButton);
            Assert.AreEqual(length + BagelLevel.lengthStep, m_Level.length);
            Assert.AreEqual($"{m_Level.length:0.#} m", m_Editor.Q<Label>("length-value").text);

            while (m_Level.length > BagelLevel.minLength)
                simulate.Click(m_Editor.shorterButton);
            Assert.IsFalse(m_Editor.shorterButton.enabledSelf, "no shorter than the tutorial");
            simulate.FrameUpdate();
            Assert.AreEqual(m_Level.length + BagelLevel.clothBeyond - BagelLevel.clothStart, m_Editor.map.resolvedStyle.width / m_Editor.map.pixelsPerMeter, 0.5f, "the counter's as long as the level");
        }

        // Tab goes round the editor, and only what it shows: never the level list hidden behind it, or a scroll bar.
        [Test]
        public void TabGoesRoundTheEditor()
        {
            VisualElement focused = null;
            rootVisualElement.RegisterCallback<FocusInEvent>(evt => focused = evt.target as VisualElement, TrickleDown.TrickleDown);
            m_Editor.map.Focus();
            simulate.FrameUpdate();
            // It may have had the focus already, as the editor opened.
            focused ??= rootVisualElement.focusController.focusedElement as VisualElement;
            Assert.AreEqual(m_Editor.map, focused);

            var visited = new List<VisualElement>();
            for (int i = 0; i < 200; i++)
            {
                // A text field takes the Tab key itself, not the navigation event (its text could need the arrows).
                if (focused.GetFirstAncestorOfType<TextField>() != null)
                    Send(focused, KeyDownEvent.GetPooled('\t', KeyCode.Tab, EventModifiers.None));
                else
                    Send(focused, NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Next));
                simulate.FrameUpdate();
                if (focused == m_Editor.map)
                    break;
                visited.Add(focused);
            }
            Assert.AreEqual(m_Editor.map, focused, "round to the counter");

            foreach (var element in visited)
            {
                Assert.IsTrue(m_Editor.Contains(element), $"{element.name} is in the editor");
                Assert.IsNull(element.GetFirstAncestorOfType<Scroller>(), $"{element.name} isn't a scroll bar");
                for (var shown = element; shown != null; shown = shown.parent)
                    Assert.AreNotEqual(DisplayStyle.None, shown.resolvedStyle.display, $"{element.name} is shown");
            }
            CollectionAssert.Contains(visited, m_Editor.doneButton);
            CollectionAssert.IsSubsetOf(m_Editor.items, visited, "every thing in the library");
        }
    }
}
