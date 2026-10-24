using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B13_Kitchen
{
    // The steam over the coffee: the shader moves its puffs when the panel has the vertex channels the material
    // reads, and they stand still when it doesn't.
    public class SteamPuffsTests : UITestFixture
    {
        SteamPuffs m_Steam;

        [SetUp]
        public void SetUp()
        {
            var assets = Resources.Load<BagelTestAssetList>("BagelTestAssetList");
            assets.steamUxml.CloneTree(rootVisualElement);
            m_Steam = rootVisualElement.Q<SteamPuffs>();
        }

        [TearDown]
        public void TearDown()
        {
            rootVisualElement.panel.extraVertexChannels = ExtraVertexChannels.None;
            rootVisualElement.Clear();
        }

        [Test]
        public void WithoutTheChannelsThePuffsStandStill()
        {
            simulate.FrameUpdate();
            Assert.IsFalse(m_Steam.movesOnTheGpu);
            Assert.IsFalse(m_Steam.ClassListContains(SteamPuffs.gpuUssClassName));
            Assert.IsNull(m_Steam.resolvedStyle.unityMaterial.material);
        }

        [Test]
        public void WithTheChannelsTheShaderMovesThePuffs()
        {
            rootVisualElement.panel.extraVertexChannels |= SteamPuffs.vertexChannels;
            simulate.FrameUpdate();
            simulate.FrameUpdate();
            Assert.IsTrue(m_Steam.movesOnTheGpu);
            Assert.IsTrue(m_Steam.ClassListContains(SteamPuffs.gpuUssClassName));
            Assert.AreEqual("Steam", m_Steam.resolvedStyle.unityMaterial.material.name);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void TheShaderClockStaysSmall()
        {
            var now = SteamPuffs.UpdateShaderTime();
            Assert.AreEqual(now, Shader.GetGlobalFloat(SteamPuffs.timeId));
            Assert.That(now, Is.InRange(0f, 1024f));
        }

        // Every panel with the channels pays for them: only the steam's settings have them.
        [Test]
        public void OnlyTheSteamsPanelSettingsHaveTheChannels()
        {
            var assets = Resources.Load<BagelTestAssetList>("BagelTestAssetList");
            var withChannels = assets.panelSettings.collection.Where(settings => settings.extraVertexChannels != ExtraVertexChannels.None).ToList();
            Assert.AreEqual(1, withChannels.Count);
            Assert.AreEqual(SteamPuffs.vertexChannels, withChannels[0].extraVertexChannels);
        }
    }
}
