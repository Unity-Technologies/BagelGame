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

        [Test]
        public void ArrowsPickTheBagelAndStopAtTheEnds()
        {
            var pane = ShowControls();
            var state = pane.state;
            Assert.AreEqual(0, state.selectedBagelIndex);
            Assert.IsFalse(pane.leftButton.enabledSelf);

            for (int i = 1; i < state.bagelTypeCount; ++i)
            {
                simulate.Click(pane.rightButton);
                simulate.FrameUpdate();
                Assert.AreEqual(i, state.selectedBagelIndex);
            }
            Assert.IsFalse(pane.rightButton.enabledSelf);
            Assert.IsTrue(pane.leftButton.enabledSelf);

            simulate.Click(pane.leftButton);
            simulate.FrameUpdate();
            Assert.AreEqual(state.bagelTypeCount - 2, state.selectedBagelIndex);
        }

        [Test]
        public void SelectPlaysAndBackLeaves()
        {
            var pane = ShowControls();
            simulate.Click(pane.selectButton);
            Assert.IsTrue(pane.state.isPlaying);

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
            Send(pane, NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Left));
            Assert.AreEqual(0, state.selectedBagelIndex);

            Send(pane, NavigationCancelEvent.GetPooled());
            Assert.IsTrue(state.isMainMenu);

            state.GoToBagelSelection();
            Send(pane, NavigationSubmitEvent.GetPooled());
            Assert.IsTrue(state.isPlaying);
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
