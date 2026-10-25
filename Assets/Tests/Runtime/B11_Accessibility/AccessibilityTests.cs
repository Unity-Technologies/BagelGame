using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B11_Accessibility
{
    // What a screen reader gets from each screen: roles, labels, values, and what invoking a node does.
    public class AccessibilityTests : UITestFixture
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

        static List<AccessibilityNode> AllNodes(AccessibilityHierarchy hierarchy)
        {
            var nodes = new List<AccessibilityNode>();
            void Add(IReadOnlyList<AccessibilityNode> list)
            {
                foreach (var node in list)
                {
                    nodes.Add(node);
                    Add(node.children);
                }
            }
            Add(hierarchy.rootNodes);
            return nodes;
        }

        static AccessibilityNode Find(AccessibilityHierarchy hierarchy, string label) =>
            AllNodes(hierarchy).Find(node => node.label == label);

        [Test]
        public void TheMainMenuReadsItsButtons()
        {
            m_BagelTestAssetList.mainMenuScreenUxml.CloneTree(rootVisualElement);
            simulate.FrameUpdate();
            var screen = rootVisualElement.Q<MainMenuScreenManager>();

            var hierarchy = BagelAccessibility.Build(screen, null);

            foreach (var label in new[] { "Play", "Settings", "Leaderboard", "Exit" })
            {
                var node = Find(hierarchy, label);
                Assert.IsNotNull(node, label);
                Assert.AreEqual(AccessibilityRole.Button, node.role, label);
            }
            Assert.AreEqual(AccessibilityRole.Header, Find(hierarchy, "Bagel").role);

            // The settings and leaderboard pages are in the background: nothing in them is read.
            Assert.IsNull(Find(hierarchy, "Field of View"));
        }

        [Test]
        public void InvokingPlayStartsTheBagelSelection()
        {
            m_BagelTestAssetList.mainMenuScreenUxml.CloneTree(rootVisualElement);
            simulate.FrameUpdate();
            var screen = rootVisualElement.Q<MainMenuScreenManager>();
            var hierarchy = BagelAccessibility.Build(screen, null);

            Assert.IsTrue(InvokeNode(Find(hierarchy, "Play")));

            Assert.IsTrue(screen.state.isBagelSelection);
        }

        [Test]
        public void LongPressesNeedNoHoldWithAScreenReader()
        {
            m_BagelTestAssetList.pausePaneUxml.CloneTree(rootVisualElement);
            simulate.FrameUpdate();
            var pane = rootVisualElement.Q<PausePaneManager>();
            pane.state.GoToPlay();
            pane.state.Pause();

            var hierarchy = BagelAccessibility.Build(pane, null);
            Assert.IsTrue(InvokeNode(Find(hierarchy, "Main Menu")));

            Assert.IsTrue(pane.state.isMainMenu);
        }

        [Test]
        public void SettingsAreAdjustable()
        {
            m_BagelTestAssetList.settingsPaneUxml.CloneTree(rootVisualElement);
            simulate.FrameUpdate();
            var pane = rootVisualElement.Q<SettingsPaneManager>();
            var settings = pane.state.settings;

            var hierarchy = BagelAccessibility.Build(pane, null);
            var tabs = AllNodes(hierarchy).FindAll(node => node.role == AccessibilityRole.TabButton);
            Assert.AreEqual(5, tabs.Count);

            var fieldOfView = Find(hierarchy, "Field of View");
            Assert.AreEqual(AccessibilityRole.Slider, fieldOfView.role);
            IncrementNode(fieldOfView);
            simulate.FrameUpdate();
            Assert.AreEqual(63f, settings.fieldOfView, 0.01f);

            // Open the Look tab, the way a screen reader user would. The theme cards read as a dropdown.
            InvokeNode(tabs[1]);
            simulate.FrameUpdate();
            hierarchy = BagelAccessibility.Build(pane, null);

            var theme = Find(hierarchy, "Theme");
            Assert.AreEqual(AccessibilityRole.Dropdown, theme.role);
            IncrementNode(theme);
            simulate.FrameUpdate();
            Assert.AreEqual(1, settings.themeIndex);

            var reduceMotion = Find(hierarchy, "Reduce Motion");
            Assert.AreEqual(AccessibilityRole.Toggle, reduceMotion.role);
            InvokeNode(reduceMotion);
            simulate.FrameUpdate();
            Assert.IsTrue(settings.reduceMotion);
        }

        // What a screen reader does when the player activates or adjusts a node.
        static bool InvokeNode(AccessibilityNode node) => BagelAccessibility.Invoke(node);
        static void IncrementNode(AccessibilityNode node) => BagelAccessibility.Increment(node);
    }
}
