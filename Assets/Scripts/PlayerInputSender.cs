using UnityEngine;
using Newtonsoft.Json.Linq;
using LiteNetLib; // DeliveryMethodのために必要

namespace OpenGS
{
    public class PlayerInputSender : MonoBehaviour
    {
        private ClientNetworkManager _networkManager;
        private string _playerId;

        private float _lastMoveSendTime;
        [SerializeField] private float moveSendInterval = 0.05f; // 20回/秒

        private void Awake()
        {
            _networkManager = FindFirstObjectByType<ClientNetworkManager>();
            if (_networkManager == null)
            {
                Debug.LogWarning("[PlayerInputSender] ClientNetworkManager not found in scene. Input sending is disabled in this scene.");
                enabled = false;
                return;
            }
            _playerId = _networkManager.ClientPlayerId;
        }

        private void Update()
        {
            SendMovementInput();
            SendActionInput();
        }

        private void SendMovementInput()
        {
            if (Time.time - _lastMoveSendTime < moveSendInterval)
            {
                return;
            }

            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");

            // 入力がない場合は送信しない
            if (Mathf.Approximately(horizontalInput, 0f) && Mathf.Approximately(verticalInput, 0f))
            {
                return;
            }

            // 現在の位置と、入力から予測される速度をJObjectにまとめる
            JObject moveInput = new JObject
            {
                ["MessageType"] = "PlayerMove",
                ["PlayerID"] = _playerId,
                ["PosX"] = transform.position.x,
                ["PosY"] = transform.position.y,
                ["VelX"] = horizontalInput, // 簡単な例として入力値を速度として扱う
                ["VelY"] = verticalInput,
                ["Timestamp"] = System.DateTime.UtcNow.Ticks
            };

            _networkManager.SendUdpInput(moveInput, DeliveryMethod.Unreliable);
            _lastMoveSendTime = Time.time;
        }

        private void SendActionInput()
        {
            if (Input.GetButtonDown("Fire1")) // マウス左クリックまたはCtrlキー
            {
                JObject fireInput = new JObject
                {
                    ["MessageType"] = "PlayerAction",
                    ["ActionType"] = "Shoot",
                    ["PlayerID"] = _playerId,
                    ["WeaponType"] = 0, // 武器の種類など (例として0)
                    ["PosX"] = transform.position.x,
                    ["PosY"] = transform.position.y,
                    ["Angle"] = transform.rotation.eulerAngles.z, // 2Dゲームを想定
                    ["Timestamp"] = System.DateTime.UtcNow.Ticks
                };
                _networkManager.SendUdpInput(fireInput, DeliveryMethod.Unreliable);
            }
        }
    }
}
