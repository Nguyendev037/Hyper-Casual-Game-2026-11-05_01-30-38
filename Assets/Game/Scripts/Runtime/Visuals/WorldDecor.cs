using System.Collections.Generic;
using UnityEngine;

namespace RHMG.Runtime.Visuals
{
    /// <summary>
    /// SPEC-P04 section 2: per-chapter props on the rim of the arena. 18..26 camera-facing sprites per floor,
    /// none within 6 m of the centre, none closer than 2 m to each other, seeded by the floor so a floor always
    /// looks the same. No collider. Missing art (RHMG/Sprites/deco_ch[k]_[a|b|c]) means no props (C3).
    /// </summary>
    public static class WorldDecor
    {
        public const string RootName = "Decor";
        public const int MinCount = 18, MaxCount = 26;
        public const float CenterClear = 6f, MinSpacing = 2f, EdgeMargin = 1.2f, GateClearX = 3f, GateClearZ = 4f;
        public const float PropHeight = 2.2f;

        /// <summary>Chapter 0..4 of a floor. Mirrors FloorPlan.Chapter (plan 05 contract 01 section 1,
        /// added to Core by pack WE); kept local so this pack compiles on its own.</summary>
        public static int ChapterOf(int floor) => Mathf.Clamp((floor - 1) / 20, 0, 4);

        public static string SpriteId(int chapter, int variant) => "deco_ch" + chapter + "_" + (char)('a' + variant);

        /// <summary>Pure placement (testable): positions on the XZ plane as (x, z). Deterministic per floor.</summary>
        public static List<Vector2> Place(int floor, float width, float depth)
        {
            var random = new System.Random(floor * 7919 + 13);
            int target = random.Next(MinCount, MaxCount + 1);
            float halfW = width * 0.5f - EdgeMargin, halfD = depth * 0.5f - EdgeMargin;
            var points = new List<Vector2>(target);
            for (int attempt = 0; attempt < target * 60 && points.Count < target; attempt++)
            {
                var p = new Vector2((float)(random.NextDouble() * 2 - 1) * halfW, (float)(random.NextDouble() * 2 - 1) * halfD);
                if (p.magnitude < CenterClear) continue;
                if (Mathf.Abs(p.x) < GateClearX && p.y > depth * 0.5f - GateClearZ) continue;
                bool crowded = false;
                for (int i = 0; i < points.Count && !crowded; i++) crowded = (points[i] - p).sqrMagnitude < MinSpacing * MinSpacing;
                if (!crowded) points.Add(p);
            }
            return points;
        }

        /// <summary>Rebuilds the Decor child of an arena for this floor. Returns the number of props created.</summary>
        public static int Rebuild(Transform arena, int floor, float width, float depth)
        {
            if (arena == null) return 0;
            Transform old = arena.Find(RootName);
            if (old != null)
            {
                old.name = RootName + "_old"; // Destroy is deferred: never leave two "Decor" children
                old.gameObject.SetActive(false);
                Object.Destroy(old.gameObject);
            }

            int chapter = ChapterOf(floor);
            var sprites = new List<Sprite>();
            for (int variant = 0; variant < 3; variant++)
            {
                Sprite sprite = Resources.Load<Sprite>("RHMG/Sprites/" + SpriteId(chapter, variant));
                if (sprite != null) sprites.Add(sprite);
            }
            if (sprites.Count == 0) return 0;

            var root = new GameObject(RootName).transform;
            root.SetParent(arena, false);
            Material material = CharacterSprites.SpriteMaterial();
            var random = new System.Random(floor * 104729 + 7);
            List<Vector2> points = Place(floor, width, depth);
            for (int i = 0; i < points.Count; i++)
            {
                Sprite sprite = sprites[random.Next(sprites.Count)];
                var go = new GameObject("Deco_" + i);
                go.transform.SetParent(root, false);
                go.transform.localPosition = new Vector3(points[i].x, 0f, points[i].y);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                if (material != null) renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                float h = sprite.bounds.size.y;
                float height = PropHeight * (0.85f + (float)random.NextDouble() * 0.35f);
                go.transform.localScale = Vector3.one * (h > 0.0001f ? height / h : 1f);
                go.AddComponent<DecorFacing>();
            }
            return points.Count;
        }

        /// <summary>Keeps a prop facing the camera like the character billboards (rotation only).</summary>
        sealed class DecorFacing : MonoBehaviour
        {
            static Camera camera;
            static int cameraFrame = -1;

            void LateUpdate()
            {
                if (cameraFrame != Time.frameCount)
                {
                    cameraFrame = Time.frameCount;
                    camera = Camera.main;
                }
                if (camera != null) transform.rotation = camera.transform.rotation;
            }
        }
    }
}
