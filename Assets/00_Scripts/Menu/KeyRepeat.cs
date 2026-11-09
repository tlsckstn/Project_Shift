namespace ProjectShift.Menu
{
    /// <summary>
    /// Hold-to-repeat for one key: one step on the press, then after <c>delay</c> seconds one step every <c>interval</c>
    /// seconds while it stays held. Releasing resets it, so the next press steps at once.
    /// </summary>
    public class KeyRepeat
    {
        // Frame times summed in float drift; a step due within this many seconds counts as reached.
        private const double Epsilon = 1e-4;

        private readonly float delay;
        private readonly float interval;
        private bool isHeld;
        private bool isIgnored;
        private double heldSeconds;
        private double nextStepSeconds;

        /// <summary>Repeats after <paramref name="delay"/> seconds held, then every <paramref name="interval"/> seconds.</summary>
        public KeyRepeat(float delay, float interval)
        {
            this.delay = delay;
            this.interval = interval;
        }

        /// <summary>
        /// Starts over. When <paramref name="heldNow"/> (the key is already down, for example held since before a menu opened),
        /// that hold gives no steps at all; the key counts again from its next press.
        /// </summary>
        public void IgnoreUntilReleased(bool heldNow)
        {
            isHeld = false;
            isIgnored = heldNow;
        }

        /// <summary>
        /// Call once per frame with whether the key is down; returns how many steps to apply this frame
        /// (1 on the press frame, more than 1 only after a long frame).
        /// </summary>
        public int Tick(bool held, float deltaTime)
        {
            if (!held)
            {
                isHeld = false;
                isIgnored = false;
                return 0;
            }

            if (isIgnored)
            {
                return 0;
            }

            if (!isHeld)
            {
                isHeld = true;
                heldSeconds = 0.0;
                nextStepSeconds = delay;
                return 1;
            }

            heldSeconds += deltaTime;
            int steps = 0;
            while (heldSeconds >= nextStepSeconds - Epsilon)
            {
                steps++;
                nextStepSeconds += interval;
            }

            return steps;
        }
    }
}
