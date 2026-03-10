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
        private Vector3 originalLocalHeadPos;

        [SerializeField]Transform jumpHeadPos;
        [SerializeField] Transform layDownPos;

        private void Start()
        {
            //if(head == null) head = GetComponent<GameObject>();


            originalLocalHeadPos = head.transform.localPosition;
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
            if (head != null && jumpHeadPos != null)
            {
                // 現在のローカル位置を保存（親＝headの親、つまりPlayer基準）
                originalLocalHeadPos = head.transform.localPosition;

                // jumpHeadPos（headの子）のワールド座標を取得し、
                // それをheadの親（＝Player）のローカル座標に変換
                Vector3 targetLocalPos = head.transform.parent.InverseTransformPoint(jumpHeadPos.position);
                head.transform.localPosition = targetLocalPos;
            }
        }

        public void OnGround()
        {
            if (head != null)
            {
                // 元のローカル位置に戻す
                head.transform.localPosition = originalLocalHeadPos;
            }
        }



        public void Sit()
        {
            if (head != null)
            {
                Sequence seq = DOTween.Sequence();
                seq.AppendInterval(0.50f); // 0.1秒待つ
                //seq.Append(head.transform.DOLocalMove(originalHeadPos + new Vector3(-0.02f, -0.03f, 0f), 0.2f)
                    //.SetEase(Ease.OutSine));
            }
        }
        public void StandUp()
        {
            if (head != null)
            {
                Sequence seq = DOTween.Sequence();
                seq.AppendInterval(0.50f); // 0.1秒ディレイ
                //seq.Append(head.transform.DOLocalMove(originalHeadPos, 0.2f).SetEase(Ease.OutSine));
            }
        }


    }




}