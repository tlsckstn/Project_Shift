using ProjectShift.Board;
using ProjectShift.Stage;
using TMPro;
using UnityEngine;

namespace ProjectShift.Custom
{
    /// <summary>
    /// Stage editor screen. The board is drawn by <see cref="BoardView.Render"/> exactly as in play; this view adds the cell
    /// cursor and the two key guide lines, and shows why the board is not valid yet on the hint line.
    /// Children are built on the first <see cref="Show"/>, like the stage select and the pause menu.
    /// </summary>
    public class StageEditorView : MonoBehaviour
    {
        [SerializeField] private BoardView view;
        [SerializeField] private TMP_FontAsset pixelFont;
        [SerializeField] private TMP_SpriteAsset keyCapSprites;
        [SerializeField] private Color cursorColor = new Color32(0xE6, 0xCC, 0x33, 0xFF);
        [SerializeField] private Color keysColor = Color.white;
        [Tooltip("Local y of the two key guide lines, top line first; keep them inside the camera's bottom edge (orthographic size 5).")]
        [SerializeField] private float[] keysLineY = { -3.95f, -4.45f };

        private static readonly string[] KeysLines =
        {
            PixelText.Key("Left") + PixelText.Key("Right") + PixelText.Key("Up") + PixelText.Key("Down") + " MOVE  " + PixelText.Key("Space") + " BIT  " +
            PixelText.Key("Minus") + PixelText.Key("Equals") + " ROWS  " + PixelText.Key("Comma") + PixelText.Key("Period") + " HOURS  " + PixelText.Key("Esc") + " TITLE",
            PixelText.Key("1") + PixelText.Key("2") + PixelText.Key("3") + " ROBOT  " + PixelText.Key("Shift") + PixelText.Key("1") + PixelText.Key("2") + PixelText.Key("3") + " EXIT  " +
            PixelText.Key("B") + " BUG  " + PixelText.Key("F") + " FIREWALL  " + PixelText.Key("L") + " LOCK  " + PixelText.Key("N") + " GOAL  " + PixelText.Key("C") + " COVER",
        };

        // Cursor ring: 2 art px wide in the 4 px gap around a 32 px cell, so it never covers the cell or the robot.
        private const float CellPixels = 32f;
        private const float RingPixels = 2f;

        [Header("Button row and verify panel")]
        [Tooltip("Art px y of the button row from the screen centre, between the bottom board row and the hint line.")]
        [SerializeField] private float buttonsY = -106f;
        [SerializeField] private Color buttonColor = new Color32(0xA6, 0xAE, 0xBD, 0xFF);
        [SerializeField] private Color disabledColor = new Color32(0x5C, 0x66, 0x78, 0xFF);
        [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, 0.6f);
        [SerializeField] private Color panelColor = new Color32(0x23, 0x2A, 0x38, 0xFF);

        private enum Button
        {
            Verify,
            CopyCode,
            PasteCode,
            Play
        }

        private static readonly string[] ButtonNames = { "VERIFY", "COPY CODE", "PASTE CODE", "PLAY" };
        // Left edge of each button label, art px from the screen centre; the Tab cap sits before VERIFY at the board's left edge.
        private static readonly float[] ButtonX = { -116f, -62f, 8f, 92f };
        private static readonly string TabGuide = PixelText.Key("Tab");
        private static readonly string CheckingKeys = PixelText.Key("Esc") + " CANCEL";
        private static readonly string ResultKeys = PixelText.Key("Enter") + " OK";
        // One lit LED running along six; the spinner has no end, unlike a progress bar.
        private static readonly string[] SpinnerFrames = BuildSpinnerFrames();

        private const float ScreenWidth = 640f;
        private const float ScreenHeight = 360f;
        private const float PanelWidth = 220f;
        private const float PanelHeight = 84f;
        private const float TabGuideX = -144f;
        // Hours a solved board gets over its par until , / . change it; every official stage is PAR + 2.
        private const int DefaultExtraHours = 2;
        // The panel stays up at least this long, so a check that ends at once is still seen.
        private const float MinPanelSeconds = 0.5f;
        private const float SpinnerStepsPerSecond = 10f;
        // Above the board and its key guide, like the pause menu (sorting orders up to 6).
        private const int DimOrder = 20;
        private const int PanelOrder = 21;
        private const int TextOrder = 23;

