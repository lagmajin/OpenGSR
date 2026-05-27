using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using OpenGSCore;
using Sirenix.OdinInspector;
using UnityEngine;


namespace OpenGS
{

    [DisallowMultipleComponent]
    public class HeadController : MonoBehaviour
    {
        //[Required]public AbstractPlayer player;
        // Start is called before the first frame update

        [SerializeField] private GameObject head;
        private Vector3 defaultLocalHeadPos;
        private Vector3 jumpLocalHeadPos;
        private Vector3 sitLocalHeadPos;
        private bool initialized;

        [SerializeField]Transform jumpHeadPos;
        [SerializeField] Transform layDownPos;

        private void Start()
        {
            InitializeHeadPoseCache();
        }

        private void OnValidate()
        {
            InitializeHeadPoseCache();
        }

        private void InitializeHeadPoseCache()
        {
            if (head == null)
            {
                return;
            }

            defaultLocalHeadPos = head.transform.localPosition;
            jumpLocalHeadPos = ResolveLocalTarget(jumpHeadPos, defaultLocalHeadPos);
            sitLocalHeadPos = ResolveLocalTarget(layDownPos, defaultLocalHeadPos);
            initialized = true;
        }

        private Vector3 ResolveLocalTarget(Transform target, Vector3 fallback)
        {
            if (head == null || target == null)
            {
                return fallback;
            }

            if (target.parent == head.transform.parent)
            {
                return target.localPosition;
            }

            return head.transform.parent.InverseTransformPoint(target.position);
        }

        public void Reset()
        {

        }

        private void Update()
        {
            
            var screenPos = Camera.main.WorldToScreenPoint(transform.position);
            var dir = Input.mousePosition - screenPos;
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            //Debug.Log("角度" + angle);
            // ここで、マウスがキャラの右側にあれば反転しない、左側にあれば反転
            if (Input.mousePosition.x < screenPos.x)  // マウスが左側
            {
                float relativeAngle = Mathf.DeltaAngle(180f, angle);

                // -30〜+30度に制限
                float clamped = Mathf.Clamp(relativeAngle, -30f, 28f);

                // 左向き（180度）から差分だけ回転
                transform.rotation = Quaternion.Euler(0f, 0f, 180f + clamped - 185f);

                //transform.rotation = Quaternion.Euler(0f, 0f, -(180 - angle));  // 左に向けて回転
            }
            else  // マウスが右側
            {
                float relativeAngle = Mathf.DeltaAngle(0f, angle);
                float clamped = Mathf.Clamp(relativeAngle, -30f, 30f);
                transform.rotation = Quaternion.Euler(0f, 0f, clamped);
            }


        }

        public void Jump()
        {
            if (head != null)
            {
                if (!initialized)
                {
                    InitializeHeadPoseCache();
                }

                head.transform.localPosition = jumpLocalHeadPos;
            }
        }

        public void OnGround()
        {
            if (head != null)
            {
                if (!initialized)
                {
                    InitializeHeadPoseCache();
                }

                head.transform.localPosition = defaultLocalHeadPos;
            }
        }



        public void Sit()
        {
            if (head != null)
            {
                if (!initialized)
                {
                    InitializeHeadPoseCache();
                }

                head.transform.localPosition = sitLocalHeadPos;
            }
        }
        public void StandUp()
        {
            if (head != null)
            {
                if (!initialized)
                {
                    InitializeHeadPoseCache();
                }

                head.transform.localPosition = defaultLocalHeadPos;
            }
        }


    }




}
