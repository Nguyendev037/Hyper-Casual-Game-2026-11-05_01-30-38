using System;

namespace RHMG.Core
{
    public sealed class DayNightClock
    {
        public const float PhaseLength = 60f;
        public const float BlendTime = 2f;
        public const float MaxTickDelta = 0.25f;

        private float _elapsed;

        public bool Paused { get; set; }

        public event Action<DayPhase> PhaseChanged;

        public float Elapsed
        {
            get { return _elapsed; }
        }

        public DayPhase Phase
        {
            get { return PhaseAt(_elapsed); }
        }

        public float PhaseRemaining
        {
            get { return PhaseLength - (_elapsed % PhaseLength); }
        }

        /// <summary>0 = day, 1 = night; linear blend during the 2 s after each boundary.</summary>
        public float NightBlend
        {
            get
            {
                if (_elapsed < PhaseLength)
                {
                    return 0f;
                }

                float k = Math.Min((_elapsed % PhaseLength) / BlendTime, 1f);
                return Phase == DayPhase.Night ? k : 1f - k;
            }
        }

        public void Tick(float dt)
        {
            if (Paused || dt <= 0f)
            {
                return;
            }

            DayPhase before = Phase;
            _elapsed += Math.Min(dt, MaxTickDelta);
            RaiseIfChanged(before);
        }

        public void Reset()
        {
            _elapsed = 0f;
        }

        public void ForceToggle()
        {
            DayPhase before = Phase;
            _elapsed = ((int)(_elapsed / PhaseLength) + 1) * PhaseLength;
            RaiseIfChanged(before);
        }

        public static float ElementMultiplier(Element e, DayPhase p)
        {
            if (p == DayPhase.Day)
            {
                return e == Element.Fire || e == Element.Light ? 1.1f : 1.0f;
            }

            return e == Element.Water || e == Element.Dark ? 1.1f : 1.0f;
        }

        private static DayPhase PhaseAt(float elapsed)
        {
            return (int)(elapsed / PhaseLength) % 2 == 0 ? DayPhase.Day : DayPhase.Night;
        }

        private void RaiseIfChanged(DayPhase before)
        {
            DayPhase after = Phase;
            if (after != before && PhaseChanged != null)
            {
                PhaseChanged(after);
            }
        }
    }
}
