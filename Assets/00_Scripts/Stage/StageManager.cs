using ProjectShift.Audio;
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
        [SerializeField] private SoundPlayer sound;
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
        // True while the stage select shows instead of a stage; selectedStage is the stage under its cursor.
        private bool isSelecting;
        private int selectedStage;
        // True while the title screen shows (at launch, after ALL CLEAR, and on Back from the stage select).
        private bool isOnTitle;
        // Stages 0..unlocked-1 are open; saved in StageProgress.
        private int unlocked;
        // Per stage: fewest shifts it was cleared with (-1 never), and the stars that best earns (0-3). Saved in StageProgress.
        private int[] bestShifts;
        private int[] stars;
        // Stage select pages, built once from the stages' chapter titles: first stage and title of each page.
        private readonly System.Collections.Generic.List<int> pageStarts = new System.Collections.Generic.List<int>();
        private readonly System.Collections.Generic.List<string> pageTitles = new System.Collections.Generic.List<string>();
        // Chapter number of each page, and per stage the stage its first lock opens (-1 without locks); read by the zone view.
        private readonly System.Collections.Generic.List<int> pageChapters = new System.Collections.Generic.List<int>();
        private int[] firstLockTargets;

        private bool IsInputBlocked => isOnTitle || isSelecting || isFinished || isAwaitingChoice || pendingTimer >= 0f || rewindTimer >= 0f || view.IsTransitioning;

        /// <summary>
        /// True on a playable puzzle board (also one out of shifts) with no move, fail, rewind, transition or row slide
        /// running; Esc then opens the pause menu. False on the title, the stage select, a cleared board and ALL CLEAR.
        /// </summary>
        public bool CanPause => !IsInputBlocked && !view.IsSliding;

        /// <summary>True while the stage select shows; InputController then routes keys to MoveSelection / PlaySelected.</summary>
        public bool IsSelecting => isSelecting;

        /// <summary>True while the title screen shows; InputController then only routes Enter to StartFromTitle.</summary>
        public bool IsOnTitle => isOnTitle;

        private void Awake()
        {
            view.TransitionStepped += sound.PlayTransitionStep;
            view.LedLit += sound.PlayLed;
        }

        private void Start()
        {
            unlocked = StageProgress.LoadUnlocked(stages.Length);
            bestShifts = new int[stages.Length];
            stars = new int[stages.Length];
            firstLockTargets = new int[stages.Length];
            for (int i = 0; i < stages.Length; i++)
            {
                firstLockTargets[i] = stages[i].FirstLockTarget;
                bestShifts[i] = StageProgress.LoadBest(stages[i].name);
                stars[i] = stages[i].StarsFor(bestShifts[i]);
            }

            BuildPages();

            ShowTitle();
        }

        /// <summary>Leaves the title for the stage select, cursor on the last open stage.</summary>
        public void StartFromTitle()
        {
            if (!isOnTitle)
            {
                return;
            }

            ShowSelect(unlocked - 1);
        }

        private void ShowTitle()
        {
            isOnTitle = true;
            isSelecting = false;
            isFinished = false;
            isAwaitingChoice = false;
            view.ShowTitle();
        }

        /// <summary>
        /// Moves the stage select cursor one stage back (-1) or on (+1) in stage order; all four arrows use it and the cursor
        /// runs on across pages. Locked stages cannot be picked.
        /// </summary>
        public void MoveSelection(int delta)
        {
            if (!isSelecting)
            {
                return;
            }

            int target = StagePages.NextSelection(selectedStage, delta, unlocked);
            if (target == selectedStage)
            {
                return;
            }

            sound.PlayWalk();
            ShowSelect(target);
        }

        /// <summary>Starts the stage under the stage select cursor.</summary>
        public void PlaySelected()
        {
            if (!isSelecting)
            {
                return;
            }

            isSelecting = false;
            view.HideStageSelect();
            LoadStage(selectedStage);
            view.PlayTransition(null, state);
        }

        /// <summary>
        /// Leaves the stage for the stage select, cursor on this stage; on the stage select goes back to the title.
        /// Ignored on the title and while a fail, clear, rewind or transition plays.
        /// </summary>
        public void Back()
        {
            // A row slide also blocks it, so Esc pressed mid-slide neither pauses (CanPause) nor leaves the board.
            if (isOnTitle || pendingTimer >= 0f || rewindTimer >= 0f || view.IsTransitioning || view.IsSliding)
            {
                return;
            }

            if (isSelecting)
            {
                ShowTitle();
                return;
            }

            ShowSelect(isFinished ? stages.Length - 1 : stageIndex);
        }

        private void BuildPages()
        {
            string[] chapterTitles = new string[stages.Length];
            for (int i = 0; i < stages.Length; i++)
            {
                chapterTitles[i] = stages[i].ChapterTitle;
            }

            StagePages.Build(chapterTitles, StageSelectView.PageSize, pageStarts, pageTitles, pageChapters);
        }

        private int PageOf(int stage) => StagePages.PageOf(pageStarts, stage);

        private void ShowSelect(int stage)
        {
            isOnTitle = false;
            isSelecting = true;
            isFinished = false;
            isAwaitingChoice = false;
            selectedStage = stage;
            int page = PageOf(stage);
            int start = pageStarts[page];
            int end = page + 1 < pageStarts.Count ? pageStarts[page + 1] : stages.Length;
            view.ShowStageSelect(start, end - start, pageTitles[page], page + 1, pageStarts.Count, pageChapters[page], unlocked, stage, stars, firstLockTargets);
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
                int used = stage.ShiftLimit - state.ShiftsLeft;
                isAwaitingChoice = true;
                view.ShowClearChoice(used, stage.Par, stage.StarsFor(used));
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
                sound.PlayWalk();
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
                sound.PlayWalk();
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
                sound.PlayShift();
                AfterAction(before);
                // Shifting left pushes out cell 0, shifting right cell 7.
                bool lostBit = before.GetCell(selectedRow, direction < 0 ? 0 : BoardState.Columns - 1);
                view.PlayShift(selectedRow, direction, lostBit, state);
            }
        }

        /// <summary>Moves the row selection one row down; from the bottom row it wraps to the top row.</summary>
        public void SelectNextRow()
        {
            if (IsInputBlocked)
            {
                return;
            }

            selectedRow = (selectedRow + 1) % state.RowCount;
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

            if (history.Count == 0)
            {
                return;
            }

            BoardState undone = state;
            state = history.Undo(state);
            Render();
            // An undone shift slides its row back; when the bit it had pushed out was lit, that bit also re-forms.
            if (BoardLogic.TryFindUndoneShift(undone, state, out int row, out int direction))
            {
                view.PlayShift(row, -direction, false, state);
                int col = direction > 0 ? BoardState.Columns - 1 : 0;
                if (state.GetCell(row, col))
                {
                    view.PlayRegenerate(row, col, direction);
                }
            }

            sound.PlayUndo();
        }

        /// <summary>Loads the next stage on a cleared board waiting for the player's choice; on the ALL CLEAR screen goes to the title.</summary>
        public void Next()
        {
            if (isFinished)
            {
                ShowTitle();
                return;
            }

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
            sound.PlayRewind();
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
                sound.PlayFail(!state.IsRobotFallen);
                return;
            }

            Render();
            bool arrived = FlashArrivals(before);

            if (stages[stageIndex].IsCleared(state))
            {
                // Saved now, so quitting on the clear screen keeps the next stage open.
                if (stageIndex + 1 < stages.Length && unlocked < stageIndex + 2)
                {
                    unlocked = stageIndex + 2;
                    StageProgress.SaveUnlocked(unlocked);
                }

                // A worse clear keeps the best and its stars.
                int used = stages[stageIndex].ShiftLimit - state.ShiftsLeft;
                if (bestShifts[stageIndex] < 0 || used < bestShifts[stageIndex])
                {
                    bestShifts[stageIndex] = used;
                    stars[stageIndex] = stages[stageIndex].StarsFor(used);
                    StageProgress.SaveBest(stages[stageIndex].name, used);
                }

                pendingTimer = clearSeconds;
                pendingClear = true;
                view.PlayClear();
                sound.PlayClear();
            }
            else if (arrived)
            {
                sound.PlayExit();
            }
        }

        // Flashes the exit of every robot that this move brought onto its own exit (it was elsewhere before the move).
        // Returns whether any robot arrived.
        private bool FlashArrivals(BoardState before)
        {
            bool arrived = false;
            for (int i = 0; i < exits.Length && i < state.RobotCount; i++)
            {
                bool wasOn = before.GetRobotRow(i) == exits[i].x && before.GetRobotCol(i) == exits[i].y;
                bool isOn = state.GetRobotRow(i) == exits[i].x && state.GetRobotCol(i) == exits[i].y;
                if (isOn && !wasOn)
                {
                    view.PlayExitFlash(i);
                    arrived = true;
                }
            }

            return arrived;
        }

        private void LoadStage(int index)
        {
            if (index >= stages.Length)
            {
                isFinished = true;
                Render(); // show the winning move before the end message
                view.ShowAllClear();
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

            view.SetZone(ChapterZones.ZoneOf(pageChapters[StagePages.PageOf(pageStarts, index)]));
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
