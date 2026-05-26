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
            m_LagCompManager = GetComponent<LagCompensationManager>();
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
            
            var transformState = new TransformState
            {
                networkId = json["NetworkId"]?.Value<uint>() ?? 0,
                playerId = playerId,
                position = new Vector3(json["PositionX"]?.Value<float>() ?? 0, json["PositionY"]?.Value<float>() ?? 0, json["PositionZ"]?.Value<float>() ?? 0),
                rotation = new Quaternion(json["RotationX"]?.Value<float>() ?? 0, json["RotationY"]?.Value<float>() ?? 0, json["RotationZ"]?.Value<float>() ?? 0, json["RotationW"]?.Value<float>() ?? 0),
                velocity = new Vector3(json["VelocityX"]?.Value<float>() ?? 0, json["VelocityY"]?.Value<float>() ?? 0, json["VelocityZ"]?.Value<float>() ?? 0),
                timestamp = json["Timestamp"]?.Value<float>() ?? 0,
                sequenceNumber = json["SequenceNumber"]?.Value<byte>() ?? 0
            };

            // ラグ補償システムに通知
            NotifyLagCompensation(transformState);
        }

        /// <summary>
        /// ラグ補償システムに通知する
        /// </summary>
        private void NotifyLagCompensation(TransformState state)
        {
            if (m_LagCompManager == null) return;
            m_LagCompManager.OnPlayerStateReceived(state);
        }

    }
}

