using System.Collections.Generic;

namespace RHMG.Core
{
    public enum StationTile { Fusion, SpiritUp, Heal, XpChest, CardChest }

    public static class StationService
    {
        public const int TileCount = 3;

        /// <summary>No floor known: the "Heal on every 10th floor" rule is not applied.</summary>
        public static List<StationTile> Roll(IRng rng, RunSpirits spirits)
        {
            return Roll(rng, spirits, 0);
        }

        /// <summary>3 distinct valid tiles. Fusion is mandatory when a pair exists; Heal is mandatory after floors divisible by 10.</summary>
        public static List<StationTile> Roll(IRng rng, RunSpirits spirits, int floorJustCleared)
        {
            List<StationTile> mandatory = new List<StationTile>();
            List<StationTile> optional = new List<StationTile>();

            string a;
            string b;
            bool fusion = spirits != null && spirits.FusablePair(out a, out b) != null;
            bool heal = floorJustCleared > 0 && floorJustCleared % 10 == 0;

            if (fusion)
            {
                mandatory.Add(StationTile.Fusion);
            }

            if (heal)
            {
                mandatory.Add(StationTile.Heal);
            }
            else
            {
                optional.Add(StationTile.Heal);
            }

            if (spirits != null && spirits.AnyLevelable())
            {
                optional.Add(StationTile.SpiritUp);
            }

            optional.Add(StationTile.XpChest);
            optional.Add(StationTile.CardChest);

            Shuffle(optional, rng);
            List<StationTile> result = new List<StationTile>(mandatory);
            for (int i = 0; i < optional.Count && result.Count < TileCount; i++)
            {
                result.Add(optional[i]);
            }

            Shuffle(result, rng);
            return result;
        }

        private static void Shuffle(List<StationTile> list, IRng rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                StationTile t = list[i];
                list[i] = list[j];
                list[j] = t;
            }
        }
    }
}
