using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;
using OpenGSCore;

namespace OpenGS
{
    /// <summary>
    /// キャラクター選択ダイアログクラス
    /// 左側にサムネイル一覧、右側に大きなキャラクター画像を表示
    /// </summary>
    public class CharacterSelectDialog : MonoBehaviour
    {
        // ─── UI要素 ─────────────────────────────────────────────────

        [Header("サムネイルリスト")]
        [SerializeField] private Transform thumbnailListContent;
        [SerializeField] private GameObject thumbnailItemPrefab;
        [SerializeField] private ScrollRect thumbnailScrollRect;
        
        [Header("キャラクタープレビュー")]
        [SerializeField] private Image largeCharacterImage;
        [SerializeField] private TextMeshProUGUI characterNameText;
        [SerializeField] private TextMeshProUGUI characterDescriptionText;
        
        [Header("キャラクター情報")]
        [SerializeField] private TextMeshProUGUI characterStatsText;
        [SerializeField] private Image[] starImages; // 評価スター
        
        [Header("ボタン")]
        [SerializeField] private Button okButton;
        [SerializeField] private Button cancelButton;
        
        [Header("エラー/ステータス")]
        [SerializeField] private TextMeshProUGUI statusText;

        // ─── 内部状態 ───────────────────────────────────────────────

        private List<EPlayerCharacter> availableCharacters = new List<EPlayerCharacter>();
        private EPlayerCharacter selectedCharacter = EPlayerCharacter.Misty;
        private Dictionary<EPlayerCharacter, Sprite> characterSprites = new Dictionary<EPlayerCharacter, Sprite>();
        private Dictionary<EPlayerCharacter, Sprite> characterThumbnails = new Dictionary<EPlayerCharacter, Sprite>();
        private List<CharacterThumbnailItem> thumbnailItems = new List<CharacterThumbnailItem>();

        // ─── デリゲート ─────────────────────────────────────────────

        public Action<EPlayerCharacter> OnCharacterSelected;
        public Action OnDialogClosed;

        // ─── Unity ライフサイクル ────────────────────────────────────

        private void Awake()
        {
            InitializeUI();
            SetupListeners();
            LoadCharacterData();
        }

        private void OnEnable()
        {
            RefreshCharacterList();
            UpdateCharacterPreview(selectedCharacter);
        }

        // ─── 初期化 ─────────────────────────────────────────────────

