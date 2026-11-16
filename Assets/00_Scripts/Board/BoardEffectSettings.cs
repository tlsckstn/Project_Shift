using System;
using UnityEngine;

namespace ProjectShift.Board
{
    /// <summary>Walking and shifting: the row slide, the bits breaking off the row end, and the walk afterimage.</summary>
    [Serializable]
    public class MoveSettings
    {
        [Tooltip("Seconds a shifted row (its cells and the robots on it) takes to slide one cell.")]
        [SerializeField] private float slideSeconds = 0.1f;
        [Tooltip("Color of the pieces a lit bit breaks into when it is pushed past the row end (art bible Platform 1).")]
        [SerializeField] private Color debrisColor = new Color32(0x4D, 0xE6, 0x80, 0xFF);
        [Tooltip("Seconds those pieces fly out and fall; they fade over the second half.")]
        [SerializeField] private float debrisSeconds = 0.4f;
        [Tooltip("Seconds the faint copy left on the cell a robot walked or descended from takes to fade out.")]
        [SerializeField] private float ghostSeconds = 0.15f;
        [Tooltip("Starting opacity of that walk afterimage.")]
        [SerializeField] private float ghostAlpha = 0.45f;

        /// <summary>Seconds a shifted row takes to slide one cell.</summary>
        public float SlideSeconds => slideSeconds;

        /// <summary>Color of the pieces of a lit bit pushed past the row end.</summary>
        public Color DebrisColor => debrisColor;

        /// <summary>Seconds those pieces fly and fade.</summary>
        public float DebrisSeconds => debrisSeconds;

        /// <summary>Seconds the walk afterimage takes to fade out.</summary>
        public float GhostSeconds => ghostSeconds;

        /// <summary>Starting opacity of the walk afterimage.</summary>
        public float GhostAlpha => ghostAlpha;
    }

    /// <summary>A robot falling off its row or entering a bug cell, the board shake, and the reappear after the restart.</summary>
    [Serializable]
    public class FailSettings
    {
        [Tooltip("How far (art pixels) a robot that fell off its row drops, accelerating, before it is gone.")]
        [SerializeField] private float fallPixels = 54f;
        [Tooltip("Seconds that drop takes; it fades over the second half. Keep the slide + this under StageManager's fail delay.")]
        [SerializeField] private float fallSeconds = 0.45f;
        [Tooltip("Seconds a robot that entered a bug cell takes to fade out. Keep the slide + this under StageManager's fail delay.")]
        [SerializeField] private float bugFadeSeconds = 0.3f;
        [Tooltip("Seconds a failed robot takes to fade back in at its start cell after the restart.")]
        [SerializeField] private float reappearSeconds = 0.15f;
        [Tooltip("How far (art pixels) the board shakes sideways at first; it settles to 0. The HUD lines stay still.")]
        [SerializeField] private float shakePixels = 3f;
        [Tooltip("Seconds that shake lasts.")]
        [SerializeField] private float shakeSeconds = 0.2f;

        /// <summary>Art pixels a fallen robot drops.</summary>
        public float FallPixels => fallPixels;

        /// <summary>Seconds that drop takes.</summary>
        public float FallSeconds => fallSeconds;

        /// <summary>Seconds a robot in a bug cell takes to fade out.</summary>
        public float BugFadeSeconds => bugFadeSeconds;

        /// <summary>Seconds a failed robot takes to fade back in after the restart.</summary>
        public float ReappearSeconds => reappearSeconds;

        /// <summary>Art pixels the board first shakes sideways.</summary>
        public float ShakePixels => shakePixels;

        /// <summary>Seconds the shake lasts.</summary>
        public float ShakeSeconds => shakeSeconds;
    }

