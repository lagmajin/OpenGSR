#nullable enable
using System;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace OpenGS.Network
{
    /// <summary>
    /// クライアント側位置同期受信システム
    /// サーバーからの位置更新を受信し、補間システムに渡す
    /// </summary>
    public class ClientPositionReceiver : MonoBehaviour
    {
        /// <summary> LagCompensationManagerへの参照</summary>
        private LagCompensationManager? m_LagCompManager;

        /// <summary> 有効かどうか</summary>
        private bool m_IsEnabled = false;

        private void Awake()
        {
            ResolveLagCompensationManager();
        }

        /// <summary>
        /// 位置同期を有効にする
        /// </summary>
        public void Enable()
        {
            m_IsEnabled = true;
        }

        /// <summary>
        /// 位置同期を無効にする
        /// </summary>
        public void Disable()
        {
            m_IsEnabled = false;
        }

        /// <summary>
        /// UDPメッセージを受信したときの処理
        /// </summary>
        public void OnUdpMessageReceived(byte[] data)
        {
            if (!m_IsEnabled) return;

            ResolveLagCompensationManager();

            try
            {
                var jsonString = System.Text.Encoding.UTF8.GetString(data);
                var json = JObject.Parse(jsonString);

                var messageType = json["MessageType"]?.ToString();

                // サーバーから送られてくるTransformStateを処理
                if (messageType == "ServerTransformState")
                {
                    ProcessServerTransformState(json);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ClientPositionReceiver] Parse error: {ex.Message}");
            }
        }

        /// <summary>
        /// サーバーからのServerTransformStateメッセージを処理する
        /// </summary>
        private void ProcessServerTransformState(JObject json)
        {
            var playerId = json["PlayerId"]?.ToString();
            if (string.IsNullOrEmpty(playerId)) return;
            
            var rotation = new Quaternion(
                ReadFinite(json, "RotationX", 0f),
                ReadFinite(json, "RotationY", 0f),
                ReadFinite(json, "RotationZ", 0f),
                ReadFinite(json, "RotationW", 1f));
            if (QuaternionMath.SqrMagnitude(rotation) < 0.0001f)
            {
                rotation = Quaternion.identity;
            }
            else
            {
                rotation = Quaternion.Normalize(rotation);
            }

            var transformState = new TransformState
            {
                networkId = ReadUInt(json, "NetworkId"),
                playerId = playerId,
                position = new Vector3(
                    ReadFinite(json, "PositionX"),
                    ReadFinite(json, "PositionY"),
                    ReadFinite(json, "PositionZ")),
                rotation = rotation,
                velocity = new Vector3(
                    ReadFinite(json, "VelocityX"),
                    ReadFinite(json, "VelocityY"),
                    ReadFinite(json, "VelocityZ")),
                // NetworkInterpolation uses the local Unity clock for render delay.
                timestamp = float.IsFinite(Time.time) && Time.time >= 0f ? Time.time : 0f,
                sequenceNumber = json["SequenceNumber"]?.Value<byte>() ?? 0
            };

            if (!IsFinite(transformState.position)
                || !IsFinite(transformState.velocity)
                || !IsFinite(transformState.rotation))
            {
                Debug.LogWarning($"[ClientPositionReceiver] Ignoring non-finite transform state for player '{playerId}'.");
                return;
            }

            // ラグ補償システムに通知
            NotifyLagCompensation(transformState);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(Quaternion value)
        {
            return IsFinite(value.x) && IsFinite(value.y)
                && IsFinite(value.z) && IsFinite(value.w);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static float ReadFinite(JObject json, string key, float fallback = 0f)
        {
            if (json == null || string.IsNullOrWhiteSpace(key))
            {
                return fallback;
            }

            try
            {
                var value = json[key]?.Value<float>();
                return value.HasValue && IsFinite(value.Value) ? value.Value : fallback;
            }
            catch
            {
                return fallback;
            }
        }

        private static uint ReadUInt(JObject json, string key)
        {
            if (json == null || string.IsNullOrWhiteSpace(key))
            {
                return 0;
            }

            try
            {
                return json[key]?.Value<uint>() ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// ラグ補償システムに通知する
        /// </summary>
        private void NotifyLagCompensation(TransformState state)
        {
            if (m_LagCompManager == null) return;
            m_LagCompManager.OnPlayerStateReceived(state);
        }

        private void ResolveLagCompensationManager()
        {
            if (m_LagCompManager != null)
            {
                return;
            }

            m_LagCompManager = GetComponent<LagCompensationManager>()
                ?? LagCompensationManager.Instance
                ?? FindFirstObjectByType<LagCompensationManager>();
        }

    }
}

