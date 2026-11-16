using UnityEngine;

namespace ProjectShift.Stage
{
    /// <summary>Saved progress: each stage's best clear, in PlayerPrefs. Which stages are open is derived from it.</summary>
    public static class StageProgress
    {
        // Keyed by the stage asset name, so inserting or reordering stages keeps every record on its own stage.
        // Renaming a stage asset drops its record.
        private const string BestKeyPrefix = "Best_";

        /// <summary>Fewest shifts the stage was ever cleared with; -1 when it was never cleared.</summary>
        public static int LoadBest(string stageName)
        {
            return PlayerPrefs.GetInt(BestKeyPrefix + stageName, -1);
        }

        /// <summary>Stores the stage's best clear. Saves at once so quitting right after a clear keeps it.</summary>
        public static void SaveBest(string stageName, int shifts)
        {
            PlayerPrefs.SetInt(BestKeyPrefix + stageName, shifts);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Open stage count, from the best clears (-1 = never cleared) in stage order: every stage up to the one after
        /// the last cleared stage. Derived instead of saved, so an inserted stage never hides later progress.
        /// </summary>
        public static int UnlockedFrom(int[] bestShifts)
        {
            int unlocked = 1;
            for (int i = 0; i < bestShifts.Length - 1; i++)
            {
                if (bestShifts[i] >= 0)
                {
                    unlocked = i + 2;
                }
            }

            return unlocked;
        }
    }
}
