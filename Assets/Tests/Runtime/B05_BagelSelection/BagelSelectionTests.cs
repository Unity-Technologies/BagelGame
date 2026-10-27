using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B05_BagelSelection
{
    public class BagelSelectionTests : UITestFixture
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

        BagelSelectionControlsPaneManager ShowControls()
        {
            m_BagelTestAssetList.bagelSelectionControlsUxml.CloneTree(rootVisualElement);
            var pane = rootVisualElement.Q<BagelSelectionControlsPaneManager>();
            pane.state.GoToBagelSelection();
            simulate.FrameUpdate();
            return pane;
        }

        static void Send(VisualElement target, EventBase evt)
        {
            evt.target = target;
            target.SendEvent(evt);
            evt.Dispose();
        }

        // The bagels go round: after the last is the first, and before the first the last.
        [Test]
        public void ArrowsGoRoundTheBagels()
        {
            var pane = ShowControls();
            var state = pane.state;
            Assert.AreEqual(0, state.selectedBagelIndex);
            Assert.IsTrue(pane.leftButton.enabledSelf);
            Assert.IsTrue(pane.rightButton.enabledSelf);

            for (int i = 1; i <= state.bagelTypeCount; ++i)
            {
                simulate.Click(pane.rightButton);
                simulate.FrameUpdate();
                Assert.AreEqual(i % state.bagelTypeCount, state.selectedBagelIndex);
            }

            simulate.Click(pane.leftButton);
            simulate.FrameUpdate();
            Assert.AreEqual(state.bagelTypeCount - 1, state.selectedBagelIndex);
            simulate.Click(pane.rightButton);
            simulate.FrameUpdate();
        }

        // The register's display shows the chosen bagel, in the language.
        [Test]
        public void TheRegisterShowsTheChosenBagel()
        {
            var pane = ShowControls();
            var state = pane.state;
            var first = state.selectedBagelType;
            Assert.AreEqual(BagelLocalization.Get(first.nameKey, first.displayName), pane.displayName.text);

            simulate.Click(pane.rightButton);
            simulate.FrameUpdate();
            var second = state.selectedBagelType;
            Assert.AreNotEqual(first, second);
            Assert.AreEqual(BagelLocalization.Get(second.nameKey, second.displayName), pane.displayName.text);

            simulate.Click(pane.leftButton);
            simulate.FrameUpdate();
        }

        // The register's face keeps the focus: its keys don't take it when clicked, a click elsewhere gives it back,
        // and a key sent to the panel itself (when a click on nothing left no panel in focus) still turns the bagels.
        [Test]
        public void TheRegisterKeepsTheFocus()
        {
            var pane = ShowControls();
            var state = pane.state;
            pane.Focus();
            simulate.FrameUpdate();
            Assert.IsFalse(pane.rightButton.focusable, "keys don't take the focus");

            simulate.Click(pane.rightButton);
            simulate.FrameUpdate();
            Assert.AreEqual(1, state.selectedBagelIndex);
            Assert.AreEqual(pane, pane.focusController.focusedElement, "clicking a key keeps the face in focus");

            pane.Blur();
            simulate.FrameUpdate();
            simulate.FrameUpdate();
            Assert.AreEqual(pane, pane.focusController.focusedElement, "the focus comes back");

            var root = pane.panel.visualTree;
            Send(root, NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Left));
            Assert.AreEqual(0, state.selectedBagelIndex, "a key sent to the panel itself turns the bagels");
        }

        [Test]
        public void OrderGoesToTheTabletAndBackLeaves()
        {
            var pane = ShowControls();
            simulate.Click(pane.selectButton);
            Assert.IsTrue(pane.state.isLevelSelection, "on to the payment tablet");

            pane.state.GoToBagelSelection();
            simulate.Click(pane.backButton);
            Assert.IsTrue(pane.state.isMainMenu);
        }

        [Test]
        public void GamepadAndKeyboardPickTheBagel()
        {
            var pane = ShowControls();
            var state = pane.state;

            Send(pane, NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Right));
            Assert.AreEqual(1, state.selectedBagelIndex);
            Assert.IsTrue(pane.rightButton.ClassListContains(BagelSelectionControlsPaneManager.keyPressedUssClassName), "the key goes down");
            Send(pane, NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Left));
            Assert.AreEqual(0, state.selectedBagelIndex);

            Send(pane, NavigationCancelEvent.GetPooled());
            Assert.IsTrue(state.isMainMenu);

            state.GoToBagelSelection();
            Send(pane, NavigationSubmitEvent.GetPooled());
            Assert.IsTrue(state.isLevelSelection);
        }

        // The face is the register's one stop for Tab: Tab and Shift+Tab turn to the bagel after and before, as right
        // and left do, and the face keeps the focus.
        [Test]
        public void TabTurnsToTheNextBagel()
        {
            var pane = ShowControls();
            var state = pane.state;
            pane.Focus();

            Send(pane, NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Next));
            Assert.AreEqual(1, state.selectedBagelIndex);
            Assert.IsTrue(pane.rightButton.ClassListContains(BagelSelectionControlsPaneManager.keyPressedUssClassName), "> goes down");
            Send(pane, NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Previous));
            Send(pane, NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Previous));
            Assert.AreEqual(state.bagelTypeCount - 1, state.selectedBagelIndex, "round to the last");
            simulate.FrameUpdate();
            Assert.AreEqual(pane, rootVisualElement.focusController.focusedElement);
        }

        [Test]
        public void TheCardShowsTheBagelsStats()
        {
            var bagelType = m_BagelTestAssetList.startState.bagelTypes.collection[1];
            m_BagelTestAssetList.bagelSelectionTypeInfoUxml.CloneTree(rootVisualElement);
            var card = rootVisualElement.Q("pane");
            card.dataSource = bagelType;
            simulate.FrameUpdate();

            Assert.AreEqual(bagelType.displayName, card.Q<Label>("name").text);
            Assert.AreEqual(bagelType.toppingsPercentile, card.Q<StatMeter>("toppings-meter").value, 1e-4f);
            Assert.AreEqual(bagelType.toppingLossPercentile, card.Q<StatMeter>("topping-loss-meter").value, 1e-4f);
            Assert.AreEqual(bagelType.controlPercentile, card.Q<StatMeter>("control-meter").value, 1e-4f);
            Assert.AreEqual(bagelType.speedPercentile, card.Q<StatMeter>("speed-meter").value, 1e-4f);
            Assert.AreEqual(bagelType.gripPercentile, card.Q<StatMeter>("grip-meter").value, 1e-4f);
            Assert.IsNotNull(card.Q(className: "b-icon--stat-grip"), "each stat has its icon");
        }
    }
}
