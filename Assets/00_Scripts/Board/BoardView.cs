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

        [SerializeField] private Sprite cellSprite;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private float rowSpacing = 2f;
        [SerializeField] private float labelFontSize = 6f;
        [SerializeField] private Color onColor = new Color(0.3f, 0.9f, 0.5f);
        [SerializeField] private Color offColor = new Color(0.15f, 0.15f, 0.2f);
        [SerializeField] private Color selectedRowColor = new Color(0.9f, 0.8f, 0.2f, 0.35f);
        [SerializeField] private Color robotColor = new Color(0.3f, 0.6f, 1f);
        [SerializeField] private Color exitColor = new Color(1f, 0.4f, 0.3f);

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
        }

        /// <summary>
        /// Redraws the board. Rows are drawn top to bottom; the robot and exit sit on top of their cell.
        /// The exit is passed in because BoardState does not hold it yet (Story 006).
        /// </summary>
        public void Render(BoardState state, int selectedRow, Vector2Int exitCell)
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
