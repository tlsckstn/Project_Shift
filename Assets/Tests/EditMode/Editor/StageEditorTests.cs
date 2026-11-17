using NUnit.Framework;
using ProjectShift.Custom;
using ProjectShift.Stage;
using UnityEngine;

namespace ProjectShift.Tests
{
    /// <summary>Edit rules of the stage editor: placing and removing every element, row count, goals and the reason line.</summary>
    public class StageEditorTests
    {
        // Row 0 all on, robot 1 at (0,0) with its exit at (0,7): the smallest valid board.
        private static StageEditor ValidBoard()
        {
            var editor = new StageEditor();
            for (int col = 0; col < 8; col++)
            {
                editor.ToggleBit();
                editor.MoveCursor(0, 1);
            }

            editor.MoveCursor(0, -7);
            editor.ToggleRobot(0);
            editor.MoveCursor(0, 7);
            editor.ToggleExit(0);
            editor.MoveCursor(0, -7);
            return editor;
        }

        [Test]
        public void NewEditor_OneEmptyRow_AsksForARobot()
        {
            var editor = new StageEditor();

            Assert.AreEqual(1, editor.RowCount);
            Assert.AreEqual(0, editor.RobotCount);
            Assert.AreEqual("PLACE A ROBOT", editor.Reason);
        }

        [Test]
        public void ValidBoard_NoReason_AndCodeRoundTrips()
        {
            StageEditor editor = ValidBoard();

            Assert.IsNull(editor.Reason);
            StageCodeData data = editor.ToCodeData();
            Assert.IsTrue(StageCode.TryParse(StageCode.Encode(data), out StageCodeData parsed, out string error), error);
            CollectionAssert.AreEqual(new byte[] { 0xFF }, parsed.Rows);
            CollectionAssert.AreEqual(new[] { 0 }, parsed.Starts);
            CollectionAssert.AreEqual(new[] { 7 }, parsed.Exits);
        }

        [Test]
        public void Cursor_StopsAtTheBoardEdge()
        {
            var editor = new StageEditor();

            editor.MoveCursor(-1, -1);
            Assert.AreEqual(0, editor.CursorRow);
            Assert.AreEqual(0, editor.CursorCol);

            editor.MoveCursor(5, 20);
            Assert.AreEqual(0, editor.CursorRow);
            Assert.AreEqual(7, editor.CursorCol);
        }

        [Test]
        public void ToggleBit_TurnsTheCursorCellOnAndOff()
        {
            var editor = new StageEditor();
            editor.MoveCursor(0, 2);

            editor.ToggleBit();
            Assert.AreEqual(0b0010_0000, editor.CreateState().GetRow(0));

            editor.ToggleBit();
            Assert.AreEqual(0, editor.CreateState().GetRow(0));
        }

        [Test]
        public void RowCount_StaysBetweenOneAndThree()
        {
            var editor = new StageEditor();

            editor.ChangeRowCount(-1);
            Assert.AreEqual(1, editor.RowCount);

            editor.ChangeRowCount(5);
            Assert.AreEqual(3, editor.RowCount);
        }

        [Test]
        public void RemovingARow_RemovesEverythingOnIt()
        {
            StageEditor editor = ValidBoard();
            editor.ChangeRowCount(1);
            editor.MoveCursor(1, 0);
            editor.ToggleBit();
            editor.ToggleBug();
            editor.ToggleFirewall();
            editor.ToggleLockedRow();
            editor.ToggleNumberLock();
            editor.ToggleRobot(1);
            editor.MoveCursor(0, 3);
            editor.ToggleExit(0);

            editor.ChangeRowCount(-1);

            Assert.AreEqual(1, editor.RowCount);
            Assert.AreEqual(0, editor.CursorRow);
            Assert.AreEqual(1, editor.RobotCount);
            Assert.AreEqual(-1, editor.GetExit(0));
            editor.ChangeRowCount(1);
            Assert.AreEqual(0, editor.CreateState().GetRow(1));
            Assert.IsFalse(editor.CreateState().IsBugCell(1, 0));
            Assert.IsFalse(editor.CreateState().IsFirewall(1, 0));
            Assert.IsFalse(editor.CreateState().IsRowLocked(1));
            Assert.AreEqual(-1, editor.GetLockTarget(1));
        }

        [Test]
        public void ToggleRobot_PlacesInOrder_MovesAndRemoves()
        {
            StageEditor editor = ValidBoard();

            editor.ToggleRobot(2);
            Assert.AreEqual(1, editor.RobotCount, "robot 3 needs robot 2 first");

            editor.MoveCursor(0, 1);
            editor.ToggleRobot(1);
            Assert.AreEqual(2, editor.RobotCount);

            editor.MoveCursor(0, 1);
            editor.ToggleRobot(1);
            Assert.AreEqual(2, editor.CreateState().GetRobotCol(1), "moved to the cursor");

            editor.ToggleRobot(1);
            Assert.AreEqual(1, editor.RobotCount, "pressed on its own cell: removed");
        }

