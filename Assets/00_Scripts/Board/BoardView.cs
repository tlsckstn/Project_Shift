using TMPro;
using UnityEngine;

namespace ProjectShift.Board
{
    /// <summary>
    /// Draws a BoardState with pooled square sprites and one decimal label per row.
    /// Read-only: it never changes the state and never calls BoardLogic.
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        private const int MaxRows = 3;
        // Must match the Puzzle map in Assets/InputSystem_Actions.inputactions.
        private const string KeysText = "←/→ walk    ↓ descend    W/S select row    Q/E shift    Z undo    R restart";

        [SerializeField] private Sprite cellSprite;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private float rowSpacing = 2f;
        [SerializeField] private float labelFontSize = 6f;
        [SerializeField] private float keysFontSize = 3.5f;
        [Tooltip("Local y of the key panel; keep it inside the camera's bottom edge (orthographic size 5).")]
        [SerializeField] private float keysPanelY = -4.3f;
        [SerializeField] private Color onColor = new Color(0.3f, 0.9f, 0.5f);
        [SerializeField] private Color offColor = new Color(0.15f, 0.15f, 0.2f);
        [SerializeField] private Color selectedRowColor = new Color(0.9f, 0.8f, 0.2f, 0.35f);
        [SerializeField] private Color robotColor = new Color(0.3f, 0.6f, 1f);
        [SerializeField] private Color exitColor = new Color(1f, 0.4f, 0.3f);
        [SerializeField] private Color shiftsColor = Color.white;
        [SerializeField] private Color noShiftsColor = new Color(1f, 0.3f, 0.3f);

        private Transform cachedTransform;
        private SpriteRenderer[] cells;
        private Transform[] cellTransforms;
        private SpriteRenderer[] rowHighlights;
        private Transform[] highlightTransforms;
        private TextMeshPro[] valueLabels;
        private Transform[] labelTransforms;
        private int[] shownValues;
        private SpriteRenderer robot;
        private Transform robotTransform;
        private SpriteRenderer exit;
        private Transform exitTransform;
        private TextMeshPro shiftsLabel;
        private int shownShifts = -1;
        private int shownPar = -1;

