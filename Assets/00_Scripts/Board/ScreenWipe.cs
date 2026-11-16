using UnityEngine;

namespace ProjectShift.Board
{
    /// <summary>
    /// The screen wipe, an old monitor switching off and on: covers (night background and circuit) close from the top and
    /// bottom onto a green centre line, the screen underneath changes, then the covers open from that line. Owned and
    /// ticked by BoardView; the cover sits above everything, the pause menu included.
    /// </summary>
    public class ScreenWipe
    {
        private const int Order = 30;
        // Steps each way: the covers close in this many steps, then open in as many.
        private const int Steps = 16;

        private readonly WipeSettings settings;
        private readonly TransitionSettings timing;
        private readonly System.Action<bool> stepped;
        private readonly GameObject root;
        // Top and bottom cover masks (left pivot), and the green line on each one's inner edge.
        private readonly Transform[] masks = new Transform[2];
        private readonly SpriteRenderer[] edgeLines = new SpriteRenderer[2];
        private readonly Vector2 screenSize;
        // -1 when idle.
        private float time = -1f;
        private int step;
        // Last tick sound played; the wipe ticks every other step so the sound keeps the stage transition rhythm.
        private int tick;
        private System.Action swap;

        /// <summary>
        /// Builds the hidden cover under <paramref name="parent"/>: <paramref name="background"/> and a copy of
        /// <paramref name="circuit"/>, so the circuit looks still while the screen is covered. <paramref name="timing"/>
        /// gives the pause at full cover; <paramref name="stepped"/> is raised per tick (false closing, true opening).
        /// </summary>
        public ScreenWipe(Transform parent, Color background, Sprite circuit, WipeSettings settings, TransitionSettings timing, System.Action<bool> stepped)
        {
            this.settings = settings;
            this.timing = timing;
            this.stepped = stepped;
            screenSize = circuit.bounds.size;
            root = new GameObject("Wipe");
            root.transform.SetParent(parent, false);
            // The colour is baked into a 1x1 texture: tinting a white sprite drifts by 2 levels on this dark colour (linear
            // vertex colour precision), which showed the cover's edges against the camera background.
            Texture2D backgroundTexture = new Texture2D(1, 1);
            backgroundTexture.SetPixel(0, 0, background);
            backgroundTexture.Apply();
            SpriteRenderer cover = BoardView.CreateSprite("WipeBackground", Sprite.Create(backgroundTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f), Order, root.transform);
            cover.transform.localScale = new Vector3(screenSize.x, screenSize.y, 1f);
            cover.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            SpriteRenderer circuitCopy = BoardView.CreateSprite("WipeCircuit", circuit, Order + 1, root.transform);
            circuitCopy.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            // The 4x4 white texture at 4 px per unit is a 1x1 unit square; the mask's copy has a left pivot.
            Sprite maskSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0f, 0.5f), 4f);
            Sprite lineSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
            for (int side = 0; side < 2; side++)
            {
                GameObject maskObject = new GameObject("WipeMask");
                maskObject.transform.SetParent(root.transform, false);
                maskObject.AddComponent<SpriteMask>().sprite = maskSprite;
                masks[side] = maskObject.transform;
                edgeLines[side] = BoardView.CreateSprite("WipeEdge", lineSprite, Order + 2, root.transform);
                edgeLines[side].color = settings.EdgeColor;
            }

            root.SetActive(false);
        }

        /// <summary>True while the wipe plays.</summary>
        public bool IsPlaying => time >= 0f;

        /// <summary>Starts the wipe; <paramref name="swap"/> runs once, on the first step past full cover.</summary>
        public void Play(System.Action swap)
        {
            this.swap = swap;
            time = 0f;
            step = -1;
            tick = -1;
            root.SetActive(true);
            Tick(0f);
        }

        /// <summary>Advances a playing wipe by <paramref name="deltaTime"/> seconds; does nothing while idle.</summary>
        public void Tick(float deltaTime)
        {
            if (time < 0f)
            {
                return;
            }

            time += deltaTime;
            // A zero step time would divide by zero below; swap at once instead.
            int now = settings.StepSeconds > 0f ? timing.StepAt(time, Steps, settings.StepSeconds) : 2 * Steps + 2;
            if (now == step)
            {
                return;
            }

            step = now;
            // Swapped on the first step past full cover; a long frame may jump straight into the arriving steps.
            if (now > Steps && swap != null)
            {
                System.Action pending = swap;
                swap = null;
                pending();
            }

            int inStep = now - Steps - 1;
            if (inStep > Steps)
            {
                time = -1f;
                root.SetActive(false);
                return;
            }

            // Share of the half height (180 art px) each cover takes, in whole pixels: grows while leaving, full in the gap,
            // shrinks back to the centre line while arriving.
            int halfPixels = Mathf.RoundToInt(screenSize.y * BoardView.PixelsPerUnit / 2f);
            int closed = now <= Steps ? now : Steps - Mathf.Max(inStep, 0);
            SetCover(Mathf.RoundToInt((float)halfPixels * closed / Steps) / BoardView.PixelsPerUnit);

            // Ticks 0, 1, 2 ... while leaving and 100, 101 ... while arriving (none in the gap); one per two steps.
            int nowTick = now <= Steps ? (now + 1) / 2 : inStep > 0 ? 100 + (inStep + 1) / 2 : tick;
            if (nowTick != tick)
            {
                tick = nowTick;
                stepped(now > Steps);
            }
        }

        // Covers `height` units from the top and the same from the bottom, with a green line centred on each inner edge; at
        // full cover both lines meet as one line across the centre.
        private void SetCover(float height)
        {
            float line = settings.EdgePixels / BoardView.PixelsPerUnit;
            float left = -screenSize.x / 2f;
            float top = screenSize.y / 2f;
            for (int side = 0; side < 2; side++)
            {
                float sign = side == 0 ? 1f : -1f;
                masks[side].localPosition = new Vector3(left, sign * (top - height / 2f), 0f);
                masks[side].localScale = new Vector3(screenSize.x, height, 1f);
                edgeLines[side].enabled = height > 0f;
                edgeLines[side].transform.localPosition = new Vector3(0f, sign * (top - height), 0f);
                edgeLines[side].transform.localScale = new Vector3(screenSize.x, line, 1f);
            }
        }
    }
}
