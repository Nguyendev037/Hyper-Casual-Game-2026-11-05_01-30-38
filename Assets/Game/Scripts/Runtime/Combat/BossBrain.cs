using System.Collections.Generic;
using UnityEngine;
using RHMG.Core;
using RHMG.Runtime.Visuals;

namespace RHMG.Runtime.Combat
{
    /// <summary>AI of the three boss families: Cage Warden variants, Sand Queen (floor 50) and Blue Hair Guy (floor 100).
    /// One attack at a time, every attack telegraphed for at least Catalog.BossMinTelegraph (SPEC-P03 §2, §2b, §4).
    /// Damage numbers come from Core (Catalog.QueenAttacks / BlueHairAttacks, DmgMult(floor), DamageScale).</summary>
    internal sealed class BossBrain
    {
        private enum ActKind
        {
            None,
            Fan,
            Dash,
            Summon,
            Spiral,
            Slam,
            Pillars,
            Ring
        }

        private sealed class Slot
        {
            public ActKind Kind;
            public string Name;
            public float Interval;
            public float Timer;
            public float Telegraph;
            public float Damage;
            public int Count;
            public int FirstPhase;
            public int LastPhase;
        }

        private struct Warning
        {
            public TelegraphView View;
            public WarnShape Shape;
        }

        private sealed class Pillar
        {
            public Vector3 Position;
            public float Left;
            public float Total;
            public TelegraphView View;
        }

        private const float PillarRadius = 1.3f;
        private const float ActionGap = 0.8f;
        private const float DashSpeedQueen = 22f;
        private const float DashSpeedBlue = 28f;
        private const float DashLengthQueen = 14f;
        private const float DashLengthBlue = 24f;
        private const float DashWidth = 1.8f;
        private const float FanArc = 80f;

        private readonly EnemyController boss;
        private readonly CombatWorld world;
        private readonly EnemyKind kind;
        private readonly int floor;
        private readonly float damageMult;
        private readonly float baseScale;
        private readonly bool scripted;
        private readonly List<Slot> slots = new List<Slot>(6);
        private readonly List<Warning> warnings = new List<Warning>(4);
        private readonly List<Pillar> pillars = new List<Pillar>(3);
        private readonly List<EnemyController> anchors = new List<EnemyController>(3);

        private int phase = 1;
        private float invulnerable;
        private float fightTime;
        private bool enrageWarned;
        private bool enraged;
        private float gap = 1.2f;

        private Slot current;
        private float telegraph;
        private float telegraphTotal;
        private float channel;
        private float channelTotal;
        private Vector3 aimDir;
        private Vector3 aimPos;
        private float emitted;
        private float spiralAngle;
        private bool dashHit;
        private float pillarSpawnTimer;
        private int pillarsToSpawn;

        private int anchorsAlive;
        private float anchorWarnLeft = -1f;
        private float anchorRespawn = -1f;
        private float purify;
        private bool kneeling;
        private float kneelLeft;
        private float dayNightTimer;
        private bool dayNightWarned;
        private readonly List<TelegraphView> anchorWarnings = new List<TelegraphView>(3);

        public int Phase => phase;
        public bool Untargetable => invulnerable > 0f || kneeling ||
            (kind == EnemyKind.BlueHairGuy && phase == 3 && purify <= 0f);
        public bool Purifying => purify > 0f;

        // Inside the safe circle at radius 11.5 (120 degrees apart): the shrinking storm (R14 to R8) is the timer for
        // killing them. At the arena corners they would lie outside the safe zone and cost 8% HP/s just to reach.
        private static readonly Vector3[] AnchorSpots =
        {
            new Vector3(0f, 0f, 11.5f), new Vector3(-9.96f, 0f, -5.75f), new Vector3(9.96f, 0f, -5.75f)
        };

