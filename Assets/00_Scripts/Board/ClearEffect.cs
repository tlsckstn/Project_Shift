using UnityEngine;

namespace ProjectShift.Board
{
    /// <summary>
    /// Exit arrivals and the stage clear: an exit flashes bright and sends white sparks out when its robot arrives; on a
    /// clear a white bar flashes over each row, the robots hold, then rise into their bright exits and fade while the screen
    /// turns to the dawn look. Owned and ticked by BoardView, which passes in its pooled robots and exits.
    /// </summary>
    public class ClearEffect
    {
        // Per exit: 8 pieces flying out in the 8 compass directions.
        private const int SparksPerExit = 8;

        private readonly ClearSettings settings;
        private readonly SpriteRenderer[] robots;
        private readonly Transform[] robotTransforms;
        private readonly SpriteRenderer[] exitMarks;
        private readonly Transform[] exitTransforms;
        private readonly Sprite[] exitFlashSprites;
        private readonly SpriteRenderer selectedFrame;
        // 0 = night look, 1 = dawn look; BoardView owns the background.
        private readonly System.Action<float> setDawn;
        private readonly SpriteRenderer[] sparks;
        private readonly Transform[] sparkTransforms;
        // One white bar over each row's cells.
        private readonly SpriteRenderer[] rowFlashes;

        // Exit arrival flash, per exit: -1 when idle. The sprite to go back to is the one Render chose.
        private readonly float[] exitFlashTimes;
        private readonly Sprite[] exitRestSprites;
        // Arrival sparks, per exit: -1 when idle.
        private readonly float[] sparkTimes;
        // Clock-out: hold, then the robots rise and fade while every exit shows bright; -1 when idle.
        private float clearTime = -1f;
        private bool clearStarted;
        private int rowCount;
        private readonly Vector3[] clearStarts;

        /// <summary>
        /// Builds the sparks (under <paramref name="decorRoot"/>) and the row bars (under <paramref name="boardRoot"/>, each
        /// <paramref name="barScale"/> times the 4x4 art-pixel <paramref name="pieceSprite"/>), hidden.
        /// </summary>
        public ClearEffect(ClearSettings settings, Transform decorRoot, Transform boardRoot, Sprite pieceSprite, Vector3 barScale,
            SpriteRenderer[] robots, Transform[] robotTransforms, SpriteRenderer[] exitMarks, Transform[] exitTransforms,
            Sprite[] exitFlashSprites, SpriteRenderer selectedFrame, System.Action<float> setDawn)
        {
            this.settings = settings;
            this.robots = robots;
            this.robotTransforms = robotTransforms;
            this.exitMarks = exitMarks;
            this.exitTransforms = exitTransforms;
            this.exitFlashSprites = exitFlashSprites;
            this.selectedFrame = selectedFrame;
            this.setDawn = setDawn;
            int exits = exitMarks.Length;
            exitFlashTimes = new float[exits];
            exitRestSprites = new Sprite[exits];
            sparkTimes = new float[exits];
            clearStarts = new Vector3[robots.Length];
            for (int i = 0; i < exits; i++)
            {
                exitFlashTimes[i] = -1f;
                sparkTimes[i] = -1f;
            }

            // Sparks are the debris piece (4x4 art pixels) in white, so they read on every crew's exit; above the robots.
            sparks = new SpriteRenderer[exits * SparksPerExit];
            sparkTransforms = new Transform[sparks.Length];
            for (int i = 0; i < sparks.Length; i++)
            {
                sparks[i] = BoardView.CreateSprite("Spark", pieceSprite, 6, decorRoot);
                sparkTransforms[i] = sparks[i].transform;
                sparks[i].enabled = false;
            }

            // The piece stretched over one row's 8 cells and above the robots; placed when a clear starts.
            rowFlashes = new SpriteRenderer[BoardView.MaxRows];
            for (int r = 0; r < rowFlashes.Length; r++)
            {
                rowFlashes[r] = BoardView.CreateSprite("ClearFlash", pieceSprite, 6, boardRoot);
                rowFlashes[r].transform.localScale = barScale;
                rowFlashes[r].enabled = false;
            }
        }

        /// <summary>
        /// Flashes exit <paramref name="exit"/> bright for a moment and sends white sparks out from it. Call right after the
        /// Render that shows its robot arriving.
        /// </summary>
        public void PlayExitFlash(int exit)
        {
            exitRestSprites[exit] = exitMarks[exit].sprite;
            exitFlashTimes[exit] = 0f;
            if (settings.SparkSeconds > 0f)
            {
                sparkTimes[exit] = 0f;
            }
        }

        /// <summary>
        /// Starts the clear on a board of <paramref name="rows"/> rows whose centres are <paramref name="rowCentres"/>
        /// (the bars are placed there when it starts). Every shown exit bursts, not only those reached on this move.
        /// </summary>
        public void PlayClear(int rows, Vector3[] rowCentres)
        {
            clearTime = 0f;
            clearStarted = false;
            rowCount = rows;
            for (int r = 0; r < rows; r++)
            {
                rowFlashes[r].transform.localPosition = rowCentres[r];
            }

            for (int i = 0; i < exitMarks.Length; i++)
            {
                if (exitMarks[i].enabled && settings.SparkSeconds > 0f)
                {
                    sparkTimes[i] = 0f;
                }
            }
        }

