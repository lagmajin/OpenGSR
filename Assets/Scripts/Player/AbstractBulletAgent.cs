//using KanKikuchi.AudioManager;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using OpenGSCore;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public abstract class AbstractBulletAgent : MonoBehaviour
    {
        [ShowInInspector] public float Damage = 0;
        [SerializeField] private string ownerPlayerId = string.Empty;
        [SerializeField] private string weaponName = "Unknown";
        [SerializeField] private ETeam team = ETeam.NoTeam;

        [SerializeField]private AudioClip[] hitObjectSounds;
        [SerializeField] AudioSource audioSource;
        [SerializeField]protected GameObject hitEffect;
        public abstract void Launch(Vector2 direction, float speed, float damage=0);

        public string OwnerPlayerId => ownerPlayerId;
        public string WeaponName => weaponName;
        public ETeam Team => team;

        public virtual void SetOwnerInfo(string ownerId, string weapon, ETeam ownerTeam)
        {
            ownerPlayerId = ownerId ?? string.Empty;
            weaponName = string.IsNullOrWhiteSpace(weapon) ? "Unknown" : weapon;
            team = ownerTeam;
        }

        protected bool TryApplyHit(Collider2D collider, Vector2 impactOrigin, eDamageType damageType, bool knockback = false)
        {
            if (collider == null)
            {
                return false;
            }

            if (ProjectileHitUtility.IsStageHit(collider.gameObject))
            {
                PlaySound(ESoundEffect.HitStageObject);

                if (hitEffect != null)
                {
                    Instantiate(hitEffect, (Vector3)collider.ClosestPoint(impactOrigin), Quaternion.identity);
                }

                return true;
            }

            if (ProjectileHitUtility.TryGetTargetPlayer(collider, out var player))
            {
                return ProjectileHitUtility.ApplyPlayerDamage(
                    player,
                    impactOrigin,
                    Damage > 0f ? Damage : 1f,
                    damageType,
                    ownerPlayerId,
                    weaponName,
                    team,
                    knockback);
            }

            return false;
        }

        // Use this for initialization
        public void PlaySound(ESoundEffect effect)
        {
            switch (effect)
            {
                case ESoundEffect.HitStageObject:  // ← enum名をフルで書く
                    if (hitObjectSounds.Length > 0)
                    {
                        var clip = hitObjectSounds[Random.Range(0, hitObjectSounds.Length)];
                        SoundManager.Instance.PlayOneShotSafe(clip, context: nameof(AbstractBulletAgent));

                        //audioSource.PlayOneShot(clip);
                    }
                    break;  // ← 他のcase追加しやすいようにbreakを忘れずに
            }
        }

    }
}
