using UnityEngine;

namespace ProjectShift.Stage
{
    /// <summary>Saved progress: how many stages are open, counted from stage 1, and each stage's best clear. Lives in PlayerPrefs.</summary>
    public static class StageProgress
    {
        private const string UnlockedKey = "UnlockedStages";
        // ponytail: keyed by stage index, so reordering the stage list moves records; key by stage asset name if stages get reordered.
        private const string BestKeyPrefix = "BestShifts";

        /// <summary>Fewest shifts the stage (0-based) was ever cleared with; -1 when it was never cleared.</summary>
        public static int LoadBest(int stage)
        {
            return PlayerPrefs.GetInt(BestKeyPrefix + stage, -1);
        }

        /// <summary>Stores the stage's best clear. Saves at once, like SaveUnlocked.</summary>
        public static void SaveBest(int stage, int shifts)
        {
            PlayerPrefs.SetInt(BestKeyPrefix + stage, shifts);
            PlayerPrefs.Save();
        }

        /// <summary>Open stage count, clamped to 1..<paramref name="stageCount"/> so a missing, broken or stale value still loads.</summary>
        public static int LoadUnlocked(int stageCount)
        {
            return Mathf.Clamp(PlayerPrefs.GetInt(UnlockedKey, 1), 1, stageCount);
        }

        /// <summary>Stores the open stage count. Saves at once: WebGL only writes PlayerPrefs to the browser on Save.</summary>
        public static void SaveUnlocked(int unlocked)
        {
            PlayerPrefs.SetInt(UnlockedKey, unlocked);
            PlayerPrefs.Save();
        }
    }
}
