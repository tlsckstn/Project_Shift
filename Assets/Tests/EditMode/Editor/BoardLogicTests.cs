using NUnit.Framework;
using ProjectShift.Board;

namespace ProjectShift.Tests
{
    public class BoardLogicTests
    {
        [Test]
        public void TryShift_Right_MovesBitsAndRobotAndSpendsShift()
        {
            BoardState state = new BoardState(new byte[] { 0b00111000 }, 0, 2, 3);

            Assert.IsTrue(BoardLogic.TryShift(state, 0, 1));
            Assert.AreEqual(0b00011100, state.GetRow(0));
            Assert.AreEqual(3, state.RobotCol);
            Assert.AreEqual(2, state.ShiftsLeft);
        }

        [Test]
        public void TryShift_NoShiftsLeft_ReturnsFalseAndChangesNothing()
        {
            BoardState state = new BoardState(new byte[] { 0b00111000 }, 0, 2, 0);

            Assert.IsFalse(BoardLogic.TryShift(state, 0, 1));
            Assert.AreEqual(0b00111000, state.GetRow(0));
            Assert.AreEqual(2, state.RobotCol);
        }

        [Test]
        public void TryShift_LockedRow_ReturnsFalseAndCostsNothing()
        {
            BoardState state = new BoardState(new byte[] { 0b00111000 }, 0, 2, 3, new[] { true });

            Assert.IsFalse(BoardLogic.TryShift(state, 0, -1));
            Assert.AreEqual(0b00111000, state.GetRow(0));
            Assert.AreEqual(3, state.ShiftsLeft);
        }

        [Test]
        public void ShiftLeft_RobotOnCellZero_FallsOffRow()
        {
            BoardState state = new BoardState(new byte[] { 0b10000000 }, 0, 0, 3);

            BoardLogic.ShiftLeft(state, 0);

            Assert.AreEqual(0, state.GetRow(0));
            Assert.AreEqual(-1, state.RobotCol);
            Assert.IsTrue(state.IsRobotFallen);
            Assert.IsTrue(state.IsFailed);
        }

        [Test]
        public void TryWalk_OntoOnCell_Moves()
        {
            BoardState state = new BoardState(new byte[] { 0b01100000 }, 0, 1, 3);

            Assert.IsTrue(BoardLogic.TryWalk(state, 1));
            Assert.AreEqual(2, state.RobotCol);
        }

        [Test]
        public void TryWalk_OntoOffCell_Fails()
        {
            BoardState state = new BoardState(new byte[] { 0b01100000 }, 0, 1, 3);

            Assert.IsFalse(BoardLogic.TryWalk(state, -1));
            Assert.AreEqual(1, state.RobotCol);
        }

        [Test]
        public void TryWalk_OntoFirewall_Fails()
        {
            bool[] firewalls = new bool[BoardState.Columns];
            firewalls[2] = true;
            BoardState state = new BoardState(new byte[] { 0b01100000 }, 0, 1, 3, null, firewalls);

            Assert.IsFalse(BoardLogic.TryWalk(state, 1));
            Assert.AreEqual(1, state.RobotCol);
        }

        [Test]
        public void TryWalk_OntoOtherRobot_Fails()
        {
            BoardState state = new BoardState(new byte[] { 0b01100000 }, new[] { 0, 0 }, new[] { 1, 2 }, 3);

            Assert.IsFalse(BoardLogic.TryWalk(state, 1));
            Assert.AreEqual(1, state.RobotCol);
        }

        [Test]
        public void TryDescend_OnCellBelow_MovesDownThenStopsAtBottomRow()
        {
            BoardState state = new BoardState(new byte[] { 0b01000000, 0b01000000 }, 0, 1, 3);

            Assert.IsTrue(BoardLogic.TryDescend(state));
            Assert.AreEqual(1, state.RobotRow);
            Assert.IsFalse(BoardLogic.TryDescend(state));
            Assert.AreEqual(1, state.RobotRow);
        }

        [Test]
        public void GetRowValue_RobotsCoverBits_CountsRobotCellAsZero()
        {
            BoardState state = new BoardState(new byte[] { 0b11000000 }, 0, 1, 3);

            Assert.AreEqual(128, BoardLogic.GetRowValue(state, 0, true));
            Assert.AreEqual(192, BoardLogic.GetRowValue(state, 0, false));
        }

        [Test]
        public void TryWalk_OntoBugCell_FailsStage()
        {
            bool[] bugCells = new bool[BoardState.Columns];
            bugCells[1] = true;
            BoardState state = new BoardState(new byte[] { 0b11000000 }, 0, 0, 3, null, null, bugCells);

            Assert.IsTrue(BoardLogic.TryWalk(state, 1));
            Assert.IsTrue(state.IsRobotOnBug);
            Assert.IsTrue(state.IsFailed);
        }

        [Test]
        public void Undo_AfterShift_RestoresStateBeforeIt()
        {
            BoardState state = new BoardState(new byte[] { 0b00111000 }, 0, 2, 3);
            BoardHistory history = new BoardHistory(state);
            BoardState before = state.Clone();
            BoardLogic.TryShift(state, 0, 1);
            history.Save(before);

            state = history.Undo(state);

            Assert.AreEqual(0b00111000, state.GetRow(0));
            Assert.AreEqual(2, state.RobotCol);
            Assert.AreEqual(3, state.ShiftsLeft);
            Assert.AreEqual(0, history.Count);
        }

        [Test]
        public void Undo_EmptyHistory_ReturnsCurrentState()
        {
            BoardState state = new BoardState(new byte[] { 0b00111000 }, 0, 2, 3);
            BoardHistory history = new BoardHistory(state);

            Assert.AreSame(state, history.Undo(state));
        }

        [Test]
        public void Restart_AfterMoves_ClearsUndoAndReturnsStartState()
        {
            BoardState state = new BoardState(new byte[] { 0b00111000 }, 0, 2, 3);
            BoardHistory history = new BoardHistory(state);
            history.Save(state);
            BoardLogic.TryShift(state, 0, 1);
            history.Save(state);
            BoardLogic.TryShift(state, 0, 1);

            BoardState restarted = history.Restart();

            Assert.AreEqual(0, history.Count);
            Assert.AreEqual(0b00111000, restarted.GetRow(0));
            Assert.AreEqual(3, restarted.ShiftsLeft);
        }
    }
}
