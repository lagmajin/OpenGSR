using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

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

        [Header("Network")]
        [SerializeField] private WaitRoomNetworkManager networkManager;
        [SerializeField] private bool subscribeToNetworkChat = true;

        private IDisposable chatSubscription;

        private void Awake()
        {
            AutoBindMissingReferences();
            ConfigureDisplayFields();
        }

        private void OnEnable()
        {
            BindChatStream();
        }

        private void OnDisable()
        {
            UnbindChatStream();
        }

        public void AppendChatLine(string playerName, string message)
        {
            var safePlayerName = string.IsNullOrWhiteSpace(playerName) ? "Player" : Sanitize(playerName);
            var safeMessage = Sanitize(message);
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

            if (networkManager == null)
            {
                Debug.LogWarning("[WaitRoomChatTextBox] WaitRoomNetworkManager is not assigned.");
                return;
            }

            var playerName = AccountManager.Instance.CurrentProfile.DisplayName;
            if (string.IsNullOrWhiteSpace(playerName))
            {
                playerName = "Player";
            }

            var playerId = AccountManager.Instance.CurrentProfile.GlobalUserId;
            if (string.IsNullOrWhiteSpace(playerId))
            {
                playerId = "local_player";
            }

            networkManager.SendWaitRoomChat(playerId, playerName, message);
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
                return;
            }

            chatSubscription = networkManager.OnChatMessageStream
                .ObserveOnMainThread()
                .Subscribe(HandleChatMessage);
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
    }
}
