using UnityEngine;

namespace RHMG.Runtime.Combat
{
    public sealed class FloatingDamageNumber : MonoBehaviour
    {
        private TextMesh text;
        private float remaining;
        private Vector3 start;

        public void Initialize(TextMesh mesh)
        {
            text = mesh;
        }

        public void SetText(string value)
        {
            text.text = value;
            start = transform.position;
            Color color = text.color;
            color.a = 1f;
            text.color = color;
        }

        public void Begin(float lifetime)
        {
            remaining = lifetime;
            start = transform.position;
        }

        public bool Tick(float deltaTime, Camera camera)
        {
            remaining -= deltaTime;
            transform.position = start + Vector3.up * (2f - remaining) * 0.35f;
            if (camera != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - camera.transform.position);
            }

            Color color = text.color;
            color.a = Mathf.Clamp01(remaining / 0.4f);
            text.color = color;
            return remaining <= 0f;
        }
    }
}
