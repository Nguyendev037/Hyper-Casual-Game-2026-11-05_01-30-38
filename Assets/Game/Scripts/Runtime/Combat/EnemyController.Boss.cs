using UnityEngine;
using RHMG.Core;
using RHMG.Runtime.Visuals;

namespace RHMG.Runtime.Combat
{
    public sealed partial class EnemyController
    {
        private void HideWarning()
        {
            if (warning == null)
            {
                return;
            }

            world.ReturnWarning(warning, warningPoolKind, warningDisc);
            warning = null;
        }

        private void Die()
        {
            HideWarning();
            CombatFeedback.Play("enemy_die");
            Vector3 position = transform.position;
            if (IsAnchor)
            {
                System.Action<EnemyController> callback = anchorKilled;
                anchorKilled = null;
                world.Enemies.Remove(this);
                callback?.Invoke(this);
                release?.Invoke(this);
                return;
            }

            if (brain != null)
            {
                brain.Cleanup();
            }

            int xp = isDummy ? 0 : XpCurve.XpValue(Kind, Elite);
            if (xp > 0)
            {
                world.Gems.Drop(position, xp);
            }

            if (IsBoss)
            {
                world.Session.NotifyBossDefeated(position, Kind);
            }

            world.Enemies.Remove(this);
            OnKilled?.Invoke(this);
            if (billboard != null && billboard.GetComponent<SpriteDeformer>() != null && Kind != EnemyKind.BlueHairGuy)
            {
                billboard.SetMotion(0f);
                billboard.PlayDeath();
                StartCoroutine(ReleaseAfterDeath());
            }
            else
            {
                release?.Invoke(this);
            }
        }

        private System.Collections.IEnumerator ReleaseAfterDeath()
        {
            yield return new WaitForSeconds(0.5f);
            release?.Invoke(this);
        }

        public void ReturnToPool()
        {
            StopAllCoroutines();
            HideWarning();
            if (brain != null)
            {
                brain.Cleanup();
                brain = null;
            }

            health = null;
            anchorKilled = null;
            gameObject.SetActive(false);
        }
    }
}