        /// <summary>Advances the exit flashes, sparks and clear; BoardView calls it only once a running shift slide has finished.</summary>
        public void Tick(float deltaTime)
        {
            TickExitFlashes(deltaTime);
            TickSparks(deltaTime);
            if (clearTime >= 0f)
            {
                TickClear(deltaTime);
            }
        }

        /// <summary>Ends everything at once: no flash, sparks or bars, the night look back and every robot fully visible.</summary>
        public void Stop()
        {
            for (int i = 0; i < exitMarks.Length; i++)
            {
                exitFlashTimes[i] = -1f;
                sparkTimes[i] = -1f;
            }

            for (int i = 0; i < sparks.Length; i++)
            {
                sparks[i].enabled = false;
            }

            if (clearTime < 0f)
            {
                return;
            }

            clearTime = -1f;
            for (int r = 0; r < rowFlashes.Length; r++)
            {
                rowFlashes[r].enabled = false;
            }

            setDawn(0f);
            for (int i = 0; i < robots.Length; i++)
            {
                BoardView.SetAlpha(robots[i], 1f);
            }
        }

        private void TickExitFlashes(float deltaTime)
        {
            for (int i = 0; i < exitMarks.Length; i++)
            {
                if (exitFlashTimes[i] < 0f)
                {
                    continue;
                }

                exitFlashTimes[i] += deltaTime;
                bool flashing = exitFlashTimes[i] < settings.ExitFlashSeconds;
                exitMarks[i].sprite = flashing ? exitFlashSprites[i] : exitRestSprites[i];
                if (!flashing)
                {
                    exitFlashTimes[i] = -1f;
                }
            }
        }

        // Piece k of an exit flies out along compass direction k (45 degrees apart), easing out, and fades. Fixed, no randomness.
        private void TickSparks(float deltaTime)
        {
            for (int e = 0; e < exitMarks.Length; e++)
            {
                if (sparkTimes[e] < 0f)
                {
                    continue;
                }

                sparkTimes[e] += deltaTime;
                float p = sparkTimes[e] / settings.SparkSeconds;
                bool done = p >= 1f;
                float eased = 1f - (1f - p) * (1f - p);
                float distance = 4f + settings.SparkPixels * eased;
                // Full brightness for the first half, then fade, like the shift debris.
                float alpha = p < 0.5f ? 1f : 2f * (1f - p);
                Vector3 centre = exitTransforms[e].localPosition;
                for (int k = 0; k < SparksPerExit; k++)
                {
                    SpriteRenderer spark = sparks[e * SparksPerExit + k];
                    spark.enabled = !done;
                    if (done)
                    {
                        continue;
                    }

                    float angle = k * Mathf.PI / 4f;
                    float x = Mathf.Round(Mathf.Cos(angle) * distance);
                    float y = Mathf.Round(Mathf.Sin(angle) * distance);
                    sparkTransforms[e * SparksPerExit + k].localPosition = centre + new Vector3(x, y, 0f) / BoardView.PixelsPerUnit;
                    BoardView.SetAlpha(spark, alpha);
                }

                if (done)
                {
                    sparkTimes[e] = -1f;
                }
            }
        }

        private void TickClear(float deltaTime)
        {
            // The robots start from where the finished slide left them.
            if (!clearStarted)
            {
                clearStarted = true;
                for (int i = 0; i < robots.Length; i++)
                {
                    clearStarts[i] = robotTransforms[i].localPosition;
                }

                for (int r = 0; r < rowFlashes.Length; r++)
                {
                    rowFlashes[r].enabled = r < rowCount;
                }
            }

            clearTime += deltaTime;
            TickRowFlashes();
            if (clearTime < settings.HoldSeconds)
            {
                return;
            }

            float p = settings.ClockOutSeconds > 0f ? Mathf.Min(1f, (clearTime - settings.HoldSeconds) / settings.ClockOutSeconds) : 1f;
            float risePixels = Mathf.Round(settings.ClockOutRisePixels * p);
            // The screen brightens as the robots fade, fast at first and settling into the dawn look (ease-out).
            setDawn(1f - (1f - p) * (1f - p));
            selectedFrame.enabled = false;
            for (int i = 0; i < robots.Length; i++)
            {
                if (exitMarks[i].enabled)
                {
                    exitMarks[i].sprite = exitFlashSprites[i];
                    exitFlashTimes[i] = -1f;
                }

                if (robots[i].enabled)
                {
                    robotTransforms[i].localPosition = clearStarts[i] + Vector3.up * (risePixels / BoardView.PixelsPerUnit);
                    BoardView.SetAlpha(robots[i], 1f - p);
                }
            }
        }

        // The white bars over the rows fade from FlashAlpha to 0 over FlashSeconds from the clear's start.
        private void TickRowFlashes()
        {
            float alpha = settings.FlashSeconds > 0f ? settings.FlashAlpha * (1f - clearTime / settings.FlashSeconds) : 0f;
            for (int r = 0; r < rowFlashes.Length; r++)
            {
                if (alpha <= 0f)
                {
                    rowFlashes[r].enabled = false;
                }
                else if (rowFlashes[r].enabled)
                {
                    BoardView.SetAlpha(rowFlashes[r], alpha);
                }
            }
        }
    }
}
