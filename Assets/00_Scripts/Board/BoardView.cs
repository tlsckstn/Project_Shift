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
        internal const int MaxRows = 3;
        private const int MaxRobots = 3;
        // Sprites are 36 pixels per unit: one cell pitch (32 px cell + 4 px gap) is 1 unit.
        internal const float PixelsPerUnit = 36f;
        // Robot sprite centre above the cell centre: its tracks (20 px sprite, bottom row empty) rest on the cell top.
        private const float RobotOffsetPixels = 25f;
        // Exit sprite centre above the cell centre: the 28 px door frame's bottom touches the cell top.
        private const float ExitOffsetPixels = 30f;
        // Must match the Puzzle map in Assets/InputSystem_Actions.inputactions (Tab still selects the row too, unlisted).
        // Both key guides must stay inside the circuit keep-out (476 art px wide): 407 and 410 px measured
        // (TMP preferred width with the key caps).
        private static readonly string KeysText =
            PixelText.Key("Left") + PixelText.Key("Right") + " WALK  " + PixelText.Key("Down") + " DOWN  " + PixelText.Key("Space") + " SELECT ROW  " +
            PixelText.Key("Shift") + PixelText.Key("Left") + PixelText.Key("Right") + " SHIFT  " + PixelText.Key("Z") + " UNDO  " + PixelText.Key("R") + " RESTART  " + PixelText.Key("Esc") + " MENU";
        // Out of shifts: replaces the HOURS line.
        private static readonly string DawnText = "DAWN  " + PixelText.Key("Z") + " UNDO  " + PixelText.Key("R") + " RESTART";
        // Replaces the key panel on a cleared board.
        private static readonly string ClearChoiceText = PixelText.Key("R") + " RETRY   " + PixelText.Key("Enter") + " NEXT   " + PixelText.Key("Esc") + " STAGES";
        private static readonly string AllClearKeysText = PixelText.Key("Enter") + " TITLE";
        private static readonly string SelectKeysText =
            PixelText.Key("Left") + PixelText.Key("Right") + PixelText.Key("Up") + PixelText.Key("Down") + " SELECT   " + PixelText.Key("Enter") + " PLAY   " + PixelText.Key("P") + " SOUND   " + PixelText.Key("Esc") + " TITLE";
        // Working title; the final name is picked in Story 006 (release cleanup).
        private const string GameTitle = "NIGHT SHIFT <<";
        private static readonly string TitleKeysText = PixelText.Key("Enter") + " START   " + PixelText.Key("E") + " EDITOR   " + PixelText.Key("P") + " SOUND";
        // Shown instead of KeysText on stages with more than one robot. Shorter words keep it inside the circuit keep-out.
        private static readonly string KeysTextMultiRobot =
            PixelText.Key("Left") + PixelText.Key("Right") + " WALK  " + PixelText.Key("Down") + " DOWN  " + PixelText.Key("Space") + " ROW  " +
            PixelText.Key("Shift") + PixelText.Key("Left") + PixelText.Key("Right") + " SHIFT  " + PixelText.Key("Q") + " ROBOT  " + PixelText.Key("Z") + " UNDO  " + PixelText.Key("R") + " RESET  " + PixelText.Key("Esc") + " MENU";

        [Header("Board Art")]
        [Tooltip("Corner brackets around the selected row, drawn inside the lock frame.")]
        [SerializeField] private Sprite rowHighlightSprite;
        [SerializeField] private Sprite platformSprite;
        [SerializeField] private Sprite gapSprite;
        [Tooltip("A 1 cell under a robot on stages where robots cover bits.")]
        [SerializeField] private Sprite coveredSprite;
        [SerializeField] private Sprite bugSprite;
        [SerializeField] private Sprite firewallSprite;
        [SerializeField] private Sprite lockFrameSprite;

        [Header("Robots and Exits")]
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

        [Header("Layout and Text")]
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
        [SerializeField] private Color shiftsColor = Color.white;
        [SerializeField] private Color noShiftsColor = new Color(1f, 0.3f, 0.3f);

        [Header("Effects")]
        [SerializeField] private MoveSettings move = new MoveSettings();
        [SerializeField] private FailSettings fail = new FailSettings();
        [SerializeField] private ClearSettings clear = new ClearSettings();
        [SerializeField] private TransitionSettings stageTransition = new TransitionSettings();
        [SerializeField] private WipeSettings wipe = new WipeSettings();

        [Header("Background")]
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

        [Header("Stage Select")]
        [Tooltip("Draws the stage select pages (computer-part zones).")]
        [SerializeField] private StageSelectView stageSelect;

        private Transform cachedTransform;
        // Parent of the whole stage (cells, row values, HUD lines, decor): hidden as one while the stage select shows.
        private Transform playRoot;
        // Parent of the board itself (cells, row values, decor), without the HUD lines and zone decor: shaken on a fail.
        private Transform boardRoot;
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
        // Set while the stage editor shows: Render then keeps the key panel empty.
        private bool isEditing;
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

        // Shift slide, lost-bit debris (and its undo replay) and the walk afterimage; built in Awake.
        private MoveEffect moveEffect;

        // Fail drop, fade and shake, and the reappear after the restart; built in Awake.
        private FailEffect failEffect;

        // Exit arrival flashes and sparks, and the stage clear (row bars, clock-out, dawn); built in Awake.
        private ClearEffect clearEffect;
        // Row centres handed to ClearEffect.PlayClear, refilled on each clear.
        private readonly Vector3[] clearRowCentres = new Vector3[MaxRows];
        private const int RatingLeds = 3;
        // Clear result: the earned LEDs light one by one after the result line shows with every LED unlit; -1 when idle.
        private float ledTime = -1f;
        private string ledResult;
        private int ledStars;
        private int ledShown;

        // Stage to stage row shift (old rows out, new rows in); built in Awake.
        private StageTransition rowTransition;

        // Screen wipe (title, stage select, puzzle), built in Awake.
        private ScreenWipe screenWipe;

        /// <summary>True while a stage transition or a screen wipe plays; StageManager ignores input meanwhile.</summary>
        public bool IsTransitioning => rowTransition.IsPlaying || screenWipe.IsPlaying;

        /// <summary>True while a shifted row slides into place.</summary>
        public bool IsSliding => moveEffect.IsSliding;

        /// <summary>
        /// Raised on every cell step of the stage transition and the screen wipe: false while the old rows (or screen) leave,
        /// true while the new ones arrive.
        /// </summary>
        public event System.Action<bool> TransitionStepped;

        /// <summary>Raised each time one rating LED lights on the clear result.</summary>
        public event System.Action LedLit;

        private void Awake()
        {
            cachedTransform = transform;
            playRoot = new GameObject("Play").transform;
            playRoot.SetParent(cachedTransform, false);
            titleRoot = new GameObject("Title").transform;
            titleRoot.SetParent(cachedTransform, false);
            titleRoot.gameObject.SetActive(false);
            boardRoot = new GameObject("Board").transform;
            boardRoot.SetParent(playRoot, false);
            decorRoot = new GameObject("Decor").transform;
            decorRoot.SetParent(boardRoot, false);
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
                    cells[i] = CreateSprite("Cell", gapSprite, 2, boardRoot);
                    cellTransforms[i] = cells[i].transform;
                    // Ring that fills the gap around a firewall cell.
                    firewallMarks[i] = CreateSprite("Firewall", firewallSprite, 1, decorRoot);
                    firewallTransforms[i] = firewallMarks[i].transform;
                    // Diamond on top of the cell, so its shape differs from the boxy robot and exit.
                    bugMarks[i] = CreateSprite("Bug", bugSprite, 3, decorRoot);
                    bugTransforms[i] = bugMarks[i].transform;
                }

                valueLabels[r] = new GameObject("RowValue").AddComponent<TextMeshPro>();
                valueLabels[r].rectTransform.SetParent(boardRoot, false);
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

            failEffect = new FailEffect(fail, boardRoot, robots, robotTransforms);

            // Below everything; centred on the board root, which sits at the camera centre.
            CreateSprite("Circuit", circuitPlayingSprite, -10, cachedTransform);
            circuitClear = CreateSprite("CircuitClear", circuitClearSprite, -9, cachedTransform);
            zonePlaying = CreateSprite("ZonePlaying", null, -8, playRoot);
            zoneClear = CreateSprite("ZoneClear", null, -7, playRoot);
            backgroundCamera = Camera.main;
            SetClearAmount(0f);

            // A built-in 4x4 white texture at the art's pixels-per-unit: each piece is a 4x4 art-pixel square, no asset needed.
            Sprite debrisSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            moveEffect = new MoveEffect(move, cellSize, decorRoot, gapSprite, debrisSprite, cells, cellTransforms, robotTransforms, selectedFrame,
                robotIdleSprites, Vector3.up * (RobotOffsetPixels / PixelsPerUnit), (row, col) => CellPosition(row, col, shownRowCount));

            // The clear's row bar is the 4x4 piece stretched over one row's 8 cells (32 art px each).
            float cellPixels = 32f / PixelsPerUnit;
            Vector3 barScale = new Vector3(((BoardState.Columns - 1) * cellSize + cellPixels) * PixelsPerUnit / 4f, cellPixels * PixelsPerUnit / 4f, 1f);
            clearEffect = new ClearEffect(clear, decorRoot, boardRoot, debrisSprite, barScale, robots, robotTransforms, exitMarks, exitTransforms, exitFlashSprites, selectedFrame, SetClearAmount);

            screenWipe = new ScreenWipe(cachedTransform, playingBackground, circuitPlayingSprite, wipe, stageTransition, arriving => TransitionStepped?.Invoke(arriving));
            rowTransition = new StageTransition(stageTransition, cells, cellTransforms, valueLabels, shownValues, platformSprite, gapSprite,
                decorRoot.gameObject, CellPosition, LabelPosition, ApplyPendingZone, arriving => TransitionStepped?.Invoke(arriving));

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
            moveEffect.Stop();
            failEffect.Stop();
            clearEffect.Stop();
            ledTime = -1f;
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

            // The stage editor draws its own key guide, so its renders leave the key panel empty.
            if (!isEditing && (multiRobot != shownMultiRobot || isKeysTextReplaced))
            {
                shownMultiRobot = multiRobot;
                isKeysTextReplaced = false;
                SetKeysText(multiRobot ? KeysTextMultiRobot : KeysText);
            }

            for (int i = 0; i < MaxRobots; i++)
            {
                // The stage editor passes (-1, -1) for a robot whose exit is not placed yet.
                bool hasExit = i < exits.Length && exits[i].x >= 0;
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
        /// next Render. The clear look stays because nothing renders meanwhile. The rating shows as three unlit LEDs, then the
        /// earned ones light one by one (LedLit each time).
        /// </summary>
        public void ShowClearChoice(int shiftsUsed, int par, int stars)
        {
            string result = par > 0 ? $"SHIFTS {shiftsUsed}  PAR {par}" : $"SHIFTS {shiftsUsed}";
            if (stars > 0)
            {
                ledResult = result + "  ";
                ledStars = stars;
                ledShown = 0;
                ledTime = 0f;
                ShowMessage(ledResult + PixelText.LedBar(0, RatingLeds));
            }
            else
            {
                ShowMessage(result);
            }

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
            isEditing = false;
            moveEffect.Stop();
            failEffect.Stop();
            clearEffect.Stop();
            ledTime = -1f;
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
            isEditing = false;
            moveEffect.Stop();
            failEffect.Stop();
            clearEffect.Stop();
            ledTime = -1f;
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
        /// Shows the board area for the stage editor: hides the title and the stage select and clears the key panel
        /// (StageEditorView draws its own key guide). The editor then draws the board with <see cref="Render"/>.
        /// </summary>
        public void ShowEditor()
        {
            moveEffect.Stop();
            failEffect.Stop();
            clearEffect.Stop();
            ledTime = -1f;
            titleRoot.gameObject.SetActive(false);
            stageSelect.Hide();
            playRoot.gameObject.SetActive(true);
            isEditing = true;
            isKeysTextReplaced = true;
            SetKeysText(string.Empty);
        }

        /// <summary>
        /// Slides row <paramref name="row"/> in from one cell back after a shift (<paramref name="direction"/> -1 left, 1 right).
        /// Call right after the Render that shows the shifted state. <paramref name="lostBit"/> is the value of the cell pushed past the row end.
        /// The cells and the robots standing on the row move; exits, firewalls and bug marks stay, they are fixed to the board.
        /// </summary>
        public void PlayShift(int row, int direction, bool lostBit, BoardState state)
        {
            moveEffect.PlayShift(row, direction, lostBit, state);
        }

        /// <summary>
        /// Undo brought back the lit bit at (<paramref name="row"/>, <paramref name="col"/>) that a shift in
        /// <paramref name="direction"/> pushed out: the break runs backwards, the pieces fly in from outside and the cell
        /// shows once they arrive. Call right after the Render that shows the restored state.
        /// </summary>
        public void PlayRegenerate(int row, int col, int direction)
        {
            moveEffect.PlayRegenerate(row, col, direction);
        }

        /// <summary>
        /// Leaves a fading copy of robot <paramref name="robot"/> on the cell it just left (walk or descend).
        /// Call right after the Render that shows the robot on its new cell.
        /// </summary>
        public void PlayWalk(int robot, int fromRow, int fromCol)
        {
            moveEffect.PlayWalk(robot, fromRow, fromCol);
        }

        /// <summary>
        /// Starts the fail effect on a failed board: a robot past its row end drops and fades, a robot on a bug cell fades out.
        /// Call right after the Render that shows the failed state; it waits for a running shift slide to finish first.
        /// </summary>
        public void PlayFail(BoardState state)
        {
            failEffect.Play(state);
            selectedFrame.enabled = false;
        }

        /// <summary>Fades the robots that failed back in at their start cells. Call right after the Render that shows the restarted stage.</summary>
        public void PlayReappear()
        {
            failEffect.PlayReappear();
        }

        /// <summary>
        /// Flashes exit <paramref name="exit"/> bright for a moment and sends white sparks out from it. Call right after the
        /// Render that shows its robot arriving; both wait for a running shift slide to finish.
        /// </summary>
        public void PlayExitFlash(int exit)
        {
            clearEffect.PlayExitFlash(exit);
        }

        /// <summary>
        /// Stage clear: every robot holds its arrival face, then rises into its exit and fades while the exits show bright.
        /// Call right after the Render that shows the clearing move; it waits for a running shift slide to finish first.
        /// </summary>
        public void PlayClear()
        {
            for (int r = 0; r < shownRowCount; r++)
            {
                clearRowCentres[r] = (CellPosition(r, 0, shownRowCount) + CellPosition(r, BoardState.Columns - 1, shownRowCount)) * 0.5f;
            }

            clearEffect.PlayClear(shownRowCount, clearRowCentres);
        }

        /// <summary>
        /// Plays the stage transition: the rows of <paramref name="from"/> shift right one cell at a time until empty, then the rows
        /// of <paramref name="to"/> shift in from the left the same way; robots, exits and marks appear once the rows are in.
        /// Pass null for <paramref name="from"/> to play only the arrival (first stage). Call right after the Render that shows <paramref name="to"/>.
        /// </summary>
        public void PlayTransition(BoardState from, BoardState to)
        {
            rowTransition.Play(from, to, shownRowCount);
        }

        private void Update()
        {
            rowTransition.Tick(Time.deltaTime);

            screenWipe.Tick(Time.deltaTime);

            // These effects start once the shift slide that caused them has finished.
            if (!moveEffect.IsSliding)
            {
                failEffect.TickFail(Time.deltaTime);
                clearEffect.Tick(Time.deltaTime);
            }

            if (ledTime >= 0f)
            {
                UpdateLeds();
            }

            failEffect.TickReappear(Time.deltaTime);
            moveEffect.Tick(Time.deltaTime);
        }

        /// <summary>
        /// Plays the screen wipe, an old monitor switching off and on: covers (night background and circuit) close from the top
        /// and bottom onto a green centre line over 16 steps, <paramref name="swap"/> changes the screen underneath, then the
        /// covers open from that line over 16 steps. Input stays blocked (IsTransitioning) until it ends.
        /// </summary>
        public void PlayWipe(System.Action swap)
        {
            screenWipe.Play(swap);
        }

        // Lights the next earned LED once its step time has passed; a long frame can light two with one tick.
        private void UpdateLeds()
        {
            ledTime += Time.deltaTime;
            int lit = clear.LedStepSeconds > 0f ? Mathf.Min(ledStars, Mathf.FloorToInt(ledTime / clear.LedStepSeconds)) : ledStars;
            if (lit == ledShown)
            {
                return;
            }

            ledShown = lit;
            ShowMessage(ledResult + PixelText.LedBar(lit, RatingLeds));
            LedLit?.Invoke();
            if (lit == ledStars)
            {
                ledTime = -1f;
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

        internal static void SetAlpha(SpriteRenderer spriteRenderer, float alpha)
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

        /// <summary>Centre of cell (<paramref name="row"/>, <paramref name="col"/>) in this view's local space, on a board of <paramref name="rowCount"/> rows.</summary>
        internal Vector3 CellPosition(int row, int col, int rowCount)
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

        internal static SpriteRenderer CreateSprite(string objectName, Sprite sprite, int sortingOrder, Transform parent)
        {
            GameObject spriteObject = new GameObject(objectName);
            spriteObject.transform.SetParent(parent, false);
            SpriteRenderer spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = sortingOrder;
            return spriteRenderer;
        }
    }
}
