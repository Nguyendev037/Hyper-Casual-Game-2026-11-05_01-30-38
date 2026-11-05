using System;
using UnityEngine;

namespace RHMG.Runtime.Combat
{
    public sealed class Health
    {
        public float Max { get; private set; }
        public float Current { get; private set; }
        public float Shield { get; private set; }
        public bool Dead => Current <= 0f;

        public event Action<float> Changed;
        public event Action Died;
        public event Action<float, Vector3> Damaged;

        public Health(float maximum)
        {
            Max = Mathf.Max(1f, maximum);
            Current = Max;
        }

        public void SetMax(float maximum, bool healIncrease)
        {
            float oldMax = Max;
            Max = Mathf.Max(1f, maximum);
            if (healIncrease && Max > oldMax)
            {
                Current = Mathf.Min(Max, Current + Max - oldMax);
            }

            Current = Mathf.Min(Current, Max);
            Changed?.Invoke(Current / Max);
        }

        public void Damage(float amount, Vector3 from)
        {
            if (Dead || amount <= 0f)
            {
                return;
            }

            float absorbed = Mathf.Min(Shield, amount);
            Shield -= absorbed;
            float dealt = amount - absorbed;
            Current = Mathf.Max(0f, Current - dealt);
            Damaged?.Invoke(amount, from);
            Changed?.Invoke(Current / Max);
            if (Dead)
            {
                Died?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (Dead || amount <= 0f)
            {
                return;
            }

            Current = Mathf.Min(Max, Current + amount);
            Changed?.Invoke(Current / Max);
        }

        public void Restore(float current)
        {
            Current = Mathf.Clamp(current, 0f, Max);
            Changed?.Invoke(Current / Max);
        }

        public void AddShield(float amount)
        {
            Shield = Mathf.Max(0f, Shield + amount);
        }
    }
}
