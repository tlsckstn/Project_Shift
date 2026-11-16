using UnityEngine;

namespace ProjectShift.Board
{
    /// <summary>
    /// The fail effect: a robot past its row end drops and fades, a robot on a bug cell fades out, and the board shakes;
    /// after the restart the failed robots fade back in. Owned and ticked by BoardView, which passes in its pooled robots.
    /// </summary>
    public class FailEffect
    {
        // Seconds per side of the shake (it swings right, left, right ...).
        private const float ShakeStepSeconds = 0.03f;
        // Per robot: fell off the row (drops) or entered a bug cell (fades). Kept through the restart for the reappear.
        private const int None = 0;
        private const int Fell = 1;
        private const int Bug = 2;

        private readonly FailSettings settings;
        private readonly Transform boardRoot;
        private readonly SpriteRenderer[] robots;
        private readonly Transform[] robotTransforms;
        private readonly int[] modes;
        private readonly Vector3[] starts;
        // -1 when idle.
        private float failTime = -1f;
        private bool started;
        private float reappearTime = -1f;

        /// <summary>Uses <paramref name="robots"/> (and their transforms) and shakes <paramref name="boardRoot"/>.</summary>
        public FailEffect(FailSettings settings, Transform boardRoot, SpriteRenderer[] robots, Transform[] robotTransforms)
        {
            this.settings = settings;
            this.boardRoot = boardRoot;
            this.robots = robots;
            this.robotTransforms = robotTransforms;
            modes = new int[robots.Length];
            starts = new Vector3[robots.Length];
        }

        /// <summary>Marks which robots fell off their row or entered a bug cell and starts the effect on the next TickFail.</summary>
        public void Play(BoardState state)
        {
            for (int i = 0; i < modes.Length; i++)
            {
                modes[i] = None;
                if (i >= state.RobotCount)
                {
                    continue;
                }

                int row = state.GetRobotRow(i);
                int col = state.GetRobotCol(i);
                if (col < 0 || col >= BoardState.Columns)
                {
                    modes[i] = Fell;
                }
                else if (state.IsBugCell(row, col))
                {
                    modes[i] = Bug;
                }
            }

            failTime = 0f;
            started = false;
        }

        /// <summary>Fades the robots that failed back in. Call right after the Render that shows the restarted stage.</summary>
        public void PlayReappear()
        {
            if (settings.ReappearSeconds <= 0f)
            {
                return;
            }

            reappearTime = 0f;
            SetFailedRobotsAlpha(0f);
        }

        /// <summary>Advances the drop, fade and shake; BoardView calls it only once a running shift slide has finished.</summary>
        public void TickFail(float deltaTime)
        {
            if (failTime < 0f)
            {
                return;
            }

            // The robots start from where the finished slide left them.
            if (!started)
            {
                started = true;
                for (int i = 0; i < starts.Length; i++)
                {
                    starts[i] = robotTransforms[i].localPosition;
                }
            }

            failTime += deltaTime;
            boardRoot.localPosition = Vector3.right * (ShakePixels(failTime) / BoardView.PixelsPerUnit);
            for (int i = 0; i < modes.Length; i++)
            {
                if (modes[i] == Fell)
                {
                    // Accelerates like gravity (distance grows with time squared), snapped to whole art pixels; fades over the second half.
                    float p = settings.FallSeconds > 0f ? Mathf.Min(1f, failTime / settings.FallSeconds) : 1f;
                    float dropPixels = Mathf.Round(settings.FallPixels * p * p);
                    robotTransforms[i].localPosition = starts[i] + Vector3.down * (dropPixels / BoardView.PixelsPerUnit);
                    BoardView.SetAlpha(robots[i], p < 0.5f ? 1f : 2f * (1f - p));
                }
                else if (modes[i] == Bug)
                {
                    float p = settings.BugFadeSeconds > 0f ? Mathf.Min(1f, failTime / settings.BugFadeSeconds) : 1f;
                    BoardView.SetAlpha(robots[i], 1f - p);
                }
            }
        }

        /// <summary>Advances the fade-in after the restart.</summary>
        public void TickReappear(float deltaTime)
        {
            if (reappearTime < 0f)
            {
                return;
            }

            reappearTime += deltaTime;
            if (reappearTime >= settings.ReappearSeconds)
            {
                StopReappear();
            }
            else
            {
                SetFailedRobotsAlpha(reappearTime / settings.ReappearSeconds);
            }
        }

        /// <summary>Ends both parts at once: the board back in place and every failed robot fully visible.</summary>
        public void Stop()
        {
            StopFail();
            StopReappear();
        }

        // Keeps modes: the restart's Render runs this, then PlayReappear still needs to know which robots failed.
        private void StopFail()
        {
            if (failTime < 0f)
            {
                return;
            }

            failTime = -1f;
            boardRoot.localPosition = Vector3.zero;
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
            for (int i = 0; i < modes.Length; i++)
            {
                modes[i] = None;
            }
        }

        // Whole art pixels: starts at ShakePixels and shrinks to 0 over ShakeSeconds, swapping sides every ShakeStepSeconds.
        private float ShakePixels(float time)
        {
            if (time >= settings.ShakeSeconds)
            {
                return 0f;
            }

            float amplitude = Mathf.Round(settings.ShakePixels * (1f - time / settings.ShakeSeconds));
            return (int)(time / ShakeStepSeconds) % 2 == 0 ? amplitude : -amplitude;
        }

        private void SetFailedRobotsAlpha(float alpha)
        {
            for (int i = 0; i < modes.Length; i++)
            {
                if (modes[i] != None)
                {
                    BoardView.SetAlpha(robots[i], alpha);
                }
            }
        }
    }
}