        /// <summary>
        /// UI要素を初期化する
        /// </summary>
        private void InitializeUI()
        {
            // 利用可能なキャラクターを設定
            availableCharacters = new List<EPlayerCharacter>
            {
                EPlayerCharacter.Ami,
                EPlayerCharacter.Yumi,
                EPlayerCharacter.Jack,
                EPlayerCharacter.Jackle,
                EPlayerCharacter.Misty,
                EPlayerCharacter.Liu,
                EPlayerCharacter.Mary,
                EPlayerCharacter.Wolf,
                EPlayerCharacter.Wyvern,
                EPlayerCharacter.Seoul,
                EPlayerCharacter.LittleJ,
                EPlayerCharacter.Shue,
                EPlayerCharacter.Swaltz
            };

            // エラーテキストをクリア
            if (statusText != null)
            {
                statusText.text = "";
                statusText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// リスナーを設定する
        /// </summary>
        private void SetupListeners()
        {
            if (okButton != null)
            {
                okButton.onClick.AddListener(OnOkButtonClicked);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelButtonClicked);
            }
        }

        /// <summary>
        /// キャラクターデータを読み込む
        /// </summary>
        private void LoadCharacterData()
        {
            // Resourcesフォルダからキャラクター画像を読み込む
            // 実際の実装では、Resources.LoadやAddressablesを使用
            foreach (var character in availableCharacters)
            {
                // サムネイル画像
                var thumbnailPath = $"Characters/{character}_Thumbnail";
                var thumbnail = Resources.Load<Sprite>(thumbnailPath);
                if (thumbnail != null)
                {
                    characterThumbnails[character] = thumbnail;
                }

                // プレビュー画像は基本的にサムネイルを拡大表示する
                // もしサムネイルが無い場合だけ従来の大画像を使う
                Sprite previewImage = thumbnail;
                if (previewImage == null)
                {
                    var largeImagePath = $"Characters/{character}_Large";
                    previewImage = Resources.Load<Sprite>(largeImagePath);
                }

                if (previewImage != null)
                {
                    characterSprites[character] = previewImage;
                }
            }
        }

        // ─── 公開メソッド ───────────────────────────────────────────

        /// <summary>
        /// ダイアログを表示する
        /// </summary>
        /// <param name="currentCharacter">現在選択中のキャラクター</param>
        public void Show(EPlayerCharacter currentCharacter = EPlayerCharacter.Misty)
        {
            selectedCharacter = currentCharacter;
            gameObject.SetActive(true);
            RefreshCharacterList();
            UpdateCharacterPreview(selectedCharacter);
        }

        /// <summary>
        /// 利用可能なキャラクターリストを設定する
        /// </summary>
        /// <param name="characters">キャラクターリスト</param>
        public void SetAvailableCharacters(List<EPlayerCharacter> characters)
        {
            availableCharacters = characters ?? new List<EPlayerCharacter>();
            RefreshCharacterList();
        }

        // ─── イベントハンドラ ─────────────────────────────────────────

        private void OnOkButtonClicked()
        {
            Debug.Log($"[CharacterSelectDialog] キャラクター選択: {selectedCharacter}");
            OnCharacterSelected?.Invoke(selectedCharacter);
            CloseDialog();
        }

        private void OnCancelButtonClicked()
        {
            CloseDialog();
        }

        // ─── キャラクターリスト管理 ─────────────────────────────────────

        /// <summary>
        /// キャラクターリストを更新する
        /// </summary>
        private void RefreshCharacterList()
        {
            // リストをクリア
            ClearCharacterList();

            // サムネイルアイテムを生成
            foreach (var character in availableCharacters)
            {
                CreateThumbnailItem(character);
            }

            // 初期選択を更新
            if (thumbnailItems.Count > 0)
            {
                SelectCharacter(selectedCharacter);
            }
        }

        /// <summary>
        /// キャラクターリストをクリアする
        /// </summary>
        private void ClearCharacterList()
        {
            if (thumbnailListContent == null) return;

            foreach (Transform child in thumbnailListContent)
            {
                Destroy(child.gameObject);
            }
            thumbnailItems.Clear();
        }

        /// <summary>
        /// サムネイルアイテムを生成する
        /// </summary>
        private void CreateThumbnailItem(EPlayerCharacter character)
        {
            if (thumbnailItemPrefab == null || thumbnailListContent == null) return;

            var item = Instantiate(thumbnailItemPrefab, thumbnailListContent);
            var itemScript = item.GetComponent<CharacterThumbnailItem>();
            
            if (itemScript != null)
            {
                // サムネイル画像を取得
                Sprite thumbnail = null;
                characterThumbnails.TryGetValue(character, out thumbnail);
                
                itemScript.Setup(character, thumbnail, OnThumbnailClicked);
                thumbnailItems.Add(itemScript);
            }
            else
            {
                // フォールバック：直接UIを設定
                var image = item.GetComponent<Image>();
                if (image != null && characterThumbnails.ContainsKey(character))
                {
                    image.sprite = characterThumbnails[character];
                }

                var button = item.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() => OnThumbnailClicked(character));
                }
            }
        }

        /// <summary>
        /// サムネイルがクリックされたときの処理
        /// </summary>
        private void OnThumbnailClicked(EPlayerCharacter character)
        {
            SelectCharacter(character);
        }

        /// <summary>
        /// キャラクターを選択する
        /// </summary>
        private void SelectCharacter(EPlayerCharacter character)
        {
            selectedCharacter = character;

            // サムネイルの選択状態を更新
            foreach (var item in thumbnailItems)
            {
                item.SetSelected(item.GetCharacter() == character);
            }

            // プレビューを更新
            UpdateCharacterPreview(character);
        }

        // ─── キャラクタープレビュー ─────────────────────────────────────

