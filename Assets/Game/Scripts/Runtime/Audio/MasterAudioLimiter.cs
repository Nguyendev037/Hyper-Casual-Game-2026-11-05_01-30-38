using UnityEngine;

namespace RHMG.Runtime.Audio
{
    /// <summary>Runs on the listener, after the sources mix. No Unity API or allocation on the DSP thread.</summary>
    [RequireComponent(typeof(AudioListener))]
    public sealed class MasterAudioLimiter : MonoBehaviour
    {
        private void OnAudioFilterRead(float[] data, int channels)
        {
            AudioLimiter.Process(data);
        }
    }
}
