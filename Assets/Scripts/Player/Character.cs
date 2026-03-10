//using KanKikuchi.AudioManager;
using OpenGS;
using System.Collections;
//using KanKikuchi.AudioManager;
using UnityEngine;

#pragma warning disable 0414
#pragma warning disable 0219


namespace OpenGS
{
    [DisallowMultipleComponent]
    public class Character : MonoBehaviour
    {
        private bool spaceKeyDown = false;
        private bool isGround = false;

        private bool onDamage = false;

        private Rigidbody2D _rigidbody;

        private SpriteRenderer _spriteRenderer;

        int boost = 100;

        public float beforeJump = 0.0f;
        public float jumpDelay = 2.0f;
        public float jumpPower = 120.0f;
        public float boosterPower = 60.0f;
        public float maxBoosterSpeed = 60.0f;

        public float moveSpeed = 0.08f;

        public GameObject ball;

        public GameObject weapon1;
        public GameObject weapon2;

        private float foreInput = 0f;
        private float backInput = 0f;

        private float foreInputTime = 30f;
        private float backInputTime = 30f;

        private bool fore = false;
        private bool back = false;

        private bool canOpenGranade = false;
        public float blinkInterval = 0.1f;

        public float throwGranadePower = 0.0f;

        public AudioClip openNormalGranadeSound;
        public AudioClip openPowerGranadeSound;
        public AudioClip openClusterGranadeSound;

        public AudioClip throwNormalGranadeSound;
        public AudioClip throwPowerGranadeSound;
        public AudioClip throwClusterGranadeSound;

        public AudioClip boostStartSound;
        public AudioClip boostLoopSound;


        private AudioSource aSource = null;
        private bool granadeOpend;
        private bool isDead = false;

        private bool canAttack = true;

        public float inviTime;
        private float timeElapsed;
        private bool isInvincible = false;



        public int Direct()
        {
            var screenPos = Camera.main.WorldToScreenPoint(transform.position);
            var direction = Input.mousePosition - screenPos;

            //Debug.Log("Sc:"+screenPos);
            //Debug.Log("Dic:"+direction);

            var trans = transform.localScale;
            if (direction.x >= 0)
            {


                return 1;

            }
            else
            {

                return -1;


            }


        }

        public static float Angle(Vector2 from, Vector2 to)
        {
            var dx = to.x - from.x;
            var dy = to.y - from.y;
            var rad = Mathf.Atan2(dy, dx);
            return rad * Mathf.Rad2Deg;
        }

        public void setInvincible(int msec, bool canAttack = false)
        {

        }

        IEnumerator Blink()
        {
            while (true)
            {
                //renderer.enabled = !renderer.enabled;
                //yield return new WaitForSeconds(interval);
            }
        }
        void ChangeTransparency(float alpha)
        {
            _spriteRenderer.color = new Color(1, 1, 1, alpha);
        }


        // Start is called before the first frame update
        void Start()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _rigidbody = GetComponent<Rigidbody2D>();

            //var obj = GameObject.FindWithTag("AudioSource");

            //aSource = obj.GetComponentInChildren<AudioSource>();



            //ChangeTransparency(0.5f);



        }




