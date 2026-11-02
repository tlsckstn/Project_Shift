using ProjectShift.Board;
using UnityEngine;

namespace ProjectShift.Stage
{
    /// <summary>
    /// Runs the stage sequence: owns the live board state and its undo history, applies player actions,
    /// restarts on a fall and loads the next stage when the robot reaches the exit.
    /// </summary>
    public class StageManager : MonoBehaviour
    {
        [SerializeField] private StageData[] stages;
        [SerializeField] private BoardView view;

        private int stageIndex;
        private BoardState state;
        private BoardHistory history;
        private int selectedRow;
        private bool isFinished;

        private void Start()
        {
            LoadStage(0);
        }

        /// <summary>Walks the robot one cell (-1 left, 1 right).</summary>
        public void Walk(int direction)
        {
            if (isFinished)
            {
                return;
            }

            BoardState before = state.Clone();
            if (BoardLogic.TryWalk(state, direction))
            {
                AfterAction(before);
            }
        }

        /// <summary>Moves the robot down one row.</summary>
        public void Descend()
        {
            if (isFinished)
            {
                return;
            }

            BoardState before = state.Clone();
            if (BoardLogic.TryDescend(state))
            {
                AfterAction(before);
            }
        }

        /// <summary>Shifts the selected row (-1 for &lt;&lt;, 1 for &gt;&gt;).</summary>
        public void Shift(int direction)
        {
            if (isFinished)
            {
                return;
            }

            BoardState before = state.Clone();
            if (BoardLogic.TryShift(state, selectedRow, direction))
            {
                AfterAction(before);
            }
        }

        /// <summary>Moves the row selection up (-1) or down (1), clamped to the board.</summary>
        public void SelectRow(int delta)
        {
            if (isFinished)
            {
                return;
            }

            selectedRow = Mathf.Clamp(selectedRow + delta, 0, state.RowCount - 1);
            Render();
        }

        /// <summary>Selects the next robot. Does nothing with one robot; costs no shift and is not an undo step.</summary>
        public void SwitchRobot()
        {
            if (isFinished || state.RobotCount < 2)
            {
                return;
            }

            state.SelectNextRobot();
            Render();
        }

        /// <summary>Reverts the last walk, descend or shift.</summary>
        public void Undo()
        {
            if (isFinished)
            {
                return;
            }

            state = history.Undo(state);
            Render();
        }

        /// <summary>Restores the current stage's start state.</summary>
        public void Restart()
        {
            if (isFinished)
            {
                return;
            }

            state = history.Restart();
            Render();
        }

        private void AfterAction(BoardState before)
        {
            history.Save(before);

            if (state.IsFailed)
            {
                string message = state.IsRobotFallen ? "FELL! Restarted" : "BUG! Restarted";
                state = history.Restart();
                Render();
                view.ShowMessage(message);
                return;
            }

            if (state.RobotRow == stages[stageIndex].Exit.x && state.RobotCol == stages[stageIndex].Exit.y)
            {
                LoadStage(stageIndex + 1);
                return;
            }

            Render();
        }

        private void LoadStage(int index)
        {
            if (index >= stages.Length)
            {
                isFinished = true;
                Render(); // show the winning move before the end message
                view.ShowMessage("ALL CLEAR");
                view.ShowHint(string.Empty);
                return;
            }

            stageIndex = index;
            state = stages[index].CreateState();
            view.ShowHint(stages[index].Hint);
            history = new BoardHistory(state);
            selectedRow = 0;
            Render();
        }

        private void Render()
        {
            StageData stage = stages[stageIndex];
            // Shifts still inside par: counts down with every shift, stops at 0 once over par; -1 hides PAR.
            int parLeft = stage.Par > 0 ? Mathf.Max(0, stage.Par - (stage.ShiftLimit - state.ShiftsLeft)) : -1;
            view.Render(state, selectedRow, stage.Exit, parLeft);
        }
    }
}
