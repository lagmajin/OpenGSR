using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using OpenGSCore;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class WaitRoomChatTextBox : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private TMP_InputField displayTmpInputField;
        [SerializeField] private InputField displayLegacyInputField;
        [SerializeField] private bool displayIsReadOnly = true;
        [SerializeField] private int maxLines = 100;

        [Header("Optional Send Input")]
        [SerializeField] private TMP_InputField sendTmpInputField;
        [SerializeField] private InputField sendLegacyInputField;

        [Header("Send Limits")]
        [SerializeField] private int maxMessageLength = 200;
        [SerializeField] private float sendCooldownSeconds = 0.5f;

        [Header("Network")]
        [SerializeField] private WaitRoomNetworkManager networkManager;
        [SerializeField] private bool subscribeToNetworkChat = true;

        private IDisposable chatSubscription;
        private GeneralServerNetworkManager generalServerManager;
        private Coroutine chatBindRoutine;
        private float nextSendTime;

        private void OnValidate()
        {
            maxLines = Mathf.Max(1, maxLines);
            maxMessageLength = Mathf.Max(1, maxMessageLength);
            sendCooldownSeconds = Mathf.Max(0f, sendCooldownSeconds);
        }

        private void Awake()
        {
            maxLines = Mathf.Max(1, maxLines);
            maxMessageLength = Mathf.Max(1, maxMessageLength);
            sendCooldownSeconds = NormalizeNonNegative(sendCooldownSeconds);
            AutoBindMissingReferences();
            ConfigureDisplayFields();
        }

        private void OnEnable()
        {
            chatBindRoutine = StartCoroutine(BindChatStreamWhenReady());
        }

        private void OnDisable()
        {
            if (chatBindRoutine != null)
            {
                StopCoroutine(chatBindRoutine);
                chatBindRoutine = null;
            }
            UnbindChatStream();
            nextSendTime = 0f;
        }

        private IEnumerator BindChatStreamWhenReady()
        {
            while (isActiveAndEnabled && chatSubscription == null)
            {
                BindChatStream();
                if (chatSubscription != null)
                {
                    chatBindRoutine = null;
                    yield break;
                }

                yield return null;
            }

            chatBindRoutine = null;
        }

        public void AppendChatLine(string playerName, string message)
        {
            var safePlayerName = string.IsNullOrWhiteSpace(playerName) ? "Player" : Sanitize(playerName);
            var safeMessage = Sanitize(message);
            if (safeMessage.Length > maxMessageLength)
            {
                safeMessage = safeMessage.Substring(0, maxMessageLength);
            }

            if (safeMessage.Length == 0)
            {
                return;
            }
            AppendRawLine(safePlayerName + ":" + safeMessage);
        }

        public void AppendRawLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            var currentText = GetDisplayText();
            var nextText = string.IsNullOrEmpty(currentText) ? line : currentText + Environment.NewLine + line;
            nextText = TrimLines(nextText, maxLines);
            SetDisplayText(nextText);
        }

        public void Clear()
        {
            SetDisplayText(string.Empty);
        }

        public void SendCurrentInput()
        {
            var message = GetSendInputText();
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            var now = Time.unscaledTime;
            if (!float.IsFinite(now) || now < 0f)
            {
                return;
            }

            if (now < nextSendTime)
            {
                return;
            }

            message = Sanitize(message).Trim();
            if (maxMessageLength > 0 && message.Length > maxMessageLength)
            {
                message = message.Substring(0, maxMessageLength);
            }

            if (message.Length == 0)
            {
                return;
            }

            if (networkManager == null)
            {
                Debug.LogWarning("[WaitRoomChatTextBox] WaitRoomNetworkManager is not assigned.");
                return;
            }

            var profile = AccountManager.Instance?.CurrentProfile;
            var playerName = profile?.DisplayName;
            if (string.IsNullOrWhiteSpace(playerName))
            {
                playerName = "Player";
            }

            var playerId = profile?.GlobalUserId;
            if (string.IsNullOrWhiteSpace(playerId))
            {
                playerId = "local_player";
            }

            networkManager.SendWaitRoomChat(playerId, playerName, message);
            nextSendTime = now + Mathf.Max(0f, sendCooldownSeconds);
            ClearSendInputText();
        }

        private void HandleChatMessage(JObject json)
        {
            if (json == null)
            {
                return;
            }

            var playerName = json["PlayerName"]?.ToString();
            var message = json["Message"]?.ToString();
            AppendChatLine(playerName, message);
        }

        private void BindChatStream()
        {
            if (!subscribeToNetworkChat || networkManager == null || chatSubscription != null)
            {
                if (!subscribeToNetworkChat || chatSubscription != null || networkManager != null)
                {
                    return;
                }

                try
                {
                    generalServerManager = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
                }
                catch
                {
                    generalServerManager = null;
                }

                if (generalServerManager == null)
                {
                    return;
                }

                chatSubscription = generalServerManager.DataReceivedStream
                    .Where(IsLobbyChatMessage)
                    .ObserveOnMainThread()
                    .Subscribe(HandleChatMessage);
                return;
            }

            chatSubscription = networkManager.OnChatMessageStream
                .ObserveOnMainThread()
                .Subscribe(HandleChatMessage);
        }

        private static bool IsLobbyChatMessage(JObject json)
        {
            var messageType = MessageType.Normalize(json?["MessageType"]?.ToString());
            return messageType == MessageType.LobbyChatNotification
                || messageType == MessageType.LobbyChat;
        }

        private void UnbindChatStream()
        {
            if (chatSubscription == null)
            {
                return;
            }

            chatSubscription.Dispose();
            chatSubscription = null;
        }

        private void AutoBindMissingReferences()
        {
            if (displayTmpInputField == null)
            {
                displayTmpInputField = GetComponent<TMP_InputField>();
            }

            if (displayLegacyInputField == null)
            {
                displayLegacyInputField = GetComponent<InputField>();
            }

            if (networkManager == null)
            {
                networkManager = GetComponentInParent<WaitRoomNetworkManager>();
                if (networkManager == null)
                {
                    networkManager = FindFirstObjectByType<WaitRoomNetworkManager>();
                }
            }
        }

        private void ConfigureDisplayFields()
        {
            if (displayTmpInputField != null)
            {
                displayTmpInputField.lineType = TMP_InputField.LineType.MultiLineNewline;
                displayTmpInputField.readOnly = displayIsReadOnly;
            }

            if (displayLegacyInputField != null)
            {
                displayLegacyInputField.lineType = InputField.LineType.MultiLineNewline;
                displayLegacyInputField.readOnly = displayIsReadOnly;
            }

            if (sendTmpInputField != null)
            {
                sendTmpInputField.characterLimit = maxMessageLength;
            }

            if (sendLegacyInputField != null)
            {
                sendLegacyInputField.characterLimit = maxMessageLength;
            }
        }

        private string GetDisplayText()
        {
            if (displayTmpInputField != null)
            {
                return displayTmpInputField.text;
            }

            if (displayLegacyInputField != null)
            {
                return displayLegacyInputField.text;
            }

            return string.Empty;
        }

        private void SetDisplayText(string value)
        {
            if (displayTmpInputField != null)
            {
                displayTmpInputField.text = value;
            }

            if (displayLegacyInputField != null)
            {
                displayLegacyInputField.text = value;
            }
        }

        private string GetSendInputText()
        {
            if (sendTmpInputField != null)
            {
                return sendTmpInputField.text;
            }

            if (sendLegacyInputField != null)
            {
                return sendLegacyInputField.text;
            }

            return string.Empty;
        }

        private void ClearSendInputText()
        {
            if (sendTmpInputField != null)
            {
                sendTmpInputField.text = string.Empty;
            }

            if (sendLegacyInputField != null)
            {
                sendLegacyInputField.text = string.Empty;
            }
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
        }

        private static string TrimLines(string text, int maxLineCount)
        {
            if (maxLineCount <= 0 || string.IsNullOrEmpty(text))
            {
                return text;
            }

            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            if (lines.Length <= maxLineCount)
            {
                return text;
            }

            var trimmedLines = new List<string>(maxLineCount);
            for (var i = lines.Length - maxLineCount; i < lines.Length; i++)
            {
                trimmedLines.Add(lines[i]);
            }

            return string.Join(Environment.NewLine, trimmedLines);
        }

        private static float NormalizeNonNegative(float value)
        {
            return float.IsFinite(value) ? Mathf.Max(0f, value) : 0f;
        }
    }
}