        // Update is called once per frame
        void Update()
        {
            // timeElapsed += Time.deltaTime;

            /*
            var screenPos = Camera.main.WorldToScreenPoint(transform.position);
            var direction = Input.mousePosition - screenPos;

            //Debug.Log("Sc:"+screenPos);
            //Debug.Log("Dic:"+direction);

            var trans = transform.localScale;
            if (direction.x>=0)
            {


                trans.x = 1;

            }
            else
            {

                trans.x = -1;


            }*/

            float x = Input.GetAxisRaw("Horizontal");

            var trans = transform.localScale;

            trans.x = Direct();

            transform.localScale = trans;


            if (!isDead)
            {
                if (Input.GetMouseButton(0))
                {
                    shot();

                }

                if (Input.GetMouseButton(1))
                {
                    booster();
                }



                if (Input.GetKey(KeyCode.A) | Input.GetKey(KeyCode.LeftArrow))
                {
                    if (isGround)
                    {
                        this.transform.Translate(-moveSpeed, 0.0f, 0.0f);

                        Debug.Log("MoveLeft");
                    }
                    else
                    {

                        this.transform.Translate(-moveSpeed, 0.0f, 0.0f);

                    }
                }
                // 右に移動
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                {
                    if (isGround)
                    {
                        this.transform.Translate(moveSpeed, 0.0f, 0.0f);

                        Debug.Log("MoveRight");
                    }
                    else
                    {
                        this.transform.Translate(0.02f, 0.0f, 0.0f);
                    }
                }
                // 前に移動
                if (Input.GetKeyDown(KeyCode.W))
                {


                    jump();
                }

                if (Input.GetKey(KeyCode.DownArrow))
                {
                    //jump();
                }


                if (Input.GetKeyDown(KeyCode.Keypad1))
                {

                }

                if (Input.GetKeyDown(KeyCode.Keypad2))
                {

                }

                if (Input.GetKeyDown(KeyCode.Keypad2))
                {

                }

                if (Input.GetKey(KeyCode.Space))
                {

                }


                if (Input.GetKey(KeyCode.T))
                {
                    backDash();
                }

                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    swapWeapon();

                }

                if (Input.GetKey(KeyCode.Space))
                {
                    openGranade();
                }

                if (Input.GetKeyUp(KeyCode.Space))
                {
                    if (granadeOpend)
                    {
                        throwGranade();
                    }

                }

                if (Input.GetKeyDown(KeyCode.LeftShift))
                {
                    dropWeapon();

                }

                if (gameObject.transform.position.y <= -100)
                {
                    burst();
                }


                if (Input.GetKeyDown(KeyCode.Y))
                {
                    dead();

                }

                if (isGround)
                {
                    if (boost <= 100)
                    {
                        boost += 1;

                    }
                }

            }

        }


        private void OnTriggerEnter2D(Collider2D collision)
        {

            if (collision.gameObject.tag == "GameOverArea")
            {
                burst();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.tag == "Ground")
            {
                if (!isGround)
                    isGround = true;

                Debug.Log("Landed");
            }

            if (collision.gameObject.tag == "FieldWeapon")
            {
                takeWeapon();
            }



            Debug.Log(collision.gameObject.tag);

        }





        private void booster()
        {
            if (boost > 0)
            {
                _rigidbody.AddForce(Vector2.up * 60);

                boost -= 1;

                if (boost < 0)
                {
                    boost = 0;
                }

                isGround = false;

            }
        }

        private void jump()
        {
            if (!isGround)
            {
                //return;
            }
            _rigidbody.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);


            isGround = false;

            Debug.Log("Jump");

        }


        private void foreDash()
        {
            if (!isGround)
            {

                return;
            }

            _rigidbody.AddForce(Vector2.up * 35, ForceMode2D.Impulse);

            if (Direct() > 0)
            {
                _rigidbody.AddForce(Vector2.right * 40, ForceMode2D.Impulse);
            }
            else
            {
                _rigidbody.AddForce(Vector2.left * 40, ForceMode2D.Impulse);
            }

            isGround = false;

        }

        private void backDash()
        {
            if (!isGround)
            {

                return;
            }

            _rigidbody.AddForce(Vector2.up * 35);

            if (Direct() > 0)
            {
                _rigidbody.AddForce(Vector2.left * 40);
            }
            else
            {
                _rigidbody.AddForce(Vector2.right * 40);
            }

            isGround = false;

        }



        private void openGranade()
        {
            if (!granadeOpend)
            {
                //aSource.PlayOneShot(openClusterGranadeSound);

                //SEManager.Instance.Play(openClusterGranadeSound);

                granadeOpend = true;
            }
            else
            {


            }
        }

        private void throwGranade()
        {

            //aSource.PlayOneShot(throwClusterGranadeSound);

            //var clone = Instantiate(ball, transform.position, Quaternion.identity);

            //var script = clone.GetComponent<BulletController>();

            //SEManager.Instance.Play(throwNormalGranadeSound);



            canOpenGranade = true;

            granadeOpend = false;
        }
        private void shot()
        {
            var clone = Instantiate(ball, transform.position, Quaternion.identity);

            var script = clone.GetComponent<BulletController>();



            var mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            var shotForward = Vector3.Scale((mouseWorldPos - transform.position), new Vector3(1, 1, 0)).normalized;

            clone.GetComponent<Rigidbody2D>().linearVelocity = shotForward * 10f;

            var ScreenPos = Camera.main.WorldToScreenPoint(transform.position);

            var angle = Angle(Vector3.zero, Input.mousePosition - ScreenPos);

            var cloneAngles = clone.transform.localEulerAngles;

            cloneAngles.z = angle;
            clone.transform.localEulerAngles = cloneAngles;

        }

        private void respawnWait()
        {

        }

        private void spawn()
        {

        }

        private void respawn()
        {

            isDead = false;

            setInvincible(5000);
        }

        private void takeDamage(int damage = 0)
        {
            if (!isInvincible)
            {

            }

        }

        private void useItem(int num = 0)
        {
            if (num <= 0 && num <= 4)
            {
                return;
            }


        }

        private void takeItem()
        {


        }

        public void dead()
        {
            if (!isDead)
            {
                _rigidbody.AddForce(Vector2.up * 600, ForceMode2D.Impulse);

                isDead = true;

            }

            bool canRespwn = false;
        }

        private void burst()
        {
            if (!isDead)
            {
                Debug.Log("Burst");

                ChangeTransparency(0.0f);

                isDead = true;
            }
        }

        private void takeWeapon()
        {
            if (!isDead)
            {

            }
        }
        private void swapWeapon()
        {
            if (!isDead)
            {
            }
        }

        private void dropWeapon()
        {
            if (!isDead)
            {
                var prefab = Resources.Load("Prefabs/MX1014");

                var position = _rigidbody.position;


                Instantiate(prefab, position, Quaternion.identity);


            }



        }

    }


}