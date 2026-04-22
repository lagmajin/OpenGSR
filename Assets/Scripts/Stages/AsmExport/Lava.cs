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

        [Required]public PlayerEffectMasterData effectPrefabMasterData;
        public void SetDamage()
        {

        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            Debug.Log(collision.name);

            var parent = collision.gameObject.transform.parent;

            if (parent != null)
            {
                if (parent.TryGetComponent<IMultipleTags>(out var tags))
                {

                    if (tags.HasPlayerTag())
                    {

                        if (parent.TryGetComponent<IDamageable>(out var damagable))
                        {

                            //Debug.LogError("Weapon Pickedup");

                            //EquipPlayer(player);

                            Debug.Log("Lava"+collision.name);

                            damagable.TakeLavaDamage();


                        }



                    }


                }

            }
            else
            {

            }


        }

    }
}
