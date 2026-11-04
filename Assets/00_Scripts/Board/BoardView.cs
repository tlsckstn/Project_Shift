using TMPro;
using UnityEngine;

namespace ProjectShift.Board
{
    /// <summary>
    /// Draws a BoardState with pooled pixel-art sprites and one decimal label per row.
    /// Read-only: it never changes the state and never calls BoardLogic.
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        private const int MaxRows = 3;
        private const int MaxRobots = 3;
        // Sprites are 36 pixels per unit: one cell pitch (32 px cell + 4 px gap) is 1 unit.
        private const float PixelsPerUnit = 36f;
        // Robot sprite centre above the cell centre: its tracks (20 px sprite, bottom row empty) rest on the cell top.
        private const float RobotOffsetPixels = 25f;
        // Exit sprite centre above the cell centre: the 28 px door frame's bottom touches the cell top.
        private const float ExitOffsetPixels = 30f;
        // Must match the Puzzle map in Assets/InputSystem_Actions.inputactions.
        private const string KeysText = "←/→ walk    ↓ descend    W/S select row    A/D shift    Z undo    R restart";
        // Shown instead of KeysText on stages with more than one robot.
        private const string KeysTextMultiRobot = "←/→ walk    ↓ descend    W/S select row    A/D shift    Q switch robot    Z undo    R restart";

        [Tooltip("Corner brackets around the selected row, drawn inside the lock frame.")]
        [SerializeField] private Sprite rowHighlightSprite;
        [SerializeField] private Sprite platformSprite;
        [SerializeField] private Sprite gapSprite;
        [Tooltip("A 1 cell under a robot on stages where robots cover bits.")]
        [SerializeField] private Sprite coveredSprite;
        [SerializeField] private Sprite bugSprite;
        [SerializeField] private Sprite firewallSprite;
        [SerializeField] private Sprite lockFrameSprite;
        [Tooltip("Per crew in robot order (blue, pink, yellow).")]
        [SerializeField] private Sprite[] robotIdleSprites;
        [Tooltip("Per crew: the robot standing on its own exit.")]
        [SerializeField] private Sprite[] robotExitSprites;
        [Tooltip("Per crew: the robot that fell off its row or entered a bug cell, shown until the stage restarts.")]
        [SerializeField] private Sprite[] robotFallSprites;
        [Tooltip("Per crew: white silhouette outline behind the controlled robot.")]
        [SerializeField] private Sprite[] robotOutlineSprites;
        [Tooltip("Per crew: exit while any number lock is unmet.")]
        [SerializeField] private Sprite[] exitLockedSprites;
        [Tooltip("Per crew: exit once every number lock is met.")]
        [SerializeField] private Sprite[] exitOpenSprites;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private float rowSpacing = 2f;
        [SerializeField] private float labelFontSize = 6f;
        [SerializeField] private float keysFontSize = 3.5f;
        [Tooltip("Local y of the key panel; keep it inside the camera's bottom edge (orthographic size 5).")]
        [SerializeField] private float keysPanelY = -4.3f;
        [SerializeField] private float hintFontSize = 4f;
        [Tooltip("Local y of the stage hint; between the bottom board row (3 rows reach about -2.5) and the key panel.")]
        [SerializeField] private float hintY = -3.4f;
        [SerializeField] private Color hintColor = new Color(0.9f, 0.8f, 0.2f);
        [Tooltip("GOAL label color once the row value matches its lock.")]
        [SerializeField] private Color onColor = new Color(0.3f, 0.9f, 0.5f);
        [SerializeField] private Color lockedRowColor = new Color(0.55f, 0.6f, 0.68f);
        [Tooltip("Seconds a shifted row (its cells and the robots on it) takes to slide one cell.")]
        [SerializeField] private float shiftSlideSeconds = 0.1f;
        [Tooltip("Seconds the faint copy left on the cell a robot walked or descended from takes to fade out.")]
        [SerializeField] private float walkGhostSeconds = 0.15f;
        [Tooltip("Starting opacity of that walk afterimage.")]
        [SerializeField] private float walkGhostAlpha = 0.45f;
        [Tooltip("How far (art pixels) a robot that fell off its row drops, accelerating, before it is gone.")]
        [SerializeField] private float fallPixels = 54f;
        [Tooltip("Seconds that drop takes; it fades over the second half. Keep slide + this under StageManager's fail delay.")]
        [SerializeField] private float fallSeconds = 0.45f;
        [Tooltip("Seconds a robot that entered a bug cell takes to fade out. Keep slide + this under StageManager's fail delay.")]
        [SerializeField] private float bugFadeSeconds = 0.3f;
        [Tooltip("Seconds a failed robot takes to fade back in at its start cell after the restart.")]
        [SerializeField] private float reappearSeconds = 0.15f;
        [SerializeField] private Color shiftsColor = Color.white;
        [SerializeField] private Color noShiftsColor = new Color(1f, 0.3f, 0.3f);