        public BossBrain(EnemyController owner, CombatWorld ownerWorld, EnemyKind bossKind, int floorIndex,
            float floorDamageMult)
        {
            boss = owner;
            world = ownerWorld;
            kind = bossKind;
            floor = floorIndex;
            damageMult = floorDamageMult;
            baseScale = BalanceProbe.DamageScale(bossKind);
            scripted = bossKind != EnemyKind.CageWarden;
            BuildSlots();
        }

        // ---------- setup ----------

        private void BuildSlots()
        {
            if (kind == EnemyKind.CageWarden)
            {
                float fanTele = Mathf.Max(Catalog.WardenFanTelegraph, Catalog.BossMinTelegraph);
                float slamDamage = Catalog.GetEnemy(EnemyKind.CageWarden).AttackDamage;
                AddSlot(ActKind.Fan, "fan", 5f, 1.6f, fanTele, Catalog.WardenFanDamage, Catalog.WardenFanProjectiles, 1, 1);
                AddSlot(ActKind.Slam, "slam", 5f, 4.1f, fanTele, slamDamage, 1, 1, 1);
                if (floor >= 30)
                {
                    AddSlot(ActKind.Spiral, "spiral", 10f, 6f, 0.7f, Catalog.WardenFanDamage, 24, 1, 1);
                }

                if (floor >= 70)
                {
                    AddSlot(ActKind.Summon, "summon", 14f, 9f, 0.8f, 0f, 2, 1, 1);
                }

                return;
            }

            IReadOnlyList<BossAttackDef> defs = Catalog.GetBossAttacks(kind);
            float stagger = 1.5f;
            for (int i = 0; i < defs.Count; i++)
            {
                BossAttackDef d = defs[i];
                ActKind act = ActFor(d.Name);
                if (act == ActKind.None)
                {
                    continue;
                }

                // Phase 1 attacks stay available in phase 3 (x1.3 rate); phase 2 attacks start in phase 2.
                int first = d.Phase;
                AddSlot(act, d.Name, d.Interval, stagger, Mathf.Max(d.Telegraph, Catalog.BossMinTelegraph),
                    d.Damage, d.Count, first, 3);
                stagger += 0.9f;
            }
        }

        private static ActKind ActFor(string name)
        {
            switch (name)
            {
                case "fan": return ActKind.Fan;
                case "dash": return ActKind.Dash;
                case "summon": return ActKind.Summon;
                case "spiral": return ActKind.Spiral;
                case "slam": return ActKind.Slam;
                case "pillars": return ActKind.Pillars;
                case "ring": return ActKind.Ring;
                default: return ActKind.None;
            }
        }

        private void AddSlot(ActKind act, string name, float interval, float first, float telegraphTime, float damage,
            int count, int firstPhase, int lastPhase)
        {
            Slot s = new Slot();
            s.Kind = act;
            s.Name = name;
            s.Interval = interval;
            s.Timer = first;
            s.Telegraph = telegraphTime;
            s.Damage = damage;
            s.Count = count;
            s.FirstPhase = firstPhase;
            s.LastPhase = lastPhase;
            slots.Add(s);
        }

        // Queen/Blue: phase 1 attacks are phase-1 only until phase 3; phase 2 attacks run in 2 and 3.
        private bool SlotActive(Slot s)
        {
            if (!scripted)
            {
                return true;
            }

            if (phase == 3)
            {
                return true;
            }

            if (phase == 1)
            {
                return s.FirstPhase == 1;
            }

            return s.FirstPhase == 2;
        }

        private float RateMult
        {
            get
            {
                if (kind == EnemyKind.CageWarden)
                {
                    return boss.HpFraction < Catalog.WardenEnrageHpFraction ? Catalog.WardenEnrageRate : 1f;
                }

                return phase == 3 ? SurvivorTuning.PhaseRateMult : 1f;
            }
        }

        private float Damage(float baseDamage)
        {
            return baseDamage * damageMult * baseScale * (enraged ? Catalog.BossEnrageDamageMult : 1f);
        }

        // ---------- damage gate (called by EnemyController) ----------

