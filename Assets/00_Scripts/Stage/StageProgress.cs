using UnityEngine;

namespace ProjectShift.Stage
{
    /// <summary>Saved progress: how many stages are open, counted from stage 1, and each stage's best clear. Lives in PlayerPrefs.</summary>
    public static class StageProgress
    {
        private const string UnlockedKey = "UnlockedStages";
        // Keyed by the stage asset name, so inserting or reordering stages keeps every record on its own stage.
        // Renaming a stage asset drops its record.
        private const string BestKeyPrefix = "Best_";

        /// <summary>Fewest shifts the stage was ever cleared with; -1 when it was never cleared.</summary>
        public static int LoadBest(string stageName)
        {
            return PlayerPrefs.GetInt(BestKeyPrefix + stageName, -1);
        }

        /// <summary>Stores the stage's best clear. Saves at once, like SaveUnlocked.</summary>
        public static void SaveBest(string stageName, int shifts)
        {
            PlayerPrefs.SetInt(BestKeyPrefix + stageName, shifts);
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