        private Transform cachedTransform;
        private SpriteRenderer[] cells;
        private Transform[] cellTransforms;
        private SpriteRenderer[] firewallMarks;
        private Transform[] firewallTransforms;
        private SpriteRenderer[] bugMarks;
        private Transform[] bugTransforms;
        private SpriteRenderer[] rowHighlights;
        private Transform[] highlightTransforms;
        private SpriteRenderer[] lockFrames;
        private Transform[] lockFrameTransforms;
        private TextMeshPro[] lockLabels;
        private TextMeshPro[] valueLabels;
        private Transform[] labelTransforms;
        private int[] shownValues;
        // Number-lock goal under each row value ("GOAL 176"), only shown on rows that have a lock.
        private TextMeshPro[] goalLabels;
        private int[] shownGoals;
        private bool[] shownGoalMet;
        private SpriteRenderer[] robots;
        private Transform[] robotTransforms;
        private SpriteRenderer selectedFrame;
        private Transform selectedFrameTransform;
        private TextMeshPro keysLabel;
        private bool shownMultiRobot;
        // One exit per robot, same order as the robots.
        private SpriteRenderer[] exitMarks;
        private Transform[] exitTransforms;
        private TextMeshPro shiftsLabel;
        private TextMeshPro hintLabel;
        private int shownShifts = -1;
        private int shownPar = -1;
        private int shownRowCount;

        // Shift slide: Render places everything at its final spot, then PlayShift offsets the row back one cell and Update eases it in.
        private int slideRow = -1;
        private int slideDirection;
        private float slideTime;
        private int slideEnterCol;
        private int slideLostCol;
        private readonly bool[] slideRobots = new bool[MaxRobots];
        private readonly Vector3[] slideRobotEnds = new Vector3[MaxRobots];
        private bool slideMovesSelectedFrame;
        private Vector3 slideSelectedFrameEnd;
        // The bit pushed past the row end: slides out and fades.
        private SpriteRenderer lostCell;
        private Transform lostCellTransform;
        // Walk afterimage: a fading copy of the robot on the cell it left.
        private SpriteRenderer walkGhost;
        private float ghostTime = -1f;

        // Fail effect, per robot: fell off the row (drops) or entered a bug cell (fades). Kept through the restart for the reappear.
        private const int FailNone = 0;
        private const int FailFell = 1;
        private const int FailBug = 2;
        private readonly int[] failModes = new int[MaxRobots];
        private readonly Vector3[] failStarts = new Vector3[MaxRobots];
        private float failTime = -1f;
        private bool failStarted;
        private float reappearTime = -1f;

