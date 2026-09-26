using Sirenix.OdinInspector;
using UnityEngine;

namespace OpenGS
{



    //[RequireComponent(MultipleTags)]
    [DisallowMultipleComponent]
    public class Lava : MonoBehaviour
    {
        //[Required][SerializeField]private 

        [SerializeField, Range(0, 100f)] private float damage = 100.0f;

        private void OnValidate()
        {
            if (!float.IsFinite(damage)) damage = 100f;
            damage = Mathf.Max(0f, damage);
        }

        [Required]public PlayerEffectMasterData effectPrefabMasterData;
        public void SetDamage()
        {
            damage = Mathf.Max(0f, damage);
        }

        public void SetDamage(float value)
        {
            damage = Mathf.Max(0f, value);
        }

        public void SetEffectPrefabMasterData(PlayerEffectMasterData masterData)
        {
            effectPrefabMasterData = masterData;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            var abstractPlayer = collision.GetComponentInParent<AbstractPlayer>();
            if (abstractPlayer != null && PlayerRegistry.Instance != null)
            {
                PlayerRegistry.Instance.ApplyDamage(
                    abstractPlayer.UniqueID(),
                    (Vector2)(abstractPlayer.transform.position - transform.position),
                    damage,
                    eDamageType.Lava,
                    string.Empty,
                    nameof(Lava),
                    false);
                abstractPlayer.TakeLavaDamage();
                return;
            }

            var tags = collision.GetComponentInParent<IMultipleTags>();
            if (tags != null && tags.HasPlayerTag())
            {
                var damageable = collision.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    damageable.AddSlipDamage(damage, nameof(Lava));
                    damageable.TakeLavaDamage();
                }
            }


        }

    }
}
