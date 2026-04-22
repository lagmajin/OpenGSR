
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

        [SerializeField] [Required] public GameObject weaponPrefab;

        //public Sound



        [SerializeField] [Required] public Rigidbody2D body;

        private void Start()
        {
            //Destroy(this.gameObject, 30f)

            Invoke("EnablePickUp", 30f);


        }

        private void Update()
        {

        }



        public void EnablePickUp()
        {

        }

        public void DisablePickUp()
        {

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




            //Debug.Log("Error weapon");



            Destroy(gameObject);



        }


        private void OnTriggerEnter2D(Collider2D collision)
        {
            //Debug.LogError("aa");

            Debug.Log(collision.name);

            var parent = collision.gameObject;

            if (parent != null)
            {
                if (parent.TryGetComponent<IMultipleTags>(out var tags))
                {

                    //Debug.LogError("weapon");

                    if (tags.HasPlayerTag())
                    {



                        if (parent.TryGetComponent<IPlayer>(out var player))
                        {

                            //Debug.LogError("Weapon Pickedup");

                            EquipPlayer(player);



                        }




                        Debug.Log("aaa");
                    }

                    if (tags.HasMyPlayerTag())
                    {

                    }

                }


            }
            else
            {
                if (collision.gameObject.TryGetComponent<IMultipleTags>(out var tags))
                {
                    Debug.LogError("weapon");

                    if (tags.HasPlayerTag())
                    {



                        if (collision.gameObject.TryGetComponent<IPlayer>(out var player))
                        {

                            Debug.LogError("Weapon Pickedup");

                            EquipPlayer(player);



                        }




                        Debug.Log("aaa");
                    }

                    if (tags.HasMyPlayerTag())
                    {

                    }

                }




            }
        }
    }


}
