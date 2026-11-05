using UnityEngine;

namespace ProjectShift.Audio
{
    /// <summary>
    /// Plays the game's sound effects and the looping background music, and mutes everything on request.
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

        private bool isMuted;

        private void Awake()
        {
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

        /// <summary>Switches all game audio off or back on.</summary>
        public void ToggleMute()
        {
            isMuted = !isMuted;
            AudioListener.volume = isMuted ? 0f : 1f;
        }
    }
}
