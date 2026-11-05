using ProjectShift.Board;
using UnityEngine;

namespace ProjectShift.Stage
{
    /// <summary>
    /// Runs the stage sequence: owns the live board state and its undo history, applies player actions,
    /// restarts on a fall and, when every robot stands on its exit, lets the player retry the stage or go to the next one.
    /// </summary>
    public class StageManager : MonoBehaviour
    {
        [SerializeField] private StageData[] stages;
        [SerializeField] private BoardView view;
        [Tooltip("Seconds the failed board stays on screen before the stage restarts. Must cover BoardView's shift slide plus its fall drop or bug fade.")]
        [SerializeField] private float failRestartSeconds = 0.6f;
        [Tooltip("Seconds the clear plays before the retry / next choice appears. Must cover BoardView's shift slide plus its clear hold and clock-out.")]
        [SerializeField] private float clearSeconds = 0.8f;
        [Tooltip("Seconds each undo step is shown while Restart rewinds the stage to its start.")]
        [SerializeField] private float rewindStepSeconds = 0.05f;
        [Tooltip("Longest a rewind may take; long histories step faster to fit.")]
        [SerializeField] private float rewindMaxSeconds = 0.6f;

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
        // Counts down while a failed or cleared board is shown; -1 when nothing is pending. Input is ignored meanwhile.
        private float pendingTimer = -1f;
        // What runs when pendingTimer runs out: offer retry / next (clear) or restart this one (fail).
        private bool pendingClear;
        private string failMessage;
        // True once a clear has played out: the cleared board waits for Restart (retry) or Next.
        private bool isAwaitingChoice;
        // Counts down to the next undo step while Restart rewinds; -1 when not rewinding. Input is ignored meanwhile.
        private float rewindTimer = -1f;
        private float rewindStep;

        private bool IsInputBlocked => isFinished || isAwaitingChoice || pendingTimer >= 0f || rewindTimer >= 0f || view.IsTransitioning;

        private void Start()
        {
            LoadStage(0);
            view.PlayTransition(null, state);
        }

        private void Update()
        {
            if (rewindTimer >= 0f)
            {
                rewindTimer -= Time.deltaTime;
                if (rewindTimer < 0f)
                {
                    StepRewind();
                }

                return;
            }

            if (pendingTimer < 0f)
            {
                return;
            }

            pendingTimer -= Time.deltaTime;
            if (pendingTimer >= 0f)
            {
                return;
            }

            if (pendingClear)
            {
                StageData stage = stages[stageIndex];
                isAwaitingChoice = true;
                view.ShowClearChoice(stage.ShiftLimit - state.ShiftsLeft, stage.Par);
                return;
            }

            state = history.Restart();
            Render();
            view.PlayReappear();
            view.ShowMessage(failMessage);
        }

        /// <summary>Walks the robot one cell (-1 left, 1 right).</summary>
        public void Walk(int direction)
        {
            if (IsInputBlocked)
            {
                return;
            }

            BoardState before = state.Clone();
            if (BoardLogic.TryWalk(state, direction))
            {
                AfterAction(before);
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
            if (BoardLogic.TryDescend(state))
            {
                AfterAction(before);
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
            if (BoardLogic.TryShift(state, selectedRow, direction))
            {
                AfterAction(before);
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

        /// <summary>Loads the next stage. Only works on a cleared board waiting for the player's choice.</summary>
        public void Next()
        {
            if (!isAwaitingChoice)
            {
                return;
            }

            isAwaitingChoice = false;
            BoardState cleared = state;
            LoadStage(stageIndex + 1);
            if (!isFinished)
            {
                view.PlayTransition(cleared, state);
            }
        }

        /// <summary>
        /// Rewinds the current stage to its start state, showing every undo step briefly. On a cleared board this is the retry choice.
        /// </summary>
        public void Restart()
        {
            isAwaitingChoice = false;
            if (IsInputBlocked)
            {
                return;
            }

            if (history.Count == 0)
            {
                state = history.Restart();
                Render();
                return;
            }

            rewindStep = Mathf.Min(rewindStepSeconds, rewindMaxSeconds / history.Count);
            rewindTimer = 0f;
            StepRewind();
        }

        // Undoes every step whose time has come (several per frame when steps are shorter than a frame), then shows the result.
        private void StepRewind()
        {
            while (rewindTimer <= 0f)
            {
                state = history.Undo(state);
                if (history.Count == 0)
                {
                    rewindTimer = -1f;
                    Render();
                    return;
                }

                rewindTimer += rewindStep;
            }

            Render();
        }

        // Shows the move's result. A failed move is held (fall effect) until Update restarts the stage; a clearing move is held
        // (clock-out effect) until Update offers retry / next. The caller then animates the move itself.
        private void AfterAction(BoardState before)
        {
            history.Save(before);

            if (state.IsFailed)
            {
                failMessage = state.IsRobotFallen ? "FELL" : "BUG";
                pendingTimer = failRestartSeconds;
                pendingClear = false;
                Render();
                view.PlayFail(state);
                return;
            }

            Render();
            FlashArrivals(before);

            if (stages[stageIndex].IsCleared(state))
            {
                pendingTimer = clearSeconds;
                pendingClear = true;
                view.PlayClear();
            }
        }

        // Flashes the exit of every robot that this move brought onto its own exit (it was elsewhere before the move).
        private void FlashArrivals(BoardState before)
        {
            for (int i = 0; i < exits.Length && i < state.RobotCount; i++)
            {
                bool wasOn = before.GetRobotRow(i) == exits[i].x && before.GetRobotCol(i) == exits[i].y;
                bool isOn = state.GetRobotRow(i) == exits[i].x && state.GetRobotCol(i) == exits[i].y;
                if (isOn && !wasOn)
                {
                    view.PlayExitFlash(i);
                }
            }
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