        private readonly StageEditor editor = new StageEditor();
        private readonly StageCheck check = new StageCheck();
        private Transform root;
        private Transform cursor;
        private Sprite pixelSprite;
        private TextMeshPro[] buttonLabels;
        private TextMeshPro tabGuide;
        private Transform panelRoot;
        private TextMeshPro panelTitle;
        private TextMeshPro panelSpinner;
        private TextMeshPro panelStates;
        private TextMeshPro panelKeys;

        private bool buttonsFocused;
        private Button selectedButton;
        private bool canVerify;
        private bool canCopy;
        // Line shown instead of the invalid-board reason until the next edit (BAD CODE, PRESS CTRL+V, COPIED).
        private string message;
        private bool pasteWaiting;

        private bool panelOpen;
        private bool showingResult;
        private float panelOpenedAt;
        private int shownStep;
        // The stage being checked, and the code of the board it was built from; null once the check has ended.
        private StageData checkStage;
        private string checkCode;
        // Code (at the 99-hour check limit) of the board VERIFY found solvable, and its par; the board counts as verified only
        // while it still encodes to that code. HOURS is then par + extraHours, capped at 99.
        private string verifiedCode;
        private int verifiedPar;
        private int extraHours = DefaultExtraHours;
        // HOURS of the last pasted code, -1 for none; the next solved VERIFY turns it into extraHours once.
        private int pastedHours = -1;

        /// <summary>Raised when PLAY is pressed on a verified board; the listener plays <see cref="CreatePlayStage"/>.</summary>
        public event System.Action PlayRequested;

        /// <summary>The board being edited; InputController applies key edits to it, then calls <see cref="Redraw"/>.</summary>
        public StageEditor Editor => editor;

        /// <summary>True while the verify panel covers the editor; board edits and the button row are blocked then.</summary>
        public bool IsPanelOpen => panelOpen;

        /// <summary>True when the verify panel shows a result (not the running check): Enter may close it.</summary>
        public bool HasResult => panelOpen && showingResult;

        /// <summary>
        /// , / . on the board: changes the hours over par by <paramref name="delta"/>, keeping HOURS within par to 99.
        /// Only a verified board has hours; returns false when nothing changed, otherwise call <see cref="Redraw"/>.
        /// </summary>
        public bool ChangeExtraHours(int delta)
        {
            int extra = Mathf.Clamp(extraHours + delta, 0, StageCode.MaxShiftLimit - verifiedPar);
            if (!canCopy || extra == extraHours)
            {
                return false;
            }

            extraHours = extra;
            return true;
        }

        /// <summary>True while the button row has the focus instead of the board.</summary>
        public bool ButtonsFocused => buttonsFocused;

        /// <summary>Shows the editor over the board area and draws the current board. Call after BoardView.ShowEditor.</summary>
        public void Show()
        {
            if (root == null)
            {
                Build();
            }

            root.gameObject.SetActive(true);
            buttonsFocused = false;
            Redraw();
        }

        /// <summary>Hides the cursor and the key guide. The edited board is kept for the next visit.</summary>
        public void Hide()
        {
            pasteWaiting = false;
            if (root != null)
            {
                root.gameObject.SetActive(false);
            }
        }

        /// <summary>Tab: moves the focus between the board and the button row.</summary>
        public void ToggleFocus()
        {
            buttonsFocused = !buttonsFocused;
            pasteWaiting = false;
            Redraw();
        }

        /// <summary>Moves the button selection by <paramref name="delta"/>, stopping at the ends.</summary>
        public void MoveButton(int delta)
        {
            selectedButton = (Button)Mathf.Clamp((int)selectedButton + delta, 0, ButtonNames.Length - 1);
            Draw();
        }

