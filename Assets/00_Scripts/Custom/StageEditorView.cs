using ProjectShift.Board;
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

        private readonly StageEditor editor = new StageEditor();
        private Transform root;
        private Transform cursor;

        /// <summary>The board being edited; InputController applies key edits to it, then calls <see cref="Redraw"/>.</summary>
        public StageEditor Editor => editor;

        /// <summary>Shows the editor over the board area and draws the current board. Call after BoardView.ShowEditor.</summary>
        public void Show()
        {
            if (root == null)
            {
                Build();
            }

            root.gameObject.SetActive(true);
            Redraw();
        }

        /// <summary>Hides the cursor and the key guide. The edited board is kept for the next visit.</summary>
        public void Hide()
        {
            if (root != null)
            {
                root.gameObject.SetActive(false);
            }
        }

        /// <summary>Redraws the board, the cursor and the reason line from the editor's current state.</summary>
        public void Redraw()
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
            view.ShowHint(editor.Reason ?? string.Empty);
        }

        private void Build()
        {
            // Under the board view, so BoardView.CellPosition (its local space) places the cursor directly.
            root = new GameObject("StageEditor").transform;
            root.SetParent(view.transform, false);

            cursor = new GameObject("Cursor").transform;
            cursor.SetParent(root, false);
            Sprite pixel = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), BoardView.PixelsPerUnit);
            float edge = (CellPixels + RingPixels) * 0.5f / BoardView.PixelsPerUnit;
            float outer = CellPixels + 2f * RingPixels;
            CreateBar(pixel, new Vector3(0f, edge, 0f), outer, RingPixels);
            CreateBar(pixel, new Vector3(0f, -edge, 0f), outer, RingPixels);
            CreateBar(pixel, new Vector3(-edge, 0f, 0f), RingPixels, CellPixels);
            CreateBar(pixel, new Vector3(edge, 0f, 0f), RingPixels, CellPixels);

            for (int i = 0; i < KeysLines.Length; i++)
            {
                TextMeshPro line = PixelText.CreateLabel("EditorKeys", root, pixelFont, BoardState.Columns * 2f);
                line.spriteAsset = keyCapSprites;
                line.color = keysColor;
                line.SetText(KeysLines[i]);
                PixelText.Place(line, new Vector3(0f, keysLineY[i], 0f));
            }
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
