#nullable enable
using System;
using System.Collections.Generic;
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

        /// <summary> UDPメッセージ受信用コールバック</summary>
        private Action<byte[]>? m_UdpReceiveCallback;

        /// <summary> 受信したプレイヤー位置データのバッファ</summary>
        private readonly Dictionary<string, Queue<PlayerPositionUpdateMessage>> m_PositionBuffer = new Dictionary<string, Queue<PlayerPositionUpdateMessage>>();

        /// <summary> 位置更新イベント</summary>
        public event Action<string, Vector3, Quaternion>? OnPositionReceived;

        /// <summary> 有効かどうか</summary>
        private bool m_IsEnabled = false;

        /// <summary> 最後に受信した時刻</summary>
        private float m_LastReceiveTime = 0f;

        /// <summary> 受信メッセージ数</summary>
        private int m_ReceivedMessageCount = 0;

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
            Debug.Log("[ClientPositionReceiver] Enabled");
        }

        /// <summary>
        /// 位置同期を無効にする
        /// </summary>
        public void Disable()
        {
            m_IsEnabled = false;
            Debug.Log($"[ClientPositionReceiver] Disabled. Received {m_ReceivedMessageCount} messages");
        }

        /// <summary>
        /// UDP受信コールバックを登録する
        /// </summary>
        public void SetUdpReceiveCallback(Action<byte[]> callback)
        {
            m_UdpReceiveCallback = callback;
        }

        /// <summary>
        /// 更新処理（毎フレーム呼び出し）
        /// </summary>
        private void Update()
        {
            if (!m_IsEnabled) return;

            // バッファに溜まった位置データを補間システムに送信
            ProcessPositionBuffer();
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

            m_ReceivedMessageCount++;
            m_LastReceiveTime = Time.time;
        }

        /// <summary>
        /// バッファに追加する
        /// </summary>
        private void AddToBuffer(string playerId, PlayerPositionUpdateMessage msg)
        {
            if (!m_PositionBuffer.TryGetValue(playerId, out var queue))
            {
                queue = new Queue<PlayerPositionUpdateMessage>();
                m_PositionBuffer[playerId] = queue;
            }

            queue.Enqueue(msg);

            // バッファサイズ制限
            while (queue.Count > 30)
            {
                queue.Dequeue();
            }
        }

        /// <summary>
        /// バッファから位置データを処理する
        /// </summary>
        private void ProcessPositionBuffer()
        {
            foreach (var kvp in m_PositionBuffer)
            {
                string playerId = kvp.Key;
                var queue = kvp.Value;

                // 最新の位置データを適用
                if (queue.Count > 0)
                {
                    var latestMsg = queue.Peek();
                    var position = latestMsg.GetPosition();
                    var rotation = Quaternion.Euler(0, latestMsg.RotationY, 0);

                    OnPositionReceived?.Invoke(playerId, position, rotation);
                }
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

        /// <summary>
        /// プレイヤーを削除する
        /// </summary>
        public void RemovePlayer(string playerId)
        {
            m_PositionBuffer.Remove(playerId);
        }

        /// <summary>
        /// 全てのバッファをクリアする
        /// </summary>
        public void ClearBuffer()
        {
            m_PositionBuffer.Clear();
        }

        /// <summary>
        /// 統計情報を取得
        /// </summary>
        public string GetDebugInfo()
        {
            return $"[ClientPositionReceiver] Enabled:{m_IsEnabled} " +
                   $"Messages:{m_ReceivedMessageCount} " +
                   $"Buffers:{m_PositionBuffer.Count}";
        }
    }
}

