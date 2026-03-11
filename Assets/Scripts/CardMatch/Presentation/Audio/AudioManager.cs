using UnityEngine;
using CardMatch.Config;

namespace CardMatch.Presentation.Audio
{
    public class AudioManager : MonoBehaviour
    {
        private const int SampleRate = 44100;

        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip flipClip;
        [SerializeField] private AudioClip matchClip;
        [SerializeField] private AudioClip mismatchClip;
        [SerializeField] private AudioClip gameOverClip;

        private void Awake()
        {
            EnsureAudioSource();
            EnsureFallbackClips();
        }

        public void PlaySfx(GameSfx sfx)
        {
            if (sfxSource == null)
            {
                return;
            }

            AudioClip clip = GetClip(sfx);

            if (clip == null)
            {
                return;
            }

            float volume = 1f;

            if (gameConfig != null)
            {
                volume = gameConfig.sfxVolume;
            }

            sfxSource.PlayOneShot(clip, volume);
        }

        private AudioClip GetClip(GameSfx sfx)
        {
            switch (sfx)
            {
                case GameSfx.Flip:
                    return flipClip;

                case GameSfx.Match:
                    return matchClip;

                case GameSfx.Mismatch:
                    return mismatchClip;

                case GameSfx.GameOver:
                    return gameOverClip;

                default:
                    return null;
            }
        }

        private void EnsureAudioSource()
        {
            if (sfxSource == null)
            {
                sfxSource = GetComponent<AudioSource>();
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
            }

            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
        }

        private void EnsureFallbackClips()
        {
            if (flipClip == null)
            {
                flipClip = CreateToneClip("FlipFallback", 880f, 0.08f, 0.12f);
            }

            if (matchClip == null)
            {
                matchClip = CreateToneClip("MatchFallback", 640f, 0.22f, 0.16f);
            }

            if (mismatchClip == null)
            {
                mismatchClip = CreateToneClip("MismatchFallback", 220f, 0.18f, 0.18f);
            }

            if (gameOverClip == null)
            {
                gameOverClip = CreateToneClip("GameOverFallback", 520f, 0.42f, 0.2f);
            }
        }

        private AudioClip CreateToneClip(string clipName, float frequency, float duration, float amplitude)
        {
            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float time = i / (float)SampleRate;
                float fade = 1f - (i / (float)sampleCount);
                float wave = Mathf.Sin(2f * Mathf.PI * frequency * time);
                samples[i] = wave * amplitude * fade;
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