        /// <summary>Enter on the selected button. Returns false when the button is off and nothing happened.</summary>
        public bool PressButton()
        {
            if (selectedButton == Button.Verify && canVerify)
            {
                StartCheck();
            }
            else if (selectedButton == Button.CopyCode && canCopy)
            {
                Clipboard.Copy(StageCode.Encode(WithShiftLimit(editor.ToCodeData(), Hours)));
                message = "COPIED";
                Draw();
            }
            else if (selectedButton == Button.Play && canCopy)
            {
                PlayRequested?.Invoke();
            }
            else if (selectedButton == Button.PasteCode)
            {
                // Drop text pasted earlier, then read the clipboard; Update takes the answer.
                Clipboard.TakePasted();
                Clipboard.RequestPaste();
                pasteWaiting = true;
            }
            else
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// A new stage of the verified board with the solver's par and HOURS as its shift limit, for PLAY. The caller owns it
        /// and destroys it when the play ends. Only valid while the board is verified.
        /// </summary>
        public StageData CreatePlayStage()
        {
            return StageData.Create(WithShiftLimit(editor.ToCodeData(), Hours), verifiedPar, out _);
        }

        /// <summary>Closes the verify panel. While the check still runs this cancels it and no result is kept.</summary>
        public void ClosePanel()
        {
            if (check.Status == StageCheckStatus.Running)
            {
                check.Cancel();
                EndCheck();
            }

            panelOpen = false;
            panelRoot.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (pasteWaiting)
            {
                PollPaste();
            }

            if (!panelOpen)
            {
                return;
            }

            check.Tick();
            bool running = check.Status == StageCheckStatus.Running;
            if (!running && checkStage != null)
            {
                if (check.Status == StageCheckStatus.Solved)
                {
                    verifiedCode = checkCode;
                    verifiedPar = check.Par;
                    if (pastedHours >= 0)
                    {
                        extraHours = Mathf.Max(0, pastedHours - verifiedPar);
                        pastedHours = -1;
                    }
                }

                EndCheck();
            }

            float elapsed = Time.unscaledTime - panelOpenedAt;
            if (!running && elapsed >= MinPanelSeconds)
            {
                if (!showingResult)
                {
                    showingResult = true;
                    ShowResult();
                }

                return;
            }

            // The spinner and the count change on a timer, not every frame: they build strings.
            int step = (int)(elapsed * SpinnerStepsPerSecond);
            if (step != shownStep)
            {
                shownStep = step;
                panelSpinner.SetText(SpinnerFrames[step % SpinnerFrames.Length]);
                PixelText.Place(panelSpinner, Pixels(0f, 2f));
                panelStates.SetText($"STATES {check.StatesVisited}");
                PixelText.Place(panelStates, Pixels(0f, -14f));
            }
        }

        private void StartCheck()
        {
            // Checked with the most hours a code allows: HOURS is set from the par afterwards.
            StageCodeData data = WithShiftLimit(editor.ToCodeData(), StageCode.MaxShiftLimit);
            checkStage = StageData.Create(data, 0, out string error);
            checkCode = StageCode.Encode(data);
            check.Start(checkStage);
            panelOpen = true;
            showingResult = false;
            panelOpenedAt = Time.unscaledTime;
            shownStep = -1;
            panelRoot.gameObject.SetActive(true);
            panelTitle.SetText("CHECKING");
            PixelText.Place(panelTitle, Pixels(0f, 24f));
            panelSpinner.gameObject.SetActive(true);
            panelStates.gameObject.SetActive(true);
            panelKeys.SetText(CheckingKeys);
            PixelText.Place(panelKeys, Pixels(0f, -30f));
        }

        // The stage only fed the solver; free it as soon as the check ends or is cancelled.
        private void EndCheck()
        {
            Destroy(checkStage);
            checkStage = null;
        }

        private void ShowResult()
        {
            StageCheckStatus status = check.Status;
            panelTitle.SetText(status == StageCheckStatus.Solved ? $"PAR {check.Par}" : status == StageCheckStatus.Unsolvable ? "UNSOLVABLE" : "TOO COMPLEX");
            PixelText.Place(panelTitle, Pixels(0f, 6f));
            panelSpinner.gameObject.SetActive(false);
            panelStates.gameObject.SetActive(false);
            panelKeys.SetText(ResultKeys);
            PixelText.Place(panelKeys, Pixels(0f, -30f));
            // The copy button changes with the result.
            Draw();
        }

        // Waits for the clipboard text after PASTE CODE; PRESS CTRL+V stays up while the browser blocks the read.
        private void PollPaste()
        {
            if (Clipboard.TakeReadBlocked())
            {
                message = "PRESS CTRL+V";
                Draw();
            }

            string text = Clipboard.TakePasted();
            if (text == null)
            {
                return;
            }

            pasteWaiting = false;
            if (StageCode.TryParse(text, out StageCodeData data, out string error))
            {
                editor.Load(data);
                verifiedCode = null;
                pastedHours = data.ShiftLimit;
                Redraw();
            }
            else
            {
                message = "BAD CODE";
                Draw();
            }
        }

        private int Hours => Mathf.Min(verifiedPar + extraHours, StageCode.MaxShiftLimit);

        private static StageCodeData WithShiftLimit(StageCodeData data, int shiftLimit)
        {
            return new StageCodeData(data.Rows, data.Starts, data.Exits, shiftLimit, data.LockedRows, data.Firewalls, data.BugCells,
                data.LockRows, data.LockTargets, data.RobotsCoverBits);
        }

        private static string[] BuildSpinnerFrames()
        {
            string[] frames = new string[6];
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i] = PixelText.LedBar(0, i) + PixelText.LedBar(1, 1) + PixelText.LedBar(0, frames.Length - 1 - i);
            }

