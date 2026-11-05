using UnityEngine;

namespace RHMG.Runtime.Visuals
{
    /// <summary>Disables the attached GameObject after a lifetime (pool compatible).</summary>
    public sealed class AutoDisable : MonoBehaviour
    {
        public float Lifetime = 1f;

        private float m_Timer;

        private void OnEnable()
        {
            m_Timer = 0f;
        }

        private void Update()
        {
            // SPEC: uses unscaled=false (Time.deltaTime)
            m_Timer += Time.deltaTime;
            if (m_Timer >= Lifetime)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
