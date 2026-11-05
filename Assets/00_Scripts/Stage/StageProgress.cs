using UnityEngine;

namespace ProjectShift.Stage
{
    /// <summary>Saved progress: how many stages are open, counted from stage 1. Lives in PlayerPrefs.</summary>
    public static class StageProgress
    {
        private const string UnlockedKey = "UnlockedStages";

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
