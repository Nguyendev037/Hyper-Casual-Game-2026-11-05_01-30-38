namespace RHMG.Core
{
    public static class DogProgress
    {
        public const int AllSevenMask = 0x7F;

        public static bool IsCommonElement(Element e)
        {
            return e >= Element.Metal && e <= Element.Light;
        }

        public static int AddSeal(int mask, Element e)
        {
            if (!IsCommonElement(e))
            {
                return mask;
            }

            return mask | (1 << ((int)e - 1));
        }

        public static int SealCount(int mask)
        {
            int count = 0;
            int bits = mask & AllSevenMask;
            while (bits != 0)
            {
                count += bits & 1;
                bits >>= 1;
            }

            return count;
        }

        public static DogTier GetTier(int mask)
        {
            if ((mask & AllSevenMask) == AllSevenMask)
            {
                return DogTier.SunMoonLion;
            }

            int count = SealCount(mask);
            if (count == 0)
            {
                return DogTier.Companion;
            }

            return count <= 2 ? DogTier.SandPup : DogTier.SpiritHound;
        }

        public static float DogDamageMultiplier(DogTier t)
        {
            switch (t)
            {
                case DogTier.SandPup:
                    return 1.10f;
                case DogTier.SpiritHound:
                    return 1.25f;
                case DogTier.SunMoonLion:
                    return 1.40f;
                default:
                    return 1.0f;
            }
        }
    }
}
