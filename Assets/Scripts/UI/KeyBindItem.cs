using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace OpenGS
{
    /// <summary>
    /// キーバインドアイテムクラス
    /// キーバインドリストの各アイテムを表示する
    /// </summary>
    public class KeyBindItem : MonoBehaviour
    {
        // ─── UI要素 ─────────────────────────────────────────────────

        [SerializeField] private TextMeshProUGUI actionNameText;
        [SerializeField] private Button keyButton;
        [SerializeField] private TextMeshProUGUI keyText;
        [SerializeField] private Image backgroundImage;

        // ─── 色設定 ─────────────────────────────────────────────────

        [Header("色設定")]
        [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f);
        [SerializeField] private Color hoverColor = new Color(0.3f, 0.3f, 0.3f);
        [SerializeField] private Color selectedColor = new Color(0.4f, 0.6f, 1.0f);

        // ─── 内部状態 ───────────────────────────────────────────────

        private string action;
        private string currentKey;
        private Action<string, string> onKeyChanged;
        private bool isWaitingForKey = false;

        // ─── 初期化 ─────────────────────────────────────────────────

        /// <summary>
        /// キーバインドアイテムをセットアップする
        /// </summary>
        /// <param name="action">アクション名</param>
        /// <param name="key">現在のキー</param>
        /// <param name="onKeyChangedCallback">キー変更時のコールバック</param>
        public void Setup(string action, string key, Action<string, string> onKeyChangedCallback)
        {
            this.action = action;
            this.currentKey = key;
            this.onKeyChanged = onKeyChangedCallback;

            UpdateUI();
            SetupListeners();
        }

        /// <summary>
        /// UIを更新する
        /// </summary>
        private void UpdateUI()
        {
            // アクション名
            if (actionNameText != null)
            {
                actionNameText.text = GetActionDisplayName(action);
            }

            // キー名
            if (keyText != null)
            {
                keyText.text = GetKeyDisplayName(currentKey);
            }

            // 背景色
            if (backgroundImage != null)
            {
                backgroundImage.color = normalColor;
            }
        }

        /// <summary>
        /// リスナーを設定する
        /// </summary>
        private void SetupListeners()
        {
            if (keyButton != null)
            {
                keyButton.onClick.AddListener(OnKeyButtonClicked);
            }
        }

        // ─── イベントハンドラ ─────────────────────────────────────────

        private void OnKeyButtonClicked()
        {
            if (isWaitingForKey) return;

            isWaitingForKey = true;

            if (backgroundImage != null)
            {
                backgroundImage.color = selectedColor;
            }

            if (keyText != null)
            {
                keyText.text = "キーを押してください...";
            }

            // キー入力を待機
            StartCoroutine(WaitForKeyInput());
        }

        private System.Collections.IEnumerator WaitForKeyInput()
        {
            while (isWaitingForKey)
            {
                // マウスボタンのチェック
                for (int i = 0; i < 3; i++)
                {
                    if (Input.GetMouseButtonDown(i))
                    {
                        string mouseKey = $"Mouse{i}";
                        SetKey(mouseKey);
                        yield break;
                    }
                }

                // キーボードのチェック
                foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKeyDown(keyCode))
                    {
                        // Escキーでキャンセル
                        if (keyCode == KeyCode.Escape)
                        {
                            CancelKeyInput();
                            yield break;
                        }

                        SetKey(keyCode.ToString());
                        yield break;
                    }
                }

                yield return null;
            }
        }

        private void SetKey(string newKey)
        {
            currentKey = newKey;
            isWaitingForKey = false;

            if (backgroundImage != null)
            {
                backgroundImage.color = normalColor;
            }

            if (keyText != null)
            {
                keyText.text = GetKeyDisplayName(newKey);
            }

            onKeyChanged?.Invoke(action, newKey);
            Debug.Log($"[KeyBindItem] キーを設定しました: {action} = {newKey}");
        }

        private void CancelKeyInput()
        {
            isWaitingForKey = false;

            if (backgroundImage != null)
            {
                backgroundImage.color = normalColor;
            }

            if (keyText != null)
            {
                keyText.text = GetKeyDisplayName(currentKey);
            }

            Debug.Log("[KeyBindItem] キー入力をキャンセルしました");
        }

        // ─── ユーティリティ ─────────────────────────────────────────

        /// <summary>
        /// アクションの表示名を取得する
        /// </summary>
        private string GetActionDisplayName(string action)
        {
            switch (action)
            {
                case "MoveForward": return "前進";
                case "MoveBackward": return "後退";
                case "MoveLeft": return "左移動";
                case "MoveRight": return "右移動";
                case "Jump": return "ジャンプ";
                case "Crouch": return "しゃがみ";
                case "Sprint": return "ダッシュ";
                case "Fire": return "攻撃";
                case "Aim": return "照準";
                case "Reload": return "リロード";
                case "Interact": return "インタラクト";
                case "Inventory": return "インベントリ";
                case "Map": return "マップ";
                case "Scoreboard": return "スコアボード";
                case "Chat": return "チャット";
                default: return action;
            }
        }

        /// <summary>
        /// キーの表示名を取得する
        /// </summary>
        private string GetKeyDisplayName(string key)
        {
            switch (key)
            {
                case "Mouse0": return "左クリック";
                case "Mouse1": return "右クリック";
                case "Mouse2": return "中クリック";
                case "LeftControl": return "左Ctrl";
                case "RightControl": return "右Ctrl";
                case "LeftShift": return "左Shift";
                case "RightShift": return "右Shift";
                case "LeftAlt": return "左Alt";
                case "RightAlt": return "右Alt";
                case "Space": return "スペース";
                case "Return": return "Enter";
                case "Backspace": return "Backspace";
                case "Delete": return "Delete";
                case "Tab": return "Tab";
                case "Escape": return "Esc";
                default: return key;
            }
        }

        // ─── 公開メソッド ───────────────────────────────────────────

        /// <summary>
        /// アクション名を取得する
        /// </summary>
        public string GetAction()
        {
            return action;
        }

        /// <summary>
        /// 現在のキーを取得する
        /// </summary>
        public string GetCurrentKey()
        {
            return currentKey;
        }
    }
}