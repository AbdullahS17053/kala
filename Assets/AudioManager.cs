using UnityEngine;

namespace CardHouse
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource; // For background music
        [SerializeField] private AudioSource sfxSource;   // For sound effects

        [Header("Background Music")]
        [SerializeField] private AudioClip backgroundMusic;

        [Header("Card Sound Effects")]
        [SerializeField] private AudioClip cardDragSound;
        [SerializeField] private AudioClip cardDropSound;
        [SerializeField] private AudioClip cardCaptureSound;
        [SerializeField] private AudioClip buildCreationSound;
        [SerializeField] private AudioClip invalidMoveSound;

        [Header("UI Sound Effects")]
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip phaseTransitionSound;

        [Header("Volume Settings")]
        [Range(0f, 1f)] public float musicVolume = 0.5f;
        [Range(0f, 1f)] public float sfxVolume = 0.7f;

        private void Awake()
        {
            // Singleton Pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Persist through scenes
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Play background music on start
            PlayMusic();
        }

        #region Music Controls
        /// <summary>
        /// Play the background music.
        /// </summary>
        public void PlayMusic()
        {
            if (musicSource != null && backgroundMusic != null)
            {
                musicSource.clip = backgroundMusic;
                musicSource.volume = musicVolume;
                musicSource.loop = true;
                musicSource.Play();
            }
        }

        /// <summary>
        /// Stop the background music.
        /// </summary>
        public void StopMusic()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }

        /// <summary>
        /// Set the music volume.
        /// </summary>
        /// <param name="volume">Volume level (0 to 1).</param>
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp(volume, 0f, 1f);
            if (musicSource != null)
            {
                musicSource.volume = musicVolume;
            }
        }
        #endregion

        #region Sound Effects
        /// <summary>
        /// Play a sound effect.
        /// </summary>
        /// <param name="clip">The audio clip to play.</param>
        public void PlaySFX(AudioClip clip)
        {
            if (sfxSource != null && clip != null)
            {
                sfxSource.PlayOneShot(clip, sfxVolume);
            }
        }

        public void PlayCardDragSound()
        {
            PlaySFX(cardDragSound);
        }

        public void PlayCardDropSound()
        {
            PlaySFX(cardDropSound);
        }

        public void PlayCardCaptureSound()
        {
            PlaySFX(cardCaptureSound);
        }

        public void PlayBuildCreationSound()
        {
            PlaySFX(buildCreationSound);
        }

        public void PlayInvalidMoveSound()
        {
            PlaySFX(invalidMoveSound);
        }

        public void PlayButtonClickSound()
        {
            PlaySFX(buttonClickSound);
        }

        public void PlayPhaseTransitionSound()
        {
            PlaySFX(phaseTransitionSound);
        }
        #endregion

        #region Volume Controls
        /// <summary>
        /// Set the sound effects volume.
        /// </summary>
        /// <param name="volume">Volume level (0 to 1).</param>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp(volume, 0f, 1f);
        }
        #endregion
    }
}
