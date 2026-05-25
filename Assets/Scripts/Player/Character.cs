using OpenGS;
using System.Collections;
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
            if (Camera.main == null) return 1;
            var screenPos = Camera.main.WorldToScreenPoint(transform.position);
            var direction = Input.mousePosition - screenPos;
            return direction.x >= 0 ? 1 : -1;
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
            this.canAttack = canAttack;
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (_spriteRenderer == null)
            {
                return;
            }

            StopAllCoroutines();
            StartCoroutine(InvincibleRoutine(msec));
        }

        IEnumerator Blink()
        {
            while (isInvincible)
            {
                ChangeTransparency(0.35f);
                yield return new WaitForSeconds(blinkInterval);
                ChangeTransparency(1.0f);
                yield return new WaitForSeconds(blinkInterval);
            }
        }

        private IEnumerator InvincibleRoutine(int msec)
        {
            isInvincible = true;
            StartCoroutine(Blink());
            yield return new WaitForSeconds(Mathf.Max(0f, msec / 1000f));
            isInvincible = false;
            ChangeTransparency(1.0f);
        }

        void ChangeTransparency(float alpha)
        {
            if (_spriteRenderer != null)
                _spriteRenderer.color = new Color(1, 1, 1, alpha);
        }

        void Start()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        void Update()
        {
            var trans = transform.localScale;
            trans.x = Direct();
            transform.localScale = trans;

            if (!isDead)
            {
                if (Input.GetMouseButton(0)) shot();
                if (Input.GetMouseButton(1)) booster();

                if (Input.GetKey(KeyCode.A) | Input.GetKey(KeyCode.LeftArrow))
                {
                    this.transform.Translate(-moveSpeed, 0.0f, 0.0f);
                }
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                {
                    this.transform.Translate(moveSpeed, 0.0f, 0.0f);
                }
                if (Input.GetKeyDown(KeyCode.W)) jump();

                if (Input.GetKeyDown(KeyCode.Tab)) swapWeapon();

                if (Input.GetKey(KeyCode.Space)) openGranade();

                if (Input.GetKeyUp(KeyCode.Space))
                {
                    if (granadeOpend) throwGranade();
                }

                if (Input.GetKeyDown(KeyCode.LeftShift)) dropWeapon();

                if (gameObject.transform.position.y <= -100) burst();

                if (Input.GetKeyDown(KeyCode.Y)) dead();

                if (isGround && boost <= 100) boost += 1;
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.tag == "GameOverArea") burst();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.tag == "Ground")
            {
                isGround = true;
            }
            if (collision.gameObject.tag == "FieldWeapon") takeWeapon();
        }

        private void booster()
        {
            if (boost > 0)
            {
                _rigidbody.AddForce(Vector2.up * 60);
                boost -= 1;
                if (boost < 0) boost = 0;
                isGround = false;
            }
        }

        private void jump()
        {
            _rigidbody.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
            isGround = false;
        }

        private void openGranade()
        {
            if (!granadeOpend)
            {
                granadeOpend = true;
            }
        }

        private void throwGranade()
        {
            canOpenGranade = true;
            granadeOpend = false;
        }

        private void shot()
        {
            if (ball == null) return;
            var clone = Instantiate(ball, transform.position, Quaternion.identity);
            var mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var shotForward = Vector3.Scale((mouseWorldPos - transform.position), new Vector3(1, 1, 0)).normalized;
            clone.GetComponent<Rigidbody2D>().linearVelocity = shotForward * 10f;

            var ScreenPos = Camera.main.WorldToScreenPoint(transform.position);
            var angle = Angle(Vector3.zero, Input.mousePosition - ScreenPos);
            var cloneAngles = clone.transform.localEulerAngles;
            cloneAngles.z = angle;
            clone.transform.localEulerAngles = cloneAngles;
        }

        public void dead()
        {
            if (!isDead)
            {
                _rigidbody.AddForce(Vector2.up * 600, ForceMode2D.Impulse);
                isDead = true;
            }
        }

        private void burst()
        {
            if (!isDead)
            {
                ChangeTransparency(0.0f);
                isDead = true;
            }
        }

        private void takeWeapon()
        {
            if (weapon1 != null)
            {
                weapon1.SetActive(true);
            }

            if (weapon2 != null)
            {
                weapon2.SetActive(false);
            }
        }

        private void swapWeapon()
        {
            var firstActive = weapon1 != null && weapon1.activeSelf;

            if (weapon1 != null)
            {
                weapon1.SetActive(!firstActive);
            }

            if (weapon2 != null)
            {
                weapon2.SetActive(firstActive);
            }
        }
        private void dropWeapon()
        {
            var prefab = Resources.Load("Prefabs/MX1014");
            if (prefab != null)
                Instantiate(prefab, _rigidbody.position, Quaternion.identity);
        }
    }
}
