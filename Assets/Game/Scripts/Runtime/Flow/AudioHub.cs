using RHMG.Runtime.Audio;
using RHMG.Runtime.Visuals;
using UnityEngine;

namespace RHMG.Runtime
{
    /// <summary>Music + SFX player (SPEC-P07 §2): two music sources with cross-fade, a pool of SFX voices,
    /// and <see cref="AudioPolicy"/> limits so dense survivor combat never stacks past full scale.</summary>
    public sealed class AudioHub : MonoBehaviour
    {
        private const float CrossFadeSeconds = 1.2f;

        private AudioSource[] m_Music;
        private int m_MusicActive;
        private string m_MusicId;
        private float m_Fade = 1f;
        private AudioSource[] m_Voices;
        private int m_NextVoice;
        private readonly AudioPolicy m_Policy = new AudioPolicy();

        /// <summary>The live hub (combat code routes sounds here so the voice limits apply everywhere).</summary>
        public static AudioHub Instance { get; private set; }

        public bool MusicOn { get; set; } = true;
        public bool SfxOn { get; set; } = true;
        public bool VibrationOn { get; set; }

        /// <summary>True for automated runs (Unity batch mode or "-rhmgMute"): no sound reaches the speakers.</summary>
        public static bool SilentRun =>
            Application.isBatchMode || System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-rhmgMute") >= 0;

        private void Awake()
        {
            if (SilentRun) AudioListener.volume = 0f;
            Instance = this;
            ProceduralAudio.PrewarmAll();
            m_Music = new AudioSource[2];
            for (int i = 0; i < 2; i++)
            {
                m_Music[i] = gameObject.AddComponent<AudioSource>();
                m_Music[i].loop = true;
                m_Music[i].playOnAwake = false;
                m_Music[i].volume = 0f;
            }
            m_Voices = new AudioSource[AudioPolicy.SfxVoices];
            for (int i = 0; i < m_Voices.Length; i++)
            {
                m_Voices[i] = gameObject.AddComponent<AudioSource>();
                m_Voices[i].playOnAwake = false;
            }
        }

        /// <summary>Starts the default run music (kept for existing callers).</summary>
        public void StartMusic()
        {
            PlayMusic(string.IsNullOrEmpty(m_MusicId) ? "music_loop" : m_MusicId);
        }

        /// <summary>Cross-fades to another music loop (music_loop / music_boss / music_calm).</summary>
        public void PlayMusic(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            AudioSource current = m_Music[m_MusicActive];
            if (id == m_MusicId && current.isPlaying) return;
            m_MusicId = id;
            if (!MusicOn) return;
            AudioClip clip = ProceduralAudio.Get(id);
            if (clip == null) return;
            if (current.isPlaying)
            {
                m_MusicActive = 1 - m_MusicActive;
                m_Fade = 0f;
            }
            else
            {
                m_Fade = 1f;
            }
            AudioSource next = m_Music[m_MusicActive];
            next.clip = clip;
            next.volume = current.isPlaying ? 0f : AudioPolicy.MusicVolume;
            next.Play();
        }

        public void Play(string id)
        {
            if (!SfxOn || string.IsNullOrEmpty(id)) return;
            AudioClip clip = ProceduralAudio.Get(id);
            if (clip == null) return;
            if (!m_Policy.TryStart(id, Time.unscaledTime, clip.length, out float volume, out float pitch)) return;
            AudioSource voice = m_Voices[m_NextVoice];
            m_NextVoice = (m_NextVoice + 1) % m_Voices.Length;
            voice.Stop();
            voice.clip = clip;
            voice.volume = volume;
            voice.pitch = pitch;
            voice.Play();
        }

        /// <summary>Gem pickup: same as Play("gem"); the policy raises the pitch along the pentatonic scale per streak.</summary>
        public void PlayGem()
        {
            Play("gem");
        }

        public void OnStrongHit(float damage)
        {
            if (VibrationOn && damage >= 15f)
            {
#if UNITY_ANDROID || UNITY_IOS
                Handheld.Vibrate();
#endif
            }
        }

        private void Update()
        {
            AudioSource active = m_Music[m_MusicActive], other = m_Music[1 - m_MusicActive];
            if (m_Fade < 1f)
            {
                m_Fade = Mathf.Min(1f, m_Fade + Time.unscaledDeltaTime / CrossFadeSeconds);
                active.volume = AudioPolicy.MusicVolume * m_Fade;
                other.volume = AudioPolicy.MusicVolume * (1f - m_Fade);
                if (m_Fade >= 1f) other.Stop();
            }
            for (int i = 0; i < 2; i++) m_Music[i].mute = !MusicOn;
            for (int i = 0; i < m_Voices.Length; i++) m_Voices[i].mute = !SfxOn;
            if (MusicOn && !active.isPlaying && !string.IsNullOrEmpty(m_MusicId) && m_Fade >= 1f)
            {
                if (active.clip == null) active.clip = ProceduralAudio.Get(m_MusicId);
                if (active.clip == null) return;
                active.volume = AudioPolicy.MusicVolume;
                active.Play();
            }
        }
    }
}
