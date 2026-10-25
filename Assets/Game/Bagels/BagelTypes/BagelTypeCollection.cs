using UnityEngine;
using System.Collections.Generic;

namespace Bagel
{
    [CreateAssetMenu(fileName = "BagelTypeCollection", menuName = "Bagel/Bagel Type Collection")]
    public class BagelTypeCollection : ScriptableObject
    {
        public List<BagelType> collection;

        // Each bagel's stats as a share of the best of them (topping loss: the least is best), for the cards.
        public void UpdatePercentiles()
        {
            if (collection == null || collection.Count == 0)
                return;

            float maxToppings = float.MinValue;
            float maxToppingLoss = float.MinValue;
            float minToppingLoss = float.MaxValue;
            float maxControl = float.MinValue;
            float maxSpeed = float.MinValue;
            float maxGrip = float.MinValue;

            foreach (var bagelType in collection)
            {
                if (bagelType == null)
                    continue;
                maxToppings = Mathf.Max(maxToppings, bagelType.toppings);
                maxToppingLoss = Mathf.Max(maxToppingLoss, bagelType.toppingLoss);
                minToppingLoss = Mathf.Min(minToppingLoss, bagelType.toppingLoss);
                maxControl = Mathf.Max(maxControl, bagelType.control);
                maxSpeed = Mathf.Max(maxSpeed, bagelType.speed);
                maxGrip = Mathf.Max(maxGrip, bagelType.grip);
            }

            var lossRange = maxToppingLoss - minToppingLoss;
            foreach (var bagelType in collection)
            {
                if (bagelType == null)
                    continue;
                bagelType.toppingsPercentile = bagelType.toppings / maxToppings;
                bagelType.toppingLossPercentile = lossRange > 0f ? (maxToppingLoss - bagelType.toppingLoss) / lossRange : 1f;
                bagelType.controlPercentile = bagelType.control / maxControl;
                bagelType.speedPercentile = bagelType.speed / maxSpeed;
                bagelType.gripPercentile = bagelType.grip / maxGrip;
            }
        }

        void OnValidate() => UpdatePercentiles();
    }
}