        private void Awake()
        {
            cachedTransform = transform;
            int cellCount = MaxRows * BoardState.Columns;
            cells = new SpriteRenderer[cellCount];
            cellTransforms = new Transform[cellCount];
            firewallMarks = new SpriteRenderer[cellCount];
            firewallTransforms = new Transform[cellCount];
            bugMarks = new SpriteRenderer[cellCount];
            bugTransforms = new Transform[cellCount];
            rowHighlights = new SpriteRenderer[MaxRows];
            highlightTransforms = new Transform[MaxRows];
            lockFrames = new SpriteRenderer[MaxRows];
            lockFrameTransforms = new Transform[MaxRows];
            lockLabels = new TextMeshPro[MaxRows];
            valueLabels = new TextMeshPro[MaxRows];
            labelTransforms = new Transform[MaxRows];
            shownValues = new int[MaxRows];
            goalLabels = new TextMeshPro[MaxRows];
            shownGoals = new int[MaxRows];
            shownGoalMet = new bool[MaxRows];

            for (int r = 0; r < MaxRows; r++)
            {
                rowHighlights[r] = CreateSprite("RowHighlight", rowHighlightSprite, 0);
                highlightTransforms[r] = rowHighlights[r].transform;

                // Hollow frame just outside the selection brackets, so a locked row still shows when selected.
                lockFrames[r] = CreateSprite("LockFrame", lockFrameSprite, -1);
                lockFrameTransforms[r] = lockFrames[r].transform;
                lockLabels[r] = new GameObject("LockLabel").AddComponent<TextMeshPro>();
                lockLabels[r].rectTransform.SetParent(cachedTransform, false);
                lockLabels[r].rectTransform.sizeDelta = new Vector2(2f * cellSize, cellSize);
                lockLabels[r].fontSize = labelFontSize * 0.6f;
                lockLabels[r].alignment = TextAlignmentOptions.MidlineRight;
                lockLabels[r].color = lockedRowColor;
                lockLabels[r].SetText("LOCK");

                for (int c = 0; c < BoardState.Columns; c++)
                {
                    int i = r * BoardState.Columns + c;
                    cells[i] = CreateSprite("Cell", gapSprite, 2);
                    cellTransforms[i] = cells[i].transform;
                    // Ring that fills the gap around a firewall cell.
                    firewallMarks[i] = CreateSprite("Firewall", firewallSprite, 1);
                    firewallTransforms[i] = firewallMarks[i].transform;
                    // Diamond on top of the cell, so its shape differs from the boxy robot and exit.
                    bugMarks[i] = CreateSprite("Bug", bugSprite, 3);
                    bugTransforms[i] = bugMarks[i].transform;
                }

                // Adding TextMeshPro replaces the Transform with a RectTransform, so cache it afterwards.
                var labelObject = new GameObject("RowValue");
                valueLabels[r] = labelObject.AddComponent<TextMeshPro>();
                labelTransforms[r] = valueLabels[r].rectTransform;
                labelTransforms[r].SetParent(cachedTransform, false);
                valueLabels[r].fontSize = labelFontSize;
                valueLabels[r].alignment = TextAlignmentOptions.MidlineLeft;
                valueLabels[r].rectTransform.sizeDelta = new Vector2(3f * cellSize, cellSize);
                valueLabels[r].rectTransform.pivot = new Vector2(0f, 0.5f);
                shownValues[r] = -1;

                goalLabels[r] = new GameObject("RowGoal").AddComponent<TextMeshPro>();
                goalLabels[r].rectTransform.SetParent(cachedTransform, false);
                goalLabels[r].fontSize = labelFontSize * 0.6f;
                goalLabels[r].alignment = TextAlignmentOptions.MidlineLeft;
                goalLabels[r].rectTransform.sizeDelta = new Vector2(3f * cellSize, cellSize);
                goalLabels[r].rectTransform.pivot = new Vector2(0f, 0.5f);
                shownGoals[r] = -2;
            }

            exitMarks = new SpriteRenderer[MaxRobots];
            exitTransforms = new Transform[MaxRobots];
            for (int i = 0; i < MaxRobots; i++)
            {
                exitMarks[i] = CreateSprite("Exit", exitLockedSprites[i], 3);
                exitTransforms[i] = exitMarks[i].transform;
            }

            // Outline one sorting order below the robots, so it shows behind the selected one. Its sprite follows the selection.
            selectedFrame = CreateSprite("SelectedRobot", robotOutlineSprites[0], 4);
            selectedFrameTransform = selectedFrame.transform;
            robots = new SpriteRenderer[MaxRobots];
            robotTransforms = new Transform[MaxRobots];
            for (int i = 0; i < MaxRobots; i++)
            {
                robots[i] = CreateSprite("Robot", robotIdleSprites[i], 5);
                robotTransforms[i] = robots[i].transform;
            }

            lostCell = CreateSprite("LostCell", gapSprite, 2);
            lostCellTransform = lostCell.transform;
            lostCell.enabled = false;
            walkGhost = CreateSprite("WalkGhost", robotIdleSprites[0], 4);
            walkGhost.enabled = false;

            shiftsLabel = new GameObject("ShiftsLeft").AddComponent<TextMeshPro>();
            shiftsLabel.rectTransform.SetParent(cachedTransform, false);
            shiftsLabel.fontSize = labelFontSize;
            shiftsLabel.alignment = TextAlignmentOptions.Center;
            shiftsLabel.rectTransform.sizeDelta = new Vector2(BoardState.Columns * cellSize * 2f, cellSize);

            // Only rebuilt when the stage switches between one robot and several (see Render).
            keysLabel = new GameObject("KeyPanel").AddComponent<TextMeshPro>();
            keysLabel.rectTransform.SetParent(cachedTransform, false);
            keysLabel.rectTransform.localPosition = new Vector3(0f, keysPanelY, 0f);
            keysLabel.rectTransform.sizeDelta = new Vector2(BoardState.Columns * cellSize * 2f, cellSize);
            keysLabel.fontSize = keysFontSize;
            keysLabel.alignment = TextAlignmentOptions.Center;
            keysLabel.color = shiftsColor;
            keysLabel.SetText(KeysText);

            hintLabel = new GameObject("StageHint").AddComponent<TextMeshPro>();
            hintLabel.rectTransform.SetParent(cachedTransform, false);
            hintLabel.rectTransform.localPosition = new Vector3(0f, hintY, 0f);
            hintLabel.rectTransform.sizeDelta = new Vector2(BoardState.Columns * cellSize * 2f, cellSize);
            hintLabel.fontSize = hintFontSize;
            hintLabel.alignment = TextAlignmentOptions.Center;
            hintLabel.color = hintColor;
            hintLabel.enabled = false;
        }

