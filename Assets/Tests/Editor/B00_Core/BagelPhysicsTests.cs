using NUnit.Framework;
using UnityEngine;

namespace Bagel.B00_Core
{
    // The maths that turn spin and impacts into lost toppings.
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
    }
}
