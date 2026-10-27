using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Bagel.B05_BagelSelection
{
    // The bagel cards' stats are worked out in the Editor, from all the bagels, and a player only has what was saved
    // in each bagel's asset: every bagel's asset keeps its stats, as they are now.
    public class BagelTypeCollectionTests
    {
        const string k_CollectionPath = "Assets/Game/Bagels/BagelTypes/BagelTypeCollection.asset";

        static readonly string[] k_Stats = { "toppingsPercentile", "toppingLossPercentile", "controlPercentile", "speedPercentile", "gripPercentile" };

        static Dictionary<string, float> Stats(BagelType bagelType) => new()
        {
            [k_Stats[0]] = bagelType.toppingsPercentile,
            [k_Stats[1]] = bagelType.toppingLossPercentile,
            [k_Stats[2]] = bagelType.controlPercentile,
            [k_Stats[3]] = bagelType.speedPercentile,
            [k_Stats[4]] = bagelType.gripPercentile,
        };

        [Test]
        public void EachBagelsAssetKeepsItsCardStats()
        {
            var collection = AssetDatabase.LoadAssetAtPath<BagelTypeCollection>(k_CollectionPath);
            collection.UpdatePercentiles();

            foreach (var bagelType in collection.collection)
            {
                var saved = File.ReadAllText(AssetDatabase.GetAssetPath(bagelType));
                foreach (var stat in Stats(bagelType))
                {
                    var match = Regex.Match(saved, $@"^  {stat.Key}: (\S+)$", RegexOptions.Multiline);
                    Assert.IsTrue(match.Success, $"{bagelType.displayName}'s {stat.Key} is saved");
                    Assert.AreEqual(stat.Value, float.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), 1e-4f, $"{bagelType.displayName}'s {stat.Key} is up to date");
                }
            }
        }

        [Test]
        public void AChangedStatMarksItsBagelToBeSaved()
        {
            var slow = ScriptableObject.CreateInstance<BagelType>();
            var fast = ScriptableObject.CreateInstance<BagelType>();
            var collection = ScriptableObject.CreateInstance<BagelTypeCollection>();
            try
            {
                slow.turnSpeed = 100f;
                fast.turnSpeed = 200f;
                collection.collection = new List<BagelType> { slow, fast };
                collection.UpdatePercentiles();
                EditorUtility.ClearDirty(slow);
                EditorUtility.ClearDirty(fast);

                collection.UpdatePercentiles();
                Assert.IsFalse(EditorUtility.IsDirty(slow), "nothing's changed");

                slow.turnSpeed = 150f;
                collection.UpdatePercentiles();
                Assert.AreEqual(0.75f, slow.controlPercentile, 1e-5f);
                Assert.IsTrue(EditorUtility.IsDirty(slow), "its control's changed");
                Assert.IsFalse(EditorUtility.IsDirty(fast), "still the best");
            }
            finally
            {
                Object.DestroyImmediate(slow);
                Object.DestroyImmediate(fast);
                Object.DestroyImmediate(collection);
            }
        }
    }
}
