using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B12_Sound
{
    // The UI asks for sounds by cue, and its styles (BagelSounds.uss and the themes) pick the patch.
    // No synth is plugged in, so these listen to what was asked for, in silence.
    public class SoundTests : UITestFixture
    {
        readonly List<SoundEvent> m_Played = new();
        BagelTestAssetList m_BagelTestAssetList;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_BagelTestAssetList = Resources.Load<BagelTestAssetList>("BagelTestAssetList");
        }

        [SetUp]
        public void SetUp()
        {
            BagelSound.played += m_Played.Add;
        }

        [TearDown]
        public void TearDown()
        {
            rootVisualElement.Clear();
            BagelSound.played -= m_Played.Add;
            m_Played.Clear();
            BagelAudio.tone = SoundTone.Wood;
        }

        TemplateContainer Show(VisualTreeAsset uxml, int theme = 0)
        {
            var screen = uxml.Instantiate();
            screen.styleSheets.Add(m_BagelTestAssetList.themes.collection[theme].theme);
            rootVisualElement.Add(screen);
            simulate.FrameUpdate();
            m_Played.Clear();
            return screen;
        }

        List<string> Played() => m_Played.Select(e => e.ToString()).ToList();

        static void Send(VisualElement target, EventBase evt)
        {
            evt.target = target;
            target.SendEvent(evt);
            evt.Dispose();
        }

        // Straight to the element: in a test-sized panel, a screen's panes can sit over each other.
        static void Click(VisualElement target) => Send(target, ClickEvent.GetPooled());

        [Test]
        public void ButtonsPopAndBackButtonsGoBack()
        {
            panelSize = new Vector2(1200f, 900f);
            var screen = Show(m_BagelTestAssetList.mainMenuScreenUxml);
            var manager = screen.Q<MainMenuScreenManager>();

            Click(screen.Q<Button>("settings-button"));
            manager.state.SetMainMenuPane(PlayManagerState.MainMenuPane.Settings);
            simulate.FrameUpdate();
            Click(manager.settingsBackButton);

            Assert.AreEqual(new[] { "pop", "back" }, Played());
        }

        [Test]
        public void TogglesAndTabsClick()
        {
            panelSize = new Vector2(1200f, 900f);
            var screen = Show(m_BagelTestAssetList.settingsPaneUxml);

            Click(screen.Q<Tab>("ui-settings-tab").tabHeader);
            Click(screen.Q<Toggle>("reduce-motion-toggle"));

            Assert.AreEqual(new[] { "click", "click" }, Played());
        }

        [Test]
        public void ARealClickPlaysToo()
        {
            var screen = Show(m_BagelTestAssetList.pausePaneUxml);
            simulate.Click(screen.Q<PausePaneManager>().settingsButton);

            Assert.AreEqual(new[] { "pop" }, Played());
        }

        [Test]
        public void AGamepadPressSoundsLikeAClick()
        {
            panelSize = new Vector2(1200f, 900f);
            var screen = Show(m_BagelTestAssetList.mainMenuScreenUxml);

            Send(screen.Q<Button>("leaderboard-button"), NavigationSubmitEvent.GetPooled());

            Assert.AreEqual(new[] { "pop" }, Played());
        }

        [Test]
        public void ALongPressConfirmsOnlyWhenItGoesAllTheWay()
        {
            var screen = Show(m_BagelTestAssetList.pausePaneUxml);
            var restart = screen.Q<PausePaneManager>().restartButton;

            // Pressing it says nothing: letting go early stays silent.
            simulate.MouseDown(restart);
            simulate.FrameUpdate(restart.holdTime * 0.5f);
            simulate.MouseUp(restart);
            simulate.FrameUpdate();
            Assert.IsEmpty(Played());

            simulate.MouseDown(restart);
            simulate.FrameUpdate(restart.holdTime);
            simulate.MouseUp(restart);
            Assert.AreEqual(new[] { "confirm: confirm" }, Played());
        }

        [Test]
        public void MovingTheFocusTicksButPointingDoesNot()
        {
            panelSize = new Vector2(1200f, 900f);
            var screen = Show(m_BagelTestAssetList.mainMenuScreenUxml);
            var play = screen.Q<Button>("play-button");
            var settings = screen.Q<Button>("settings-button");

            play.Focus();
            simulate.FrameUpdate();
            Assert.IsEmpty(Played());

            Send(play, NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Down));
            simulate.FrameUpdate();
            Assert.AreEqual(new[] { "focus: tick" }, Played());
            Assert.AreNotEqual(play, screen.focusController.focusedElement);

            m_Played.Clear();
            simulate.MouseDown(settings);
            simulate.MouseUp(settings);
            Assert.IsFalse(Played().Contains("focus: tick"));
        }

        [Test]
        public void PressingSomethingDisabledSaysNo()
        {
            panelSize = new Vector2(1200f, 900f);
            var screen = Show(m_BagelTestAssetList.settingsPaneForGameUxml);
            var manager = screen.Q<SettingsPaneManagerForGame>();
            manager.state.settings.sound = false;
            simulate.FrameUpdate();
            m_Played.Clear();

            Assert.IsFalse(manager.volumeSlider.enabledInHierarchy);
            Send(manager.volumeSlider, NavigationSubmitEvent.GetPooled());

            Assert.AreEqual(new[] { "disabled: nope" }, Played());
        }

        // Each theme plays the melodic sounds on its own instrument, the game's sounds too.
        [TestCase(0, SoundTone.Wood)]
        [TestCase(1, SoundTone.Pluck)]
        [TestCase(2, SoundTone.Bell)]
        [TestCase(3, SoundTone.Glass)]
        public void EachThemePicksAnInstrument(int theme, SoundTone tone)
        {
            panelSize = new Vector2(1200f, 900f);
            Show(m_BagelTestAssetList.mainMenuScreenUxml, theme);

            Assert.AreEqual(tone, BagelAudio.tone);
        }
    }
}
