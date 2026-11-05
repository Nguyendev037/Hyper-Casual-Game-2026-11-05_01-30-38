using System.Collections.Generic;
using UnityEngine;
using RHMG.Core;
using RHMG.Runtime.Visuals;

namespace RHMG.Runtime.Combat
{
    /// <summary>Spirit stones (XP) lying on the arena: three sizes, magnet pull, merge above the cap (SPEC-P01 §4).</summary>
    public sealed class GemField
    {
        private sealed class Gem
        {
            public Transform Body;
            public MeshRenderer Renderer;
            public SpriteRenderer Sprite;   // gem_small/mid/large art (lot E); null = primitive sphere fallback
            public int Value;
            public bool Pulled;
        }

        public const int SmallBelow = 5, LargeFrom = 20;

        private readonly CombatWorld world;
        private readonly Transform root;
        private readonly List<Gem> active = new List<Gem>(320);
        private readonly Stack<Gem> pool = new Stack<Gem>();
        private bool pullAll;
        private float pullAllElapsed;

        public int Count => active.Count;
        public long TotalValueOnField
        {
            get
            {
                long sum = 0;
                for (int i = 0; i < active.Count; i++)
                {
                    sum += active[i].Value;
                }

                return sum;
            }
        }

        /// <summary>Number of gems picked up since this field was created.</summary>
        public int Picked { get; private set; }

        public GemField(CombatWorld owner)
        {
            world = owner;
            root = new GameObject("Gems").transform;
            root.SetParent(owner.transform, false);
        }

        public static int TierOf(int value)
        {
            return value < SmallBelow ? 0 : (value < LargeFrom ? 1 : 2);
        }

        public static Color TierColor(int tier)
        {
            switch (tier)
            {
                case 0: return new Color(0.45f, 0.88f, 1f, 1f);
                case 1: return new Color(0.55f, 0.62f, 1f, 1f);
                default: return new Color(0.9f, 0.55f, 1f, 1f);
            }
        }

        public static float TierSize(int tier)
        {
            return tier == 0 ? 0.26f : (tier == 1 ? 0.38f : 0.56f);
        }

        public void Drop(Vector3 position, int value)
        {
            if (value <= 0)
            {
                return;
            }

            Gem gem = pool.Count > 0 ? pool.Pop() : CreateGem();
            gem.Value = value;
            gem.Pulled = pullAll;
            Vector2 scatter = Random.insideUnitCircle * 0.5f;
            gem.Body.position = new Vector3(position.x + scatter.x, 0.3f, position.z + scatter.y);
            gem.Body.gameObject.SetActive(true);
            Apply(gem);
            active.Add(gem);
            if (active.Count > SurvivorTuning.MaxGemsOnField)
            {
                MergeFarthest();
            }
        }

        /// <summary>Floor end: every gem flies to the girl within ~0.6 s.</summary>
        public void PullAll()
        {
            pullAll = true;
            pullAllElapsed = 0f;
            for (int i = 0; i < active.Count; i++)
            {
                active[i].Pulled = true;
            }
        }

        public void StopPullAll()
        {
            pullAll = false;
        }

