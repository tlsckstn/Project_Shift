using NUnit.Framework;
using ProjectShift.Board;
using ProjectShift.Stage;
using UnityEditor;
using UnityEngine;

namespace ProjectShift.Tests
{
    public class StageDataTests
    {
        private StageData stage;
        private SerializedObject serialized;

        [SetUp]
        public void SetUp()
        {
            stage = ScriptableObject.CreateInstance<StageData>();
            serialized = new SerializedObject(stage);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(stage);
        }

        [Test]
        public void StarsFor_WithinPar_ReturnsThree()
        {
            SetBoard(4, "01100000");

            Assert.AreEqual(3, stage.StarsFor(3));
            Assert.AreEqual(3, stage.StarsFor(4));
        }

        [Test]
        public void StarsFor_OneOverPar_ReturnsTwo()
        {
            SetBoard(4, "01100000");

            Assert.AreEqual(2, stage.StarsFor(5));
        }

        [Test]
        public void StarsFor_FurtherOverPar_ReturnsOne()
        {
            SetBoard(4, "01100000");

            Assert.AreEqual(1, stage.StarsFor(9));
        }

        [Test]
        public void StarsFor_NeverClearedOrNoPar_ReturnsZero()
        {
            SetBoard(4, "01100000");
            Assert.AreEqual(0, stage.StarsFor(-1));

            SetBoard(0, "01100000");
            Assert.AreEqual(0, stage.StarsFor(1));
        }

        [Test]
        public void CreateState_ParsesRowsRobotAndFlags()
        {
            SetBoard(4, "01100000", "00010000");
            SetCells("firewalls", new Vector2Int(1, 3));
            SerializedProperty locked = serialized.FindProperty("lockedRows");
            locked.arraySize = 1;
            locked.GetArrayElementAtIndex(0).intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            BoardState state = stage.CreateState();

            Assert.AreEqual(0b01100000, state.GetRow(0));
            Assert.AreEqual(0b00010000, state.GetRow(1));
            Assert.AreEqual(0, state.RobotRow);
            Assert.AreEqual(1, state.RobotCol);
            Assert.AreEqual(5, state.ShiftsLeft);
            Assert.IsFalse(state.IsRowLocked(0));
            Assert.IsTrue(state.IsRowLocked(1));
            Assert.IsTrue(state.IsFirewall(1, 3));
        }

        [Test]
        public void IsCleared_RobotReachesExit_ReturnsTrue()
        {
            SetBoard(4, "01100000");
            BoardState state = stage.CreateState();
            Assert.IsFalse(stage.IsCleared(state));

            BoardLogic.TryWalk(state, 1);

            Assert.IsTrue(stage.IsCleared(state));
        }

        [Test]
        public void IsCleared_RobotOnExitButLockUnmet_ReturnsFalse()
        {
            SetBoard(4, "01100000");
            SetCells("numberLocks", new Vector2Int(0, 100));
            BoardState state = stage.CreateState();

            BoardLogic.TryWalk(state, 1);

            Assert.IsFalse(stage.IsCleared(state));
        }

        // Robot starts at (0, 1), exit at (0, 2), 5 shifts.
        private void SetBoard(int par, params string[] rows)
        {
            SerializedProperty rowsProperty = serialized.FindProperty("rows");
            rowsProperty.arraySize = rows.Length;
            for (int i = 0; i < rows.Length; i++)
            {
                rowsProperty.GetArrayElementAtIndex(i).stringValue = rows[i];
            }

            serialized.FindProperty("par").intValue = par;
            serialized.FindProperty("shiftLimit").intValue = 5;
            serialized.FindProperty("start").vector2IntValue = new Vector2Int(0, 1);
            serialized.FindProperty("exit").vector2IntValue = new Vector2Int(0, 2);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private void SetCells(string field, params Vector2Int[] cells)
        {
            SerializedProperty property = serialized.FindProperty(field);
            property.arraySize = cells.Length;
            for (int i = 0; i < cells.Length; i++)
            {
                property.GetArrayElementAtIndex(i).vector2IntValue = cells[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
