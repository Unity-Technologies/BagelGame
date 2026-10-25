using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B03_MainMenu
{
    public class MainMenuScreenTests : UITestFixture
    {
        BagelTestAssetList m_BagelTestAssetList;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_BagelTestAssetList = Resources.Load<BagelTestAssetList>("BagelTestAssetList");
        }

        [TearDown]
        public void TearDown()
        {
            rootVisualElement.Clear();
        }

        [Test]
        public void MainMenu()
        {
            m_BagelTestAssetList.mainMenuUxml.CloneTree(rootVisualElement);
            simulate.FrameUpdate();

            bool playClicked = false;
            bool exitClicked = false;

            // Get the elements.
            var mainMenuPaneManager = rootVisualElement.Q<MainMenuPaneManager>();
            mainMenuPaneManager.BindUI(new MainMenuPaneManager.Callbacks
            {
                onPlay = () => playClicked = true,
                onExit = () => exitClicked = true
            });

            // Play Button
            simulate.Click(mainMenuPaneManager.playButton);
            simulate.FrameUpdate();
            Assert.IsTrue(playClicked);

            // Exit Button
            simulate.Click(mainMenuPaneManager.exitButton);
            simulate.FrameUpdate();
            Assert.IsTrue(exitClicked);
        }

        // The chalk filter's grain starts at the corner of the menu's texture, which the menu pins to its own corner by
        // clipping: the title, at its biggest in the hover bounce too, has to fit on the blackboard, in every theme.
        [Test]
        public void TheTitleFitsOnTheBoard()
        {
            panelSize = new Vector2(1200f, 900f);
            foreach (var theme in m_BagelTestAssetList.themes.collection)
            {
                rootVisualElement.Clear();
                var screen = m_BagelTestAssetList.mainMenuScreenUxml.Instantiate();
                screen.styleSheets.Add(theme.theme);
                rootVisualElement.Add(screen);
                var board = screen.Q("main-menu-pane-manager");
                foreach (var name in new[] { "title-bagel", "title-game" })
                    board.Q(name).style.scale = new Scale(new Vector2(1.2f, 1.2f));
                simulate.FrameUpdate();

                Assert.AreEqual(Overflow.Hidden, board.resolvedStyle.overflow, theme.themeName);
                var edges = board.worldBound;
                foreach (var part in board.Q("title-box").Query().ToList())
                {
                    var bounds = part.worldBound;
                    Assert.IsTrue(bounds.xMin >= edges.xMin && bounds.yMin >= edges.yMin && bounds.xMax <= edges.xMax && bounds.yMax <= edges.yMax,
                        $"[{theme.themeName}] {part.name} {bounds} is off the board {edges}");
                }
            }
        }

        // Away from the menu (choosing a bagel, playing), the board is only chalk on the wall: nothing on it can be
        // clicked, and it isn't faded as it is while Settings are open.
        [Test]
        public void TheBoardIsOnlyChalkAwayFromTheMenu()
        {
            m_BagelTestAssetList.mainMenuScreenUxml.CloneTree(rootVisualElement);
            var screen = rootVisualElement.Q<MainMenuScreenManager>();
            var state = screen.state;
            state.GoToMainMenu();
            simulate.FrameUpdate();
            Assert.IsTrue(screen.mainMenuPane.enabledInHierarchy);

            try
            {
                state.GoToBagelSelection();
                simulate.FrameUpdate();
                Assert.IsFalse(screen.mainMenuPane.enabledInHierarchy, "nothing to click while choosing a bagel");
                Assert.IsFalse(screen.settingsPane.enabledInHierarchy);
                Assert.IsTrue(screen.ClassListContains(MainMenuScreenManager.awayUssClassName));
                Assert.AreEqual(1f, screen.mainMenuPane.resolvedStyle.opacity, "not faded");
            }
            finally
            {
                state.GoToMainMenu();
            }
            simulate.FrameUpdate();
            Assert.IsTrue(screen.mainMenuPane.enabledInHierarchy, "back at the menu, it works again");
            Assert.IsFalse(screen.ClassListContains(MainMenuScreenManager.awayUssClassName));
        }
    }
}
