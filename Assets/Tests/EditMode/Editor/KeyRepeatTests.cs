using NUnit.Framework;
using ProjectShift.Menu;

namespace ProjectShift.Tests
{
    public class KeyRepeatTests
    {
        private const float Delay = 0.3f;
        private const float Interval = 0.06f;

        [Test]
        public void Tick_FirstHeldFrame_StepsOnce()
        {
            KeyRepeat repeat = new KeyRepeat(Delay, Interval);

            Assert.AreEqual(1, repeat.Tick(true, 0.016f));
        }

        [Test]
        public void Tick_HeldBeforeDelay_NoMoreSteps()
        {
            KeyRepeat repeat = new KeyRepeat(Delay, Interval);
            repeat.Tick(true, 0f);

            Assert.AreEqual(0, repeat.Tick(true, 0.25f));
        }

        [Test]
        public void Tick_HeldOneSecond_StepsOncePlusRepeats()
        {
            KeyRepeat repeat = new KeyRepeat(Delay, Interval);

            int steps = repeat.Tick(true, 0f);
            for (int i = 0; i < 100; i++)
            {
                steps += repeat.Tick(true, 0.01f);
            }

            // 1 on press, 1 at 0.3 s, then every 0.06 s up to 1.0 s: 0.36 ... 0.96 = 11.
            Assert.AreEqual(1 + 1 + 11, steps);
        }

        [Test]
        public void Tick_ReleaseThenPress_StepsImmediately()
        {
            KeyRepeat repeat = new KeyRepeat(Delay, Interval);
            repeat.Tick(true, 0f);
            repeat.Tick(true, 0.2f);

            Assert.AreEqual(0, repeat.Tick(false, 0.016f));
            Assert.AreEqual(1, repeat.Tick(true, 0.016f));
        }

        [Test]
        public void Tick_HeldSinceIgnore_NoStepsUntilReleased()
        {
            KeyRepeat repeat = new KeyRepeat(Delay, Interval);

            repeat.IgnoreUntilReleased(true);
            int steps = 0;
            for (int i = 0; i < 100; i++)
            {
                steps += repeat.Tick(true, 0.01f);
            }

            Assert.AreEqual(0, steps);
            Assert.AreEqual(0, repeat.Tick(false, 0.016f));
            Assert.AreEqual(1, repeat.Tick(true, 0.016f));
        }

        [Test]
        public void Tick_NotHeldAtIgnore_StepsOnNextPress()
        {
            KeyRepeat repeat = new KeyRepeat(Delay, Interval);

            repeat.IgnoreUntilReleased(false);

            Assert.AreEqual(1, repeat.Tick(true, 0.016f));
        }

        [Test]
        public void Tick_NotHeld_NoSteps()
        {
            KeyRepeat repeat = new KeyRepeat(Delay, Interval);

            Assert.AreEqual(0, repeat.Tick(false, 1f));
        }
    }
}