        /// <summary>Shows the stage's one-line hint, or hides the hint line when it is empty. Call on stage load only.</summary>
        public void ShowHint(string hint)
        {
            bool hasHint = !string.IsNullOrEmpty(hint);
            hintLabel.enabled = hasHint;
            if (hasHint)
            {
                hintLabel.SetText(hint);
            }
        }

        /// <summary>
        /// Redraws the board. Rows are drawn top to bottom; the robot and exit sit on top of their cell.
        /// The exits (row, column), one per robot in robot order, come from StageData, not BoardState, because they never change during a stage.
        /// With more than one robot each exit takes its robot's color.
        /// <paramref name="parLeft"/> is the shifts still inside par; a negative value hides the PAR text.
        /// <paramref name="rowValues"/> is the decimal value to show for each row, and <paramref name="lockTargets"/> the number-lock
        /// target of each row (-1 for no lock); both are computed by StageManager, so the view never calls BoardLogic.
        /// A row whose value equals its target shows its goal in the "on" color; exits show locked while any lock is unmet.
        /// With <paramref name="robotsCoverBits"/> a 1 cell under a robot is drawn as covered (it counts as 0).
        /// </summary>
        public void Render(BoardState state, int selectedRow, Vector2Int[] exits, int parLeft, int[] rowValues, int[] lockTargets, bool robotsCoverBits)
        {
            // Every Render snaps to the new state: an action during a slide or afterimage finishes it at once.
            StopSlide();
            StopGhost();
            StopFail();
            StopReappear();
            shownRowCount = state.RowCount;
            bool anyLockUnmet = false;
            for (int r = 0; r < MaxRows; r++)
            {
                bool used = r < state.RowCount;
                rowHighlights[r].enabled = used && r == selectedRow;
                bool locked = used && state.IsRowLocked(r);
                lockFrames[r].enabled = locked;
                lockLabels[r].enabled = locked;
                valueLabels[r].enabled = used;
                goalLabels[r].enabled = used && lockTargets[r] >= 0;

                for (int c = 0; c < BoardState.Columns; c++)
                {
                    int i = r * BoardState.Columns + c;
                    cells[i].enabled = used;
                    firewallMarks[i].enabled = used && state.IsFirewall(r, c);
                    bugMarks[i].enabled = used && state.IsBugCell(r, c);
                    if (!used)
                    {
                        continue;
                    }

                    Sprite cellLook = gapSprite;
                    if (state.GetCell(r, c))
                    {
                        cellLook = robotsCoverBits && HasRobotOn(state, r, c) ? coveredSprite : platformSprite;
                    }

                    cells[i].sprite = cellLook;
                    cellTransforms[i].localPosition = CellPosition(r, c, state.RowCount);
                    firewallTransforms[i].localPosition = cellTransforms[i].localPosition;
                    bugTransforms[i].localPosition = cellTransforms[i].localPosition;
                }

                if (!used)
                {
                    continue;
                }

                Vector3 rowCenter = new Vector3(0f, CellPosition(r, 0, state.RowCount).y, 0f);
                highlightTransforms[r].localPosition = rowCenter;
                lockFrameTransforms[r].localPosition = rowCenter;
                lockLabels[r].rectTransform.localPosition = rowCenter + Vector3.left * (BoardState.Columns * 0.5f * cellSize + 1.4f * cellSize);
                labelTransforms[r].localPosition = rowCenter + Vector3.right * (BoardState.Columns * 0.5f * cellSize + 0.5f);

                // Only rebuild the label text when the value changed.
                int value = rowValues[r];
                if (shownValues[r] != value)
                {
                    shownValues[r] = value;
                    valueLabels[r].SetText("{0}", value);
                }

                int goal = lockTargets[r];
                if (goal >= 0)
                {
                    bool met = value == goal;
                    anyLockUnmet |= !met;
                    goalLabels[r].rectTransform.localPosition = labelTransforms[r].localPosition + Vector3.down * (0.7f * cellSize);
                    if (shownGoals[r] != goal || shownGoalMet[r] != met)
                    {
                        shownGoals[r] = goal;
                        shownGoalMet[r] = met;
                        goalLabels[r].color = met ? onColor : lockedRowColor;
                        goalLabels[r].SetText("GOAL {0}", goal);
                    }
                }
            }

            Vector3 standOffset = Vector3.up * (RobotOffsetPixels / PixelsPerUnit);
            Vector3 exitOffset = Vector3.up * (ExitOffsetPixels / PixelsPerUnit);
            bool multiRobot = state.RobotCount > 1;
            for (int i = 0; i < MaxRobots; i++)
            {
                bool present = i < state.RobotCount;
                robots[i].enabled = present;
                if (!present)
                {
                    continue;
                }

                int robotRow = state.GetRobotRow(i);
                int robotCol = state.GetRobotCol(i);
                robotTransforms[i].localPosition = CellPosition(robotRow, robotCol, state.RowCount) + standOffset;
                bool onOwnExit = i < exits.Length && exits[i].x == robotRow && exits[i].y == robotCol;
                // A fallen robot's column is past the row end; it is drawn there, in the air.
                bool failed = robotCol < 0 || robotCol >= BoardState.Columns || state.IsBugCell(robotRow, robotCol);
                robots[i].sprite = failed ? robotFallSprites[i] : onOwnExit ? robotExitSprites[i] : robotIdleSprites[i];
                // The controlled robot draws on top when robots overlap.
                robots[i].sortingOrder = multiRobot && i == state.SelectedRobot ? 6 : 5;
            }

            selectedFrame.enabled = multiRobot;
            if (multiRobot)
            {
                selectedFrame.sprite = robotOutlineSprites[state.SelectedRobot];
                selectedFrameTransform.localPosition = robotTransforms[state.SelectedRobot].localPosition;
            }

            if (multiRobot != shownMultiRobot)
            {
                shownMultiRobot = multiRobot;
                keysLabel.SetText(multiRobot ? KeysTextMultiRobot : KeysText);
            }

            for (int i = 0; i < MaxRobots; i++)
            {
                bool hasExit = i < exits.Length;
                exitMarks[i].enabled = hasExit;
                if (!hasExit)
                {
                    continue;
                }

                exitMarks[i].sprite = anyLockUnmet ? exitLockedSprites[i] : exitOpenSprites[i];
                exitTransforms[i].localPosition = CellPosition(exits[i].x, exits[i].y, state.RowCount) + exitOffset;
            }

            // Sits above the top row; only rebuilt when the count or par changes.
            shiftsLabel.rectTransform.localPosition = new Vector3(0f, CellPosition(0, 0, state.RowCount).y + rowSpacing, 0f);
            if (shownShifts != state.ShiftsLeft || shownPar != parLeft)
            {
                shownShifts = state.ShiftsLeft;
                shownPar = parLeft;
                if (shownShifts > 0)
                {
                    shiftsLabel.color = shiftsColor;
                    if (parLeft >= 0)
                    {
                        shiftsLabel.SetText("HOURS {0}  PAR {1}", shownShifts, parLeft);
                    }
                    else
                    {
                        shiftsLabel.SetText("HOURS {0}", shownShifts);
                    }
                }
                else
                {
                    // Out of shifts the night is over: this label is the "Dawn" message. It comes from the state alone, so a clear,
                    // a fall, an undo or a restart on the same action needs no special case.
                    shiftsLabel.color = noShiftsColor;
                    shiftsLabel.SetText("DAWN  Z undo / R restart");
                }
            }
        }

