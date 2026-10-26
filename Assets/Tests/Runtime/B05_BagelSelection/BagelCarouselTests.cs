using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Bagel.B05_BagelSelection
{
    // The bagels to choose from on the lazy Susan, in the real level: it turns the chosen one to the front, the short
    // way round, its card and its bagel riding along; what's knocked off is set again next time; an order rings up
    // the register.
    public class BagelCarouselTests
    {
        PlayManager m_PlayManager;
        BagelCarousel m_Carousel;

        PlayManagerState state => m_PlayManager.state;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var assets = Resources.Load<BagelTestAssetList>("BagelTestAssetList");
            yield return SceneManager.LoadSceneAsync(assets.mainSceneName, LoadSceneMode.Single);
            yield return null;
            m_PlayManager = Object.FindAnyObjectByType<PlayManager>();
            m_Carousel = Object.FindAnyObjectByType<BagelCarousel>();
            state.SelectBagel(0);
            state.GoToBagelSelection();
            yield return Seconds(2.5f);
        }

        [TearDown]
        public void TearDown()
        {
            if (m_PlayManager != null)
                state.SelectBagel(0);
        }

        static IEnumerator Seconds(float seconds)
        {
            var end = Time.time + seconds;
            while (Time.time < end)
                yield return null;
        }

        // Where a station is round the lazy Susan, against its front (degrees).
        float AngleFromFront(BagelCarouselStation station)
        {
            var center = m_Carousel.turntable.position;
            var toPlate = station.plate.position - center;
            toPlate.y = 0f;
            return Vector3.Angle(toPlate, m_Carousel.transform.forward);
        }

        [UnityTest]
        public IEnumerator TheChosenBagelComesRoundToTheFront()
        {
            var stations = m_Carousel.stations;
            Assert.AreEqual(state.bagelTypeCount, stations.Count, "a station for each kind");
            Assert.Less(AngleFromFront(stations[0]), 1f, "the chosen one is at the front");
            Assert.IsTrue(stations[0].selected);

            var start = m_Carousel.targetAngle;
            state.SelectNextBagel();
            Assert.IsTrue(stations[1].selected);
            Assert.IsFalse(stations[0].selected);
            Assert.AreEqual(start + m_Carousel.step, m_Carousel.targetAngle, 1e-3f, "one step on");

            // While it turns, the cards sway, and nothing comes off.
            var swayed = 0f;
            for (var end = Time.time + 2.5f; Time.time < end;)
            {
                foreach (var station in stations)
                    swayed = Mathf.Max(swayed, Vector3.Angle(station.card.transform.up, Vector3.up));
                yield return new WaitForFixedUpdate();
            }
            Assert.Greater(swayed, 2f, "the cards sway as it turns");
            Assert.Less(AngleFromFront(stations[1]), 2f, "the next one is at the front");
            foreach (var station in stations)
            {
                Assert.IsFalse(station.knocked, $"{station.name} stays on");
                Assert.Less(Vector3.Angle(station.card.transform.up, Vector3.up), 5f, $"{station.name}'s card stands");
                var onPlate = station.bagel.position - station.plate.position;
                onPlate.y = 0f;
                Assert.Less(onPlate.magnitude, 0.3f, $"{station.name}'s bagel is on its plate");
            }
        }

        [UnityTest]
        public IEnumerator ItTurnsTheShortWayRound()
        {
            var start = m_Carousel.targetAngle;
            state.SelectPreviousBagel();
            Assert.AreEqual(state.bagelTypeCount - 1, state.selectedBagelIndex);
            Assert.AreEqual(start - m_Carousel.step, m_Carousel.targetAngle, 1e-3f, "back one, not forward round the rest");
            yield return Seconds(2f);
            Assert.Less(AngleFromFront(m_Carousel.stations[state.selectedBagelIndex]), 2f);
        }

        // A bagel rolling back up the counter into it knocks things off, and spins it; next time, it's set again.
        [UnityTest]
        public IEnumerator WhatsKnockedOffIsSetAgain()
        {
            var station = m_Carousel.stations[0];
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.transform.localScale = Vector3.one * 1.2f;
            var target = station.card.transform.TransformPoint(new Vector3(0f, 1.2f, 0f));
            var from = target + m_Carousel.transform.forward * 3f;
            ball.transform.position = from;
            var body = ball.AddComponent<Rigidbody>();
            body.mass = 4f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.useGravity = false;
            body.linearVelocity = (target - from).normalized * 12f;
            yield return Seconds(0.6f);
            Object.Destroy(ball);
            Assert.IsTrue(station.knocked, "knocked off");

            state.GoToMainMenu();
            yield return null;
            state.GoToBagelSelection();
            yield return Seconds(2.5f);
            Assert.IsFalse(station.knocked, "set again");
            Assert.Less(Vector3.Angle(station.card.transform.up, Vector3.up), 5f);
            Assert.AreEqual(Vector3.one, station.card.transform.localScale, "back to its size");
        }

        [UnityTest]
        public IEnumerator AnOrderRingsUpTheRegister()
        {
            var register = Object.FindAnyObjectByType<BagelRegister>();
            Assert.IsFalse(register.ringing);
            state.GoToLevelSelection();
            yield return null;
            Assert.IsTrue(register.ringing, "the drawer shoots out");
            yield return Seconds(2f);
            Assert.IsFalse(register.ringing, "and goes back in");
        }

        // A click on the register's keys reaches them: nothing of the register is in front of its face.
        [UnityTest]
        public IEnumerator TheRegistersKeysCanBeClicked()
        {
            yield return null;
            var face = GameObject.Find("BagelSelectionControlsPanelUI");
            var camera = Camera.main;
            var hits = Physics.RaycastAll(camera.transform.position, face.transform.position - camera.transform.position,
                Vector3.Distance(camera.transform.position, face.transform.position) + 1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            Assert.Greater(hits.Length, 0);
            Assert.AreEqual(face, hits[0].collider.gameObject, $"the face is hit first, not {hits[0].collider.name}");
        }
    }
}
