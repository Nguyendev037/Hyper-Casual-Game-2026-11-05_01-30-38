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
        BlueHairGuy,
        // Plan 05 chapter enemies (appended; values 0..5 are frozen). 01-CONTRACTS section 2.
        EmberImp,
        SparkTurret,
        AnvilGolem,
        BookBat,
        MirrorGhost,
        InkSlime,
        PlagueKnight,
        BoneArcher,
        BombMite,
        ShadowWolf,
        StarWisp,
        VoidEye,
        // Chapter guardians (01-CONTRACTS section 3). Keep these four LAST: EnemyKinds.IsBoss relies on the range.
        ForgeGolem,
        MirrorWitch,
        CorruptKnight,
        TwilightDrake
    }

    /// <summary>Behaviour family of an enemy (SPEC-P02 section 1). The runtime brain is chosen from this.</summary>
    public enum EnemyArchetype
    {
        Chaser,
        Shooter,
        Charger,
        Tank,
        Flyer,
        Teleporter,
        Splitter,
        Exploder,
        Orbiter,
        Beam,
        Boss
    }

    /// <summary>Slot of a chapter roster that a regular enemy fills (01-CONTRACTS section 1).</summary>
    public enum SpawnRole
    {
        Chaser,
        Shooter,
        Special
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
