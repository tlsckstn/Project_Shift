using UnityEngine;

namespace ProjectShift.Audio
{
    /// <summary>
    /// BGM and SFX volume as levels 0-8 (shown as 8 LEDs; 0 is silent, 8 is the AudioSource's own volume), saved in PlayerPrefs.
    /// </summary>
    public static class VolumeLevels
    {
        /// <summary>Highest level; also the default for a player who never changed it.</summary>
        public const int Max = 8;

        private const string BgmKey = "BgmLevel";
        private const string SfxKey = "SfxLevel";

        /// <summary>The level limited to 0..<see cref="Max"/>.</summary>
        public static int Clamp(int level)
        {
            return Mathf.Clamp(level, 0, Max);
        }

        /// <summary>The level after one or more steps, stopping at 0 and <see cref="Max"/>.</summary>
        public static int Step(int level, int delta)
        {
            return Clamp(level + delta);
        }

        /// <summary>AudioSource volume for a level: <paramref name="baseVolume"/> at <see cref="Max"/>, scaled linearly down to 0.</summary>
        public static float ToVolume(float baseVolume, int level)
        {
            return baseVolume * Clamp(level) / Max;
        }

        /// <summary>Saved BGM level, clamped so a broken or stale value still loads; <see cref="Max"/> when never saved.</summary>
        public static int LoadBgm()
        {
            return Clamp(PlayerPrefs.GetInt(BgmKey, Max));
        }

        /// <summary>Saved SFX level, clamped; <see cref="Max"/> when never saved.</summary>
        public static int LoadSfx()
        {
            return Clamp(PlayerPrefs.GetInt(SfxKey, Max));
        }

        /// <summary>Stores the BGM level. Saves at once: WebGL only writes PlayerPrefs to the browser on Save.</summary>
        public static void SaveBgm(int level)
        {
            PlayerPrefs.SetInt(BgmKey, level);
            PlayerPrefs.Save();
        }

        /// <summary>Stores the SFX level and saves at once.</summary>
        public static void SaveSfx(int level)
        {
            PlayerPrefs.SetInt(SfxKey, level);
            PlayerPrefs.Save();
        }
    }
}