        private void Awake()
        {
            cachedTransform = transform;
            int cellCount = MaxRows * BoardState.Columns;
            cells = new SpriteRenderer[cellCount];
            cellTransforms = new Transform[cellCount];
            rowHighlights = new SpriteRenderer[MaxRows];
            highlightTransforms = new Transform[MaxRows];
            valueLabels = new TextMeshPro[MaxRows];
            labelTransforms = new Transform[MaxRows];
            shownValues = new int[MaxRows];

            Vector3 highlightScale = new Vector3(BoardState.Columns * cellSize + 0.4f, cellSize + 0.4f, 1f);
            for (int r = 0; r < MaxRows; r++)
            {
                rowHighlights[r] = CreateSquare("RowHighlight", selectedRowColor, 0, highlightScale);
                highlightTransforms[r] = rowHighlights[r].transform;

                for (int c = 0; c < BoardState.Columns; c++)
                {
                    int i = r * BoardState.Columns + c;
                    cells[i] = CreateSquare("Cell", offColor, 1, Vector3.one * (cellSize * 0.9f));
                    cellTransforms[i] = cells[i].transform;
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
            }

            exit = CreateSquare("Exit", exitColor, 2, Vector3.one * (cellSize * 0.8f));
            exitTransform = exit.transform;
            robot = CreateSquare("Robot", robotColor, 3, Vector3.one * (cellSize * 0.5f));
            robotTransform = robot.transform;

            shiftsLabel = new GameObject("ShiftsLeft").AddComponent<TextMeshPro>();
            shiftsLabel.rectTransform.SetParent(cachedTransform, false);
            shiftsLabel.fontSize = labelFontSize;
            shiftsLabel.alignment = TextAlignmentOptions.Center;
            shiftsLabel.rectTransform.sizeDelta = new Vector2(BoardState.Columns * cellSize * 2f, cellSize);

            // Static: set once, never rebuilt.
            var keysLabel = new GameObject("KeyPanel").AddComponent<TextMeshPro>();
            keysLabel.rectTransform.SetParent(cachedTransform, false);
            keysLabel.rectTransform.localPosition = new Vector3(0f, keysPanelY, 0f);
            keysLabel.rectTransform.sizeDelta = new Vector2(BoardState.Columns * cellSize * 2f, cellSize);
            keysLabel.fontSize = keysFontSize;
            keysLabel.alignment = TextAlignmentOptions.Center;
            keysLabel.color = shiftsColor;
            keysLabel.SetText(KeysText);
        }

        /// <summary>
        /// Redraws the board. Rows are drawn top to bottom; the robot and exit sit on top of their cell.
        /// The exit (row, column) and par come from StageData, not BoardState, because they never change during a stage.
        /// A par of 0 hides the PAR text.
        /// </summary>
        public void Render(BoardState state, int selectedRow, Vector2Int exitCell, int par)
        {
            for (int r = 0; r < MaxRows; r++)
            {
                bool used = r < state.RowCount;
                rowHighlights[r].enabled = used && r == selectedRow;
                valueLabels[r].enabled = used;

                for (int c = 0; c < BoardState.Columns; c++)
                {
                    int i = r * BoardState.Columns + c;
                    cells[i].enabled = used;
                    if (!used)
                    {
                        continue;
                    }

                    cells[i].color = state.GetCell(r, c) ? onColor : offColor;
                    cellTransforms[i].localPosition = CellPosition(r, c, state.RowCount);
                }

                if (!used)
                {
                    continue;
                }

                Vector3 rowCenter = new Vector3(0f, CellPosition(r, 0, state.RowCount).y, 0f);
                highlightTransforms[r].localPosition = rowCenter;
                labelTransforms[r].localPosition = rowCenter + Vector3.right * (BoardState.Columns * 0.5f * cellSize + 0.5f);

                // Only rebuild the label text when the value changed.
                int value = state.GetRow(r);
                if (shownValues[r] != value)
                {
                    shownValues[r] = value;
                    valueLabels[r].SetText("{0}", value);
                }
            }

            Vector3 standOffset = Vector3.up * (cellSize * 0.75f);
            robotTransform.localPosition = CellPosition(state.RobotRow, state.RobotCol, state.RowCount) + standOffset;
            exitTransform.localPosition = CellPosition(exitCell.x, exitCell.y, state.RowCount) + standOffset;

            // Sits above the top row; only rebuilt when the count or par changes.
            shiftsLabel.rectTransform.localPosition = new Vector3(0f, CellPosition(0, 0, state.RowCount).y + rowSpacing, 0f);
            if (shownShifts != state.ShiftsLeft || shownPar != par)
            {
                shownShifts = state.ShiftsLeft;
                shownPar = par;
                if (shownShifts > 0)
                {
                    shiftsLabel.color = shiftsColor;
                    if (par > 0)
                    {
                        shiftsLabel.SetText("SHIFTS {0}  PAR {1}", shownShifts, par);
                    }
                    else
                    {
                        shiftsLabel.SetText("SHIFTS {0}", shownShifts);
                    }
                }
                else
                {
                    shiftsLabel.color = noShiftsColor;
                    shiftsLabel.SetText("SHIFTS 0  Z undo / R restart");
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

        private Vector3 CellPosition(int row, int col, int rowCount)
        {
            float x = (col - (BoardState.Columns - 1) * 0.5f) * cellSize;
            float y = ((rowCount - 1) * 0.5f - row) * rowSpacing;
            return new Vector3(x, y, 0f);
        }

        private SpriteRenderer CreateSquare(string objectName, Color color, int sortingOrder, Vector3 scale)
        {
            var square = new GameObject(objectName);
            Transform squareTransform = square.transform;
            squareTransform.SetParent(cachedTransform, false);
            squareTransform.localScale = scale;
            var spriteRenderer = square.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = cellSprite;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = sortingOrder;
            return spriteRenderer;
        }
    }
}
