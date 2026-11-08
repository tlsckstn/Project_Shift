using ProjectShift.Stage;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

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
        internal const float PixelsPerUnit = 36f;
        // Robot sprite centre above the cell centre: its tracks (20 px sprite, bottom row empty) rest on the cell top.
        private const float RobotOffsetPixels = 25f;
        // Exit sprite centre above the cell centre: the 28 px door frame's bottom touches the cell top.
        private const float ExitOffsetPixels = 30f;
        // Must match the Puzzle map in Assets/InputSystem_Actions.inputactions.
        // Both key guides must stay inside the circuit keep-out (476 art px wide): 470 and 473 px measured.
        private static readonly string KeysText =
            PixelText.Key("Left") + PixelText.Key("Right") + " WALK  " + PixelText.Key("Down") + " DESCEND  " + PixelText.Key("Tab") + " SELECT ROW  " +
            PixelText.Key("A") + PixelText.Key("D") + " SHIFT  " + PixelText.Key("Z") + " UNDO  " + PixelText.Key("R") + " RESTART  " + PixelText.Key("M") + " MUTE  " + PixelText.Key("Esc") + " STAGES";
        // Out of shifts: replaces the HOURS line.
        private static readonly string DawnText = "DAWN  " + PixelText.Key("Z") + " UNDO  " + PixelText.Key("R") + " RESTART";
        // Replaces the key panel on a cleared board.
        private static readonly string ClearChoiceText = PixelText.Key("R") + " RETRY   " + PixelText.Key("Enter") + " NEXT   " + PixelText.Key("Esc") + " STAGES";
        private static readonly string AllClearKeysText = PixelText.Key("Enter") + " TITLE";
        private static readonly string SelectKeysText =
            PixelText.Key("Left") + PixelText.Key("Right") + PixelText.Key("Up") + PixelText.Key("Down") + " SELECT   " + PixelText.Key("Enter") + " PLAY   " + PixelText.Key("M") + " MUTE   " + PixelText.Key("Esc") + " TITLE";
        // Working title; the final name is picked in Story 006 (release cleanup).
        private const string GameTitle = "NIGHT SHIFT <<";
        private static readonly string TitleKeysText = PixelText.Key("Enter") + " START";
        // Shown instead of KeysText on stages with more than one robot. Shorter words keep it inside the circuit keep-out.
        private static readonly string KeysTextMultiRobot =
            PixelText.Key("Left") + PixelText.Key("Right") + " WALK  " + PixelText.Key("Down") + " DESCEND  " + PixelText.Key("Tab") + " ROW  " +
            PixelText.Key("A") + PixelText.Key("D") + " SHIFT  " + PixelText.Key("Q") + " ROBOT  " + PixelText.Key("Z") + " UNDO  " + PixelText.Key("R") + " RESET  " + PixelText.Key("M") + " MUTE  " + PixelText.Key("Esc") + " STAGES";

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
        [Tooltip("Per crew: bright exit, shown briefly when its robot arrives and during the stage-clear clock-out.")]
        [SerializeField] private Sprite[] exitFlashSprites;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private float rowSpacing = 2f;
        [Tooltip("Silkscreen bitmap font asset for every label on the board (Assets/01_Art/Fonts).")]
        [FormerlySerializedAs("keysFont")]
        [SerializeField] private TMP_FontAsset pixelFont;
        [Tooltip("Key cap sprites used inline in the key panel and the Dawn message; sprite names match Key(...) calls.")]
        [SerializeField] private TMP_SpriteAsset keyCapSprites;
        [Tooltip("Local y of the key panel; keep it inside the camera's bottom edge (orthographic size 5).")]
        [SerializeField] private float keysPanelY = -4.3f;
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
        [Tooltip("Seconds an exit flashes bright when its robot arrives.")]
        [SerializeField] private float exitFlashSeconds = 0.12f;
        [Tooltip("Stage clear: seconds every robot holds its arrival face before clocking out.")]
        [SerializeField] private float clearHoldSeconds = 0.2f;
        [Tooltip("Stage clear: seconds the robots take to rise into their bright exits and fade. Keep slide + hold + this under StageManager's clear delay.")]
        [SerializeField] private float clockOutSeconds = 0.4f;
        [Tooltip("Stage clear: how far (art pixels) the robots rise while clocking out.")]
        [SerializeField] private float clockOutRisePixels = 3f;
        [Tooltip("Stage transition: seconds per one-cell shift. The old rows shift out right in 8 steps, then the new rows shift in.")]
        [SerializeField] private float transitionStepSeconds = 0.035f;
        [Tooltip("Stage transition: empty pause between the old rows leaving and the new rows arriving.")]
        [SerializeField] private float transitionGapSeconds = 0.1f;
        [SerializeField] private Color shiftsColor = Color.white;
        [SerializeField] private Color noShiftsColor = new Color(1f, 0.3f, 0.3f);
        [Tooltip("Full-screen 640x360 circuit layer behind the board while playing.")]
        [SerializeField] private Sprite circuitPlayingSprite;
        [Tooltip("The same circuit one step brighter, shown with the clear background while the robots clock out.")]
        [SerializeField] private Sprite circuitClearSprite;
        [Tooltip("Chapter zone decor behind the board while playing, indexed by Zone (Ram, Bus, Cores, Alu).")]
        [SerializeField] private Sprite[] zonePlayingSprites;
        [Tooltip("The same zone decor one step brighter, faded in on a stage clear.")]
        [SerializeField] private Sprite[] zoneClearSprites;
        [SerializeField] private Color playingBackground = new Color32(0x14, 0x1A, 0x26, 0xFF);
        [SerializeField] private Color clearBackground = new Color32(0x17, 0x1F, 0x2F, 0xFF);
        [Tooltip("Draws the stage select pages (computer-part zones).")]
        [SerializeField] private StageSelectView stageSelect;

        private Transform cachedTransform;
        // Parent of the whole stage (cells, row values, HUD lines, decor): hidden as one while the stage select shows.
        private Transform playRoot;
        // Parent of everything drawn on the board except the cells and row values: hidden as one while a stage transition runs.
        private Transform decorRoot;
        // Title screen: the game title, built on first show. Shares the circuit and key panel like the stage select.
        private Transform titleRoot;
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
        private int[] shownValues;
        // Number-lock goal under each row value ("GOAL 176", "GOAL 176 OK" once met), only shown on rows that have a lock.
        private TextMeshPro[] goalLabels;
        private int[] shownGoals;
        private bool[] shownGoalMet;
        private SpriteRenderer[] robots;
        private Transform[] robotTransforms;
        private SpriteRenderer selectedFrame;
        private Transform selectedFrameTransform;
        private TextMeshPro keysLabel;
        private bool shownMultiRobot;
        // Set while the key panel shows the clear choice; the next Render brings the key guide back.
        private bool isKeysTextReplaced;
        // One exit per robot, same order as the robots.
        private SpriteRenderer[] exitMarks;
        private Transform[] exitTransforms;
        private TextMeshPro shiftsLabel;
        private TextMeshPro hintLabel;
        // Fixed background: never moves. The brighter clear circuit sits on top of the night one and fades in on a stage clear ("dawn").
        private SpriteRenderer circuitClear;
        private SpriteRenderer zonePlaying;
        private SpriteRenderer zoneClear;
        // Set by SetZone, shown by the next transition (see ApplyPendingZone).
        private Zone pendingZone;
        private bool hasPendingZone;
        private Camera backgroundCamera;
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

        // Exit arrival flash, per exit: -1 when idle. The sprite to go back to is the one Render chose.
        private readonly float[] exitFlashTimes = { -1f, -1f, -1f };
        private readonly Sprite[] exitRestSprites = new Sprite[MaxRobots];
        // Stage clear clock-out: hold, then the robots rise and fade while every exit shows bright.
        private float clearTime = -1f;
        private bool clearStarted;
        private readonly Vector3[] clearStarts = new Vector3[MaxRobots];

        // Stage transition: Render has already drawn the new stage; the cells and row values are overridden step by step
        // and restored from what Render chose when it ends.
        private float transitionTime = -1f;
        private int transitionStep = -1;
        private int transitionOldRowCount;
        private readonly byte[] transitionOldRows = new byte[MaxRows];
        private readonly byte[] transitionNewRows = new byte[MaxRows];
        private Sprite[] restCellSprites;

        /// <summary>True while a stage transition plays; StageManager ignores input meanwhile.</summary>
        public bool IsTransitioning => transitionTime >= 0f;

        /// <summary>Raised on every cell step of the stage transition: false while the old rows leave, true while the new rows arrive.</summary>
        public event System.Action<bool> TransitionStepped;

        private void Awake()
        {
            cachedTransform = transform;
            playRoot = new GameObject("Play").transform;
            playRoot.SetParent(cachedTransform, false);
            titleRoot = new GameObject("Title").transform;
            titleRoot.SetParent(cachedTransform, false);
            titleRoot.gameObject.SetActive(false);
            decorRoot = new GameObject("Decor").transform;
            decorRoot.SetParent(playRoot, false);
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
            shownValues = new int[MaxRows];
            goalLabels = new TextMeshPro[MaxRows];
            shownGoals = new int[MaxRows];
            shownGoalMet = new bool[MaxRows];

            for (int r = 0; r < MaxRows; r++)
            {
                rowHighlights[r] = CreateSprite("RowHighlight", rowHighlightSprite, 0, decorRoot);
                highlightTransforms[r] = rowHighlights[r].transform;

                // Hollow frame just outside the selection brackets, so a locked row still shows when selected.
                lockFrames[r] = CreateSprite("LockFrame", lockFrameSprite, -1, decorRoot);
                lockFrameTransforms[r] = lockFrames[r].transform;
                lockLabels[r] = new GameObject("LockLabel").AddComponent<TextMeshPro>();
                lockLabels[r].rectTransform.SetParent(decorRoot, false);
                lockLabels[r].rectTransform.sizeDelta = new Vector2(2f * cellSize, cellSize);
                lockLabels[r].font = pixelFont;
                lockLabels[r].fontSize = PixelText.FontSize;
                lockLabels[r].alignment = TextAlignmentOptions.MidlineRight;
                lockLabels[r].color = lockedRowColor;
                lockLabels[r].SetText("LOCK");

                for (int c = 0; c < BoardState.Columns; c++)
                {
                    int i = r * BoardState.Columns + c;
                    cells[i] = CreateSprite("Cell", gapSprite, 2, playRoot);
                    cellTransforms[i] = cells[i].transform;
                    // Ring that fills the gap around a firewall cell.
                    firewallMarks[i] = CreateSprite("Firewall", firewallSprite, 1, decorRoot);
                    firewallTransforms[i] = firewallMarks[i].transform;
                    // Diamond on top of the cell, so its shape differs from the boxy robot and exit.
                    bugMarks[i] = CreateSprite("Bug", bugSprite, 3, decorRoot);
                    bugTransforms[i] = bugMarks[i].transform;
                }

                valueLabels[r] = new GameObject("RowValue").AddComponent<TextMeshPro>();
                valueLabels[r].rectTransform.SetParent(playRoot, false);
                valueLabels[r].font = pixelFont;
                valueLabels[r].fontSize = PixelText.LargeFontSize;
                valueLabels[r].alignment = TextAlignmentOptions.MidlineLeft;
                valueLabels[r].rectTransform.sizeDelta = new Vector2(3f * cellSize, cellSize);
                valueLabels[r].rectTransform.pivot = new Vector2(0f, 0.5f);
                shownValues[r] = -1;

                goalLabels[r] = new GameObject("RowGoal").AddComponent<TextMeshPro>();
                goalLabels[r].rectTransform.SetParent(decorRoot, false);
                goalLabels[r].font = pixelFont;
                goalLabels[r].fontSize = PixelText.FontSize;
                goalLabels[r].alignment = TextAlignmentOptions.MidlineLeft;
                goalLabels[r].rectTransform.sizeDelta = new Vector2(3f * cellSize, cellSize);
                goalLabels[r].rectTransform.pivot = new Vector2(0f, 0.5f);
                shownGoals[r] = -2;
            }

            exitMarks = new SpriteRenderer[MaxRobots];
            exitTransforms = new Transform[MaxRobots];
            for (int i = 0; i < MaxRobots; i++)
            {
                exitMarks[i] = CreateSprite("Exit", exitLockedSprites[i], 3, decorRoot);
                exitTransforms[i] = exitMarks[i].transform;
            }

            // Outline one sorting order below the robots, so it shows behind the selected one. Its sprite follows the selection.
            selectedFrame = CreateSprite("SelectedRobot", robotOutlineSprites[0], 4, decorRoot);
            selectedFrameTransform = selectedFrame.transform;
            robots = new SpriteRenderer[MaxRobots];
            robotTransforms = new Transform[MaxRobots];
            for (int i = 0; i < MaxRobots; i++)
            {
                robots[i] = CreateSprite("Robot", robotIdleSprites[i], 5, decorRoot);
                robotTransforms[i] = robots[i].transform;
            }

            // Below everything; centred on the board root, which sits at the camera centre.
            CreateSprite("Circuit", circuitPlayingSprite, -10, cachedTransform);
            circuitClear = CreateSprite("CircuitClear", circuitClearSprite, -9, cachedTransform);
            zonePlaying = CreateSprite("ZonePlaying", null, -8, playRoot);
            zoneClear = CreateSprite("ZoneClear", null, -7, playRoot);
            backgroundCamera = Camera.main;
            SetClearAmount(0f);

            lostCell = CreateSprite("LostCell", gapSprite, 2, decorRoot);
            lostCellTransform = lostCell.transform;
            lostCell.enabled = false;
            walkGhost = CreateSprite("WalkGhost", robotIdleSprites[0], 4, decorRoot);
            walkGhost.enabled = false;

            shiftsLabel = new GameObject("ShiftsLeft").AddComponent<TextMeshPro>();
            shiftsLabel.rectTransform.SetParent(playRoot, false);
            shiftsLabel.font = pixelFont;
            shiftsLabel.spriteAsset = keyCapSprites;
            shiftsLabel.fontSize = PixelText.LargeFontSize;
            shiftsLabel.alignment = TextAlignmentOptions.Center;
            shiftsLabel.rectTransform.sizeDelta = new Vector2(BoardState.Columns * cellSize * 2f, cellSize);

            // Only rebuilt when the stage switches between one robot and several (see Render).
            keysLabel = new GameObject("KeyPanel").AddComponent<TextMeshPro>();
            keysLabel.rectTransform.SetParent(cachedTransform, false);
            keysLabel.rectTransform.sizeDelta = new Vector2(BoardState.Columns * cellSize * 2f, cellSize);
            keysLabel.font = pixelFont;
            keysLabel.spriteAsset = keyCapSprites;
            keysLabel.fontSize = PixelText.FontSize;
            keysLabel.alignment = TextAlignmentOptions.Center;
            keysLabel.color = shiftsColor;
            SetKeysText(KeysText);

            hintLabel = new GameObject("StageHint").AddComponent<TextMeshPro>();
            hintLabel.rectTransform.SetParent(playRoot, false);
            hintLabel.rectTransform.sizeDelta = new Vector2(BoardState.Columns * cellSize * 2f, cellSize);
            hintLabel.font = pixelFont;
            hintLabel.fontSize = PixelText.FontSize;
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
                PixelText.Place(hintLabel, new Vector3(0f, hintY, 0f));
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
            StopExitFlashes();
            StopClear();
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
                PixelText.Place(lockLabels[r], rowCenter + Vector3.left * (BoardState.Columns * 0.5f * cellSize + 1.4f * cellSize));

                // Only rebuild the label text when the value changed.
                int value = rowValues[r];
                if (shownValues[r] != value)
                {
                    shownValues[r] = value;
                    valueLabels[r].SetText("{0}", value);
                }

                PixelText.Place(valueLabels[r], LabelPosition(r, state.RowCount));

                int goal = lockTargets[r];
                if (goal >= 0)
                {
                    bool met = value == goal;
                    anyLockUnmet |= !met;
                    if (shownGoals[r] != goal || shownGoalMet[r] != met)
                    {
                        shownGoals[r] = goal;
                        shownGoalMet[r] = met;
                        goalLabels[r].color = met ? onColor : lockedRowColor;
                        // OK backs up the gray-to-green change for colour-blind players (Silkscreen has no check glyph).
                        goalLabels[r].SetText(met ? "GOAL {0} OK" : "GOAL {0}", goal);
                    }

                    PixelText.Place(goalLabels[r], LabelPosition(r, state.RowCount) + Vector3.down * (0.7f * cellSize));
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

            if (multiRobot != shownMultiRobot || isKeysTextReplaced)
            {
                shownMultiRobot = multiRobot;
                isKeysTextReplaced = false;
                SetKeysText(multiRobot ? KeysTextMultiRobot : KeysText);
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
                    shiftsLabel.SetText(DawnText);
                }
            }

            PixelText.Place(shiftsLabel, ShiftsLabelPosition());
        }

        /// <summary>Replaces the shifts label with a message until the next Render (e.g. a fall or the end screen).</summary>
        public void ShowMessage(string text)
        {
            shownShifts = -1; // forces the next Render to bring the shifts count back
            shiftsLabel.color = shiftsColor;
            shiftsLabel.SetText(text);
            PixelText.Place(shiftsLabel, ShiftsLabelPosition());
        }

        /// <summary>
        /// Shows the cleared stage's result (shifts used, and par when the stage has one) and the retry / next keys until the
        /// next Render. The clear look stays because nothing renders meanwhile.
        /// </summary>
        public void ShowClearChoice(int shiftsUsed, int par, int stars)
        {
            string result = par > 0 ? $"SHIFTS {shiftsUsed}  PAR {par}" : $"SHIFTS {shiftsUsed}";
            ShowMessage(stars > 0 ? result + "  " + PixelText.Stars(stars) : result);
            SetKeysText(ClearChoiceText);
            isKeysTextReplaced = true;
        }

        /// <summary>End screen after the last stage: the ALL CLEAR message, and Enter leads to the stage select.</summary>
        public void ShowAllClear()
        {
            ShowMessage("ALL CLEAR");
            SetKeysText(AllClearKeysText);
            isKeysTextReplaced = true;
        }

        /// <summary>
        /// Hides the stage and shows one stage select page through StageSelectView.Show (same parameters), with the select
        /// key guide. Call again to move the cursor or turn the page.
        /// </summary>
        public void ShowStageSelect(int pageStart, int pageCount, string title, int page, int pageTotal, int chapter, int unlocked, int selected, int[] stars, int[] firstLockTargets)
        {
            StopSlide();
            StopGhost();
            StopFail();
            StopReappear();
            StopExitFlashes();
            StopClear();
            playRoot.gameObject.SetActive(false);
            titleRoot.gameObject.SetActive(false);
            stageSelect.Show(pageStart, pageCount, title, page, pageTotal, chapter, unlocked, selected, stars, firstLockTargets);
            SetKeysText(SelectKeysText);
            isKeysTextReplaced = true;
        }

        /// <summary>Hides the stage select and shows the stage again. Call before loading the stage to play.</summary>
        public void HideStageSelect()
        {
            stageSelect.Hide();
            playRoot.gameObject.SetActive(true);
        }

        /// <summary>Hides the stage and the stage select and shows the title screen with the Enter START key guide.</summary>
        public void ShowTitle()
        {
            StopSlide();
            StopGhost();
            StopFail();
            StopReappear();
            StopExitFlashes();
            StopClear();
            playRoot.gameObject.SetActive(false);
            stageSelect.Hide();
            // Active before building: TMP only lays out active text.
            titleRoot.gameObject.SetActive(true);
            if (titleRoot.childCount == 0)
            {
                TextMeshPro title = PixelText.CreateLabel("GameTitle", titleRoot, pixelFont, BoardState.Columns * cellSize * 2f);
                title.fontSize = PixelText.LargeFontSize;
                title.color = shiftsColor;
                title.SetText(GameTitle);
                // Board centre: inside the circuit keep-out of the 3-row board.
                PixelText.Place(title, Vector3.zero);
            }

            SetKeysText(TitleKeysText);
            isKeysTextReplaced = true;
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

        /// <summary>Flashes exit <paramref name="exit"/> bright for a moment. Call right after the Render that shows its robot arriving.</summary>
        public void PlayExitFlash(int exit)
        {
            exitRestSprites[exit] = exitMarks[exit].sprite;
            exitFlashTimes[exit] = 0f;
        }

        /// <summary>
        /// Stage clear: every robot holds its arrival face, then rises into its exit and fades while the exits show bright.
        /// Call right after the Render that shows the clearing move; it waits for a running shift slide to finish first.
        /// </summary>
        public void PlayClear()
        {
            clearTime = 0f;
            clearStarted = false;
        }

        /// <summary>
        /// Plays the stage transition: the rows of <paramref name="from"/> shift right one cell at a time until empty, then the rows
        /// of <paramref name="to"/> shift in from the left the same way; robots, exits and marks appear once the rows are in.
        /// Pass null for <paramref name="from"/> to play only the arrival (first stage). Call right after the Render that shows <paramref name="to"/>.
        /// </summary>
        public void PlayTransition(BoardState from, BoardState to)
        {
            transitionOldRowCount = from == null ? 0 : from.RowCount;
            for (int r = 0; r < MaxRows; r++)
            {
                transitionOldRows[r] = r < transitionOldRowCount ? from.GetRow(r) : (byte)0;
                transitionNewRows[r] = r < to.RowCount ? to.GetRow(r) : (byte)0;
            }

            if (restCellSprites == null)
            {
                restCellSprites = new Sprite[cells.Length];
            }

            for (int i = 0; i < cells.Length; i++)
            {
                restCellSprites[i] = cells[i].sprite;
            }

            decorRoot.gameObject.SetActive(false);
            transitionTime = 0f;
            transitionStep = -1;
            UpdateTransition();
        }

        private void Update()
        {
            if (transitionTime >= 0f)
            {
                transitionTime += Time.deltaTime;
                UpdateTransition();
            }

            // These effects start once the shift slide that caused them has finished.
            if (slideRow < 0)
            {
                if (failTime >= 0f)
                {
                    UpdateFail();
                }

                UpdateExitFlashes();
                if (clearTime >= 0f)
                {
                    UpdateClear();
                }
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

        // Steps: 1..8 shift the old rows out (skipped without old rows), one empty step for the gap, then 8 steps shift the new rows in.
        private void UpdateTransition()
        {
            // A zero step time would divide by zero below and never end the transition, locking input.
            if (transitionStepSeconds <= 0f)
            {
                EndTransition();
                return;
            }

            int outSteps = transitionOldRowCount > 0 ? BoardState.Columns : 0;
            int gapSteps = outSteps > 0 ? 1 : 0;
            float outSeconds = outSteps * transitionStepSeconds;
            float gapSeconds = gapSteps * transitionGapSeconds;
            int step;
            if (transitionTime < outSeconds)
            {
                step = Mathf.FloorToInt(transitionTime / transitionStepSeconds) + 1;
            }
            else if (transitionTime < outSeconds + gapSeconds)
            {
                step = outSteps + 1;
            }
            else
            {
                step = outSteps + gapSteps + 1 + Mathf.FloorToInt((transitionTime - outSeconds - gapSeconds) / transitionStepSeconds);
            }

            if (step == transitionStep)
            {
                return;
            }

            transitionStep = step;
            // The old rows are gone from here on; a long frame can skip the gap step, so this is not tied to it.
            if (step > outSteps)
            {
                ApplyPendingZone();
            }

            int inStep = step - outSteps - gapSteps;
            if (step <= outSteps)
            {
                // Old rows pushed right: bits leave past cell 7, zeros come in at cell 0.
                ShowTransitionRows(transitionOldRows, transitionOldRowCount, step, 0);
                TransitionStepped?.Invoke(false);
            }
            else if (inStep <= 0)
            {
                ShowTransitionRows(transitionOldRows, 0, 0, 0);
            }
            else if (inStep <= BoardState.Columns)
            {
                // New rows arrive from the left: at in-step k the row is still 8 - k cells short of its place.
                ShowTransitionRows(transitionNewRows, shownRowCount, 0, BoardState.Columns - inStep);
                TransitionStepped?.Invoke(true);
            }
            else
            {
                EndTransition();
            }
        }

        private void ShowTransitionRows(byte[] rows, int rowCount, int shiftRight, int shiftLeft)
        {
            for (int r = 0; r < MaxRows; r++)
            {
                bool used = r < rowCount;
                valueLabels[r].enabled = used;
                byte value = used ? (byte)((rows[r] >> shiftRight) << shiftLeft) : (byte)0;
                for (int c = 0; c < BoardState.Columns; c++)
                {
                    int i = r * BoardState.Columns + c;
                    cells[i].enabled = used;
                    if (used)
                    {
                        // Cell 0 is the top bit, as in BoardState.
                        bool on = (value & (1 << (BoardState.Columns - 1 - c))) != 0;
                        cells[i].sprite = on ? platformSprite : gapSprite;
                        cellTransforms[i].localPosition = CellPosition(r, c, rowCount);
                    }
                }

                if (used)
                {
                    valueLabels[r].SetText("{0}", value);
                    PixelText.Place(valueLabels[r], LabelPosition(r, rowCount));
                }
            }
        }

        // Puts back exactly what the Render of the new stage drew.
        private void EndTransition()
        {
            transitionTime = -1f;
            ApplyPendingZone();
            for (int r = 0; r < MaxRows; r++)
            {
                bool used = r < shownRowCount;
                valueLabels[r].enabled = used;
                for (int c = 0; c < BoardState.Columns; c++)
                {
                    int i = r * BoardState.Columns + c;
                    cells[i].enabled = used;
                    cells[i].sprite = restCellSprites[i];
                    cellTransforms[i].localPosition = CellPosition(r, c, shownRowCount);
                }

                if (used)
                {
                    valueLabels[r].SetText("{0}", shownValues[r]);
                    PixelText.Place(valueLabels[r], LabelPosition(r, shownRowCount));
                }
            }

            decorRoot.gameObject.SetActive(true);
        }

        private void UpdateExitFlashes()
        {
            for (int i = 0; i < MaxRobots; i++)
            {
                if (exitFlashTimes[i] < 0f)
                {
                    continue;
                }

                exitFlashTimes[i] += Time.deltaTime;
                bool flashing = exitFlashTimes[i] < exitFlashSeconds;
                exitMarks[i].sprite = flashing ? exitFlashSprites[i] : exitRestSprites[i];
                if (!flashing)
                {
                    exitFlashTimes[i] = -1f;
                }
            }
        }

        private void UpdateClear()
        {
            if (!clearStarted)
            {
                clearStarted = true;
                for (int i = 0; i < MaxRobots; i++)
                {
                    clearStarts[i] = robotTransforms[i].localPosition;
                }
            }

            clearTime += Time.deltaTime;
            if (clearTime < clearHoldSeconds)
            {
                return;
            }

            float p = clockOutSeconds > 0f ? Mathf.Min(1f, (clearTime - clearHoldSeconds) / clockOutSeconds) : 1f;
            float risePixels = Mathf.Round(clockOutRisePixels * p);
            // The screen brightens as the robots fade.
            SetClearAmount(p);
            selectedFrame.enabled = false;
            for (int i = 0; i < MaxRobots; i++)
            {
                if (exitMarks[i].enabled)
                {
                    exitMarks[i].sprite = exitFlashSprites[i];
                    exitFlashTimes[i] = -1f;
                }

                if (robots[i].enabled)
                {
                    robotTransforms[i].localPosition = clearStarts[i] + Vector3.up * (risePixels / PixelsPerUnit);
                    SetAlpha(robots[i], 1f - p);
                }
            }
        }

        private void StopExitFlashes()
        {
            for (int i = 0; i < MaxRobots; i++)
            {
                exitFlashTimes[i] = -1f;
            }
        }

        private void StopClear()
        {
            if (clearTime < 0f)
            {
                return;
            }

            clearTime = -1f;
            SetClearAmount(0f);
            for (int i = 0; i < MaxRobots; i++)
            {
                SetAlpha(robots[i], 1f);
            }
        }

        /// <summary>
        /// Picks the chapter zone decor behind the board. It shows from the next PlayTransition: at once when no old rows
        /// leave, otherwise once they have left, so a chapter change swaps the decor on the empty board.
        /// </summary>
        public void SetZone(Zone zone)
        {
            pendingZone = zone;
            hasPendingZone = true;
        }

        private void ApplyPendingZone()
        {
            if (!hasPendingZone)
            {
                return;
            }

            hasPendingZone = false;
            zonePlaying.sprite = zonePlayingSprites[(int)pendingZone];
            zoneClear.sprite = zoneClearSprites[(int)pendingZone];
        }

        // 0 = night look, 1 = clear ("dawn") look; in between the brighter circuit fades in over the night one.
        private void SetClearAmount(float amount)
        {
            circuitClear.enabled = amount > 0f;
            SetAlpha(circuitClear, amount);
            zoneClear.enabled = amount > 0f;
            SetAlpha(zoneClear, amount);
            // No camera tagged MainCamera: keep the board working, only the background colour stays put.
            if (backgroundCamera != null)
            {
                backgroundCamera.backgroundColor = Color.Lerp(playingBackground, clearBackground, amount);
            }
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

        // Row value label: right of the row, fixed while the cells shift.
        private Vector3 LabelPosition(int row, int rowCount)
        {
            return new Vector3((BoardState.Columns * 0.5f * cellSize) + 0.5f, CellPosition(row, 0, rowCount).y, 0f);
        }

        private void SetKeysText(string text)
        {
            keysLabel.SetText(text);
            PixelText.Place(keysLabel, new Vector3(0f, keysPanelY, 0f));
        }

        // Above the top row, whatever the row count.
        private Vector3 ShiftsLabelPosition()
        {
            return new Vector3(0f, CellPosition(0, 0, shownRowCount).y + rowSpacing, 0f);
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

        private SpriteRenderer CreateSprite(string objectName, Sprite sprite, int sortingOrder, Transform parent)
        {
            var spriteObject = new GameObject(objectName);
            spriteObject.transform.SetParent(parent, false);
            var spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = sortingOrder;
            return spriteRenderer;
        }
    }
}
