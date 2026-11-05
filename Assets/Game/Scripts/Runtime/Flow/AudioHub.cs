using RHMG.Runtime.Visuals;
using UnityEngine;

namespace RHMG.Runtime
{
    public sealed class AudioHub : MonoBehaviour
    {
        AudioSource music;
        AudioSource sfx;

        public bool MusicOn { get; set; } = true;
        public bool SfxOn { get; set; } = true;
        public bool VibrationOn { get; set; }

        void Awake()
        {
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            music.clip = ProceduralAudio.Get("music_loop");
            music.volume = 0.25f;
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
        }

        public void StartMusic()
        {
            if (MusicOn && music.clip != null && !music.isPlaying)
            {
                music.Play();
            }
        }

        public void Play(string id)
        {
            if (SfxOn)
            {
                sfx.PlayOneShot(ProceduralAudio.Get(id));
            }
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

        void Update()
        {
            music.mute = !MusicOn;
            sfx.mute = !SfxOn;
        }
    }
}
