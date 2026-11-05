namespace RHMG.Runtime.Visuals
{
    public enum AnimKind { Biped, Quadruped, Floating, Crawler, Eye, Heavy }

    /// <summary>Per-family deformation bands (SPEC-P06 §2). All Y/X values are normalized image coordinates
    /// (0..1, left-bottom origin, as drawn facing right). Amplitudes are fractions of image height.</summary>
    public sealed class AnimProfile
    {
        public AnimKind Kind;
        public bool Crowd;          // true = may fall back to a flat sprite when far (SPEC-P06 §4)
        public int Cols = 8, Rows = 12;
        public float LegTop = 0.24f, HipY = 0.30f, ChestY = 0.48f, HeadY = 0.62f;
        public float HemFrom = 0.20f, HemTo = 0.46f;           // skirt/cloak band (bottom moves most)
        public float HairFromY = 0.36f, HairToY = 0.86f, HairMaxX = 0.48f; // hair hangs on the back side
        public float StepHz = 4.5f, LegLift = 0.035f, Stride = 0.04f;
        public float HairSway = 0.05f, HemSway = 0.035f, BreathAmp = 0.025f;
        public float FloatAmp = 0.10f, PulseAmp = 0.06f;
        public float TailMaxX = 0.22f, TailFromY = 0.35f, TailToY = 0.80f, TailWag = 0.04f;

        public AnimProfile Clone() { return (AnimProfile)MemberwiseClone(); }
    }

    public static class AnimProfiles
    {
        public static readonly AnimProfile Girl = new AnimProfile { Kind = AnimKind.Biped };

        public static readonly AnimProfile Boss = new AnimProfile
        {
            Kind = AnimKind.Heavy, LegTop = 0.18f, HipY = 0.30f, ChestY = 0.50f, HeadY = 0.70f,
            HemFrom = 0.10f, HemTo = 0.40f, HairFromY = 0.45f, HairToY = 0.92f,
            StepHz = 2f, LegLift = 0.02f, Stride = 0.025f, HairSway = 0.03f, HemSway = 0.04f, BreathAmp = 0.02f
        };

        public static readonly AnimProfile Dog = new AnimProfile
        {
            Kind = AnimKind.Quadruped, LegTop = 0.26f, HipY = 0.30f, ChestY = 0.45f, HeadY = 0.60f,
            StepHz = 5.5f, LegLift = 0.04f, Stride = 0.035f, HairSway = 0.03f, HemSway = 0f, BreathAmp = 0.02f
        };

        public static readonly AnimProfile Spirit = new AnimProfile
        {
            Kind = AnimKind.Floating, Cols = 6, Rows = 6, FloatAmp = 0.10f, PulseAmp = 0.06f, HairSway = 0.04f
        };

        public static readonly AnimProfile Beetle = new AnimProfile
        {
            Kind = AnimKind.Crawler, Crowd = true, Cols = 6, Rows = 6, LegTop = 0.30f, StepHz = 14f, Stride = 0.015f
        };

        public static readonly AnimProfile Eye = new AnimProfile
        {
            Kind = AnimKind.Eye, Crowd = true, Cols = 6, Rows = 8, PulseAmp = 0.05f
        };

        public static readonly AnimProfile Sentinel = new AnimProfile
        {
            Kind = AnimKind.Heavy, Crowd = true, Cols = 6, Rows = 10, LegTop = 0.22f, StepHz = 2f, LegLift = 0.03f,
            Stride = 0.03f, HemSway = 0f, HairSway = 0f, BreathAmp = 0.02f
        };

        /// <summary>Profile for a sprite asset name (CharacterSprites ids); null = no deformation.</summary>
        public static AnimProfile ForSpriteName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (name.EndsWith("_step")) name = name.Substring(0, name.Length - 5);
            if (name.StartsWith("girl_")) return Girl;
            if (name.StartsWith("dog_")) return Dog;
            if (name.StartsWith("spirit_")) return Spirit;
            if (name == "enemy_beetle") return Beetle;
            if (name == "enemy_eye") return Eye;
            if (name == "enemy_sentinel") return Sentinel;
            if (name == "enemy_warden" || name.StartsWith("boss_")) return name == "boss_anchor" ? null : Boss;
            return null;
        }
    }
}
