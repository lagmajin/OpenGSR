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
        private Vector3 jumpLocalOffset;
        private Vector3 sitLocalOffset;
        private bool initialized;

        [SerializeField]Transform jumpHeadPos;
        [SerializeField] Transform layDownPos;
        [SerializeField] private float jumpBlendSpeed = 18f;
        [SerializeField] private float sitBlendSpeed = 18f;
        [SerializeField] private float standBobHeadMultiplier = 1.0f;

        public Vector3 StandingBobOffset { get; private set; }
        private PlayerAgent owner;

        private enum HeadPose
        {
            Default,
            Jump,
            Sit
        }

        private HeadPose targetPose = HeadPose.Default;
        private HeadPose currentPose = HeadPose.Default;

        private void Start()
        {
            owner = GetComponentInParent<PlayerAgent>();
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
            jumpLocalOffset = ResolveLocalTarget(jumpHeadPos, defaultLocalHeadPos) - defaultLocalHeadPos;
            sitLocalOffset = ResolveLocalTarget(layDownPos, defaultLocalHeadPos) - defaultLocalHeadPos;
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

        private void LateUpdate()
        {
            if (head == null)
            {
                return;
            }

            if (!initialized)
            {
                InitializeHeadPoseCache();
            }

            if (currentPose != targetPose)
            {
                currentPose = targetPose;
            }

            var targetOffset = currentPose switch
            {
                HeadPose.Jump => jumpLocalOffset,
                HeadPose.Sit => sitLocalOffset,
                _ => Vector3.zero
            };

            StandingBobOffset = owner != null ? owner.GetStandingBobOffset() : Vector3.zero;
            var desiredLocalPos = defaultLocalHeadPos + targetOffset + StandingBobOffset * standBobHeadMultiplier;
            head.transform.localPosition = Vector3.Lerp(head.transform.localPosition, desiredLocalPos, Time.deltaTime * GetBlendSpeed(currentPose));
        }

        private float GetBlendSpeed(HeadPose pose)
        {
            return pose switch
            {
                HeadPose.Jump => jumpBlendSpeed,
                HeadPose.Sit => sitBlendSpeed,
                _ => Mathf.Max(jumpBlendSpeed, sitBlendSpeed)
            };
        }

        public void Jump()
        {
            targetPose = HeadPose.Jump;
        }

        public void OnGround()
        {
            targetPose = HeadPose.Default;
        }

        public void Sit()
        {
            targetPose = HeadPose.Sit;
        }

        public void StandUp()
        {
            targetPose = HeadPose.Default;
        }


    }




}
