using NUnit.Framework;
using ProjectShift.Stage;

namespace ProjectShift.Tests
{
    public class StageProgressTests
    {
        [Test]
        public void UnlockedFrom_NothingCleared_OpensFirstOnly()
        {
            int[] bestShifts = { -1, -1, -1 };

            int unlocked = StageProgress.UnlockedFrom(bestShifts);

            Assert.AreEqual(1, unlocked);
        }

        [Test]
        public void UnlockedFrom_UnclearedStageBeforeLastClear_KeepsLaterOpen()
        {
            // Stage 1 was inserted after the player cleared stages 0, 2 and 3.
            int[] bestShifts = { 4, -1, 5, 6, -1, -1 };

            int unlocked = StageProgress.UnlockedFrom(bestShifts);

            Assert.AreEqual(5, unlocked);
        }

        [Test]
        public void UnlockedFrom_LastStageCleared_StaysAtStageCount()
        {
            int[] bestShifts = { 3, 4, 5 };

            int unlocked = StageProgress.UnlockedFrom(bestShifts);

            Assert.AreEqual(3, unlocked);
        }
    }
}
