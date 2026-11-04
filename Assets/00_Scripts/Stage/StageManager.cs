using ProjectShift.Board;
using UnityEngine;

namespace ProjectShift.Stage
{
    /// <summary>
    /// Runs the stage sequence: owns the live board state and its undo history, applies player actions,
    /// restarts on a fall and loads the next stage when every robot stands on its exit.
    /// </summary>
    public class StageManager : MonoBehaviour
    {
        [SerializeField] private StageData[] stages;
        [SerializeField] private BoardView view;
        [Tooltip("Seconds the failed board stays on screen before the stage restarts. Must cover BoardView's shift slide plus its fall drop or bug fade.")]
        [SerializeField] private float failRestartSeconds = 0.6f;

        private int stageIndex;
        private BoardState state;
        private BoardHistory history;
        private int selectedRow;
        private Vector2Int[] exits;
        // Per row, rebuilt on stage load and refilled on every Render: the decimal value the player reads (robots cover their cell
        // when the stage says so) and the number-lock target (-1 when the row has none).
        private int[] rowValues;
        private int[] lockTargets;
        private bool isFinished;
        // Counts down while a failed move is shown; -1 when no restart is pending. Input is ignored meanwhile.
        private float failTimer = -1f;
        private string failMessage;

        private bool IsInputBlocked => isFinished || failTimer >= 0f;

        private void Start()
        {
            LoadStage(0);
        }

        private void Update()
        {
            if (failTimer < 0f)
            {
                return;
            }

            failTimer -= Time.deltaTime;
            if (failTimer < 0f)
            {
                state = history.Restart();
                Render();
                view.PlayReappear();
                view.ShowMessage(failMessage);
            }
        }

        /// <summary>Walks the robot one cell (-1 left, 1 right).</summary>
        public void Walk(int direction)
        {
            if (IsInputBlocked)
            {
                return;
            }

            BoardState before = state.Clone();
            if (BoardLogic.TryWalk(state, direction) && AfterAction(before))
            {
                view.PlayWalk(state.SelectedRobot, before.RobotRow, before.RobotCol);
            }
        }

        /// <summary>Moves the robot down one row.</summary>
        public void Descend()
        {
            if (IsInputBlocked)
            {
                return;
            }

            BoardState before = state.Clone();
            if (BoardLogic.TryDescend(state) && AfterAction(before))
            {
                view.PlayWalk(state.SelectedRobot, before.RobotRow, before.RobotCol);
            }
        }

        /// <summary>Shifts the selected row (-1 for &lt;&lt;, 1 for &gt;&gt;).</summary>
        public void Shift(int direction)
        {
            if (IsInputBlocked)
            {
                return;
            }

            BoardState before = state.Clone();
            if (BoardLogic.TryShift(state, selectedRow, direction) && AfterAction(before))
            {
                // Shifting left pushes out cell 0, shifting right cell 7.
                bool lostBit = before.GetCell(selectedRow, direction < 0 ? 0 : BoardState.Columns - 1);
                view.PlayShift(selectedRow, direction, lostBit, state);
            }
        }

        /// <summary>Moves the row selection up (-1) or down (1), clamped to the board.</summary>
        public void SelectRow(int delta)
        {
            if (IsInputBlocked)
            {
                return;
            }

            selectedRow = Mathf.Clamp(selectedRow + delta, 0, state.RowCount - 1);
            Render();
        }

        /// <summary>Selects the next robot. Does nothing with one robot; costs no shift and is not an undo step.</summary>
        public void SwitchRobot()
        {
            if (IsInputBlocked || state.RobotCount < 2)
            {
                return;
            }

            state.SelectNextRobot();
            Render();
        }

        /// <summary>Reverts the last walk, descend or shift.</summary>
        public void Undo()
        {
            if (IsInputBlocked)
            {
                return;
            }

            state = history.Undo(state);
            Render();
        }

        /// <summary>Restores the current stage's start state.</summary>
        public void Restart()
        {
            if (IsInputBlocked)
            {
                return;
            }

            state = history.Restart();
            Render();
        }

        // Returns true when the move's result stays on screen, so the caller may animate it: a normal move, or a failed one
        // that is shown (fall frame) until Update restarts the stage. False after a stage clear, which snaps to a new board.
        private bool AfterAction(BoardState before)
        {
            history.Save(before);

            if (state.IsFailed)
            {
                failMessage = state.IsRobotFallen ? "FELL! Restarted" : "BUG! Restarted";
                failTimer = failRestartSeconds;
                Render();
                view.PlayFail(state);
                return true;
            }

            if (stages[stageIndex].IsCleared(state))
            {
                LoadStage(stageIndex + 1);
                return false;
            }

            Render();
            return true;
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
            exits = stages[index].GetExits();
            rowValues = new int[state.RowCount];
            lockTargets = new int[state.RowCount];
            for (int r = 0; r < lockTargets.Length; r++)
            {
                lockTargets[r] = -1;
            }

            for (int i = 0; i < stages[index].NumberLockCount; i++)
            {
                Vector2Int numberLock = stages[index].GetNumberLock(i);
                lockTargets[numberLock.x] = numberLock.y;
            }

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
            for (int r = 0; r < rowValues.Length; r++)
            {
                rowValues[r] = BoardLogic.GetRowValue(state, r, stage.RobotsCoverBits);
            }

            view.Render(state, selectedRow, exits, parLeft, rowValues, lockTargets, stage.RobotsCoverBits);
        }
    }
}
