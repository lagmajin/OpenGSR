using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using OpenGSCore;

namespace OpenGS
{
    /// <summary>
    /// キャラクターサムネイルアイテムクラス
    /// キャラクター選択ダイアログの左側リストに表示されるサムネイル
    /// </summary>
    public class CharacterThumbnailItem : MonoBehaviour
    {
        // ─── UI要素 ─────────────────────────────────────────────────

        [SerializeField] private Image thumbnailImage;
        [SerializeField] private Image selectedBorder;
        [SerializeField] private Image lockedOverlay;
        [SerializeField] private TextMeshProUGUI characterNameText;
        [SerializeField] private Button selectButton;

        // ─── 内部状態 ───────────────────────────────────────────────

        private EPlayerCharacter character;
        private Action<EPlayerCharacter> onSelected;
        private bool isSelected = false;
        private bool isLocked = false;

        // ─── 初期化 ─────────────────────────────────────────────────

        /// <summary>
        /// サムネイルアイテムをセットアップする
        /// </summary>
        /// <param name="character">キャラクター</param>
        /// <param name="thumbnail">サムネイル画像</param>
        /// <param name="onSelectedCallback">選択時のコールバック</param>
        /// <param name="locked">ロック状態</param>
        public void Setup(EPlayerCharacter character, Sprite thumbnail, Action<EPlayerCharacter> onSelectedCallback, bool locked = false)
        {
            this.character = character;
            this.onSelected = onSelectedCallback;
            this.isLocked = locked;

            UpdateUI(thumbnail);
            SetupListeners();
        }

        /// <summary>
        /// UIを更新する
        /// </summary>
        private void UpdateUI(Sprite thumbnail)
        {
            // サムネイル画像
            if (thumbnailImage != null)
            {
                thumbnailImage.sprite = thumbnail;
            }

            // キャラクター名
            if (characterNameText != null)
            {
                characterNameText.text = GetCharacterName(character);
            }

            // 選択ボーダーを非表示
            if (selectedBorder != null)
            {
                selectedBorder.gameObject.SetActive(false);
            }

            // ロックオーバーレイ
            if (lockedOverlay != null)
            {
                lockedOverlay.gameObject.SetActive(isLocked);
            }
        }

        /// <summary>
        /// リスナーを設定する
        /// </summary>
        private void SetupListeners()
        {
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(OnSelectButtonClicked);
            }
        }

        // ─── イベントハンドラ ─────────────────────────────────────────

        private void OnSelectButtonClicked()
        {
            if (isLocked)
            {
                Debug.Log($"[CharacterThumbnailItem] キャラクターはロックされています: {character}");
                return;
            }

            // 選択状態を切り替え
            isSelected = !isSelected;

            if (selectedBorder != null)
            {
                selectedBorder.gameObject.SetActive(isSelected);
            }

            // コールバックを発火
            if (isSelected)
            {
                onSelected?.Invoke(character);
            }
        }

        // ─── 公開メソッド ───────────────────────────────────────────

        /// <summary>
        /// 選択状態を設定する
        /// </summary>
        /// <param name="selected">選択状態</param>
        public void SetSelected(bool selected)
        {
            isSelected = selected;

            if (selectedBorder != null)
            {
                selectedBorder.gameObject.SetActive(isSelected);
            }
        }

        /// <summary>
        /// ロック状態を設定する
        /// </summary>
        /// <param name="locked">ロック状態</param>
        public void SetLocked(bool locked)
        {
            isLocked = locked;

            if (lockedOverlay != null)
            {
                lockedOverlay.gameObject.SetActive(isLocked);
            }
        }

        /// <summary>
        /// キャラクターを取得する
        /// </summary>
        /// <returns>キャラクター</returns>
        public EPlayerCharacter GetCharacter()
        {
            return character;
        }

        /// <summary>
        /// 選択状態を取得する
        /// </summary>
        /// <returns>選択状態</returns>
        public bool IsSelected()
        {
            return isSelected;
        }

        /// <summary>
        /// ロック状態を取得する
        /// </summary>
        /// <returns>ロック状態</returns>
        public bool IsLocked()
        {
            return isLocked;
        }

        // ─── ユーティリティ ─────────────────────────────────────────

        /// <summary>
        /// キャラクター名を取得する
        /// </summary>
        private string GetCharacterName(EPlayerCharacter character)
        {
            return CharacterVisualResolver.GetDisplayName(character);
        }
    }
}