        /// <summary>Replaces the shifts label with a message until the next Render (e.g. a fall or the end screen).</summary>
        public void ShowMessage(string text)
        {
            shownShifts = -1; // forces the next Render to bring the shifts count back
            shiftsLabel.color = shiftsColor;
            shiftsLabel.SetText(text);
        }

        /// <summary>
        /// Slides row <paramref name="row"/> in from one cell back after a shift (<paramref name="direction"/> -1 left, 1 right).
        /// Call right after the Render that shows the shifted state. <paramref name="lostBit"/> is the value of the cell pushed past the row end.
        /// The cells and the robots standing on the row move; exits, firewalls and bug marks stay, they are fixed to the board.
        /// </summary>
        public void PlayShift(int row, int direction, bool lostBit, BoardState state)
        {
            if (shiftSlideSeconds <= 0f)
            {
                return;
            }

            slideRow = row;
            slideDirection = direction;
            slideTime = 0f;
            // Shifting right pushes out cell 7 and a new 0 enters at cell 0; shifting left the other way round.
            slideEnterCol = direction > 0 ? 0 : BoardState.Columns - 1;
            slideLostCol = direction > 0 ? BoardState.Columns - 1 : 0;
            slideMovesSelectedFrame = false;
            slideSelectedFrameEnd = selectedFrameTransform.localPosition;
            for (int i = 0; i < MaxRobots; i++)
            {
                slideRobots[i] = i < state.RobotCount && state.GetRobotRow(i) == row;
                slideRobotEnds[i] = robotTransforms[i].localPosition;
                slideMovesSelectedFrame |= slideRobots[i] && selectedFrame.enabled && i == state.SelectedRobot;
            }

            lostCell.sprite = lostBit ? platformSprite : gapSprite;
            lostCell.enabled = true;
            ApplySlide(0f);
        }

