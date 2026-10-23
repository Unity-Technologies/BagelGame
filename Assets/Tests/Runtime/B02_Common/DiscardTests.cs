using NUnit.Framework;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B02_Common
{
    public class DiscardTests : UITestFixture
    {
        [Test]
        public void DiscardedElementsAreReleasedAfterTheNextUpdate()
        {
            var parent = new VisualElement();
            var child = new Label("Toast");
            parent.Add(child);
            rootVisualElement.Add(parent);
            simulate.FrameUpdate();

            Discard.Children(parent);
            Assert.AreEqual(0, parent.childCount);
            Assert.IsFalse(child.resourcesReleased, "the pointer may still be over it until the panel updates");

            simulate.FrameUpdate();
            Assert.IsTrue(child.resourcesReleased);
        }

        [Test]
        public void ElementsPutBackAreNotReleased()
        {
            var element = new Label("Kept");
            rootVisualElement.Add(element);
            simulate.FrameUpdate();

            Discard.Element(element);
            rootVisualElement.Add(element);
            simulate.FrameUpdate();

            Assert.IsFalse(element.resourcesReleased);
        }
    }
}
