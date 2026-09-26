using UnityEngine;
using Newtonsoft.Json.Linq;
using LiteNetLib; // DeliveryMethodのために必要
using Zenject;

namespace OpenGS
{
    public class PlayerInputSender : MonoBehaviour
    {
        private ClientNetworkManager _networkManager;
        private string _playerId;
        [Inject] private IInputService inputService;

        private float _lastMoveSendTime;
        private byte _moveSequenceNumber;
        private bool _positionSynchronized;
        private float _nextNetworkManagerLookupTime;
        [SerializeField] private float moveSendInterval = 0.05f; // 20回/秒

        private void OnValidate()
        {
            if (!float.IsFinite(moveSendInterval)) moveSendInterval = 0.05f;
            moveSendInterval = Mathf.Max(0.01f, moveSendInterval);
        }

        private void Awake()
        {
            if (!float.IsFinite(moveSendInterval)) moveSendInterval = 0.05f;
            moveSendInterval = Mathf.Max(0.01f, moveSendInterval);

            if (inputService == null)
            {
                inputService = new UnityInputService();
                Debug.LogWarning("[PlayerInputSender] IInputService was not injected. Falling back to UnityInputService.");
            }

            TryResolveNetworkManager(true);
        }

        private void OnDestroy()
        {
            if (inputService is VirtualInputService virtualInput)
            {
                virtualInput.ReleaseAll();
            }
            if (_networkManager != null)
            {
                _networkManager.MatchUdpConnectionChanged -= OnMatchUdpConnectionChanged;
            }
        }

        private void OnDisable()
        {
            ReleaseTransientInputState();
        }

        private void OnEnable()
        {
            _positionSynchronized = false;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                ReleaseTransientInputState();
            }
            else
            {
                _positionSynchronized = false;
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                ReleaseTransientInputState();
            }
            else
            {
                _positionSynchronized = false;
            }
        }

        private void ReleaseTransientInputState()
        {
            _positionSynchronized = false;
            if (inputService is VirtualInputService virtualInput)
            {
                virtualInput.ReleaseAll();
            }
        }

        private void OnMatchUdpConnectionChanged(bool connected, string reason)
        {
            if (!connected)
            {
                _positionSynchronized = false;
                if (inputService is VirtualInputService virtualInput)
                {
                    virtualInput.ReleaseAll();
                }
            }
        }

        private void Update()
        {
            TryResolveNetworkManager();

            if (_networkManager != null && !_networkManager.IsMatchUdpConnected)
            {
                _positionSynchronized = false;
            }

            SendMovementInput();
        }

        private void TryResolveNetworkManager(bool force = false)
        {
            var now = Time.unscaledTime;
            if (!force && IsFinite(now) && now < _nextNetworkManagerLookupTime)
            {
                return;
            }

            _nextNetworkManagerLookupTime = IsFinite(now) ? now + 1f : 1f;
            if (_networkManager != null)
            {
                return;
            }

            _networkManager = FindFirstObjectByType<ClientNetworkManager>();
            if (_networkManager == null)
            {
                return;
            }

            _playerId = _networkManager.ClientPlayerId;
            _networkManager.MatchUdpConnectionChanged -= OnMatchUdpConnectionChanged;
            _networkManager.MatchUdpConnectionChanged += OnMatchUdpConnectionChanged;
        }

        private void SendMovementInput()
        {
            // Login can replace the provisional client id. Keep movement
            // input associated with the authenticated player after that
            // response arrives.
            if (_networkManager != null && !string.IsNullOrWhiteSpace(_networkManager.ClientPlayerId)
                && _playerId != _networkManager.ClientPlayerId)
            {
                _playerId = _networkManager.ClientPlayerId;
                _positionSynchronized = false;
            }

            if (string.IsNullOrWhiteSpace(_playerId))
            {
                _playerId = _networkManager != null ? _networkManager.ClientPlayerId : string.Empty;
                if (string.IsNullOrWhiteSpace(_playerId))
                {
                    return;
                }
            }

            if (_networkManager == null)
            {
                return;
            }

            if (!_networkManager.IsMatchUdpConnected)
            {
                return;
            }

            bool needsPositionSync = !_positionSynchronized;

            var now = Time.time;
            if (!IsFinite(now) || now < 0f)
            {
                return;
            }

            if (!needsPositionSync && now - _lastMoveSendTime < moveSendInterval)
            {
                return;
            }

            float horizontalInput = inputService != null ? inputService.GetHorizontalAxis() : Input.GetAxis("Horizontal");
            float verticalInput = inputService != null ? inputService.GetVerticalAxis() : Input.GetAxis("Vertical");
            if (!IsFinite(transform.position) || !IsFinite(horizontalInput) || !IsFinite(verticalInput))
            {
                Debug.LogWarning("[PlayerInputSender] Skipping movement packet with non-finite state.");
                return;
            }

            horizontalInput = Mathf.Clamp(horizontalInput, -1f, 1f);
            verticalInput = Mathf.Clamp(verticalInput, -1f, 1f);

            var deltaTime = Mathf.Clamp(now - _lastMoveSendTime, 0.001f, 0.25f);
            if (!IsFinite(deltaTime))
            {
                return;
            }
            _moveSequenceNumber++;

            // 現在の位置と、入力から予測される速度をJObjectにまとめる
            JObject moveInput = new JObject
            {
                ["MessageType"] = "PlayerMove",
                ["PlayerID"] = _playerId,
                ["PosX"] = transform.position.x,
                ["PosY"] = transform.position.y,
                ["PosZ"] = transform.position.z,
                ["VelX"] = horizontalInput, // 簡単な例として入力値を速度として扱う
                ["VelY"] = verticalInput,
                ["SequenceNumber"] = _moveSequenceNumber,
                ["DeltaTime"] = deltaTime,
                ["Timestamp"] = now
            };

            var sent = _networkManager.SendUdpInput(
                moveInput,
                needsPositionSync ? DeliveryMethod.ReliableOrdered : DeliveryMethod.Unreliable);
            if (sent)
            {
                _lastMoveSendTime = now;
                if (needsPositionSync)
                {
                    _positionSynchronized = true;
                }
            }
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

    }
}
