using UnityEngine;

namespace ProjectShift.Board
{
    /// <summary>
    /// Walking and shifting: a shifted row slides in from one cell back (its robots ride along), the bit pushed past the row
    /// end slides out and fades or, when lit, breaks into pieces (an undo plays the break backwards), and a walking robot
    /// leaves a fading copy on the cell it left. Owned and ticked by BoardView, which passes in its pooled cells and robots.
    /// </summary>
    public class MoveEffect
    {
        private const int DebrisCount = 6;
        private const float DebrisGravityPixels = 500f;
        // Share of the break an undo replays backwards (pieces still high, close by), so the cell comes back quickly.
        private const float RegenShare = 0.6f;

        private readonly MoveSettings settings;
        private readonly float cellSize;
        private readonly SpriteRenderer[] cells;
        private readonly Transform[] cellTransforms;
        private readonly Transform[] robotTransforms;
        private readonly SpriteRenderer selectedFrame;
        private readonly Transform selectedFrameTransform;
        private readonly Sprite[] robotIdleSprites;
        private readonly Vector3 robotOffset;
        // Cell centre (row, column) on the board BoardView shows now.
        private readonly System.Func<int, int, Vector3> cellPosition;

        // Shift slide: Render places everything at its final spot, then PlayShift offsets the row back one cell and it eases in.
        private int slideRow = -1;
        private int slideDirection;
        private float slideTime;
        private int slideEnterCol;
        private int slideLostCol;
        private readonly bool[] slideRobots;
        private readonly Vector3[] slideRobotEnds;
        private bool slideMovesSelectedFrame;
        private Vector3 slideSelectedFrameEnd;
        // The bit pushed past the row end: an off bit slides out and fades, a lit one breaks into debris pieces.
        private readonly SpriteRenderer lostCell;
        private readonly Transform lostCellTransform;
        private readonly SpriteRenderer[] debris = new SpriteRenderer[DebrisCount];
        private readonly Transform[] debrisTransforms = new Transform[DebrisCount];
        private Vector3 debrisOrigin;
        private int debrisDirection;
        private float debrisTime = -1f;
        private float debrisDuration;
        // Undo plays the break backwards: pieces fly into this cell (index into cells), hidden until they arrive; -1 when none.
        private bool debrisReversed;
        private int regenCell = -1;
        // Walk afterimage: a fading copy of the robot on the cell it left.
        private readonly SpriteRenderer walkGhost;
        private float ghostTime = -1f;

        /// <summary>
        /// Builds the lost cell (<paramref name="gapSprite"/>), the debris pieces (<paramref name="pieceSprite"/>) and the walk
        /// copy under <paramref name="decorRoot"/>, hidden. <paramref name="robotOffset"/> lifts a robot from its cell centre.
        /// </summary>
        public MoveEffect(MoveSettings settings, float cellSize, Transform decorRoot, Sprite gapSprite, Sprite pieceSprite,
            SpriteRenderer[] cells, Transform[] cellTransforms, Transform[] robotTransforms, SpriteRenderer selectedFrame,
            Sprite[] robotIdleSprites, Vector3 robotOffset, System.Func<int, int, Vector3> cellPosition)
        {
            this.settings = settings;
            this.cellSize = cellSize;
            this.cells = cells;
            this.cellTransforms = cellTransforms;
            this.robotTransforms = robotTransforms;
            this.selectedFrame = selectedFrame;
            selectedFrameTransform = selectedFrame.transform;
            this.robotIdleSprites = robotIdleSprites;
            this.robotOffset = robotOffset;
            this.cellPosition = cellPosition;
            slideRobots = new bool[robotTransforms.Length];
            slideRobotEnds = new Vector3[robotTransforms.Length];

            lostCell = BoardView.CreateSprite("LostCell", gapSprite, 2, decorRoot);
            lostCellTransform = lostCell.transform;
            lostCell.enabled = false;
            for (int i = 0; i < DebrisCount; i++)
            {
                debris[i] = BoardView.CreateSprite("Debris", pieceSprite, 3, decorRoot);
                debris[i].color = settings.DebrisColor;
                debrisTransforms[i] = debris[i].transform;
                debris[i].enabled = false;
            }

            walkGhost = BoardView.CreateSprite("WalkGhost", robotIdleSprites[0], 4, decorRoot);
            walkGhost.enabled = false;
        }