        [Test]
        public void RemovingRobotOne_RenumbersTheRestWithTheirExits()
        {
            StageEditor editor = ValidBoard();
            editor.MoveCursor(0, 1);
            editor.ToggleRobot(1);
            editor.MoveCursor(0, 5);
            editor.ToggleExit(1);
            editor.MoveCursor(0, -6);

            editor.ToggleRobot(0);

            Assert.AreEqual(1, editor.RobotCount);
            Assert.AreEqual(1, editor.CreateState().GetRobotCol(0));
            Assert.AreEqual(6, editor.GetExit(0));
        }

        [Test]
        public void ToggleExit_NeedsItsRobot_AndTogglesOff()
        {
            var editor = new StageEditor();

            editor.ToggleExit(0);
            Assert.AreEqual(0, editor.RobotCount);

            StageEditor valid = ValidBoard();
            valid.MoveCursor(0, 7);
            valid.ToggleExit(0);
            Assert.AreEqual(-1, valid.GetExit(0));
            Assert.AreEqual("ROBOT 1 NEEDS AN EXIT", valid.Reason);
        }

        [Test]
        public void BugFirewallLockAndCover_ReachTheCode()
        {
            StageEditor editor = ValidBoard();
            editor.MoveCursor(0, 3);

            editor.ToggleBug();
            editor.MoveCursor(0, 1);
            editor.ToggleFirewall();
            editor.ToggleLockedRow();
            editor.ToggleCover();
            editor.ChangeShiftLimit(3);

            Assert.IsNull(editor.Reason);
            StageCodeData data = editor.ToCodeData();
            CollectionAssert.AreEqual(new[] { 3 }, data.BugCells);
            CollectionAssert.AreEqual(new[] { 4 }, data.Firewalls);
            CollectionAssert.AreEqual(new[] { 0 }, data.LockedRows);
            Assert.IsTrue(data.RobotsCoverBits);
            Assert.AreEqual(8, data.ShiftLimit);
        }

        [Test]
        public void NumberLock_GoalIsTheRowValueWhenSet_ThenTogglesOff()
        {
            StageEditor editor = ValidBoard();

            editor.ToggleNumberLock();
            editor.MoveCursor(0, 7);
            editor.ToggleBit();

            Assert.AreEqual(255, editor.GetLockTarget(0));
            Assert.AreEqual(0xFE, editor.CreateState().GetRow(0));
            CollectionAssert.AreEqual(new[] { 255 }, editor.ToCodeData().LockTargets);

            editor.ToggleNumberLock();
            Assert.AreEqual(-1, editor.GetLockTarget(0));
        }

        [Test]
        public void ShiftLimit_StaysInsideTheCodeRange()
        {
            var editor = new StageEditor();

            editor.ChangeShiftLimit(-100);
            Assert.AreEqual(0, editor.ShiftLimit);

            editor.ChangeShiftLimit(500);
            Assert.AreEqual(StageCode.MaxShiftLimit, editor.ShiftLimit);
        }

        [Test]
        public void RobotOnZeroCell_Reason()
        {
            StageEditor editor = ValidBoard();

            editor.ToggleBit();

            Assert.AreEqual("ROBOT 1 IS ON A 0 CELL", editor.Reason);
        }

        [Test]
        public void RobotOnBug_Reason()
        {
            StageEditor editor = ValidBoard();

            editor.ToggleBug();

            Assert.AreEqual("ROBOT 1 IS ON A BUG", editor.Reason);
        }

        [Test]
        public void TwoRobotsOnOneCell_Reason()
        {
            StageEditor editor = ValidBoard();
            editor.MoveCursor(0, 1);
            editor.ToggleRobot(1);
            editor.MoveCursor(0, -1);

            editor.ToggleRobot(1);

            Assert.AreEqual("ROBOTS 1 AND 2 SHARE A CELL", editor.Reason);
        }

        [Test]
        public void ValidBoard_MakesAPlayableStageData()
        {
            StageEditor editor = ValidBoard();

            StageData stage = StageData.Create(editor.ToCodeData(), 0, out string error);

            Assert.IsNotNull(stage, error);
            Assert.AreEqual(1, stage.RobotCount);
            Assert.AreEqual(new Vector2Int(0, 7), stage.GetExit(0));
            Object.DestroyImmediate(stage);
        }
    }
}
