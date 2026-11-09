using UnityEngine;

namespace ProjectShift.Audio
{
    /// <summary>
    /// Plays the game's sound effects and the looping background music, each at its own saved 0-8 level.
    /// Clips come from art-src/gen/sounds.py (synthesized, no third-party samples).
    /// </summary>
    public class SoundPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource effectsSource;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioClip walkClip;
        [SerializeField] private AudioClip shiftClip;
        [SerializeField] private AudioClip failClip;
        [SerializeField] private AudioClip exitClip;
        [SerializeField] private AudioClip clearClip;
        [SerializeField] private AudioClip rewindClip;
        [SerializeField] private AudioClip undoClip;
        [SerializeField] private AudioClip transitionOutClip;
        [SerializeField] private AudioClip transitionInClip;

        private float musicBase;
        private float effectsBase;
        private int bgmLevel;
        private int sfxLevel;

        /// <summary>Music level 0-8 (0 is silent).</summary>
        public int BgmLevel => bgmLevel;

        /// <summary>Sound effect level 0-8 (0 is silent).</summary>
        public int SfxLevel => sfxLevel;

        private void Awake()
        {
            // The scene's AudioSource volumes are the level-8 loudness.
            musicBase = musicSource.volume;
            effectsBase = effectsSource.volume;
            bgmLevel = VolumeLevels.LoadBgm();
            sfxLevel = VolumeLevels.LoadSfx();
            musicSource.volume = VolumeLevels.ToVolume(musicBase, bgmLevel);
            effectsSource.volume = VolumeLevels.ToVolume(effectsBase, sfxLevel);

            // The clips import with Preload Audio Data off, so each one would only start loading on its first play and that
            // play would be silent (seen on WebGL: "Trying to get length of sound which is not loaded yet").
            AudioClip[] clips = { walkClip, shiftClip, failClip, exitClip, clearClip, rewindClip, undoClip, transitionOutClip, transitionInClip, musicSource.clip };
            foreach (AudioClip clip in clips)
            {
                clip.LoadAudioData();
            }
        }

        /// <summary>A robot walked or descended.</summary>
        public void PlayWalk() => effectsSource.PlayOneShot(walkClip);

        /// <summary>A row was shifted.</summary>
        public void PlayShift() => effectsSource.PlayOneShot(shiftClip);

        /// <summary>A robot fell off a row or entered a bug cell.</summary>
        public void PlayFail() => effectsSource.PlayOneShot(failClip);

        /// <summary>A robot reached its own exit without clearing the stage.</summary>
        public void PlayExit() => effectsSource.PlayOneShot(exitClip);

        /// <summary>The stage was cleared.</summary>
        public void PlayClear() => effectsSource.PlayOneShot(clearClip);

        /// <summary>A restart started rewinding the stage.</summary>
        public void PlayRewind() => effectsSource.PlayOneShot(rewindClip);

        /// <summary>One move was undone.</summary>
        public void PlayUndo() => effectsSource.PlayOneShot(undoClip);

        /// <summary>One cell step of the stage transition (<paramref name="arriving"/>: the new stage's rows are coming in).</summary>
        public void PlayTransitionStep(bool arriving) => effectsSource.PlayOneShot(arriving ? transitionInClip : transitionOutClip);

        /// <summary>Moves the music level by <paramref name="delta"/> (stops at 0 and 8); applies and saves it when it changed.</summary>
        public void ChangeBgm(int delta)
        {
            int level = VolumeLevels.Step(bgmLevel, delta);
            if (level == bgmLevel)
            {
                return;
            }

            bgmLevel = level;
            musicSource.volume = VolumeLevels.ToVolume(musicBase, level);
            VolumeLevels.SaveBgm(level);
        }

        /// <summary>Moves the effect level by <paramref name="delta"/>; when it changed, applies, saves and plays the shift sound at the new level.</summary>
        public void ChangeSfx(int delta)
        {
            int level = VolumeLevels.Step(sfxLevel, delta);
            if (level == sfxLevel)
            {
                return;
            }

            sfxLevel = level;
            effectsSource.volume = VolumeLevels.ToVolume(effectsBase, level);
            VolumeLevels.SaveSfx(level);
            effectsSource.PlayOneShot(shiftClip);
        }
    }
}