        /// <summary>
        /// Leaves a fading copy of robot <paramref name="robot"/> on the cell it just left (walk or descend).
        /// Call right after the Render that shows the robot on its new cell.
        /// </summary>
        public void PlayWalk(int robot, int fromRow, int fromCol)
        {
            if (walkGhostSeconds <= 0f)
            {
                return;
            }

            walkGhost.sprite = robotIdleSprites[robot];
            walkGhost.transform.localPosition = CellPosition(fromRow, fromCol, shownRowCount) + Vector3.up * (RobotOffsetPixels / PixelsPerUnit);
            walkGhost.enabled = true;
            ghostTime = 0f;
            SetAlpha(walkGhost, walkGhostAlpha);
        }

        /// <summary>
        /// Starts the fail effect on a failed board: a robot past its row end drops and fades, a robot on a bug cell fades out.
        /// Call right after the Render that shows the failed state; it waits for a running shift slide to finish first.
        /// </summary>
        public void PlayFail(BoardState state)
        {
            for (int i = 0; i < MaxRobots; i++)
            {
                failModes[i] = FailNone;
                if (i >= state.RobotCount)
                {
                    continue;
                }

                int row = state.GetRobotRow(i);
                int col = state.GetRobotCol(i);
                if (col < 0 || col >= BoardState.Columns)
                {
                    failModes[i] = FailFell;
                }
                else if (state.IsBugCell(row, col))
                {
                    failModes[i] = FailBug;
                }
            }

            failTime = 0f;
            failStarted = false;
            selectedFrame.enabled = false;
        }

