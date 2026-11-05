using System;
using System.Collections.Generic;

namespace RHMG.Core
{
    public enum CardKind { Stat, SpiritNew, SpiritUp, Heal }

    public sealed class LevelUpCard
    {
        /// <summary>Stat: the upgrade id. SpiritNew/SpiritUp: "card.spirit.new.dew" / "card.spirit.up.dew". Heal: upgrade.heal20.</summary>
        public string Id;
        public CardKind Kind;
        /// <summary>Stat: upgrade id. Spirit cards: full spirit id ("spirit.dew"). Heal: null.</summary>
        public string TargetId;
    }

    public sealed class LevelUpService
    {
        public const float WeightStat = 1.0f, WeightSpiritNew = 1.5f, WeightSpiritUp = 1.2f;
        public const string SpiritNewPrefix = "card.spirit.new.";
        public const string SpiritUpPrefix = "card.spirit.up.";

        private readonly IRng _rng;

        public LevelUpService(IRng rng)
        {
            _rng = rng;
        }

        /// <summary>Total number of choices a run could ever take: stat stacks + (new + 4 level-ups) per slot.</summary>
        public static int PoolCapacity
        {
            get
            {
                int total = 0;
                for (int i = 0; i < Catalog.UpgradePool.Count; i++)
                {
                    total += Catalog.UpgradePool[i].MaxStacks;
                }

                return total + SurvivorTuning.SpiritSlots * SurvivorTuning.SpiritMaxLevel;
            }
        }

        public List<LevelUpCard> Roll(RunUpgrades upgrades, RunSpirits spirits)
        {
            List<LevelUpCard> candidates = new List<LevelUpCard>();
            List<float> weights = new List<float>();

            for (int i = 0; i < Catalog.UpgradePool.Count; i++)
            {
                UpgradeDefinition def = Catalog.UpgradePool[i];
                if (upgrades.StackCount(def.Id) < def.MaxStacks)
                {
                    candidates.Add(Card(def.Id, CardKind.Stat, def.Id));
                    weights.Add(WeightStat);
                }
            }

            if (spirits != null)
            {
                if (spirits.Slots.Count < SurvivorTuning.SpiritSlots)
                {
                    for (int i = 0; i < Catalog.BaseSpiritIds.Count; i++)
                    {
                        string id = Catalog.BaseSpiritIds[i];
                        if (spirits.IsUnlocked(id) && !spirits.Has(id))
                        {
                            candidates.Add(Card(SpiritNewPrefix + Short(id), CardKind.SpiritNew, id));
                            weights.Add(WeightSpiritNew);
                        }
                    }
                }

                for (int i = 0; i < spirits.Slots.Count; i++)
                {
                    RunSpiritEntry e = spirits.Slots[i];
                    if (e.level < SurvivorTuning.SpiritMaxLevel)
                    {
                        candidates.Add(Card(SpiritUpPrefix + Short(e.spiritId), CardKind.SpiritUp, e.spiritId));
                        weights.Add(WeightSpiritUp);
                    }
                }
            }

            List<LevelUpCard> result = new List<LevelUpCard>();
            while (result.Count < SurvivorTuning.CardsPerRoll && candidates.Count > 0)
            {
                float total = 0f;
                for (int i = 0; i < weights.Count; i++)
                {
                    total += weights[i];
                }

                float r = _rng.NextFloat() * total;
                int pick = candidates.Count - 1;
                float acc = 0f;
                for (int i = 0; i < weights.Count; i++)
                {
                    acc += weights[i];
                    if (r < acc)
                    {
                        pick = i;
                        break;
                    }
                }

                result.Add(candidates[pick]);
                candidates.RemoveAt(pick);
                weights.RemoveAt(pick);
            }

            if (result.Count < SurvivorTuning.CardsPerRoll)
            {
                result.Add(Card(Catalog.HealFallbackId, CardKind.Heal, null));
            }

            return result;
        }

        /// <summary>Throws ArgumentException when the card is invalid for the current state (nothing is changed).</summary>
        public void Apply(LevelUpCard card, RunUpgrades upgrades, RunSpirits spirits)
        {
            if (card == null)
            {
                throw new ArgumentNullException("card");
            }

            switch (card.Kind)
            {
                case CardKind.Stat:
                    upgrades.Take(card.TargetId);
                    break;
                case CardKind.SpiritNew:
                    if (spirits == null || !spirits.Add(card.TargetId))
                    {
                        throw new ArgumentException("Cannot add spirit: " + card.TargetId, "card");
                    }

                    break;
                case CardKind.SpiritUp:
                    if (spirits == null || !spirits.LevelUp(card.TargetId))
                    {
                        throw new ArgumentException("Cannot level spirit: " + card.TargetId, "card");
                    }

                    break;
                case CardKind.Heal:
                    break;
                default:
                    throw new ArgumentException("Unknown card kind", "card");
            }
        }

        private static LevelUpCard Card(string id, CardKind kind, string target)
        {
            LevelUpCard c = new LevelUpCard();
            c.Id = id;
            c.Kind = kind;
            c.TargetId = target;
            return c;
        }

        private static string Short(string spiritId)
        {
            return spiritId.StartsWith("spirit.", StringComparison.Ordinal) ? spiritId.Substring("spirit.".Length) : spiritId;
        }
    }
}
