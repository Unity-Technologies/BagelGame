using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements.TestFramework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel.B14_LevelSelection
{
    // The Bagel Levels window: the game's own levels in the tablet's level editor, with the library's live pictures,
    // every change an undo step. The tests edit copies made in memory, never the level assets.
    public class BagelLevelsWindowTests : EditorWindowUITestFixture<BagelLevelsWindow>
    {
        BagelLevelCollection m_Copies;

        [SetUp]
        public void SetUp()
        {
            panelSize = new Vector2(1200f, 760f);
            var levels = AssetDatabase.LoadAssetAtPath<BagelLevelCollection>("Assets/Game/Levels/BagelLevelCollection.asset");
            m_Copies = ScriptableObject.CreateInstance<BagelLevelCollection>();
            m_Copies.hideFlags = HideFlags.DontSave;
            m_Copies.library = levels.library;
            foreach (var level in levels.collection)
            {
                var copy = Object.Instantiate(level);
                copy.hideFlags = HideFlags.DontSave;
                m_Copies.collection.Add(copy);
            }
            window.SetCollection(m_Copies);
            simulate.FrameUpdate();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var level in m_Copies.collection)
                Object.DestroyImmediate(level);
            Object.DestroyImmediate(m_Copies);
        }

        [Test]
        public void TheGamesLevelsAreListedAndTheChosenOneIsInTheEditor()
        {
            Assert.AreEqual(3, window.list.itemsSource.Count);
            Assert.AreEqual(m_Copies.collection[0], window.levelEditor.level, "the tutorial first");
            window.Select(2);
            Assert.AreEqual(m_Copies.collection[2], window.levelEditor.level);
            Assert.AreEqual(m_Copies.collection[2].pieceCount, window.levelEditor.map.pieceElements.Count);
            Assert.IsFalse(window.levelEditor.showDone, "the window has no Done");
            Assert.IsFalse(window.levelEditor.showDelete, "and deletes from its toolbar");
        }

        [Test]
        public void TheLibrarysLivePicturesAreThere()
        {
            Assert.IsNotNull(window.studio);
            Assert.IsNotNull(window.studio.texture);
            var picture = window.levelEditor.items[1].Q<Image>();
            Assert.AreEqual(window.studio.texture, picture.image);
            Assert.AreNotEqual(Rect.zero, picture.uv);
            Assert.IsTrue(window.studio.root.scene.IsValid() && window.studio.root.scene != UnityEngine.SceneManagement.SceneManager.GetActiveScene(), "in a scene of their own");
        }

        [Test]
        public void EveryChangeIsAnUndoStep()
        {
            var level = m_Copies.collection[1];
            window.Select(1);
            var count = level.pieceCount;
            var editor = window.levelEditor;
            editor.ChooseBrush("Orange");
            Assert.IsTrue(editor.PlaceAt(new Vector2(-4f, 20f)));
            Assert.AreEqual(count + 1, level.pieceCount);

            Undo.PerformUndo();
            Assert.AreEqual(count, level.pieceCount, "undone");
            simulate.FrameUpdate();
            Assert.AreEqual(count, editor.map.pieceElements.Count, "and shown undone");
            Undo.PerformRedo();
            Assert.AreEqual(count + 1, level.pieceCount);
        }

        [Test]
        public void LevelsAreAddedAndDeletedButNotTheTutorial()
        {
            var added = window.AddLevel();
            Assert.AreEqual(4, m_Copies.collection.Count);
            Assert.AreEqual(added, window.selectedLevel);
            Assert.AreEqual(BagelLevel.newLength, added.length);
            Assert.AreEqual(0, added.pieceCount);
            var ids = new HashSet<string>();
            foreach (var level in m_Copies.collection)
                Assert.IsTrue(ids.Add(level.id), $"{level.id} once");

            window.DeleteLevel(added, false);
            Assert.AreEqual(3, m_Copies.collection.Count);
            Assert.AreEqual(m_Copies.collection[2], window.selectedLevel, "the one before is chosen");

            window.DeleteLevel(m_Copies.collection[0], false);
            Assert.AreEqual(3, m_Copies.collection.Count, "the tutorial stays");
            window.Select(0);
            Assert.IsFalse(rootVisualElement.Q<Button>("delete-button").enabledSelf);
        }
    }
}