        /// <summary>Fades the robots that failed back in at their start cells. Call right after the Render that shows the restarted stage.</summary>
        public void PlayReappear()
        {
            if (reappearSeconds <= 0f)
            {
                return;
            }

            reappearTime = 0f;
            SetFailedRobotsAlpha(0f);
        }

        private void Update()
        {
            // The fail effect starts once the shift slide that caused it has finished.
            if (failTime >= 0f && slideRow < 0)
            {
                UpdateFail();
            }

            if (reappearTime >= 0f)
            {
                reappearTime += Time.deltaTime;
                if (reappearTime >= reappearSeconds)
                {
                    StopReappear();
                }
                else
                {
                    SetFailedRobotsAlpha(reappearTime / reappearSeconds);
                }
            }

            if (slideRow >= 0)
            {
                slideTime += Time.deltaTime;
                float t = slideTime / shiftSlideSeconds;
                if (t >= 1f)
                {
                    StopSlide();
                }
                else
                {
                    ApplySlide(t);
                }
            }

            if (ghostTime >= 0f)
            {
                ghostTime += Time.deltaTime;
                float t = ghostTime / walkGhostSeconds;
                if (t >= 1f)
                {
                    StopGhost();
                }
                else
                {
                    SetAlpha(walkGhost, walkGhostAlpha * (1f - t));
                }
            }
        }

        // t is 0..1 linear time; the motion eases out. Positions snap to whole art pixels so the pixel art stays crisp.
        private void ApplySlide(float t)
        {
            float eased = 1f - (1f - t) * (1f - t);
            float offsetPixels = Mathf.Round((1f - eased) * -slideDirection * PixelsPerUnit * cellSize);
            Vector3 offset = Vector3.right * (offsetPixels / PixelsPerUnit);

            for (int c = 0; c < BoardState.Columns; c++)
            {
                int i = slideRow * BoardState.Columns + c;
                cellTransforms[i].localPosition = CellPosition(slideRow, c, shownRowCount) + offset;
            }

            SetAlpha(cells[slideRow * BoardState.Columns + slideEnterCol], eased);
            lostCellTransform.localPosition = CellPosition(slideRow, slideLostCol, shownRowCount) + Vector3.right * (slideDirection * cellSize) + offset;
            SetAlpha(lostCell, 1f - eased);

            for (int i = 0; i < MaxRobots; i++)
            {
                if (slideRobots[i])
                {
                    robotTransforms[i].localPosition = slideRobotEnds[i] + offset;
                }
            }

            if (slideMovesSelectedFrame)
            {
                selectedFrameTransform.localPosition = slideSelectedFrameEnd + offset;
            }
        }