        public float FilterDamage(float amount, Health health)
        {
            if (Untargetable)
            {
                return 0f;
            }

            if (kind == EnemyKind.BlueHairGuy && phase == 3 && purify > 0f)
            {
                amount *= SurvivorTuning.PurifyDamageMult;
            }

            if (scripted && phase < 3)
            {
                float threshold = health.Max * (phase == 1 ? 0.66f : 0.33f);
                float allowed = health.Current - threshold;
                if (allowed <= 0f)
                {
                    return 0f;
                }

                amount = Mathf.Min(amount, allowed);
            }
            else if (kind == EnemyKind.BlueHairGuy && amount >= health.Current - 0.5f)
            {
                // The final blow never kills: he kneels instead (SPEC-P03 §2b).
                amount = Mathf.Max(0f, health.Current - 1f);
            }

            return amount;
        }

        public void AfterDamage(Health health)
        {
            if (!scripted || kneeling)
            {
                return;
            }

            if (phase < 3)
            {
                float threshold = health.Max * (phase == 1 ? 0.66f : 0.33f);
                if (health.Current <= threshold + 0.05f)
                {
                    EnterPhase(phase + 1);
                }

                return;
            }

            if (kind == EnemyKind.BlueHairGuy && health.Current <= 1.05f)
            {
                StartKneel();
            }
        }

        private void EnterPhase(int next)
        {
            phase = next;
            CombatFeedback.Play("boss_phase");
            invulnerable = SurvivorTuning.BossPhaseInvulnerable;
            CancelAction();
            world.ClearEnemyProjectiles();
            CombatFeedback.Shake(0.5f);
            world.Session.Notice(next == 2 ? "Pha 2!" : "Pha cuối!");
            gap = 1.0f;
            if (next == 3)
            {
                if (kind == EnemyKind.SandQueen)
                {
                    world.Storm.Begin();
                    world.Session.Notice("Bão cát đang tới — vào vùng an toàn!");
                }
                else
                {
                    BeginAnchorWarnings();
                    world.Session.Notice("Phá ba neo hắc hoá!");
                }
            }
            else if (kind == EnemyKind.BlueHairGuy && next == 2)
            {
                dayNightTimer = 0f;
                dayNightWarned = false;
            }
        }

        private void StartKneel()
        {
            kneeling = true;
            kneelLeft = SurvivorTuning.KneelSeconds;
            CancelAction();
            world.ClearEnemyProjectiles();
            world.Storm.End();
            ClearAnchorWarnings();
            boss.KneelVisual();
            world.Session.Notice("Lõi tháp vỡ — anh ấy gục xuống");
        }

        // ---------- per-frame ----------

        public void Step(float deltaTime, Vector3 toPlayer, float moveSpeed, float slowFactor)
        {
            fightTime += deltaTime;
            if (!enrageWarned && fightTime >= Catalog.BossEnrageWarnSeconds)
            {
                enrageWarned = true;
                world.Session.Notice("Trùm sắp cuồng nộ!");
            }

            if (!enraged && fightTime >= Catalog.BossEnrageSeconds)
            {
                enraged = true;
                world.Session.Notice("Trùm cuồng nộ! Sát thương x2");
            }

            invulnerable = Mathf.Max(0f, invulnerable - deltaTime);
            StepPillars(deltaTime);
            if (kneeling)
            {
                kneelLeft -= deltaTime;
                if (kneelLeft <= 0f)
                {
                    kneeling = false;
                    boss.ForceKill();
                }

                return;
            }

            StepAnchors(deltaTime);
            if (invulnerable > 0f || purify > 0f)
            {
                return;
            }

            StepDayNight(deltaTime);
            if (current != null)
            {
                StepAction(deltaTime);
                return;
            }

            Approach(deltaTime, toPlayer, moveSpeed, slowFactor);
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].Timer -= deltaTime;
            }

            gap -= deltaTime;
            if (gap > 0f)
            {
                return;
            }

            Slot pick = null;
            for (int i = 0; i < slots.Count; i++)
            {
                Slot s = slots[i];
                if (s.Timer <= 0f && SlotActive(s) && (pick == null || s.Timer < pick.Timer))
                {
                    pick = s;
                }
            }

