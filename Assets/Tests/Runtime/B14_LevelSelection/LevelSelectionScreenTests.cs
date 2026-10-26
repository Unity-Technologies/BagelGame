using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B14_LevelSelection
{
    // The payment tablet's screen on its own: a row for each level with its map, the chosen one the focused one, Pay &
    // Play, Back, and New Table and Edit opening the level editor on the player's own levels only.
    public class LevelSelectionScreenTests : UITestFixture
    {
        BagelTestAssetList m_BagelTestAssetList;
        LevelSelectionScreenManager m_Screen;

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
            simulate.FrameUpdate();
            simulate.FrameUpdate();
        }

        [TearDown]
        public void TearDown() => rootVisualElement.Clear();

        static void Send(VisualElement target, EventBase evt)
        {
            using (evt)
            {
                evt.target = target;
                target.SendEvent(evt);
            }
        }

        [Test]
        public void EveryLevelHasARowWithItsMap()
        {
            Assert.AreEqual(state.levelCount, m_Screen.rows.Count);
            for (int i = 0; i < state.levelCount; i++)
            {
                var row = m_Screen.rows[i];
                Assert.AreEqual(LevelSelectionScreenManager.LevelName(state.levels[i]), row.Q<Label>("name").text);
                var map = row.Q<BagelLevelMap>();
                Assert.AreEqual(state.levels[i], map.level);
                Assert.AreEqual(state.levels[i].pieceCount, map.pieceElements.Count, "every piece on its map");
            }
            Assert.Greater(m_Screen.rows[2].Q<BagelLevelMap>().resolvedStyle.width, m_Screen.rows[0].Q<BagelLevelMap>().resolvedStyle.width, "a longer level, a longer strip");
            Assert.IsTrue(m_Screen.rows[0].ClassListContains(LevelSelectionScreenManager.selectedRowUssClassName));
            Assert.AreEqual(m_Screen.rows[0], m_Screen.focusController.focusedElement, "the chosen level has the focus");
        }

        [Test]
        public void FocusingARowChoosesItsLevelAndSubmitPlaysIt()
        {
            m_Screen.rows[2].Focus();
            simulate.FrameUpdate();
            Assert.AreEqual(2, state.selectedLevelIndex);
            Assert.IsTrue(m_Screen.rows[2].ClassListContains(LevelSelectionScreenManager.selectedRowUssClassName));
            Assert.IsFalse(m_Screen.rows[0].ClassListContains(LevelSelectionScreenManager.selectedRowUssClassName));

            Send(m_Screen.rows[2], NavigationSubmitEvent.GetPooled());
            Assert.IsTrue(state.isPlaying);
            Assert.AreEqual(2, state.selectedLevelIndex, "playing the chosen level");
        }

        [Test]
        public void BackGoesBackToTheBagels()
        {
            simulate.Click(m_Screen.backButton);
            Assert.IsTrue(state.isBagelSelection);

            state.GoToLevelSelection();
            simulate.FrameUpdate();
            Send(m_Screen.rows[0], NavigationCancelEvent.GetPooled());
            Assert.IsTrue(state.isBagelSelection, "and Cancel, from anywhere on the tablet");
        }

        [Test]
        public void OnlyThePlayersOwnLevelsAreEdited()
        {
            Assert.IsFalse(m_Screen.editButton.enabledSelf, "the game's own levels are edited in the Editor");

            simulate.Click(m_Screen.newButton);
            simulate.FrameUpdate();
            Assert.AreEqual(4, state.levelCount, "a new level, after the others");
            Assert.IsTrue(state.selectedLevel.custom);
            Assert.IsTrue(state.isEditingLevel, "straight into the editor");
            Assert.IsTrue(m_Screen.ClassListContains(LevelSelectionScreenManager.editingUssClassName));
            Assert.AreEqual(DisplayStyle.Flex, m_Screen.editor.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, m_Screen.Q("levels-page").resolvedStyle.display);
            Assert.AreEqual(state.selectedLevel, m_Screen.editor.level);
            Assert.AreEqual(4, m_Screen.rows.Count, "and a row for it");

            simulate.Click(m_Screen.editor.doneButton);
            simulate.FrameUpdate();
            simulate.FrameUpdate();
            Assert.IsFalse(state.isEditingLevel);
            Assert.AreEqual(DisplayStyle.None, m_Screen.editor.resolvedStyle.display);
            Assert.IsTrue(m_Screen.editButton.enabledSelf, "the player's own can be edited again");
            Assert.AreEqual(m_Screen.rows[3], m_Screen.focusController.focusedElement, "back on its row");
        }

        [Test]
        public void CancelInTheEditorGoesBackToTheList()
        {
            m_Screen.NewLevel();
            simulate.FrameUpdate();
            Send(m_Screen.editor.doneButton, NavigationCancelEvent.GetPooled());
            Assert.IsFalse(state.isEditingLevel);
            Assert.IsTrue(state.isLevelSelection, "still choosing");
        }

        [Test]
        public void HoldingDeleteTakesThePlayersLevelAway()
        {
            m_Screen.NewLevel();
            simulate.FrameUpdate();
            var delete = m_Screen.editor.deleteButton;
            Assert.AreEqual(DisplayStyle.Flex, delete.resolvedStyle.display);
            simulate.MouseDown(delete);
            for (var time = 0f; time < delete.holdTime + 0.2f; time += 0.1f)
                simulate.FrameUpdate(0.1);
            simulate.MouseUp(delete);
            simulate.FrameUpdate();

            Assert.AreEqual(3, state.levelCount, "gone");
            Assert.IsFalse(state.isEditingLevel);
            Assert.AreEqual(2, state.selectedLevelIndex, "the one before is chosen");
        }
    }
}
