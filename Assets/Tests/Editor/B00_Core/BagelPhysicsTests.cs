using NUnit.Framework;
using UnityEngine;

namespace Bagel.B00_Core
{
    // The maths that turn spin and impacts into lost toppings, and speed into how fast it turns.
    public class BagelPhysicsTests
    {
        BagelType m_BagelType;

        [SetUp]
        public void SetUp()
        {
            m_BagelType = ScriptableObject.CreateInstance<BagelType>();
            m_BagelType.spinToppingLossFactor = 0.01f;
            m_BagelType.impactToppingLossFactor = 0.5f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_BagelType);
        }

        [Test]
        public void NoToppingsLostAtRest()
        {
            Assert.AreEqual(0f, BagelController.ToppingLoss(m_BagelType, 0f, 0f));
        }

        [Test]
        public void SpinAndImpactsBothLoseToppings()
        {
            Assert.AreEqual(0.1f, BagelController.ToppingLoss(m_BagelType, 0f, 10f), 1e-5f);
            Assert.AreEqual(1.0f, BagelController.ToppingLoss(m_BagelType, 2f, 0f), 1e-5f);
            Assert.AreEqual(1.1f, BagelController.ToppingLoss(m_BagelType, 2f, 10f), 1e-5f);
        }

        [Test]
        public void RestingOnTheGroundIsNoShock()
        {
            // Resting: the ground pushes back with exactly the bagel's weight for one step.
            const float mass = 4f, gravity = 9.81f, dt = 0.02f;
            var restingImpulse = Vector3.up * (mass * gravity * dt);

            Assert.AreEqual(0f, BagelController.ShockGs(restingImpulse, Vector3.up, dt, mass, gravity), 1e-4f);
        }

        [Test]
        public void ShocksAreMeasuredAlongTheContactNormal()
        {
            const float mass = 4f, gravity = 9.81f, dt = 0.02f;
            // A 4 g hit against a wall, plus a sideways scrape that shouldn't count.
            var impulse = (Vector3.left * 4f * mass * gravity + Vector3.forward * 100f) * dt;

            Assert.AreEqual(3f, BagelController.ShockGs(impulse, Vector3.right * 2f, dt, mass, gravity), 1e-3f);
        }

        // Like a car: standing still it turns at its turn speed, and rolling at the falloff speed, forward or back,
        // half as fast.
        [Test]
        public void TheFasterItRollsTheLessItTurns()
        {
            Assert.AreEqual(100f, BagelController.TurnSpeed(100f, 0f, 8f), 1e-4f);
            Assert.AreEqual(50f, BagelController.TurnSpeed(100f, 8f, 8f), 1e-4f);
            Assert.AreEqual(50f, BagelController.TurnSpeed(100f, -8f, 8f), 1e-4f, "backing up");
            Assert.Less(BagelController.TurnSpeed(100f, 12f, 8f), BagelController.TurnSpeed(100f, 6f, 8f));
            Assert.AreEqual(100f, BagelController.TurnSpeed(100f, 12f, 0f), "no falloff");
        }

        // A turn pulls it sideways by its speed times how fast it turns, and past the falloff speed that hardly
        // grows, however fast it rolls: it only grips the ground so hard.
        [Test]
        public void AFastTurnPullsNoHarderThanItGrips()
        {
            float Pull(float speed) => speed * BagelController.TurnSpeed(100f, speed, 8f);

            Assert.AreEqual(Pull(8f) * 2f, 100f * 8f, 1e-3f, "half its grip at the falloff speed");
            Assert.Less(Pull(100f), 100f * 8f);
            Assert.Greater(Pull(100f), Pull(16f));
        }
    }
}
