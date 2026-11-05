using System.Collections.Generic;
using UnityEngine;
using RHMG.Runtime.Visuals;

namespace RHMG.Runtime.Combat
{
    public sealed class TrainingDummy : MonoBehaviour
    {
        private readonly Queue<DamageRecord> recentHits = new Queue<DamageRecord>();
        private Health health;
        private TextMesh dpsLabel;
        private float elapsed;

        public float Dps { get; private set; }
        public float CurrentHp => health == null ? 0f : health.Current;

        public void Init()
        {
            health = new Health(99999f);
            GameObject label = new GameObject("DummyDps");
            label.transform.SetParent(transform, false);
            label.transform.localPosition = Vector3.up * 2f;
            dpsLabel = label.AddComponent<TextMesh>();
            dpsLabel.anchor = TextAnchor.MiddleCenter;
            dpsLabel.alignment = TextAlignment.Center;
            dpsLabel.fontSize = 32;
            dpsLabel.characterSize = 0.08f;
            dpsLabel.color = Palette.LightGold;
            dpsLabel.text = "DPS 0";
        }

        public void TakeDamage(float damage, Vector3 from)
        {
            if (damage <= 0f)
            {
                return;
            }

            Record(damage);
        }

        public void Record(float damage)
        {
            if (damage <= 0f)
            {
                return;
            }

            health.Damage(damage, transform.position);
            recentHits.Enqueue(new DamageRecord(elapsed, damage));
            CombatFeedback.ShowDamage(transform.position + Vector3.up, damage);
        }

        public void Step(float deltaTime)
        {
            elapsed += deltaTime;
            while (recentHits.Count > 0 && elapsed - recentHits.Peek().Time > 3f)
            {
                recentHits.Dequeue();
            }

            float total = 0f;
            foreach (DamageRecord hit in recentHits)
            {
                total += hit.Damage;
            }

            Dps = total / 3f;
            dpsLabel.text = "DPS " + Mathf.RoundToInt(Dps);
        }

        private void Update()
        {
            Step(Time.deltaTime);
        }

        private readonly struct DamageRecord
        {
            public readonly float Time;
            public readonly float Damage;

            public DamageRecord(float time, float damage)
            {
                Time = time;
                Damage = damage;
            }
        }
    }
}
