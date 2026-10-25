using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B04_Settings
{
    // Settings with the keyboard or a gamepad: the tabs are a column, each shown as soon as it has the focus, Back
    // under them; across from them is the chosen tab's page, setting to setting; Cancel goes back to the tab.
    public class SettingsNavigationTests : UITestFixture
    {
        BagelTestAssetList m_BagelTestAssetList;
        SettingsPaneManager m_Settings;
        VisualElement m_Focused;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_BagelTestAssetList = Resources.Load<BagelTestAssetList>("BagelTestAssetList");
        }

        [SetUp]
        public void SetUp()
        {
            panelSize = new Vector2(800f, 500f);
            m_BagelTestAssetList.settingsPaneUxml.CloneTree(rootVisualElement);
            m_Settings = rootVisualElement.Q<SettingsPaneManager>();
            // The element itself that has the focus (a field hands it on to its input, a row of choices to a choice):
            // navigation events go to it.
            m_Settings.RegisterCallback<FocusInEvent>(evt => m_Focused = evt.target as VisualElement, TrickleDown.TrickleDown);
            m_Settings.tabView.activeTab = m_Settings.tabs[0];
            simulate.FrameUpdate();
            simulate.FrameUpdate();
            m_Settings.FocusTabs();
        }

        [TearDown]
        public void TearDown()
        {
            rootVisualElement.Clear();
        }

        void Move(NavigationMoveEvent.Direction direction)
        {
            using (var evt = NavigationMoveEvent.GetPooled(direction))
            {
                evt.target = m_Focused;
                m_Focused.SendEvent(evt);
            }
            simulate.FrameUpdate();
        }

        void Cancel()
        {
            using (var evt = NavigationCancelEvent.GetPooled())
            {
                evt.target = m_Focused;
                m_Focused.SendEvent(evt);
            }
            simulate.FrameUpdate();
        }

        Tab tab(string name) => m_Settings.tabView.Q<Tab>(name);

        [Test]
        public void TheTabsAreAColumnShownAsTheFocusGoesDownIt()
        {
            var tabs = m_Settings.tabs;
            Assert.AreEqual(tabs[0].tabHeader, m_Focused, "Settings open on the chosen tab");

            for (int i = 1; i < tabs.Count; i++)
            {
                Move(NavigationMoveEvent.Direction.Down);
                Assert.AreEqual(tabs[i].tabHeader, m_Focused);
                Assert.AreEqual(tabs[i], m_Settings.tabView.activeTab, "a tab shows as soon as it has the focus");
            }

            Move(NavigationMoveEvent.Direction.Down);
            Assert.AreEqual(m_Settings.backButton, m_Focused, "Back is under the tabs");
            Move(NavigationMoveEvent.Direction.Down);
            Assert.AreEqual(m_Settings.backButton, m_Focused, "nothing under Back");
            Move(NavigationMoveEvent.Direction.Up);
            Assert.AreEqual(tabs[^1].tabHeader, m_Focused);

            for (int i = 0; i < tabs.Count + 1; i++)
                Move(NavigationMoveEvent.Direction.Up);
            Assert.AreEqual(tabs[0].tabHeader, m_Focused, "nothing over the first tab");
            Move(NavigationMoveEvent.Direction.Left);
            Assert.AreEqual(tabs[0].tabHeader, m_Focused, "nothing before the tabs");
        }

        [Test]
        public void AcrossIntoThePageSettingBySettingAndBack()
        {
            var game = m_Settings.forGame;
            var fov = game.Q<Slider>("fov-slider");
            var sound = game.Q<Toggle>("sound-toggle");
            var volume = game.Q<Slider>("volume-slider");

            Move(NavigationMoveEvent.Direction.Right);
            Assert.IsTrue(fov.Contains(m_Focused), "the page's first setting");
            Move(NavigationMoveEvent.Direction.Down);
            Assert.IsTrue(sound.Contains(m_Focused));
            Move(NavigationMoveEvent.Direction.Down);
            Assert.IsTrue(volume.Contains(m_Focused));
            Move(NavigationMoveEvent.Direction.Down);
            var english = game.Q<Button>("language-en");
            Assert.AreEqual(english, m_Focused, "a row of choices from its first");
            Move(NavigationMoveEvent.Direction.Right);
            Assert.AreEqual(game.Q<Button>("language-fr"), m_Focused, "choice to choice");
            Move(NavigationMoveEvent.Direction.Down);
            Assert.IsInstanceOf<LongPressButton>(m_Focused);

            // Never out of the page: nothing below the last row.
            var last = m_Focused;
            Move(NavigationMoveEvent.Direction.Down);
            Assert.AreEqual(last, m_Focused);

            // Back across to the tab, from where nothing is further across in the page.
            Move(NavigationMoveEvent.Direction.Up);
            Move(NavigationMoveEvent.Direction.Left);
            Move(NavigationMoveEvent.Direction.Left);
            Assert.AreEqual(m_Settings.tabs[0].tabHeader, m_Focused);
            Assert.AreEqual(m_Settings.tabs[0], m_Settings.tabView.activeTab, "the same tab, not the one beside the row");

            // Into the page again: where the focus was in it.
            Move(NavigationMoveEvent.Direction.Right);
            Assert.AreEqual(english, m_Focused);

            // Cancel goes back to the tab, and lets Settings go from there.
            Cancel();
            Assert.AreEqual(m_Settings.tabs[0].tabHeader, m_Focused);
            var left = false;
            m_Settings.parent.RegisterCallback<NavigationCancelEvent>(_ => left = true);
            Cancel();
            Assert.IsTrue(left, "Cancel on the tabs is for whoever opened Settings");
        }

        [Test]
        public void LeftAndRightMoveASlider()
        {
            var fov = m_Settings.forGame.Q<Slider>("fov-slider");
            var before = fov.value;
            Move(NavigationMoveEvent.Direction.Right);
            Assert.IsTrue(fov.Contains(m_Focused));
            Assert.AreEqual(before, fov.value, "going to it doesn't move it");
            Move(NavigationMoveEvent.Direction.Right);
            Move(NavigationMoveEvent.Direction.Right);
            Assert.Greater(fov.value, before);
            Assert.IsTrue(fov.Contains(m_Focused), "the slider keeps the focus");
            Move(NavigationMoveEvent.Direction.Left);
            Move(NavigationMoveEvent.Direction.Left);
            Assert.AreEqual(before, fov.value, 1e-3f);
            Assert.IsTrue(fov.Contains(m_Focused));
        }

        [Test]
        public void TheThemeCardsAreARow()
        {
            Move(NavigationMoveEvent.Direction.Down);
            Assert.AreEqual(tab("ui-settings-tab"), m_Settings.tabView.activeTab);
            Move(NavigationMoveEvent.Direction.Right);
            var ui = m_Settings.forUI;
            Assert.AreEqual(ui.GetThemeCard(0), m_Focused);
            Move(NavigationMoveEvent.Direction.Right);
            Assert.AreEqual(ui.GetThemeCard(1), m_Focused);
            Move(NavigationMoveEvent.Direction.Down);
            Assert.IsTrue(ui.Q<Toggle>("reduce-motion-toggle").Contains(m_Focused));
        }

        // The shoulder buttons, or Q and E: the tab before or after, round the ends, from anywhere in the window.
        [Test]
        public void TheShoulderButtonsChangeTab()
        {
            Assert.IsNotNull(InputSystem.actions?.FindAction(SettingsPaneManager.previousTabActionName), "a UI action for the tab before");
            Assert.IsNotNull(InputSystem.actions?.FindAction(SettingsPaneManager.nextTabActionName), "a UI action for the tab after");

            var tabs = m_Settings.tabs;
            m_Settings.ShowPreviousTab();
            Assert.AreEqual(tabs[^1], m_Settings.tabView.activeTab, "round the end");
            Assert.AreEqual(tabs[^1].tabHeader, m_Focused, "on the tabs, the focus goes with the tab");
            m_Settings.ShowNextTab();
            Assert.AreEqual(tabs[0], m_Settings.tabView.activeTab);

            // From a setting, to the new tab's first setting, once it's shown.
            Move(NavigationMoveEvent.Direction.Right);
            m_Settings.ShowNextTab();
            simulate.FrameUpdate();
            simulate.FrameUpdate();
            Assert.AreEqual(tabs[1], m_Settings.tabView.activeTab);
            Assert.AreEqual(m_Settings.forUI.GetThemeCard(0), m_Focused);
        }
    }
}