        /// <summary>True while a shifted row slides into place.</summary>
        public bool IsSliding => slideRow >= 0;

        /// <summary>
        /// Slides row <paramref name="row"/> in from one cell back after a shift (<paramref name="direction"/> -1 left, 1 right).
        /// <paramref name="lostBit"/> is the value of the cell pushed past the row end.
        /// </summary>
        public void PlayShift(int row, int direction, bool lostBit, BoardState state)
        {
            if (settings.SlideSeconds <= 0f)
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
            for (int i = 0; i < slideRobots.Length; i++)
            {
                slideRobots[i] = i < state.RobotCount && state.GetRobotRow(i) == row;
                slideRobotEnds[i] = robotTransforms[i].localPosition;
                slideMovesSelectedFrame |= slideRobots[i] && selectedFrame.enabled && i == state.SelectedRobot;
            }

            // A lit bit breaks apart at once just past the row end; an off bit slides out and fades as before. The pieces start
            // outside the row (half a cell + 8 px, the piece grid's half width) so none lands on the cell that now fills the end.
            lostCell.enabled = !lostBit;
            if (lostBit)
            {
                float outsidePixels = cellSize * BoardView.PixelsPerUnit / 2f + 8f;
                StartDebris(cellPosition(row, slideLostCol) + Vector3.right * (direction * outsidePixels / BoardView.PixelsPerUnit), direction, false);
            }

            ApplySlide(0f);
        }

        /// <summary>
        /// Undo brought back the lit bit at (<paramref name="row"/>, <paramref name="col"/>) that a shift in
        /// <paramref name="direction"/> pushed out: the break runs backwards and the cell shows once the pieces arrive.
        /// </summary>
        public void PlayRegenerate(int row, int col, int direction)
        {
            if (settings.DebrisSeconds <= 0f)
            {
                return;
            }

            StartDebris(cellPosition(row, col), direction, true);
            regenCell = row * BoardState.Columns + col;
            BoardView.SetAlpha(cells[regenCell], 0f);
        }

        /// <summary>Leaves a fading copy of robot <paramref name="robot"/> on the cell it just left (walk or descend).</summary>
        public void PlayWalk(int robot, int fromRow, int fromCol)
        {
            if (settings.GhostSeconds <= 0f)
            {
                return;
            }

            walkGhost.sprite = robotIdleSprites[robot];
            walkGhost.transform.localPosition = cellPosition(fromRow, fromCol) + robotOffset;
            walkGhost.enabled = true;
            ghostTime = 0f;
            BoardView.SetAlpha(walkGhost, settings.GhostAlpha);
        }

        /// <summary>Advances the slide, the debris and the walk copy, in that order.</summary>
        public void Tick(float deltaTime)
        {
            if (slideRow >= 0)
            {
                slideTime += deltaTime;
                float t = slideTime / settings.SlideSeconds;
                if (t >= 1f)
                {
                    StopSlide();
                }
                else
                {
                    ApplySlide(t);
                }
            }

            if (debrisTime >= 0f)
            {
                debrisTime += deltaTime;
                TickDebris();
            }

            if (ghostTime >= 0f)
            {
                ghostTime += deltaTime;
                float t = ghostTime / settings.GhostSeconds;
                if (t >= 1f)
                {
                    StopGhost();
                }
                else
                {
                    BoardView.SetAlpha(walkGhost, settings.GhostAlpha * (1f - t));
                }
            }
        }

        /// <summary>Snaps a running slide to its end and hides the walk copy; debris keeps flying.</summary>
        public void Stop()
        {
            StopSlide();
            StopGhost();
        }

