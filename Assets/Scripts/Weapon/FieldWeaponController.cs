
using Sirenix.OdinInspector;
using UnityEngine;


#pragma warning disable 0414


namespace OpenGS
{

    [DisallowMultipleComponent]
    public class FieldWeaponController : OpenGSBaseClass, IFieldWeaponController
    {
        private int bulletCount = 0;


        private float time = 30.0f;

        private float picupableDelay = 1.0f;

        public bool pickupableOnTime = true;

        public bool pickupable = false;

        private Collider2D pickupCollider;

        [SerializeField] [Required] public GameObject weaponPrefab;

        //public Sound



        [SerializeField] [Required] public Rigidbody2D body;

        private void Start()
        {
            pickupCollider = GetComponent<Collider2D>();
            if (pickupCollider)
            {
                pickupCollider.enabled = false;
            }

            pickupable = false;

            if (pickupableOnTime)
            {
                Invoke(nameof(EnablePickUp), Mathf.Max(0f, picupableDelay));
            }
            else
            {
                EnablePickUp();
            }

            if (time > 0f)
            {
                Invoke(nameof(DestroySelf), time);
            }
        }

        private void Update()
        {

        }



        public void EnablePickUp()
        {
            pickupable = true;
            if (pickupCollider)
            {
                pickupCollider.enabled = true;
            }
        }

        public void DisablePickUp()
        {
            pickupable = false;
            if (pickupCollider)
            {
                pickupCollider.enabled = false;
            }
        }

        private void DestroySelf()
        {
            CancelInvoke(nameof(EnablePickUp));
            CancelInvoke(nameof(DestroySelf));
            Destroy(gameObject);
        }

        private void EquipPlayer(IPlayer p)
        {
            if (!weaponPrefab)
            {
                Debug.Log("Error weapon");

                return;
            }

            if (p.CanEquip())
            {
                p.EquipWeapon(weaponPrefab);
            }
            else
            {
                return;
            }
            Destroy(gameObject);
        }


        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!pickupable)
            {
                return;
            }

            Debug.Log(collision.name);

            var parent = collision.gameObject;

            if (parent != null)
            {
                if (parent.TryGetComponent<IMultipleTags>(out var tags))
                {
                    if (tags.HasPlayerTag())
                    {
                        if (parent.TryGetComponent<IPlayer>(out var player))
                        {
                            EquipPlayer(player);
                        }
                    }

                    if (tags.HasMyPlayerTag() && parent.TryGetComponent<IPlayer>(out var myPlayer))
                    {
                        EquipPlayer(myPlayer);
                    }

                }


            }
            else
            {
                if (collision.gameObject.TryGetComponent<IMultipleTags>(out var tags))
                {
                    if (tags.HasPlayerTag())
                    {
                        if (collision.gameObject.TryGetComponent<IPlayer>(out var player))
                        {
                            EquipPlayer(player);
                        }
                    }

                    if (tags.HasMyPlayerTag() && collision.gameObject.TryGetComponent<IPlayer>(out var myPlayer))
                    {
                        EquipPlayer(myPlayer);
                    }

                }




            }
        }
    }


}
