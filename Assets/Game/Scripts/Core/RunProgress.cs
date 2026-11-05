using System;

namespace RHMG.Core
{
    public sealed class RunProgress
    {
        public RunProgress()
        {
            Level = 1;
        }

        public RunProgress(int level, int xp)
        {
            int required = XpCurve.Required(level);
            if (xp < 0 || xp >= required) throw new ArgumentOutOfRangeException("xp");
            Level = level;
            Xp = xp;
            TotalXp = Xp;
        }

        public int Level { get; private set; }
        public int Xp { get; private set; }
        public int PendingLevelUps { get; private set; }
        public long TotalXp { get; private set; }

        /// <summary>Returns the number of new level-ups. amount &lt;= 0 gives 0.</summary>
        public int AddXp(int amount, float xpGainMult)
        {
            if (amount <= 0)
            {
                return 0;
            }

            if (float.IsNaN(xpGainMult) || float.IsInfinity(xpGainMult))
                throw new ArgumentOutOfRangeException("xpGainMult");
            int gained = (int)Math.Min(int.MaxValue, Math.Round(amount * (double)Math.Max(0f, xpGainMult), MidpointRounding.AwayFromZero));
            if (gained < 1)
            {
                gained = 1;
            }

            TotalXp = TotalXp > long.MaxValue - gained ? long.MaxValue : TotalXp + gained;
            long remaining = (long)Xp + gained;
            int ups = 0;
            int need = XpCurve.Required(Level);
            while (Level < XpCurve.MaxLevel && remaining >= need)
            {
                remaining -= need;
                Level++;
                ups++;
                need = XpCurve.Required(Level);
            }

            // At the representation boundary retain a valid residual without overflowing the level.
            Xp = (int)Math.Min(remaining, need - 1L);

            PendingLevelUps += ups;
            return ups;
        }

        /// <summary>Card chest: one choice without spending XP.</summary>
        public void GrantFreeLevelUp()
        {
            PendingLevelUps++;
        }

        public bool ConsumeLevelUp()
        {
            if (PendingLevelUps <= 0)
            {
                return false;
            }

            PendingLevelUps--;
            return true;
        }
    }
}
