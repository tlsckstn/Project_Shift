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

            if (state.IsRobotFallen)
            {
                state = history.Restart();
                Render();
                view.ShowMessage("FELL! Restarted");
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
                return;
            }

            stageIndex = index;
            state = stages[index].CreateState();
            history = new BoardHistory(state);
            selectedRow = 0;
            Render();
        }

        private void Render()
        {
            view.Render(state, selectedRow, stages[stageIndex].Exit, stages[stageIndex].Par);
        }
    }
}
