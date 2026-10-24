using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel.B03_MainMenu
{
    // The intro chalks the board up in place. The board is drawn flat into the chalk filter's texture: anything on
    // it moved off its plane (translate z) falls out of that texture and isn't drawn until it's back.
    public class MainMenuIntroTests
    {
        const string k_IntroPath = "Assets/UI/Runtime/B03_MainMenu/MainMenuIntroAnimation.anim";

        [Test]
        public void TheIntroKeepsEverythingOnTheBoard()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(k_IntroPath);
            var curves = 0;
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.type != typeof(PanelRenderer))
                    continue;
                ++curves;
                Assert.IsFalse(binding.propertyName.EndsWith("Translate.z"), binding.propertyName);
            }
            Assert.Greater(curves, 0, "The intro no longer animates the board");
        }
    }
}