            if (pick != null)
            {
                Begin(pick);
            }
        }

        private void Approach(float deltaTime, Vector3 toPlayer, float moveSpeed, float slowFactor)
        {
            float speed = kind == EnemyKind.CageWarden ? Mathf.Min(moveSpeed, 1f) : moveSpeed;
            speed = Mathf.Min(speed, Catalog.BaseMoveSpeed * 0.9f) * slowFactor;
            Vector3 to = world.Player.transform.position - boss.transform.position;
            if (CombatMath.Flat(to).magnitude > CombatTuning.BossStopDistance)
            {
                boss.transform.position = CombatMath.ClampArena(boss.transform.position + toPlayer * speed * deltaTime);
            }
        }

        private void Begin(Slot slot)
        {
            current = slot;
            slot.Timer = slot.Interval / RateMult;
            Vector3 pos = boss.transform.position;
            Vector3 playerPos = world.Player.transform.position;
            aimDir = CombatMath.Direction(pos, playerPos);
            if (aimDir.sqrMagnitude < 0.0001f)
            {
                aimDir = Vector3.forward;
            }

            aimPos = playerPos;
            telegraph = slot.Telegraph;
            telegraphTotal = telegraph;
            boss.PlayWindup(telegraph);
            channel = 0f;
            dashHit = false;
            ClearWarnings();
            switch (slot.Kind)
            {
                case ActKind.Fan:
                    if (kind == EnemyKind.CageWarden)
                    {
                        AddWarn(WarnShape.Fan60, pos, aimDir, Vector3.one);
                    }
                    else
                    {
                        AddWarn(WarnShape.BossFan, pos, aimDir, Vector3.one * 1.6f);
                    }

                    break;
                case ActKind.Dash:
                    {
                        float length = kind == EnemyKind.BlueHairGuy ? DashLengthBlue : DashLengthQueen;
                        AddWarn(WarnShape.Line, pos, aimDir,
                            new Vector3(DashWidth / CombatTuning.SentinelTelegraphWidth, 1f,
                                length / CombatTuning.SentinelChargeDistance));
                        break;
                    }
                case ActKind.Summon:
                    AddWarn(WarnShape.Disc, pos, Vector3.forward, Vector3.one * (kind == EnemyKind.CageWarden ? 0.85f : 1f));
                    break;
                case ActKind.Spiral:
                    AddWarn(WarnShape.Disc, pos, Vector3.forward, Vector3.one * 0.7f);
                    break;
                case ActKind.Slam:
                    if (kind == EnemyKind.CageWarden)
                    {
                        AddWarn(WarnShape.Disc, aimPos, Vector3.forward, Vector3.one);
                    }
                    else
                    {
                        AddWarn(WarnShape.Disc, pos, Vector3.forward, Vector3.one * (4f / Catalog.WardenRingRadius));
                    }

                    break;
                case ActKind.Ring:
                    AddWarn(WarnShape.Disc, pos, Vector3.forward, Vector3.one * (3.5f / Catalog.WardenRingRadius));
                    break;
                case ActKind.Pillars:
                    // the pillar circles themselves are the warnings (spawned over the first 0.8 s)
                    pillarsToSpawn = slot.Count;
                    pillarSpawnTimer = 0f;
                    telegraph = 0f;
                    channelTotal = 0.4f * (slot.Count - 1) + slot.Telegraph;
                    channel = channelTotal;
                    break;
            }
        }

        private void StepAction(float deltaTime)
        {
            if (telegraph > 0f)
            {
                telegraph -= deltaTime;
                float fill = 1f - Mathf.Max(0f, telegraph) / telegraphTotal;
                for (int i = 0; i < warnings.Count; i++)
                {
                    warnings[i].View.SetFill(fill);
                }

                if (telegraph <= 0f)
                {
                    Resolve();
                }

                return;
            }

            if (channel > 0f)
            {
                channel -= deltaTime;
                StepChannel(deltaTime);
                if (channel <= 0f)
                {
                    EndAction();
                }
            }
            else
            {
                EndAction();
            }
        }

        private void Resolve()
        {
            Vector3 pos = boss.transform.position;
            Vector3 playerPos = world.Player.transform.position;
            ClearWarnings();
            switch (current.Kind)
            {
                case ActKind.Fan:
                    {
                        float arc = kind == EnemyKind.CageWarden ? CombatTuning.WardenFanAngle : FanArc;
                        int count = current.Count;
                        for (int i = 0; i < count; i++)
                        {
                            float angle = count == 1 ? 0f : Mathf.Lerp(-arc * 0.5f, arc * 0.5f, i / (float)(count - 1));
                            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * aimDir;
                            Shoot(pos, dir, current.Damage, CombatTuning.WardenProjectileSpeed);
                        }

                        EndAction();
                        break;
                    }
                case ActKind.Dash:
                    channelTotal = (kind == EnemyKind.BlueHairGuy ? DashLengthBlue : DashLengthQueen) /
                        (kind == EnemyKind.BlueHairGuy ? DashSpeedBlue : DashSpeedQueen);
                    channel = channelTotal;
                    break;
                case ActKind.Summon:
                    SummonMinions(pos);
                    EndAction();
                    break;
                case ActKind.Spiral:
                    channelTotal = 2.5f;
                    channel = channelTotal;
                    emitted = 0f;
                    spiralAngle = Random.value * 360f;
                    break;
                case ActKind.Slam:
                    {
                        Vector3 center = kind == EnemyKind.CageWarden ? aimPos : pos;
                        float radius = kind == EnemyKind.CageWarden ? Catalog.WardenRingRadius : 4f;
                        if (CombatMath.Overlap(playerPos, 0f, center, radius))
                        {
                            world.Player.TakeDamage(Damage(current.Damage), center);
                        }

                        world.PlayBurst(Palette.WarnRed, radius, center);
                        EndAction();
                        break;
                    }
                case ActKind.Ring:
                    if (CombatMath.Overlap(playerPos, 0f, pos, 3.5f))
                    {
                        world.Player.TakeDamage(Damage(current.Damage), pos);
                    }

                    world.PlayBurst(Palette.WaterBlue, 3.5f, pos);
                    EndAction();
                    break;
                default:
                    EndAction();
                    break;
            }
        }

        private void StepChannel(float deltaTime)
        {
            switch (current.Kind)
            {
                case ActKind.Dash:
                    {
                        float speed = kind == EnemyKind.BlueHairGuy ? DashSpeedBlue : DashSpeedQueen;
                        boss.transform.position = CombatMath.ClampArena(boss.transform.position + aimDir * speed * deltaTime);
                        if (!dashHit && CombatMath.Overlap(boss.transform.position, 0.7f, world.Player.transform.position,
                            CombatTuning.PlayerRadius))
                        {
                            dashHit = true;
                            world.Player.TakeDamage(Damage(current.Damage), boss.transform.position);
                        }

                        break;
                    }
                case ActKind.Spiral:
                    {
                        float elapsedChannel = channelTotal - Mathf.Max(0f, channel);
                        int should = Mathf.Min(current.Count, Mathf.FloorToInt(elapsedChannel / channelTotal * current.Count) + 1);
                        while (emitted < should)
                        {
                            float angle = spiralAngle + emitted * (720f / current.Count);
                            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;
                            Shoot(boss.transform.position, dir, current.Damage, 5f);
                            emitted += 1f;
                        }

                        break;
                    }
                case ActKind.Pillars:
                    pillarSpawnTimer -= deltaTime;
                    if (pillarsToSpawn > 0 && pillarSpawnTimer <= 0f)
                    {
                        SpawnPillar(world.Player.transform.position, current.Telegraph);
                        pillarsToSpawn--;
                        pillarSpawnTimer = 0.4f;
                    }

                    break;
            }
        }

        private void EndAction()
        {
            ClearWarnings();
            current = null;
            gap = kind == EnemyKind.CageWarden ? CombatTuning.WardenRecovery / RateMult : ActionGap / RateMult;
            telegraph = 0f;
            channel = 0f;
        }

        private void CancelAction()
        {
            ClearWarnings();
            current = null;
            telegraph = 0f;
            channel = 0f;
            pillarsToSpawn = 0;
            for (int i = 0; i < pillars.Count; i++)
            {
                if (pillars[i].View != null)
                {
                    world.ReturnWarningShape(pillars[i].View, WarnShape.Disc);
                }
            }

            pillars.Clear();
        }

        // ---------- helpers ----------

        private void Shoot(Vector3 origin, Vector3 dir, float baseDamage, float speed)
        {
            world.Fire(ProjectileOwner.Enemy, origin + Vector3.up * 0.5f, dir, Damage(baseDamage), speed, null,
                Palette.EnemyPurple);
        }

        private void SummonMinions(Vector3 around)
        {
            EnemyKind minion = kind == EnemyKind.CageWarden ? EnemyKind.Sentinel : EnemyKind.SandBeetle;
            SpawnBudget budget = FloorPlan.Budget(floor);
            int count = current.Count;
            for (int i = 0; i < count; i++)
            {
                if (world.RegularAlive >= SurvivorTuning.MaxAliveEnemies)
                {
                    break;
                }

                float angle = i / (float)count * Mathf.PI * 2f;
                Vector3 spot = CombatMath.ClampArena(around + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * 3f);
                world.SpawnEnemy(minion, spot, budget.HpMult, budget.DmgMult, false);
            }
        }

        private void SpawnPillar(Vector3 position, float seconds)
        {
            Pillar p = new Pillar();
            p.Position = position;
            p.Left = seconds;
            p.Total = seconds;
            p.View = world.GetWarningShape(WarnShape.Disc);
            p.View.transform.position = new Vector3(position.x, 0.05f, position.z);
            p.View.transform.rotation = Quaternion.identity;
            p.View.transform.localScale = Vector3.one * (PillarRadius / Catalog.WardenRingRadius);
            pillars.Add(p);
        }

        private void StepPillars(float deltaTime)
        {
            for (int i = pillars.Count - 1; i >= 0; i--)
            {
                Pillar p = pillars[i];
                p.Left -= deltaTime;
                if (p.View != null)
                {
                    p.View.SetFill(1f - Mathf.Max(0f, p.Left) / p.Total);
                }

                if (p.Left > 0f)
                {
                    continue;
                }

                if (CombatMath.Overlap(world.Player.transform.position, 0f, p.Position, PillarRadius))
                {
                    world.Player.TakeDamage(Damage(PillarDamage()), p.Position);
                }

                world.PlayBurst(Palette.Sand, PillarRadius, p.Position);
                if (p.View != null)
                {
                    world.ReturnWarningShape(p.View, WarnShape.Disc);
                }

                pillars.RemoveAt(i);
            }
        }

        private float PillarDamage()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].Kind == ActKind.Pillars)
                {
                    return slots[i].Damage;
                }
            }

            return 0f;
        }

        private void AddWarn(WarnShape shape, Vector3 pos, Vector3 forward, Vector3 scale)
        {
            TelegraphView view = world.GetWarningShape(shape);
            view.transform.position = new Vector3(pos.x, 0.05f, pos.z);
            view.transform.rotation = forward.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(forward) : Quaternion.identity;
            view.transform.localScale = scale;
            Warning w = new Warning();
            w.View = view;
            w.Shape = shape;
            warnings.Add(w);
        }

        private void ClearWarnings()
        {
            for (int i = 0; i < warnings.Count; i++)
            {
                world.ReturnWarningShape(warnings[i].View, warnings[i].Shape);
            }

            warnings.Clear();
        }

        // ---------- Blue Hair Guy: anchors, purify window, day/night ----------

        private void BeginAnchorWarnings()
        {
            ClearAnchorWarnings();
            anchorWarnLeft = SurvivorTuning.AnchorWarnSeconds;
            for (int i = 0; i < AnchorSpots.Length; i++)
            {
                TelegraphView view = world.GetWarningShape(WarnShape.Disc);
                view.transform.position = new Vector3(AnchorSpots[i].x, 0.05f, AnchorSpots[i].z);
                view.transform.rotation = Quaternion.identity;
                view.transform.localScale = Vector3.one * 0.4f;
                anchorWarnings.Add(view);
            }
        }

        private void ClearAnchorWarnings()
        {
            for (int i = 0; i < anchorWarnings.Count; i++)
            {
                world.ReturnWarningShape(anchorWarnings[i], WarnShape.Disc);
            }

            anchorWarnings.Clear();
            anchorWarnLeft = -1f;
        }

        private void StepAnchors(float deltaTime)
        {
            if (kind != EnemyKind.BlueHairGuy || phase < 3)
            {
                return;
            }

            if (purify > 0f)
            {
                purify -= deltaTime;
                if (purify <= 0f)
                {
                    anchorRespawn = SurvivorTuning.AnchorRespawnSeconds;
                    world.Session.Notice("Neo hắc hoá hồi sinh!");
                }

                return;
            }

            if (anchorRespawn > 0f)
            {
                anchorRespawn -= deltaTime;
                if (anchorRespawn <= 0f)
                {
                    anchorRespawn = -1f;
                    BeginAnchorWarnings();
                }

                return;
            }

            if (anchorWarnLeft > 0f)
            {
                anchorWarnLeft -= deltaTime;
                float fill = 1f - Mathf.Max(0f, anchorWarnLeft) / SurvivorTuning.AnchorWarnSeconds;
                for (int i = 0; i < anchorWarnings.Count; i++)
                {
                    anchorWarnings[i].SetFill(fill);
                }

                if (anchorWarnLeft <= 0f)
                {
                    SpawnAnchors();
                }
            }
        }

        private void SpawnAnchors()
        {
            ClearAnchorWarnings();
            anchors.Clear();
            float hp = boss.MaxHealth * SurvivorTuning.BlueHairAnchorHpFraction;
            for (int i = 0; i < SurvivorTuning.BlueHairAnchors && i < AnchorSpots.Length; i++)
            {
                anchors.Add(world.SpawnAnchor(AnchorSpots[i], hp, OnAnchorKilled));
            }

            anchorsAlive = anchors.Count;
            world.Storm.Begin();
        }

        private void OnAnchorKilled(EnemyController anchor)
        {
            CombatFeedback.Play("anchor_break");
            anchorsAlive = Mathf.Max(0, anchorsAlive - 1);
            if (anchorsAlive == 0 && phase == 3 && !kneeling)
            {
                purify = SurvivorTuning.PurifyWindowSeconds;
                world.Storm.End();
                CancelAction();
                world.ClearEnemyProjectiles();
                world.Session.Notice("Cửa sổ thanh tẩy! Sát thương x3");
                CombatFeedback.Shake(0.4f);
            }
        }

        private void StepDayNight(float deltaTime)
        {
            if (kind != EnemyKind.BlueHairGuy || phase != 2)
            {
                return;
            }

            dayNightTimer += deltaTime;
            if (!dayNightWarned && dayNightTimer >= SurvivorTuning.DayNightForceSeconds - 1f)
            {
                dayNightWarned = true;
                world.Session.Notice("Nhịp ngày đêm bị bẻ!");
            }

            if (dayNightTimer >= SurvivorTuning.DayNightForceSeconds)
            {
                dayNightTimer = 0f;
                dayNightWarned = false;
                world.Session.RequestDayNightToggle();
            }
        }

        public void Cleanup()
        {
            CancelAction();
            ClearAnchorWarnings();
            if (kind == EnemyKind.SandQueen || kind == EnemyKind.BlueHairGuy)
            {
                world.Storm.End();
            }
        }
    }
}