        /// <summary>Picks up whatever is left at once (safety net so XP is never lost).</summary>
        public void AbsorbAll()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Collect(i);
            }
        }

        public void Clear()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Recycle(active[i]);
                active.RemoveAt(i);
            }

            pullAll = false;
        }

        public void Step(float deltaTime, Vector3 playerPosition, float magnetRadius)
        {
            if (pullAll)
            {
                pullAllElapsed += deltaTime;
            }

            float radiusSqr = magnetRadius * magnetRadius;
            float pickupSqr = CombatTuning.GemPickupRadius * CombatTuning.GemPickupRadius;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Gem gem = active[i];
                Vector3 position = gem.Body.position;
                Vector3 delta = CombatMath.Flat(playerPosition - position);
                float sqr = delta.sqrMagnitude;
                if (!gem.Pulled && sqr <= radiusSqr)
                {
                    gem.Pulled = true;
                }

                if (gem.Pulled)
                {
                    float distance = Mathf.Sqrt(sqr);
                    float speed = SurvivorTuning.MagnetPullSpeed;
                    if (pullAll)
                    {
                        speed = Mathf.Max(speed, distance / CombatTuning.FloorEndCollectSeconds);
                    }

                    float step = Mathf.Min(distance, speed * deltaTime);
                    if (distance > 0.0001f)
                    {
                        gem.Body.position = position + delta / distance * step;
                    }

                    sqr = CombatMath.Flat(playerPosition - gem.Body.position).sqrMagnitude;
                }

                if (sqr <= pickupSqr)
                {
                    Collect(i);
                }
            }
        }

        /// <summary>Nearest gem position to <paramref name="from"/>, or null when the field is empty (autopilot).</summary>
        public Vector3? NearestTo(Vector3 from)
        {
            Vector3? best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < active.Count; i++)
            {
                Vector3 p = active[i].Body.position;
                float d = CombatMath.Flat(p - from).sqrMagnitude;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = p;
                }
            }

            return best;
        }

        public void Dispose()
        {
            active.Clear();
            pool.Clear();
        }

        private void Collect(int index)
        {
            Gem gem = active[index];
            active.RemoveAt(index);
            int value = gem.Value;
            Recycle(gem);
            Picked++;
            CombatFeedback.Play("gem");
            world.Session.NotifyXp(value);
        }

        private void Recycle(Gem gem)
        {
            gem.Body.gameObject.SetActive(false);
            pool.Push(gem);
        }

        // SPEC-P01 §4: over the cap the farthest gem merges into the gem nearest to it.
        private void MergeFarthest()
        {
            Vector3 center = world.Player != null ? world.Player.transform.position : Vector3.zero;
            int farthest = -1;
            float best = -1f;
            for (int i = 0; i < active.Count; i++)
            {
                float d = CombatMath.Flat(active[i].Body.position - center).sqrMagnitude;
                if (d > best)
                {
                    best = d;
                    farthest = i;
                }
            }

            if (farthest < 0)
            {
                return;
            }

            Gem source = active[farthest];
            int nearest = -1;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < active.Count; i++)
            {
                if (i == farthest)
                {
                    continue;
                }

                float d = CombatMath.Flat(active[i].Body.position - source.Body.position).sqrMagnitude;
                if (d < nearestDistance)
                {
                    nearestDistance = d;
                    nearest = i;
                }
            }

            if (nearest < 0)
            {
                return;
            }

            active[nearest].Value += source.Value;
            Apply(active[nearest]);
            active.RemoveAt(farthest);
            Recycle(source);
        }

        private static Sprite[] s_GemSprites;

        private static Sprite GemSprite(int tier)
        {
            if (s_GemSprites == null)
            {
                s_GemSprites = new[]
                {
                    CharacterSprites.Prop("gem_small"), CharacterSprites.Prop("gem_mid"), CharacterSprites.Prop("gem_large")
                };
            }

            return s_GemSprites[Mathf.Clamp(tier, 0, 2)];
        }

        private Gem CreateGem()
        {
            if (GemSprite(0) != null)
            {
                GameObject flat = new GameObject("Gem");
                flat.transform.SetParent(root, false);
                SpriteRenderer sr = flat.AddComponent<SpriteRenderer>();
                Material material = CharacterSprites.SpriteMaterial();
                if (material != null)
                {
                    sr.sharedMaterial = material;
                }

                sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                return new Gem { Body = flat.transform, Sprite = sr };
            }

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Gem";
            body.transform.SetParent(root, false);
            Collider collider = body.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            Gem gem = new Gem();
            gem.Body = body.transform;
            gem.Renderer = body.GetComponent<MeshRenderer>();
            if (gem.Renderer != null)
            {
                gem.Renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            return gem;
        }

        private static void Apply(Gem gem)
        {
            int tier = TierOf(gem.Value);
            float size = TierSize(tier);
            if (gem.Sprite != null)
            {
                Sprite art = GemSprite(tier);
                gem.Sprite.sprite = art;
                float worldHeight = size * 2.4f;
                float scale = art != null ? worldHeight / Mathf.Max(0.01f, art.bounds.size.y) : 1f;
                gem.Body.localScale = new Vector3(scale, scale, scale);
                Camera cam = Camera.main;
                if (cam != null)
                {
                    gem.Body.rotation = cam.transform.rotation;
                }

                return;
            }

            gem.Body.localScale = new Vector3(size, size * 1.25f, size);
            if (gem.Renderer != null)
            {
                gem.Renderer.sharedMaterial = MaterialLibrary.Unlit(TierColor(tier));
            }
        }
    }
}
