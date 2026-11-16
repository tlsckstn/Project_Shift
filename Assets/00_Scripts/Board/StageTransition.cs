using TMPro;
using UnityEngine;

namespace ProjectShift.Board
{
    /// <summary>
    /// Stage to stage: the old rows shift right one cell per step until empty, a pause, then the new rows shift in from the
    /// left; robots, exits and marks (the decor) appear once the rows are in. Owned and ticked by BoardView, which has
    /// already drawn the new stage: this overrides the cells and row values step by step and puts Render's back at the end.
    /// </summary>
    public class StageTransition
    {
        private readonly TransitionSettings settings;
        private readonly SpriteRenderer[] cells;
        private readonly Transform[] cellTransforms;
        private readonly TextMeshPro[] valueLabels;
        // The values Render shows on each row; BoardView keeps them up to date.
        private readonly int[] shownValues;
        private readonly Sprite platformSprite;
        private readonly Sprite gapSprite;
        private readonly GameObject decor;
        // Cell centre (row, column, row count) and row value label position (row, row count).
        private readonly System.Func<int, int, int, Vector3> cellPosition;
        private readonly System.Func<int, int, Vector3> labelPosition;
        // Shows the chapter decor picked for the new stage once the old rows are gone.
        private readonly System.Action applyZone;
        // Raised per cell step: false while the old rows leave, true while the new ones arrive.
        private readonly System.Action<bool> stepped;

        // -1 when idle.
        private float time = -1f;
        private int step = -1;
        private int oldRowCount;
        private int newRowCount;
        private readonly byte[] oldRows = new byte[BoardView.MaxRows];
        private readonly byte[] newRows = new byte[BoardView.MaxRows];
        private readonly Sprite[] restCellSprites;

        /// <summary>Works on BoardView's pooled <paramref name="cells"/> and <paramref name="valueLabels"/>; hides <paramref name="decor"/> while it plays.</summary>
        public StageTransition(TransitionSettings settings, SpriteRenderer[] cells, Transform[] cellTransforms, TextMeshPro[] valueLabels,
            int[] shownValues, Sprite platformSprite, Sprite gapSprite, GameObject decor, System.Func<int, int, int, Vector3> cellPosition,
            System.Func<int, int, Vector3> labelPosition, System.Action applyZone, System.Action<bool> stepped)
        {
            this.settings = settings;
            this.cells = cells;
            this.cellTransforms = cellTransforms;
            this.valueLabels = valueLabels;
            this.shownValues = shownValues;
            this.platformSprite = platformSprite;
            this.gapSprite = gapSprite;
            this.decor = decor;
            this.cellPosition = cellPosition;
            this.labelPosition = labelPosition;
            this.applyZone = applyZone;
            this.stepped = stepped;
            restCellSprites = new Sprite[cells.Length];
        }

        /// <summary>True while the transition plays.</summary>
        public bool IsPlaying => time >= 0f;

        /// <summary>
        /// Plays from <paramref name="from"/> (null: only the arrival) to <paramref name="to"/>, which BoardView has just
        /// drawn with <paramref name="rowCount"/> rows.
        /// </summary>
        public void Play(BoardState from, BoardState to, int rowCount)
        {
            oldRowCount = from == null ? 0 : from.RowCount;
            newRowCount = rowCount;
            for (int r = 0; r < oldRows.Length; r++)
            {
                oldRows[r] = r < oldRowCount ? from.GetRow(r) : (byte)0;
                newRows[r] = r < to.RowCount ? to.GetRow(r) : (byte)0;
            }

            for (int i = 0; i < cells.Length; i++)
            {
                restCellSprites[i] = cells[i].sprite;
            }

            decor.SetActive(false);
            time = 0f;
            step = -1;
            Show();
        }

        /// <summary>Advances a playing transition by <paramref name="deltaTime"/> seconds; does nothing while idle.</summary>
        public void Tick(float deltaTime)
        {
            if (time < 0f)
            {
                return;
            }

            time += deltaTime;
            Show();
        }

        // Steps: 1..8 shift the old rows out (skipped without old rows), one empty step for the gap, then 8 steps shift the new rows in.
        private void Show()
        {
            // A zero step time would divide by zero below and never end the transition, locking input.
            if (settings.StepSeconds <= 0f)
            {
                End();
                return;
            }

            int outSteps = oldRowCount > 0 ? BoardState.Columns : 0;
            int gapSteps = outSteps > 0 ? 1 : 0;
            int now = settings.StepAt(time, outSteps, settings.StepSeconds);
            if (now == step)
            {
                return;
            }

            step = now;
            // The old rows are gone from here on; a long frame can skip the gap step, so this is not tied to it.
            if (now > outSteps)
            {
                applyZone();
            }

            int inStep = now - outSteps - gapSteps;
            if (now <= outSteps)
            {
                // Old rows pushed right: bits leave past cell 7, zeros come in at cell 0.
                ShowRows(oldRows, oldRowCount, now, 0);
                stepped(false);
            }
            else if (inStep <= 0)
            {
                ShowRows(oldRows, 0, 0, 0);
            }
            else if (inStep <= BoardState.Columns)
            {
                // New rows arrive from the left: at in-step k the row is still 8 - k cells short of its place.
                ShowRows(newRows, newRowCount, 0, BoardState.Columns - inStep);
                stepped(true);
            }
            else
            {
                End();
            }
        }

        private void ShowRows(byte[] rows, int rowCount, int shiftRight, int shiftLeft)
        {
            for (int r = 0; r < BoardView.MaxRows; r++)
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
                        cellTransforms[i].localPosition = cellPosition(r, c, rowCount);
                    }
                }

                if (used)
                {
                    valueLabels[r].SetText("{0}", value);
                    PixelText.Place(valueLabels[r], labelPosition(r, rowCount));
                }
            }
        }

        // Puts back exactly what the Render of the new stage drew.
        private void End()
        {
            time = -1f;
            applyZone();
            for (int r = 0; r < BoardView.MaxRows; r++)
            {
                bool used = r < newRowCount;
                valueLabels[r].enabled = used;
                for (int c = 0; c < BoardState.Columns; c++)
                {
                    int i = r * BoardState.Columns + c;
                    cells[i].enabled = used;
                    cells[i].sprite = restCellSprites[i];
                    cellTransforms[i].localPosition = cellPosition(r, c, newRowCount);
                }

                if (used)
                {
                    valueLabels[r].SetText("{0}", shownValues[r]);
                    PixelText.Place(valueLabels[r], labelPosition(r, newRowCount));
                }
            }

            decor.SetActive(true);
        }
    }
}
