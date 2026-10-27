using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Bagel.B00_Bakery
{
    // What's in the bakery stays put on its walls, ceiling and blackboard as the room's measurements are edited
    // (BakeryAnchor), in a bakery of its own: what hangs on a wall goes with the wall, what runs along the room
    // stretches with it, what's past the far wall is hidden, the blackboard's rails fit it, a move by hand is kept, and
    // undoing an edit puts everything back.
    public class BakeryAnchorTests
    {
        const BakeryAnchor.Side k_Start = BakeryAnchor.Side.Start;
        const BakeryAnchor.Side k_Middle = BakeryAnchor.Side.Middle;
        const BakeryAnchor.Side k_End = BakeryAnchor.Side.End;
        const BakeryAnchor.Side k_Both = BakeryAnchor.Side.Both;

        Bakery m_Bakery;

        static BakeryAnchor.Axis Room(BakeryAnchor.Side side) => new(Bakery.Part.Room, side);
        static BakeryAnchor.Axis Board(BakeryAnchor.Side side) => new(Bakery.Part.Blackboard, side);
        static BakeryAnchor.Axis Television(BakeryAnchor.Side side) => new(Bakery.Part.Television, side);

        // A room 20 wide (x: -10 to 10), 8 high, 30 long (z: -10 to 20), with a blackboard on the west wall and a
        // television on the far wall.
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            m_Bakery = new GameObject("Bakery").AddComponent<Bakery>();
            Measure(bakery =>
            {
                bakery.FindProperty("m_WestSide").floatValue = 10f;
                bakery.FindProperty("m_EastSide").floatValue = 10f;
                bakery.FindProperty("m_SouthSide").floatValue = 10f;
                bakery.FindProperty("m_NorthSide").floatValue = 20f;
                bakery.FindProperty("m_Height").floatValue = 8f;
                bakery.FindProperty("m_BlackboardWidth").floatValue = 6f;
                bakery.FindProperty("m_BlackboardHeight").floatValue = 3f;
                bakery.FindProperty("m_BlackboardThickness").floatValue = 0.2f;
                bakery.FindProperty("m_BlackboardWallXY").vector2Value = new Vector2(0f, 4f);
                bakery.FindProperty("m_TelevisionWallXY").vector2Value = new Vector2(1f, 3f);
            }, false);
            yield return Updated();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Bakery.gameObject);
        }

        // As the inspector edits them (to be undone, or not).
        void Measure(Action<SerializedObject> change, bool undoable = true)
        {
            var bakery = new SerializedObject(m_Bakery);
            change(bakery);
            if (!undoable)
            {
                bakery.ApplyModifiedPropertiesWithoutUndo();
                return;
            }
            Undo.IncrementCurrentGroup();
            bakery.ApplyModifiedProperties();
        }

        // The Editor updates the bakery (and what's anchored to it) once something's changed.
        static IEnumerator Updated()
        {
            EditorApplication.QueuePlayerLoopUpdate();
            yield return null;
        }

        BakeryAnchor Anchored(string name, Vector3 position, Vector3 scale, BakeryAnchor.Axis across, BakeryAnchor.Axis up, BakeryAnchor.Axis along, bool onlyInTheRoom = false, float margin = 0f)
        {
            var thing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            thing.name = name;
            thing.transform.SetParent(m_Bakery.transform, false);
            thing.transform.SetPositionAndRotation(position, Quaternion.identity);
            thing.transform.localScale = scale;
            var anchor = thing.AddComponent<BakeryAnchor>();
            anchor.Anchor(across, up, along, onlyInTheRoom, margin);
            return anchor;
        }

        static void AssertAt(Vector3 expected, Vector3 actual, string what)
        {
            Assert.Less(Vector3.Distance(expected, actual), 1e-3f, $"{what} is at {actual:F3}, not {expected:F3}");
        }

        [UnityTest]
        public IEnumerator WhatsOnAWallGoesWithTheWall()
        {
            // A clock high on the far wall, in the middle; a frame on the west wall, near the far corner.
            var clock = Anchored("Clock", new Vector3(0f, 7f, 19.8f), Vector3.one, Room(k_Middle), Room(k_End), Room(k_End));
            var frame = Anchored("Frame", new Vector3(-9.9f, 4f, 18f), Vector3.one, Room(k_Start), Room(k_Start), Room(k_End));

            Measure(bakery =>
            {
                bakery.FindProperty("m_NorthSide").floatValue = 25f;
                bakery.FindProperty("m_WestSide").floatValue = 12f;
                bakery.FindProperty("m_Height").floatValue = 10f;
            });
            yield return Updated();
            AssertAt(new Vector3(-1f, 9f, 24.8f), clock.transform.position, "the clock, in the middle of the wall, under the ceiling");
            AssertAt(new Vector3(-11.9f, 4f, 23f), frame.transform.position, "the frame, on the west wall, near the far corner");
        }

        [UnityTest]
        public IEnumerator WhatRunsAlongTheRoomStretchesWithIt()
        {
            // A trim along the west wall from the near wall to the far one; a beam across the ceiling, there only while
            // it's a meter inside the far wall.
            var trim = Anchored("Trim", new Vector3(-9.8f, 7.5f, 5f), new Vector3(0.2f, 0.5f, 30f), Room(k_Start), Room(k_End), Room(k_Both));
            var beam = Anchored("Beam", new Vector3(0f, 7.6f, 15f), new Vector3(20f, 0.8f, 1f), Room(k_Both), Room(k_End), Room(k_Start), true, 1f);
            Assert.IsTrue(beam.shown);

            Measure(bakery =>
            {
                bakery.FindProperty("m_NorthSide").floatValue = 40f;
                bakery.FindProperty("m_EastSide").floatValue = 15f;
            });
            yield return Updated();
            AssertAt(new Vector3(-9.8f, 7.5f, 15f), trim.transform.position, "the trim, along the longer wall");
            Assert.AreEqual(50f, trim.transform.localScale.z, 1e-3f, "the trim's as long as the wall");
            AssertAt(new Vector3(2.5f, 7.6f, 15f), beam.transform.position, "the beam, across the wider room");
            Assert.AreEqual(25f, beam.transform.localScale.x, 1e-3f, "the beam's as long as the room's wide");

            Measure(bakery => bakery.FindProperty("m_NorthSide").floatValue = 15.5f);
            yield return Updated();
            Assert.IsFalse(beam.shown, "past the far wall");
            Assert.IsFalse(beam.GetComponent<Renderer>().enabled);
            Assert.IsTrue(trim.shown);
        }

        [UnityTest]
        public IEnumerator WhatsOnTheBlackboardGoesWithItAndTheRailsFitIt()
        {
            // A rail along the blackboard's top, a little past its ends; a screen in front of the television.
            var rail = Anchored("Rail", new Vector3(-9.7f, 5.6f, 0f), new Vector3(0.2f, 0.2f, 6.4f), Board(k_End), Board(k_End), Board(k_Both));
            var screen = Anchored("Screen", new Vector3(1f, 3f, 19.6f), Vector3.one, Television(k_Middle), Television(k_Middle), Television(k_Middle));

            Measure(bakery =>
            {
                bakery.FindProperty("m_BlackboardWidth").floatValue = 10f;
                bakery.FindProperty("m_BlackboardWallXY").vector2Value = new Vector2(2f, 5f);
                bakery.FindProperty("m_TelevisionWallXY").vector2Value = new Vector2(-3f, 4f);
                bakery.FindProperty("m_NorthSide").floatValue = 22f;
            });
            yield return Updated();
            AssertAt(new Vector3(-9.7f, 6.6f, 2f), rail.transform.position, "the rail, on the blackboard's top");
            Assert.AreEqual(10.4f, rail.transform.localScale.z, 1e-3f, "the rail fits the wider blackboard");
            AssertAt(new Vector3(-3f, 4f, 21.6f), screen.transform.position, "the screen, with the television");
        }

        [UnityTest]
        public IEnumerator MovedByHandItStaysThereAndKeepsItsDistances()
        {
            var clock = Anchored("Clock", new Vector3(0f, 7f, 19.8f), Vector3.one, Room(k_Middle), Room(k_End), Room(k_End));
            clock.transform.position += new Vector3(2f, -1f, 0f);
            yield return Updated();
            AssertAt(new Vector3(2f, 6f, 19.8f), clock.transform.position, "where it was moved to");

            Measure(bakery => bakery.FindProperty("m_NorthSide").floatValue = 25f);
            yield return Updated();
            AssertAt(new Vector3(2f, 6f, 24.8f), clock.transform.position, "with the wall, as far from its middle as it was moved");
        }

        [UnityTest]
        public IEnumerator UndoingAnEditPutsEverythingBack()
        {
            var clock = Anchored("Clock", new Vector3(0f, 7f, 19.8f), Vector3.one, Room(k_Middle), Room(k_End), Room(k_End));
            var trim = Anchored("Trim", new Vector3(-9.8f, 7.5f, 5f), new Vector3(0.2f, 0.5f, 30f), Room(k_Start), Room(k_End), Room(k_Both));

            Measure(bakery =>
            {
                bakery.FindProperty("m_NorthSide").floatValue = 30f;
                bakery.FindProperty("m_Height").floatValue = 12f;
            });
            yield return Updated();
            AssertAt(new Vector3(0f, 11f, 29.8f), clock.transform.position, "edited");

            Undo.PerformUndo();
            yield return Updated();
            AssertAt(new Vector3(0f, 7f, 19.8f), clock.transform.position, "the clock, back");
            AssertAt(new Vector3(-9.8f, 7.5f, 5f), trim.transform.position, "the trim, back");
            Assert.AreEqual(30f, trim.transform.localScale.z, 1e-3f, "as long as it was");

            Undo.PerformRedo();
            yield return Updated();
            AssertAt(new Vector3(0f, 11f, 29.8f), clock.transform.position, "redone");
        }
    }
}
