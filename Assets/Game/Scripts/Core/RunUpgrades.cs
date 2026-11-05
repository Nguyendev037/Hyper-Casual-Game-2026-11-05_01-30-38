using System;
using System.Collections.Generic;

namespace RHMG.Core
{
    public sealed class RunUpgrades
    {
        private readonly IRng _rng;
        private readonly List<string> _taken = new List<string>();

        public RunUpgrades(IRng rng)
        {
            _rng = rng;
        }

        /// <summary>One entry per stack taken, in order. The heal fallback is never recorded.</summary>
        public IReadOnlyList<string> Taken
        {
            get { return _taken; }
        }

        public int StackCount(string id)
        {
            int count = 0;
            for (int i = 0; i < _taken.Count; i++)
            {
                if (_taken[i] == id)
                {
                    count++;
                }
            }

            return count;
        }

        public List<string> RollChoices(int count = 3)
        {
            List<string> valid = new List<string>();
            for (int i = 0; i < Catalog.UpgradePool.Count; i++)
            {
                UpgradeDefinition def = Catalog.UpgradePool[i];
                if (StackCount(def.Id) < def.MaxStacks)
                {
                    valid.Add(def.Id);
                }
            }

            if (valid.Count == 0)
            {
                return new List<string> { Catalog.HealFallbackId };
            }

            for (int i = valid.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                string swap = valid[i];
                valid[i] = valid[j];
                valid[j] = swap;
            }

            if (count < valid.Count)
            {
                valid.RemoveRange(Math.Max(count, 0), valid.Count - Math.Max(count, 0));
            }

            return valid;
        }

        public void Take(string id)
        {
            UpgradeDefinition def = Catalog.GetUpgrade(id);
            if (def == null)
            {
                throw new ArgumentException("Unknown upgrade: " + id, "id");
            }

            if (id == Catalog.HealFallbackId)
            {
                return;
            }

            if (StackCount(id) >= def.MaxStacks)
            {
                throw new ArgumentException("Upgrade already at max stacks: " + id, "id");
            }

            _taken.Add(id);
        }
    }
}