            return frames;
        }

        /// <summary>Redraws the board, the cursor and the reason line from the editor's current state; an edit also drops any message.</summary>
        public void Redraw()
        {
            message = null;
            Draw();
        }

        private void Draw()
        {
            BoardState state = editor.CreateState();
            Vector2Int[] exits = new Vector2Int[editor.RobotCount];
            for (int i = 0; i < exits.Length; i++)
            {
                int exit = editor.GetExit(i);
                // (-1, -1) tells BoardView to draw no exit for that robot yet.
                exits[i] = exit < 0 ? new Vector2Int(-1, -1) : new Vector2Int(exit / BoardState.Columns, exit % BoardState.Columns);
            }

            int[] rowValues = new int[state.RowCount];
            int[] lockTargets = new int[state.RowCount];
            for (int r = 0; r < state.RowCount; r++)
            {
                rowValues[r] = BoardLogic.GetRowValue(state, r, editor.RobotsCoverBits);
                lockTargets[r] = editor.GetLockTarget(r);
            }

            view.Render(state, editor.CursorRow, exits, -1, rowValues, lockTargets, editor.RobotsCoverBits);
            cursor.localPosition = view.CellPosition(editor.CursorRow, editor.CursorCol, editor.RowCount);
            string reason = editor.Reason;
            view.ShowHint(message ?? reason ?? string.Empty);
            canVerify = reason == null;
            canCopy = reason == null && verifiedCode != null
                && StageCode.Encode(WithShiftLimit(editor.ToCodeData(), StageCode.MaxShiftLimit)) == verifiedCode;
            // The board's own shift limit is not shown: HOURS exists only once VERIFY has found a par.
            view.ShowMessage(canCopy ? $"HOURS {Hours}  PAR {verifiedPar}" : "HOURS ?");
            cursor.gameObject.SetActive(!buttonsFocused);
            for (int i = 0; i < buttonLabels.Length; i++)
            {
                bool isOn = i == (int)Button.PasteCode || (i == (int)Button.Verify ? canVerify : canCopy);
                bool selected = buttonsFocused && i == (int)selectedButton;
                buttonLabels[i].color = !isOn ? disabledColor : selected ? cursorColor : buttonColor;
                buttonLabels[i].SetText(selected ? "> " + ButtonNames[i] : ButtonNames[i]);
                PixelText.Place(buttonLabels[i], Pixels(ButtonX[i], buttonsY));
            }

            PixelText.Place(tabGuide, Pixels(TabGuideX, buttonsY));
        }

        private static Vector3 Pixels(float x, float y)
        {
            return new Vector3(x, y, 0f) / BoardView.PixelsPerUnit;
        }

