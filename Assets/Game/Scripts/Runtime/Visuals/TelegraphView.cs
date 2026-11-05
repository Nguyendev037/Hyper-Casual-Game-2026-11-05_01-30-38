using UnityEngine;

namespace RHMG.Runtime.Visuals
{
    /// <summary>Visual presentation of an enemy attack telegraph with growing fill indicator.</summary>
    public sealed class TelegraphView : MonoBehaviour
    {
        [SerializeField] private Transform m_FillTransform;

        public Transform FillTransform
        {
            get => m_FillTransform;
            set => m_FillTransform = value;
        }

        private void Awake()
        {
            if (m_FillTransform == null)
            {
                var fill = transform.Find("Fill");
                if (fill != null) m_FillTransform = fill;
            }
        }

        public void SetFill(float t01)
        {
            t01 = Mathf.Clamp01(t01);
            if (m_FillTransform != null)
            {
                m_FillTransform.localScale = new Vector3(t01, 1f, t01);
            }
        }
    }
}
