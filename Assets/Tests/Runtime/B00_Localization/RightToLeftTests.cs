using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B00_Localization
{
    // Arabic reads right to left: the screens say so (languageDirection, for the text generator) and turn their
    // layout around (b-rtl). The other languages read left to right.
    public class RightToLeftTests : UITestFixture
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
            BagelLocalization.SetLocale("en");
        }

        [Test]
        public void AScreenTurnsAroundForArabic()
        {
            m_BagelTestAssetList.mainMenuScreenUxml.CloneTree(rootVisualElement);
            simulate.FrameUpdate();
            var screen = rootVisualElement.Q<MainMenuScreenManager>();
            var pane = screen.Q<MainMenuPaneManager>();
            Assert.AreEqual(LanguageDirection.LTR, screen.languageDirection);
            Assert.IsFalse(screen.ClassListContains(FollowsLanguage.rtlUssClassName));

            BagelLocalization.SetLocale("ar");
            simulate.FrameUpdate();
            Assert.IsTrue(BagelLocalization.isRightToLeft);
            Assert.AreEqual(LanguageDirection.RTL, screen.languageDirection);
            Assert.IsTrue(screen.ClassListContains(FollowsLanguage.rtlUssClassName));

            // The panes inside follow their screen.
            Assert.AreEqual(LanguageDirection.Inherit, pane.languageDirection);
            Assert.IsFalse(pane.ClassListContains(FollowsLanguage.rtlUssClassName));

            BagelLocalization.SetLocale("zh-Hans");
            simulate.FrameUpdate();
            Assert.AreEqual(LanguageDirection.LTR, screen.languageDirection);
            Assert.IsFalse(screen.ClassListContains(FollowsLanguage.rtlUssClassName));
        }

        // The bagels' tags have no pane: the component turns them around.
        [Test]
        public void UIWithoutAPaneFollowsTheLanguage()
        {
            m_BagelTestAssetList.bagelSelectionTypeInfoUxml.CloneTree(rootVisualElement);
            BagelLocalization.SetLocale("ar");
            simulate.FrameUpdate();
            var tag = rootVisualElement.Q("pane");
            Assert.IsTrue(tag.HasComponent<FollowsLanguage>());
            Assert.AreEqual(LanguageDirection.RTL, tag.languageDirection);
            Assert.IsTrue(tag.ClassListContains(FollowsLanguage.rtlUssClassName));
        }

        // < and > move along the bagels on the counter, left and right in every language. Right to left, the text
        // generator would mirror them.
        [Test]
        public void TheArrowsAlongTheBagelsStayLeftToRight()
        {
            m_BagelTestAssetList.bagelSelectionControlsUxml.CloneTree(rootVisualElement);
            BagelLocalization.SetLocale("ar");
            simulate.FrameUpdate();
            Assert.AreEqual(LanguageDirection.LTR, rootVisualElement.Q<Button>("left-button").languageDirection);
            Assert.AreEqual(LanguageDirection.LTR, rootVisualElement.Q<Button>("right-button").languageDirection);
        }

        // Rows that read in order run from the right; the table's first column is on the right.
        [Test]
        public void RowsRunFromTheRight()
        {
            panelSize = new Vector2(1200f, 900f);
            var screen = m_BagelTestAssetList.mainMenuScreenUxml.Instantiate();
            screen.styleSheets.Add(m_BagelTestAssetList.themes.collection[0].theme);
            rootVisualElement.Add(screen);
            BagelLocalization.SetLocale("ar");
            simulate.FrameUpdate();
            simulate.FrameUpdate();

            var settings = rootVisualElement.Q<Button>("settings-button");
            var leaderboard = rootVisualElement.Q<Button>("leaderboard-button");
            Assert.Greater(settings.worldBound.x, leaderboard.worldBound.x, "Settings, the first of the two, is on the right");
            Assert.AreEqual(TextAnchor.UpperRight, rootVisualElement.Q<MainMenuScreenManager>().resolvedStyle.unityTextAlign);
        }
    }
}
