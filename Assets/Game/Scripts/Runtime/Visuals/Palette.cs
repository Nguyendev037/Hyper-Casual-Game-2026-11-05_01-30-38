using UnityEngine;
using RHMG.Core;

namespace RHMG.Runtime.Visuals
{
    /// <summary>Global color palette for Red Hair Magic Girl.</summary>
    public static class Palette
    {
        public static readonly Color HairRed      = new Color(0.851f, 0.294f, 0.220f, 1f); // #D94B38
        public static readonly Color Sand         = new Color(0.902f, 0.784f, 0.576f, 1f); // #E6C893
        public static readonly Color SandShade    = new Color(0.863f, 0.741f, 0.525f, 1f); // #DCBD86
        public static readonly Color SandMortar   = new Color(0.788f, 0.651f, 0.431f, 1f); // #C9A66E
        public static readonly Color HairShadow   = new Color(0.557f, 0.180f, 0.133f, 1f); // #8E2E22
        public static readonly Color Night        = new Color(0.149f, 0.212f, 0.322f, 1f); // #263652
        public static readonly Color LightGold    = new Color(0.965f, 0.843f, 0.478f, 1f); // #F6D77A
        public static readonly Color WaterBlue    = new Color(0.333f, 0.718f, 0.808f, 1f); // #55B7CE
        public static readonly Color Skin         = new Color(0.957f, 0.827f, 0.710f, 1f); // #F4D3B5
        public static readonly Color Ink          = new Color(0.169f, 0.133f, 0.200f, 1f); // #2B2233
        public static readonly Color FireOrange   = new Color(0.941f, 0.541f, 0.235f, 1f); // #F08A3C
        public static readonly Color LeafGreen    = new Color(0.420f, 0.686f, 0.353f, 1f); // #6BAF5A
        public static readonly Color AshGrey      = new Color(0.431f, 0.392f, 0.439f, 1f); // #6E6470
        public static readonly Color Rose         = new Color(0.898f, 0.604f, 0.627f, 1f); // #E59AA0
        public static readonly Color CloakTeal    = new Color(0.247f, 0.498f, 0.525f, 1f); // #3F7F86
        public static readonly Color Wood         = new Color(0.541f, 0.353f, 0.231f, 1f); // #8A5A3B
        public static readonly Color Silver       = new Color(0.788f, 0.827f, 0.878f, 1f); // #C9D3E0
        public static readonly Color EnemyPurple  = new Color(0.478f, 0.243f, 0.557f, 1f); // #7A3E8E
        public static readonly Color WarnRed      = new Color(1.000f, 0.231f, 0.188f, 1f); // #FF3B30

        /// <summary>Returns the signature theme color for a spirit element.</summary>
        public static Color ForElement(Element e)
        {
            switch (e)
            {
                case Element.Metal:  return Silver;
                case Element.Wood:   return LeafGreen;
                case Element.Water:  return WaterBlue;
                case Element.Fire:   return FireOrange;
                case Element.Earth:  return Sand;
                case Element.Dark:   return EnemyPurple;
                case Element.Light:  return LightGold;
                case Element.Space:  return Ink;
                case Element.Time:   return Silver;
                case Element.Divine: return LightGold;
                default:             return Sand;
            }
        }
    }
}
