using System.Collections.Generic;

namespace RHMG.Core
{
    public sealed class StatModifier
    {
        public readonly StatKind Stat;
        public readonly float Percent;

        public StatModifier(StatKind stat, float percent)
        {
            Stat = stat;
            Percent = percent;
        }
    }

    public sealed class SpiritDefinition
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly Element Element;
        public readonly bool IsFusion;
        public readonly float AutoDamage;
        public readonly float AutoInterval;
        public readonly float AutoRadius;
        public readonly float AutoSlow;
        public readonly float AutoBurnDps;
        public readonly float CommandDamage;
        public readonly float CommandRadius;
        public readonly float CommandShield;
        public readonly float CommandSlow;
        public readonly string AutoText;
        public readonly string CommandText;
        /// <summary>Base cooldown of the auto-cast skill in seconds (SPEC-P02 §2).</summary>
        public readonly float CommandCooldown;
        /// <summary>HP restored per auto shot that hits (before WoodHealPower).</summary>
        public readonly float AutoHeal;
        /// <summary>HP restored by the auto-cast skill.</summary>
        public readonly float CommandHeal;

        public SpiritDefinition(
            string id,
            string displayName,
            Element element,
            bool isFusion,
            float autoDamage,
            float autoInterval,
            float autoRadius,
            float autoSlow,
            float autoBurnDps,
            float commandDamage,
            float commandRadius,
            float commandShield,
            float commandSlow,
            string autoText,
            string commandText,
            float commandCooldown = 12f,
            float autoHeal = 0f,
            float commandHeal = 0f)
        {
            Id = id;
            DisplayName = displayName;
            Element = element;
            IsFusion = isFusion;
            AutoDamage = autoDamage;
            AutoInterval = autoInterval;
            AutoRadius = autoRadius;
            AutoSlow = autoSlow;
            AutoBurnDps = autoBurnDps;
            CommandDamage = commandDamage;
            CommandRadius = commandRadius;
            CommandShield = commandShield;
            CommandSlow = commandSlow;
            AutoText = autoText;
            CommandText = commandText;
            CommandCooldown = commandCooldown;
            AutoHeal = autoHeal;
            CommandHeal = commandHeal;
        }
    }

    public sealed class FusionRecipe
    {
        public readonly string Id;
        public readonly string IngredientA;
        public readonly string IngredientB;
        public readonly string OutputId;

        public FusionRecipe(string id, string ingredientA, string ingredientB, string outputId)
        {
            Id = id;
            IngredientA = ingredientA;
            IngredientB = ingredientB;
            OutputId = outputId;
        }

        public bool Matches(string a, string b)
        {
            return (IngredientA == a && IngredientB == b) || (IngredientA == b && IngredientB == a);
        }
    }

    public sealed class OutfitDefinition
    {
        public readonly string Id;
        public readonly OutfitSlot Slot;
        public readonly string DisplayName;
        public readonly int Price;
        public readonly bool DefaultOwned;
        public readonly string AppearanceId;
        public readonly string PerkId;
        public readonly IReadOnlyList<StatModifier> Modifiers;
        public readonly string BenefitText;
        public readonly string TradeoffText;

        public OutfitDefinition(
            string id,
            OutfitSlot slot,
            string displayName,
            int price,
            bool defaultOwned,
            IReadOnlyList<StatModifier> modifiers,
            string benefitText,
            string tradeoffText)
        {
            Id = id;
            Slot = slot;
            DisplayName = displayName;
            Price = price;
            DefaultOwned = defaultOwned;
            AppearanceId = id;
            PerkId = "perk." + id.Substring("outfit.".Length);
            Modifiers = modifiers;
            BenefitText = benefitText;
            TradeoffText = tradeoffText;
        }
    }

    public sealed class UpgradeDefinition
    {
        public readonly string Id;
        public readonly StatKind? Stat;
        public readonly float Percent;
        public readonly float FlatHp;
        public readonly int MaxStacks;
        public readonly string Title;
        public readonly string Description;

        public UpgradeDefinition(
            string id,
            StatKind? stat,
            float percent,
            float flatHp,
            int maxStacks,
            string title,
            string description)
        {
            Id = id;
            Stat = stat;
            Percent = percent;
            FlatHp = flatHp;
            MaxStacks = maxStacks;
            Title = title;
            Description = description;
        }
    }

    public sealed class EnemyDefinition
    {
        public readonly EnemyKind Kind;
        public readonly string DisplayName;
        public readonly float MaxHp;
        public readonly float ContactDamage;
        public readonly float AttackDamage;
        public readonly float AttackInterval;
        public readonly float MoveSpeed;
        public readonly float Radius;
        public readonly float TelegraphTime;

        public EnemyDefinition(
            EnemyKind kind,
            string displayName,
            float maxHp,
            float contactDamage,
            float attackDamage,
            float attackInterval,
            float moveSpeed,
            float radius,
            float telegraphTime)
        {
            Kind = kind;
            DisplayName = displayName;
            MaxHp = maxHp;
            ContactDamage = contactDamage;
            AttackDamage = attackDamage;
            AttackInterval = attackInterval;
            MoveSpeed = moveSpeed;
            Radius = radius;
            TelegraphTime = telegraphTime;
        }
    }

    public sealed class WaveDefinition
    {
        public readonly IReadOnlyList<(EnemyKind kind, int count)> Spawns;
        public readonly float DelayAfterPrevious;

        public WaveDefinition(IReadOnlyList<(EnemyKind kind, int count)> spawns, float delayAfterPrevious)
        {
            Spawns = spawns;
            DelayAfterPrevious = delayAfterPrevious;
        }
    }

    public sealed class FloorDefinition
    {
        public readonly int Index;
        public readonly bool IsGuardianFloor;
        public readonly IReadOnlyList<WaveDefinition> Waves;
        public readonly string LearnHint;

        public FloorDefinition(int index, bool isGuardianFloor, IReadOnlyList<WaveDefinition> waves, string learnHint)
        {
            Index = index;
            IsGuardianFloor = isGuardianFloor;
            Waves = waves;
            LearnHint = learnHint;
        }
    }
}
