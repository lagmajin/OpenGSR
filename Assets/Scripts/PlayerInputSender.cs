using UnityEngine;
using Newtonsoft.Json.Linq;
using LiteNetLib; // DeliveryMethodのために必要

namespace OpenGS
{
    public class PlayerInputSender : MonoBehaviour
    {
        private ClientNetworkManager _networkManager;
        private string _playerId;
        [SerializeField] private KeyCode grenadeKey = KeyCode.G;

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
                var aimDirection = GetAimDirection();
                _networkManager.SendShootRequest(transform.position, aimDirection, ResolveWeaponType());
            }

            if (Input.GetKeyDown(grenadeKey))
            {
                var aimDirection = GetAimDirection();
                _networkManager.SendGrenadeThrow(transform.position, aimDirection, ResolveGrenadeType());
            }
        }

        private Vector2 GetAimDirection()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return transform.localScale.x < 0f ? Vector2.left : Vector2.right;
            }

            var screenPos = camera.WorldToScreenPoint(transform.position);
            var worldMouse = camera.ScreenToWorldPoint(Input.mousePosition);
            var dir = (Vector2)(worldMouse - transform.position);
            if (dir.sqrMagnitude <= Mathf.Epsilon)
            {
                dir = transform.localScale.x < 0f ? Vector2.left : Vector2.right;
            }

            return dir.normalized;
        }

        private string ResolveWeaponType()
        {
            var weaponSlots = GetComponentInChildren<WeaponSlots>();
            var currentGun = weaponSlots != null ? weaponSlots.GetCurrentGun() : null;
            return currentGun != null ? currentGun.Name : "Unknown";
        }

        private string ResolveGrenadeType()
        {
            var grenadeComponent = GetComponent<PlayerGrenadeComponent>();
            return grenadeComponent != null ? grenadeComponent.CurrentGrenadeType.ToString() : "Normal";
        }
    }
}