        private void Build()
        {
            // Under the board view, so BoardView.CellPosition (its local space) places the cursor directly.
            root = new GameObject("StageEditor").transform;
            root.SetParent(view.transform, false);

            cursor = new GameObject("Cursor").transform;
            cursor.SetParent(root, false);
            pixelSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), BoardView.PixelsPerUnit);
            float edge = (CellPixels + RingPixels) * 0.5f / BoardView.PixelsPerUnit;
            float outer = CellPixels + 2f * RingPixels;
            CreateBar(pixelSprite, new Vector3(0f, edge, 0f), outer, RingPixels);
            CreateBar(pixelSprite, new Vector3(0f, -edge, 0f), outer, RingPixels);
            CreateBar(pixelSprite, new Vector3(-edge, 0f, 0f), RingPixels, CellPixels);
            CreateBar(pixelSprite, new Vector3(edge, 0f, 0f), RingPixels, CellPixels);

            for (int i = 0; i < KeysLines.Length; i++)
            {
                TextMeshPro line = PixelText.CreateLabel("EditorKeys", root, pixelFont, BoardState.Columns * 2f);
                line.spriteAsset = keyCapSprites;
                line.color = keysColor;
                line.SetText(KeysLines[i]);
                PixelText.Place(line, new Vector3(0f, keysLineY[i], 0f));
            }

            buttonLabels = new TextMeshPro[ButtonNames.Length];
            for (int i = 0; i < buttonLabels.Length; i++)
            {
                buttonLabels[i] = CreateLabel("EditorButton", TextAlignmentOptions.Left, 0f);
            }

            tabGuide = CreateLabel("EditorTabGuide", TextAlignmentOptions.Left, 0f);
            tabGuide.spriteAsset = keyCapSprites;
            tabGuide.SetText(TabGuide);
            tabGuide.color = buttonColor;

            // Verify panel: hidden until VERIFY, drawn over everything like the pause menu.
            panelRoot = new GameObject("VerifyPanel").transform;
            panelRoot.SetParent(root, false);
            CreateRect("Dim", ScreenWidth, ScreenHeight, dimColor, DimOrder);
            CreateRect("Frame", PanelWidth + 2f, PanelHeight + 2f, disabledColor, PanelOrder);
            CreateRect("Panel", PanelWidth, PanelHeight, panelColor, PanelOrder + 1);
            panelTitle = CreateLabel("VerifyTitle", TextAlignmentOptions.Center, 0.5f, panelRoot);
            panelTitle.fontSize = PixelText.LargeFontSize;
            panelTitle.color = Color.white;
            panelSpinner = CreateLabel("VerifySpinner", TextAlignmentOptions.Center, 0.5f, panelRoot);
            panelSpinner.spriteAsset = keyCapSprites;
            panelStates = CreateLabel("VerifyStates", TextAlignmentOptions.Center, 0.5f, panelRoot);
            panelStates.color = buttonColor;
            panelKeys = CreateLabel("VerifyKeys", TextAlignmentOptions.Center, 0.5f, panelRoot);
            panelKeys.spriteAsset = keyCapSprites;
            panelKeys.color = buttonColor;
            panelRoot.gameObject.SetActive(false);
        }

        // Centred around the panel's origin; the pixel sprite is pivoted in the middle.
        private void CreateRect(string objectName, float widthPixels, float heightPixels, Color color, int sortingOrder)
        {
            SpriteRenderer rect = BoardView.CreateSprite(objectName, pixelSprite, sortingOrder, panelRoot);
            rect.color = color;
            rect.transform.localScale = new Vector3(widthPixels / 4f, heightPixels / 4f, 1f);
        }

        // The pivot follows the alignment, so x is the left edge, centre or right edge of the text.
        private TextMeshPro CreateLabel(string objectName, TextAlignmentOptions alignment, float pivotX, Transform parent = null)
        {
            TextMeshPro label = PixelText.CreateLabel(objectName, parent != null ? parent : root, pixelFont, 8f);
            label.color = buttonColor;
            label.sortingOrder = parent != null ? TextOrder : 0;
            label.alignment = alignment;
            label.rectTransform.pivot = new Vector2(pivotX, 0.5f);
            return label;
        }

        // One side of the cursor ring, sized in art pixels (the pixel sprite is 4 px square).
        private void CreateBar(Sprite pixel, Vector3 localPosition, float widthPixels, float heightPixels)
        {
            SpriteRenderer bar = BoardView.CreateSprite("CursorBar", pixel, 7, cursor);
            bar.color = cursorColor;
            bar.transform.localPosition = localPosition;
            bar.transform.localScale = new Vector3(widthPixels / 4f, heightPixels / 4f, 1f);
        }
    }
}