        private void StopSlide()
        {
            if (slideRow < 0)
            {
                return;
            }

            int row = slideRow;
            ApplySlide(1f);
            SetAlpha(cells[row * BoardState.Columns + slideEnterCol], 1f);
            lostCell.enabled = false;
            slideRow = -1;
        }

        private void UpdateFail()
        {
            if (!failStarted)
            {
                failStarted = true;
                for (int i = 0; i < MaxRobots; i++)
                {
                    failStarts[i] = robotTransforms[i].localPosition;
                }
            }

            failTime += Time.deltaTime;
            for (int i = 0; i < MaxRobots; i++)
            {
                if (failModes[i] == FailFell)
                {
                    // Accelerates like gravity (distance grows with time squared), snapped to whole art pixels; fades over the second half.
                    float p = fallSeconds > 0f ? Mathf.Min(1f, failTime / fallSeconds) : 1f;
                    float dropPixels = Mathf.Round(fallPixels * p * p);
                    robotTransforms[i].localPosition = failStarts[i] + Vector3.down * (dropPixels / PixelsPerUnit);
                    SetAlpha(robots[i], p < 0.5f ? 1f : 2f * (1f - p));
                }
                else if (failModes[i] == FailBug)
                {
                    float p = bugFadeSeconds > 0f ? Mathf.Min(1f, failTime / bugFadeSeconds) : 1f;
                    SetAlpha(robots[i], 1f - p);
                }
            }
        }

        // Keeps failModes: the restart's Render runs this, then PlayReappear still needs to know which robots failed.
        private void StopFail()
        {
            if (failTime < 0f)
            {
                return;
            }

            failTime = -1f;
            SetFailedRobotsAlpha(1f);
        }

        private void StopReappear()
        {
            if (reappearTime < 0f)
            {
                return;
            }

            reappearTime = -1f;
            SetFailedRobotsAlpha(1f);
            for (int i = 0; i < MaxRobots; i++)
            {
                failModes[i] = FailNone;
            }
        }

        private void SetFailedRobotsAlpha(float alpha)
        {
            for (int i = 0; i < MaxRobots; i++)
            {
                if (failModes[i] != FailNone)
                {
                    SetAlpha(robots[i], alpha);
                }
            }
        }

        private void StopGhost()
        {
            ghostTime = -1f;
            walkGhost.enabled = false;
        }

        private static void SetAlpha(SpriteRenderer spriteRenderer, float alpha)
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }

        private Vector3 CellPosition(int row, int col, int rowCount)
        {
            float x = (col - (BoardState.Columns - 1) * 0.5f) * cellSize;
            float y = ((rowCount - 1) * 0.5f - row) * rowSpacing;
            return new Vector3(x, y, 0f);
        }

        // A fallen robot's column is outside the row, so it never matches a cell.
        private static bool HasRobotOn(BoardState state, int row, int col)
        {
            for (int i = 0; i < state.RobotCount; i++)
            {
                if (state.GetRobotRow(i) == row && state.GetRobotCol(i) == col)
                {
                    return true;
                }
            }

            return false;
        }

        private SpriteRenderer CreateSprite(string objectName, Sprite sprite, int sortingOrder)
        {
            var spriteObject = new GameObject(objectName);
            spriteObject.transform.SetParent(cachedTransform, false);
            var spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = sortingOrder;
            return spriteRenderer;
        }
    }
}
