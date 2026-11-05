namespace RHMG.Core
{
    public enum Element
    {
        None = 0,
        Metal = 1,
        Wood = 2,
        Water = 3,
        Fire = 4,
        Earth = 5,
        Dark = 6,
        Light = 7,
        Space = 8,
        Time = 9,
        Divine = 10
    }

    public enum OutfitSlot
    {
        Head = 0,
        Body = 1,
        Staff = 2
    }

    public enum DayPhase
    {
        Day = 0,
        Night = 1
    }

    public enum StatKind
    {
        MaxHp,
        MoveSpeed,
        StaffDamage,
        AttackInterval,
        DogDamage,
        CommandCooldown,
        ShieldPower,
        BurnPower,
        WoodHealPower,
        Range,
        MagnetRadius,
        ProjectileCount,
        Pierce,
        Armor,
        Regen,
        XpGain,
        SpiritPower
    }

    public enum EnemyKind
    {
        SandBeetle,
        SandEye,
        Sentinel,
        CageWarden,
        SandQueen,
        BlueHairGuy
    }

    public enum DogTier
    {
        Companion = 0,
        SandPup = 1,
        SpiritHound = 2,
        SunMoonLion = 3
    }

    public enum RunPhase
    {
        None,
        Combat,
        Upgrade,
        Rescue,
        Rest,
        Defeat,
        LevelUp,
        Station
    }

    public enum LoadOutcome
    {
        Fresh,
        LoadedPrimary,
        RestoredFromBackup,
        ResetCorrupt
    }

    public enum PurchaseResult
    {
        Success,
        AlreadyOwned,
        InsufficientGold,
        UnknownItem
    }

    public enum FusionCheck
    {
        Ok,
        NoRecipe,
        MissingIngredient,
        IngredientLocked,
        SameSpirit
    }
}