        // t is 0..1 linear time; the motion eases out. Positions snap to whole art pixels so the pixel art stays crisp.
        private void ApplySlide(float t)
        {
            float eased = 1f - (1f - t) * (1f - t);
            float offsetPixels = Mathf.Round((1f - eased) * -slideDirection * BoardView.PixelsPerUnit * cellSize);
            Vector3 offset = Vector3.right * (offsetPixels / BoardView.PixelsPerUnit);

            for (int c = 0; c < BoardState.Columns; c++)
            {
                int i = slideRow * BoardState.Columns + c;
                cellTransforms[i].localPosition = cellPosition(slideRow, c) + offset;
            }

            // The entering cell fades in, unless an undo is re-forming it from debris (it shows when the pieces arrive).
            int enterCell = slideRow * BoardState.Columns + slideEnterCol;
            if (enterCell != regenCell)
            {
                BoardView.SetAlpha(cells[enterCell], eased);
            }
            lostCellTransform.localPosition = cellPosition(slideRow, slideLostCol) + Vector3.right * (slideDirection * cellSize) + offset;
            BoardView.SetAlpha(lostCell, 1f - eased);

            for (int i = 0; i < slideRobots.Length; i++)
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
            if (row * BoardState.Columns + slideEnterCol != regenCell)
            {
                BoardView.SetAlpha(cells[row * BoardState.Columns + slideEnterCol], 1f);
            }
            lostCell.enabled = false;
            slideRow = -1;
        }

        private void StartDebris(Vector3 origin, int direction, bool reversed)
        {
            if (settings.DebrisSeconds <= 0f)
            {
                return;
            }

            ShowRegenCell();
            debrisOrigin = origin;
            debrisDirection = direction;
            debrisReversed = reversed;
            // Backwards it replays only the first part of the flight (pieces still high, close by) so the cell comes back quickly.
            debrisDuration = reversed ? settings.DebrisSeconds * RegenShare : settings.DebrisSeconds;
            debrisTime = 0f;
            for (int i = 0; i < DebrisCount; i++)
            {
                debris[i].enabled = true;
            }

            TickDebris();
        }

        // Piece i sits in a 3 x 2 grid over the cell and flies outward (the shift direction) and up, then falls with gravity.
        // Fixed per-piece speeds, no randomness, so every lost bit breaks the same way. Positions snap to whole art pixels.
        private void TickDebris()
        {
            float p = debrisTime / debrisDuration;
            if (p >= 1f)
            {
                debrisTime = -1f;
                for (int i = 0; i < DebrisCount; i++)
                {
                    debris[i].enabled = false;
                }

                ShowRegenCell();
                return;
            }

            // Backwards: flight time runs from the end of the replayed part down to 0, where the pieces sit on the cell.
            float time = debrisReversed ? debrisDuration - debrisTime : debrisTime;
            float alpha = debrisReversed || p < 0.5f ? 1f : 2f * (1f - p);
            for (int i = 0; i < DebrisCount; i++)
            {
                int column = i % 3;
                int row = i / 3;
                float startX = (column - 1) * 8f * debrisDirection;
                float startY = row == 0 ? 6f : -6f;
                float speedX = (50f + 25f * column) * debrisDirection;
                float speedY = (row == 0 ? 110f : 60f) + 10f * column;
                float x = Mathf.Round(startX + speedX * time);
                float y = Mathf.Round(startY + speedY * time - 0.5f * DebrisGravityPixels * time * time);
                debrisTransforms[i].localPosition = debrisOrigin + new Vector3(x, y, 0f) / BoardView.PixelsPerUnit;
                BoardView.SetAlpha(debris[i], alpha);
            }
        }

        // Ends a pending regenerate: its cell shows again (also when another break or regenerate cuts it short).
        private void ShowRegenCell()
        {
            if (regenCell < 0)
            {
                return;
            }

            BoardView.SetAlpha(cells[regenCell], 1f);
            regenCell = -1;
        }

        private void StopGhost()
        {
            ghostTime = -1f;
            walkGhost.enabled = false;
        }
    }
}
