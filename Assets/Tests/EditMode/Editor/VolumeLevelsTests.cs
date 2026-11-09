using NUnit.Framework;
using ProjectShift.Audio;

namespace ProjectShift.Tests
{
    public class VolumeLevelsTests
    {
        private const float Tolerance = 1e-6f;

        [Test]
        public void Clamp_OutOfRange_ClampsToZeroAndMax()
        {
            Assert.AreEqual(0, VolumeLevels.Clamp(-3));
            Assert.AreEqual(8, VolumeLevels.Clamp(99));
            Assert.AreEqual(5, VolumeLevels.Clamp(5));
        }

        [Test]
        public void Step_PastEnds_StaysAtEnds()
        {
            Assert.AreEqual(0, VolumeLevels.Step(0, -1));
            Assert.AreEqual(8, VolumeLevels.Step(8, 1));
            Assert.AreEqual(5, VolumeLevels.Step(6, -1));
        }

        [Test]
        public void ToVolume_FullLevel_KeepsBaseVolume()
        {
            Assert.AreEqual(0.35f, VolumeLevels.ToVolume(0.35f, 8), Tolerance);
        }

        [Test]
        public void ToVolume_ZeroLevel_IsSilent()
        {
            Assert.AreEqual(0f, VolumeLevels.ToVolume(0.8f, 0), Tolerance);
        }

        [Test]
        public void ToVolume_HalfLevel_IsHalfBase()
        {
            Assert.AreEqual(0.4f, VolumeLevels.ToVolume(0.8f, 4), Tolerance);
        }
    }
}