        /// <summary>
        /// キャラクタープレビューを更新する
        /// </summary>
        private void UpdateCharacterPreview(EPlayerCharacter character)
        {
            // 大きな画像を更新
            if (largeCharacterImage != null)
            {
                if (characterSprites.ContainsKey(character))
                {
                    largeCharacterImage.sprite = characterSprites[character];
                    largeCharacterImage.gameObject.SetActive(true);
                }
                else
                {
                    largeCharacterImage.gameObject.SetActive(false);
                }
            }

            // キャラクター名を更新
            if (characterNameText != null)
            {
                characterNameText.text = GetCharacterName(character);
            }

            // キャラクター説明を更新
            if (characterDescriptionText != null)
            {
                characterDescriptionText.text = GetCharacterDescription(character);
            }

            // ステータスを更新
            if (characterStatsText != null)
            {
                characterStatsText.text = GetCharacterStats(character);
            }

            // 評価スターを更新
            UpdateStarRating(character);
        }

        /// <summary>
        /// キャラクター名を取得する
        /// </summary>
        private string GetCharacterName(EPlayerCharacter character)
        {
            switch (character)
            {
                case EPlayerCharacter.Ami: return "アミ";
                case EPlayerCharacter.Yumi: return "ユミ";
                case EPlayerCharacter.Jack: return "ジャック";
                case EPlayerCharacter.Jackle: return "ジャックル";
                case EPlayerCharacter.Misty: return "ミスティ";
                case EPlayerCharacter.Liu: return "リュウ";
                case EPlayerCharacter.Mary: return "メアリー";
                case EPlayerCharacter.Wolf: return "ウルフ";
                case EPlayerCharacter.Wyvern: return "ワイバーン";
                case EPlayerCharacter.Seoul: return "ソウル";
                case EPlayerCharacter.LittleJ: return "リトルJ";
                case EPlayerCharacter.Shue: return "シュウ";
                case EPlayerCharacter.Swaltz: return "スワルツ";
                default: return character.ToString();
            }
        }

        /// <summary>
        /// キャラクター説明を取得する
        /// </summary>
        private string GetCharacterDescription(EPlayerCharacter character)
        {
            switch (character)
            {
                case EPlayerCharacter.Ami: return "バランス型のキャラクター";
                case EPlayerCharacter.Yumi: return "スピードに優れたキャラクター";
                case EPlayerCharacter.Jack: return "パワー型のキャラクター";
                case EPlayerCharacter.Jackle: return "防御に優れたキャラクター";
                case EPlayerCharacter.Misty: return "テクニック型のキャラクター";
                case EPlayerCharacter.Liu: return "攻撃型のキャラクター";
                case EPlayerCharacter.Mary: return "サポート型のキャラクター";
                case EPlayerCharacter.Wolf: return "アグレッシブなキャラクター";
                case EPlayerCharacter.Wyvern: return "エアリアル戦闘に優れたキャラクター";
                case EPlayerCharacter.Seoul: return "バランス型のキャラクター";
                case EPlayerCharacter.LittleJ: return "小回りの利くキャラクター";
                case EPlayerCharacter.Shue: return "スピード型のキャラクター";
                case EPlayerCharacter.Swaltz: return "テクニカルなキャラクター";
                default: return "";
            }
        }

        /// <summary>
        /// キャラクターステータスを取得する
        /// </summary>
        private string GetCharacterStats(EPlayerCharacter character)
        {
            // 実際の実装では、キャラクターごとのステータスをデータベースから取得
            return "HP: 100\n攻撃力: 80\n防御力: 70\nスピード: 90";
        }

        /// <summary>
        /// 評価スターを更新する
        /// </summary>
        private void UpdateStarRating(EPlayerCharacter character)
        {
            if (starImages == null) return;

            // 実際の実装では、キャラクターごとの評価を取得
            int rating = 3; // 仮の評価

            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] != null)
                {
                    starImages[i].gameObject.SetActive(i < rating);
                }
            }
        }

        // ─── ダイアログ制御 ─────────────────────────────────────────

        /// <summary>
        /// ダイアログを閉じる
        /// </summary>
        private void CloseDialog()
        {
            gameObject.SetActive(false);
            OnDialogClosed?.Invoke();
        }
    }
}