    /// <summary>Exit arrivals, the stage clear (row flash, clock-out, dawn) and the rating LEDs on the result.</summary>
    [Serializable]
    public class ClearSettings
    {
        [Tooltip("Seconds an exit flashes bright when its robot arrives.")]
        [SerializeField] private float exitFlashSeconds = 0.12f;
        [Tooltip("Seconds the white sparks around an arriving robot's exit fly out and fade.")]
        [SerializeField] private float sparkSeconds = 0.25f;
        [Tooltip("How far (art pixels) those sparks travel from the exit's centre.")]
        [SerializeField] private float sparkPixels = 14f;
        [Tooltip("Starting opacity of the white flash over the board's rows; it fades to 0.")]
        [SerializeField] private float flashAlpha = 0.5f;
        [Tooltip("Seconds that flash takes to fade.")]
        [SerializeField] private float flashSeconds = 0.12f;
        [Tooltip("Clear result: seconds between the earned rating LEDs lighting one by one.")]
        [SerializeField] private float ledStepSeconds = 0.15f;
        [Tooltip("Seconds every robot holds its arrival face before clocking out.")]
        [SerializeField] private float holdSeconds = 0.2f;
        [Tooltip("Seconds the robots take to rise into their bright exits and fade. Keep the slide + hold + this under StageManager's clear delay.")]
        [SerializeField] private float clockOutSeconds = 0.4f;
        [Tooltip("How far (art pixels) the robots rise while clocking out.")]
        [SerializeField] private float clockOutRisePixels = 3f;

        /// <summary>Seconds an exit flashes when its robot arrives.</summary>
        public float ExitFlashSeconds => exitFlashSeconds;

        /// <summary>Seconds the arrival sparks fly and fade.</summary>
        public float SparkSeconds => sparkSeconds;

        /// <summary>Art pixels the arrival sparks travel.</summary>
        public float SparkPixels => sparkPixels;

        /// <summary>Starting opacity of the clear flash over the rows.</summary>
        public float FlashAlpha => flashAlpha;

        /// <summary>Seconds the clear flash takes to fade.</summary>
        public float FlashSeconds => flashSeconds;

        /// <summary>Seconds between rating LEDs lighting.</summary>
        public float LedStepSeconds => ledStepSeconds;

        /// <summary>Seconds the robots hold their arrival face before clocking out.</summary>
        public float HoldSeconds => holdSeconds;

        /// <summary>Seconds the clock-out rise and fade takes.</summary>
        public float ClockOutSeconds => clockOutSeconds;

        /// <summary>Art pixels the robots rise while clocking out.</summary>
        public float ClockOutRisePixels => clockOutRisePixels;
    }

    /// <summary>Stage to stage: the old rows shift out right one cell per step, a pause, then the new rows shift in.</summary>
    [Serializable]
    public class TransitionSettings
    {
        [Tooltip("Seconds per one-cell shift. The old rows shift out right in 8 steps, then the new rows shift in.")]
        [SerializeField] private float stepSeconds = 0.035f;
        [Tooltip("Empty pause between the old rows leaving and the new rows arriving (the screen wipe pauses as long at full cover).")]
        [SerializeField] private float gapSeconds = 0.1f;

        /// <summary>Seconds per one-cell step.</summary>
        public float StepSeconds => stepSeconds;

        /// <summary>Seconds of the pause between leaving and arriving; the screen wipe uses it too.</summary>
        public float GapSeconds => gapSeconds;
    }

    /// <summary>Screen change (title, stage select, puzzle): covers close from the top and bottom onto a green line, then open.</summary>
    [Serializable]
    public class WipeSettings
    {
        [Tooltip("The line on each cover's inner edge, one line across the centre at full cover (Platform green).")]
        [SerializeField] private Color edgeColor = new Color32(0x4D, 0xE6, 0x80, 0xFF);
        [Tooltip("Thickness of that edge line in art pixels.")]
        [SerializeField] private float edgePixels = 2f;
        [Tooltip("Seconds per step (16 steps each way, plus the stage transition gap in between).")]
        [SerializeField] private float stepSeconds = 0.025f;

        /// <summary>Color of the cover edge line.</summary>
        public Color EdgeColor => edgeColor;

        /// <summary>Art pixels of the cover edge line.</summary>
        public float EdgePixels => edgePixels;

        /// <summary>Seconds per wipe step.</summary>
        public float StepSeconds => stepSeconds;
    }
}
