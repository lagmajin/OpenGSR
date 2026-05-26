using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace OpenGS.EditorTools
{
    public static class CharacterSelectDialogPrefabBuilder
    {
        private const string DialogPrefabPath = "Assets/Prefabs/UI/CharacterSelectDialog.prefab";
        private const string ThumbnailPrefabPath = "Assets/Prefabs/UI/CharacterThumbnailItem.prefab";
        private const string DialogMasterDataPath = "Assets/Resources/MasterData/UI/DialogMasterData.asset";
        private const string DialogMasterDataDesktopPath = "Assets/Resources/MasterData/UI/DialogMasterData-DESKTOP-U3LDHLH.asset";
        private const string JapaneseFontPath = "Assets/TextMesh Pro/Fonts/NotoSansJP-VF.ttf";
        private const string JapaneseFontAssetPath = "Assets/TextMesh Pro/Fonts/NotoSansJP-VF SDF.asset";
        private const string OkButtonSpritePath = "Assets/Sprites/PlayerSelect/Common_Btn_OK.png";
        private const string CancelButtonSpritePath = "Assets/Sprites/PlayerSelect/Common_Btn_Cancel.png";

        [MenuItem("OpenGSR/Tools/Rebuild Character Select Dialog Prefabs")]
        public static void Rebuild()
        {
            EnsureDirectory("Assets/Prefabs/UI");
            EnsureJapaneseFontFallback();

            BuildThumbnailPrefab();
            BuildDialogPrefab();
            UpdateDialogMasterDataReference(DialogMasterDataPath);
            UpdateDialogMasterDataReference(DialogMasterDataDesktopPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[CharacterSelectDialogPrefabBuilder] Rebuilt CharacterSelectDialog prefab and thumbnail prefab.");
        }

        private static void BuildThumbnailPrefab()
        {
            var root = new GameObject("CharacterThumbnailItem", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(CharacterThumbnailItem), typeof(LayoutElement));
            try
            {
                var rect = root.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(180f, 120f);
                SetStretch(rect);

                var image = root.GetComponent<Image>();
                image.sprite = GetBuiltinSprite("UI/Skin/UISprite.psd");
                image.type = Image.Type.Sliced;
                image.color = new Color(0.16f, 0.18f, 0.22f, 1f);

                var button = root.GetComponent<Button>();
                button.transition = Selectable.Transition.ColorTint;

                var layout = root.GetComponent<LayoutElement>();
                layout.preferredWidth = 180f;
                layout.preferredHeight = 120f;
                layout.minWidth = 180f;
                layout.minHeight = 120f;

                var thumbnail = CreateChildImage(root.transform, "Thumbnail", new Color(1f, 1f, 1f, 1f), root.transform, stretch: true);
                thumbnail.rectTransform.offsetMin = Vector2.zero;
                thumbnail.rectTransform.offsetMax = Vector2.zero;
                thumbnail.rectTransform.SetAsFirstSibling();

                var selectedBorder = CreateChildImage(root.transform, "SelectedBorder", new Color(1f, 0.82f, 0.25f, 0.18f), root.transform, stretch: true);
                selectedBorder.rectTransform.offsetMin = Vector2.zero;
                selectedBorder.rectTransform.offsetMax = Vector2.zero;
                selectedBorder.rectTransform.SetAsLastSibling();

                var lockedOverlay = CreateChildImage(root.transform, "LockedOverlay", new Color(0f, 0f, 0f, 0.55f), root.transform, stretch: true);
                lockedOverlay.rectTransform.offsetMin = Vector2.zero;
                lockedOverlay.rectTransform.offsetMax = Vector2.zero;
                lockedOverlay.rectTransform.SetAsLastSibling();

                var nameText = CreateText(root.transform, "CharacterName", "Character", 18, TextAlignmentOptions.Center, Color.white);
                ConfigureRect(nameText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.26f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 6f));
                nameText.rectTransform.SetAsLastSibling();

                SetPrivateField(root.GetComponent<CharacterThumbnailItem>(), "thumbnailImage", thumbnail);
                SetPrivateField(root.GetComponent<CharacterThumbnailItem>(), "selectedBorder", selectedBorder);
                SetPrivateField(root.GetComponent<CharacterThumbnailItem>(), "lockedOverlay", lockedOverlay);
                SetPrivateField(root.GetComponent<CharacterThumbnailItem>(), "characterNameText", nameText);
                SetPrivateField(root.GetComponent<CharacterThumbnailItem>(), "selectButton", button);

                ApplyJapaneseFont(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, ThumbnailPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildDialogPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(DialogPrefabPath);
            try
            {
                var canvas = FindDirectChild(root.transform, "Canvas");
                if (canvas == null)
                {
                    canvas = CreateChild(root.transform, "Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                }

                canvas.gameObject.layer = 5;
                var canvasRect = canvas.GetComponent<RectTransform>();
                SetStretch(canvasRect);

                var canvasComponent = canvas.GetComponent<Canvas>();
                canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;

                var scaler = canvas.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;

                var raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster == null)
                {
                    canvas.gameObject.AddComponent<GraphicRaycaster>();
                }

                var canvasTransform = canvas.transform;

                var window = FindDirectChild(canvasTransform, "Image");
                if (window == null)
                {
                    window = CreateChild(canvasTransform, "Window", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                }
                else
                {
                    window.name = "Window";
                    EnsureComponent<RectTransform>(window);
                    EnsureComponent<CanvasRenderer>(window);
                    EnsureComponent<Image>(window);
                }

                var windowImage = window.GetComponent<Image>();
                ConfigureRect(window.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1500f, 900f), Vector2.zero);
                windowImage.color = new Color(0.09f, 0.11f, 0.15f, 0.96f);
                windowImage.sprite = GetBuiltinSprite("UI/Skin/Background.psd");
                windowImage.type = Image.Type.Sliced;

                var title = CreateText(window.transform, "TitleText", "キャラクター選択", 42, TextAlignmentOptions.Center, Color.white);
                ConfigureRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(900f, 70f), new Vector2(0f, -36f));
                title.fontStyle = FontStyles.Bold;

                ReparentOrCreate(window.transform, canvasTransform, "Ok");
                ReparentOrCreate(window.transform, canvasTransform, "Cancel");

                var listPanel = FindOrCreateImagePanel(window.transform, "ThumbnailPanel");
                ConfigureRect(listPanel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(330f, 690f), new Vector2(30f, 10f));
                listPanel.color = new Color(0.14f, 0.16f, 0.2f, 0.95f);
                listPanel.sprite = GetBuiltinSprite("UI/Skin/Background.psd");
                listPanel.type = Image.Type.Sliced;

                var listLabel = CreateText(listPanel.transform, "ListLabel", "キャラクター一覧", 24, TextAlignmentOptions.Left, new Color(0.95f, 0.95f, 0.95f, 1f));
                ConfigureRect(listLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(-24f, 36f), new Vector2(12f, -20f));
                listLabel.fontStyle = FontStyles.Bold;

                var scrollRoot = CreateChild(listPanel.transform, "ThumbnailScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
                var scrollRect = scrollRoot.GetComponent<ScrollRect>();
                var scrollImage = scrollRoot.GetComponent<Image>();
                scrollImage.sprite = GetBuiltinSprite("UI/Skin/UISprite.psd");
                scrollImage.type = Image.Type.Sliced;
                scrollImage.color = new Color(0f, 0f, 0f, 0.2f);
                ConfigureRect(scrollRoot.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-24f, -56f), new Vector2(0f, -20f));

                var viewport = CreateChild(scrollRoot.transform, "Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
                var viewportRect = viewport.GetComponent<RectTransform>();
                SetStretch(viewportRect);
                viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
                var mask = viewport.GetComponent<Mask>();
                mask.showMaskGraphic = false;

                var content = CreateChild(viewport.transform, "Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                var contentRect = content.GetComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(1f, 1f);
                contentRect.pivot = new Vector2(0.5f, 1f);
                contentRect.anchoredPosition = Vector2.zero;
                contentRect.sizeDelta = new Vector2(0f, 0f);

                var layout = content.GetComponent<VerticalLayoutGroup>();
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlHeight = true;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;
                layout.spacing = 10f;
                layout.padding = new RectOffset(10, 10, 10, 10);

                var fitter = content.GetComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                scrollRect.viewport = viewportRect;
                scrollRect.content = contentRect;
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.scrollSensitivity = 24f;

                var previewPanel = FindOrCreateImagePanel(window.transform, "PreviewPanel");
                ConfigureRect(previewPanel.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1040f, 690f), new Vector2(-30f, 10f));
                previewPanel.color = new Color(0.13f, 0.15f, 0.18f, 0.95f);
                previewPanel.sprite = GetBuiltinSprite("UI/Skin/Background.psd");
                previewPanel.type = Image.Type.Sliced;

                var previewHeader = CreateText(previewPanel.transform, "PreviewLabel", "キャラクタープレビュー", 24, TextAlignmentOptions.Left, new Color(0.95f, 0.95f, 0.95f, 1f));
                ConfigureRect(previewHeader.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(-24f, 36f), new Vector2(12f, -20f));
                previewHeader.fontStyle = FontStyles.Bold;

                ReparentOrCreate(previewPanel.transform, canvasTransform, "SelectedCharacterPreview");
                var previewImage = FindOrCreateImage(previewPanel.transform, "SelectedCharacterPreview");
                ConfigureRect(previewImage.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(620f, 450f), new Vector2(0f, -78f));
                previewImage.preserveAspect = true;
                previewImage.color = Color.white;

                var nameText = CreateText(previewPanel.transform, "SelectedCharacterNameText", "Misty", 34, TextAlignmentOptions.Center, Color.white);
                ConfigureRect(nameText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(860f, 52f), new Vector2(0f, -154f));
                nameText.fontStyle = FontStyles.Bold;

                var starRow = CreateChild(previewPanel.transform, "RatingStars", typeof(RectTransform));
                var starRowRect = starRow.GetComponent<RectTransform>();
                ConfigureRect(starRowRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(220f, 42f), new Vector2(0f, -194f));

                var starImages = new Image[5];
                for (var i = 0; i < starImages.Length; i++)
                {
                    var star = CreateChildImage(starRow.transform, $"Star{i + 1}", new Color(0.97f, 0.81f, 0.18f, 1f), starRow.transform, stretch: false);
                    ConfigureRect(star.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 28f), new Vector2(16f + i * 36f, 0f));
                    starImages[i] = star;
                }

                var descriptionText = CreateText(previewPanel.transform, "SelectedCharacterDescriptionText", "バランス型のキャラクター", 24, TextAlignmentOptions.Left, new Color(0.92f, 0.92f, 0.92f, 1f));
                ConfigureRect(descriptionText.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-40f, 90f), new Vector2(20f, -248f));

                var statsText = CreateText(previewPanel.transform, "SelectedCharacterStatsText", "HP: 100\n攻撃力: 80\n防御力: 70\nスピード: 90", 22, TextAlignmentOptions.Left, new Color(0.88f, 0.9f, 0.95f, 1f));
                ConfigureRect(statsText.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(380f, 130f), new Vector2(20f, 24f));

                var statusText = CreateText(window.transform, "MessageText", string.Empty, 20, TextAlignmentOptions.Left, new Color(0.94f, 0.58f, 0.58f, 1f));
                ConfigureRect(statusText.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(540f, 40f), new Vector2(36f, 28f));
                statusText.gameObject.SetActive(false);

                var okButton = FindDirectChild(window.transform, "Ok");
                EnsureComponent<Image>(okButton);
                EnsureComponent<Button>(okButton);
                ConfigureRect(okButton.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(220f, 64f), new Vector2(-40f, 26f));

                var cancelButton = FindDirectChild(window.transform, "Cancel");
                EnsureComponent<Image>(cancelButton);
                EnsureComponent<Button>(cancelButton);
                ConfigureRect(cancelButton.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(220f, 64f), new Vector2(-278f, 26f));

                var okImage = okButton.GetComponent<Image>();
                var cancelImage = cancelButton.GetComponent<Image>();
                okImage.color = Color.white;
                cancelImage.color = Color.white;
                okImage.sprite = LoadSprite(OkButtonSpritePath);
                cancelImage.sprite = LoadSprite(CancelButtonSpritePath);
                okImage.type = Image.Type.Simple;
                cancelImage.type = Image.Type.Simple;

                var okLabel = EnsureLabel(okButton.transform, "Label", "決定");
                var cancelLabel = EnsureLabel(cancelButton.transform, "Label", "キャンセル");

                SetPrivateField(root.GetComponent<OpenGS.CharacterSelectDialog>(), "thumbnailListContent", contentRect);
                SetPrivateField(root.GetComponent<OpenGS.CharacterSelectDialog>(), "thumbnailItemPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(ThumbnailPrefabPath));
                SetPrivateField(root.GetComponent<OpenGS.CharacterSelectDialog>(), "thumbnailScrollRect", scrollRect);
                SetPrivateField(root.GetComponent<OpenGS.CharacterSelectDialog>(), "largeCharacterImage", previewImage);
                SetPrivateField(root.GetComponent<OpenGS.CharacterSelectDialog>(), "characterNameText", nameText);
                SetPrivateField(root.GetComponent<OpenGS.CharacterSelectDialog>(), "characterDescriptionText", descriptionText);
                SetPrivateField(root.GetComponent<OpenGS.CharacterSelectDialog>(), "characterStatsText", statsText);
                SetPrivateField(root.GetComponent<OpenGS.CharacterSelectDialog>(), "starImages", starImages);
                SetPrivateField(root.GetComponent<OpenGS.CharacterSelectDialog>(), "okButton", okButton.GetComponent<Button>());
                SetPrivateField(root.GetComponent<OpenGS.CharacterSelectDialog>(), "cancelButton", cancelButton.GetComponent<Button>());
                SetPrivateField(root.GetComponent<OpenGS.CharacterSelectDialog>(), "statusText", statusText);

                ApplyJapaneseFont(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, DialogPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void UpdateDialogMasterDataReference(string assetPath)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (asset == null)
            {
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DialogPrefabPath);
            if (prefab == null)
            {
                return;
            }

            var serialized = new SerializedObject(asset);
            var property = serialized.FindProperty("CharacterSelectDialog");
            if (property == null)
            {
                return;
            }

            property.objectReferenceValue = prefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static GameObject FindDirectChild(Transform parent, string name)
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == name)
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private static GameObject CreateChild(Transform parent, string name, params Type[] components)
        {
            var go = new GameObject(name, components);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void ReparentOrCreate(Transform newParent, Transform searchRoot, string name)
        {
            var existing = FindDirectChild(searchRoot, name);
            if (existing == null)
            {
                return;
            }

            existing.transform.SetParent(newParent, false);
        }

        private static Image FindOrCreateImagePanel(Transform parent, string name)
        {
            var go = FindDirectChild(parent, name);
            if (go == null)
            {
                go = CreateChild(parent, name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            }
            else
            {
                EnsureComponent<RectTransform>(go);
                EnsureComponent<CanvasRenderer>(go);
                EnsureComponent<Image>(go);
            }

            return go.GetComponent<Image>();
        }

        private static Image FindOrCreateImage(Transform parent, string name)
        {
            var go = FindDirectChild(parent, name);
            if (go == null)
            {
                go = CreateChild(parent, name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            }
            else
            {
                EnsureComponent<RectTransform>(go);
                EnsureComponent<CanvasRenderer>(go);
                EnsureComponent<Image>(go);
            }

            return go.GetComponent<Image>();
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            return go.GetComponent<T>() ?? go.AddComponent<T>();
        }

        private static TextMeshProUGUI EnsureLabel(Transform parent, string name, string text)
        {
            var labelObject = FindDirectChild(parent, name);
            if (labelObject == null)
            {
                labelObject = CreateChild(parent, name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            }
            else
            {
                EnsureComponent<RectTransform>(labelObject);
                EnsureComponent<CanvasRenderer>(labelObject);
                EnsureComponent<TextMeshProUGUI>(labelObject);
            }

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.font = GetDefaultFontAsset();
            label.fontSize = 22;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            ConfigureRect(label.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            return label;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, int fontSize, TextAlignmentOptions alignment, Color color)
        {
            var go = CreateChild(parent, name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.font = GetDefaultFontAsset();
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.enableWordWrapping = true;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Image CreateChildImage(Transform parent, string name, Color color, Transform keepAsChildOf, bool stretch)
        {
            var go = CreateChild(parent, name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var image = go.GetComponent<Image>();
            image.sprite = GetBuiltinSprite("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = color;

            if (stretch)
            {
                SetStretch(go.GetComponent<RectTransform>());
            }

            return image;
        }

        private static void ConfigureRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 anchoredPosition)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
        }

        private static void SetStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null)
            {
                return;
            }

            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
            var field = target.GetType().GetField(fieldName, flags);
            if (field == null)
            {
                return;
            }

            field.SetValue(target, value);
            EditorUtility.SetDirty(target as UnityEngine.Object);
        }

        private static Sprite GetBuiltinSprite(string path)
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>(path);
        }

        private static Sprite LoadSprite(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static TMP_FontAsset GetDefaultFontAsset()
        {
            var japaneseFont = GetJapaneseFontAsset();
            if (japaneseFont != null)
            {
                return japaneseFont;
            }

            if (TMP_Settings.defaultFontAsset != null)
            {
                return TMP_Settings.defaultFontAsset;
            }

            var fallback = AssetDatabase.FindAssets("t:TMP_FontAsset")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path))
                .FirstOrDefault(font => font != null);
            return fallback;
        }

        private static TMP_FontAsset GetJapaneseFontAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(JapaneseFontAssetPath);
            if (existing != null)
            {
                return existing;
            }

            return TMP_Settings.defaultFontAsset;
        }

        private static void EnsureJapaneseFontFallback()
        {
            var japaneseFont = GetJapaneseFontAsset();
            if (japaneseFont == null)
            {
                return;
            }

            var settings = TMP_Settings.instance;
            if (settings == null)
            {
                return;
            }

            var serializedSettings = new SerializedObject(settings);
            var fallbackFonts = serializedSettings.FindProperty("m_fallbackFontAssets");
            if (fallbackFonts == null)
            {
                return;
            }

            fallbackFonts.ClearArray();
            fallbackFonts.InsertArrayElementAtIndex(0);
            fallbackFonts.GetArrayElementAtIndex(0).objectReferenceValue = japaneseFont;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        private static void ApplyJapaneseFont(Transform root)
        {
            var japaneseFont = GetJapaneseFontAsset();
            if (japaneseFont == null)
            {
                return;
            }

            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                text.font = japaneseFont;
                text.fontSharedMaterial = japaneseFont.material;
                EditorUtility.SetDirty(text);
            }
        }

        private static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var folderName = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureDirectory(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
