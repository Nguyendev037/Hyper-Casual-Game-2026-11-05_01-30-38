using System.Collections.Generic;
using UnityEngine;

namespace RHMG.Runtime.Visuals
{
    /// <summary>Stack pool of VFX quads plus the global creation counter used by the P03 acceptance test.</summary>
    public sealed class VfxPool
    {
        /// <summary>Number of VFX objects ever created (quads, particle systems). Must not grow once combat is warm.</summary>
        public static int Created { get; private set; }

        private readonly Stack<VfxSprite> idle = new Stack<VfxSprite>();
        private readonly Transform parent;
        private readonly string name;
        private readonly int cap;
        private int made;

        public VfxPool(Transform parentTransform, string poolName, int capacity)
        {
            parent = parentTransform;
            name = poolName;
            cap = capacity;
        }

        public static void NoteCreated()
        {
            Created++;
        }

        public static void ResetCounter()
        {
            Created = 0;
        }

        public void Prewarm(int count)
        {
            List<VfxSprite> temp = new List<VfxSprite>(count);
            for (int i = 0; i < count; i++)
            {
                VfxSprite item = Get();
                if (item != null)
                {
                    temp.Add(item);
                }
            }

            for (int i = 0; i < temp.Count; i++)
            {
                Release(temp[i]);
            }
        }

        /// <summary>Returns an active quad, or null when the budget cap is reached (the effect is skipped).</summary>
        public VfxSprite Get()
        {
            VfxSprite item = null;
            while (idle.Count > 0 && item == null)
            {
                item = idle.Pop();
            }

            if (item == null)
            {
                if (made >= cap)
                {
                    return null;
                }

                item = VfxSprite.Create(parent, name + made);
                made++;
                NoteCreated();
            }

            item.gameObject.SetActive(true);
            return item;
        }

        public void Release(VfxSprite item)
        {
            if (item == null)
            {
                return;
            }

            item.gameObject.SetActive(false);
            idle.Push(item);
        }
    }
}
