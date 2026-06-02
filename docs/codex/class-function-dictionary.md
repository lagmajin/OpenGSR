# Class / Function Dictionary

この辞典は、OpenGSR の「どのクラスが何をしているか」を短時間で引けるようにするための入口です。

## どう読むか

- まず `Class` を見て、担当領域を把握する
- 次に `Notes` を見て、どこに依存しているかや落とし穴を確認する
- 実際に触るときは `Key methods` からコードへ飛ぶ

## 使い方

- 新しいクラスを追加したら、ここに1行でもいいので追記する
- 1クラスにつき「役割」と「代表メソッド」を最低限書く
- 仕様変更があったら、古い説明を消してから更新する
- 迷ったら「そのクラスを見れば何ができるか」が分かる粒度にする

## 記法

- `Class` = クラス名
- `Role` = 何を担当するか
- `Key methods` = 代表的な公開メソッドや重要メソッド
- `Notes` = 注意点、依存先、補足

## Class Dictionary

| Class | Location | Role | Key methods | Notes |
| --- | --- | --- | --- | --- |
| `GameConfig` | `Assets/Scripts/BaseLib/GameConfig.cs` | BGM / SE の音量を保持する軽量シングルトン | `GetInstance()`, `MuteBGM()`, `MuteSE()` | 音量の入口。UI や設定反映の起点になりやすい |
| `UserSaveManager` | `Assets/Scripts/Systems/UserSaveManager.cs` | 所持品・装備・お気に入り武器を JSON 保存する | `IsPurchased()`, `SetPurchased()`, `EquipItem()`, `EquipToSlot()`, `GetEquippedId()`, `GetEquippedInstantItems()`, `EquipGrenadeToSlot()`, `GetEquippedGrenadeSlots()`, `ToggleFavoriteWeapon()` | 装備データの中心。`InstantItem` と `Grenade` のスロット管理もここ |
| `PlayerEquipLoader` | `Assets/Scripts/Systems/PlayerEquipLoader.cs` | ローカルテスト用の装備データ読み込み | `Load()` | `PlayerEquip.json` を versioned JSON として読む。古い形式の互換処理あり |
| `PlayerStatus` | `Assets/Scripts/Interface/PlayerStatus.cs` | HP / Armor / Booster / Grenade の実行時状態を保持する | `AddHp()`, `ReduceHp()`, `AddArmor()`, `ReduceArmor()`, `FullRecovery()`, `LoadGrenadeSlots()`, `RefillGrenade()`, `UseGrenade()`, `ConsumeGrenade()`, `ResetCombatStats()` | `ReactiveProperty` を持つので UI 追従に向く。グレネードは3スロット管理 |
| `CharaController` | `Assets/Scripts/Player/CharaController.cs` | プレイヤー操作の実体。射撃・ジャンプ・装備反映・死亡復帰をまとめる | `OnSpawn()`, `OnReSpawn()`, `Update()`, `UseItem()`, `ThrowGrenade()`, `Shot()`, `OpenGrenade()`, `ReloadStart()`, `FlipWeapon()`, `DropWeapon()` | `UserSaveManager` と `PlayerStatus` を結びつける中心クラス |
| `PlayerGrenadeComponent` | `Assets/Scripts/Player/AsmExport/PlayerGrenadeComponent.cs` | 現在選択中のグレネード種別と投擲処理を担当する | `SetGrenadeType()`, `ThrowGrenade()`, `CurrentChargeRatio` | `Status.UseGrenade(grenadeType)` を消費してから投げる。UI のチャージ表示もここから取れる |
| `WeaponControllerBase` | `Assets/Scripts/Weapon/WeaponControllerBase.cs` | 武器の共通基底。向き反転などの基本挙動を持つ | `Start()`, `Update()`, `OnCollisionEnter2D()`, `shot()` | 現状は薄い基底。派生武器側の挙動確認に使う |
| `WeaponSelectDialog` | `Assets/Scripts/UI/WeaponSelectDialog.cs` | 武器選択ダイアログの表示・選択・バン処理を行う | `Show()`, `Hide()`, `SetLeftWeapons()`, `SetRightWeapons()`, `RefreshData()`, `RefreshUI()`, `OnLeftSlotClicked()`, `OnRightSlotClicked()` | 左はお気に入り武器、右はバン候補。`MatchRoomManager` と `UserSaveManager` にまたがる |
| `WeaponSelectDialogSlot` | `Assets/Scripts/UI/WeaponSelectDialogSlot.cs` | 武器選択ダイアログ内の1スロット表示を担当する | `CacheReferences()`, `SetWeaponType()`, `SetDetailText()`, `SetVisualState()` | 表示ロジックの部品。色・バッジ・アイコンをまとめて更新する |
| `PlayerStatusUIManager` | `Assets/Scripts/UI/PlayerStatusUIManager.cs` | 自分の HP / Armor / Booster / 武器 / グレネード表示を統合する | `OnEnable()`, `OnDisable()`, `InitializeUI()`, `UpdateHPDisplay()`, `UpdateArmorDisplay()`, `UpdateBoosterDisplay()`, `UpdateWeaponDisplay()`, `UpdateGrenadeDisplay()`, `RefreshInstantItemDisplays()`, `RefreshGrenadeSlotDisplays()` | `PlayerRegistry` と `GameEventBroker` を購読して UI を追従させる |
| `WeaponVisualResolver` | `Assets/Scripts/UI/WeaponVisualResolver.cs` | 武器 ID から表示名・画像・マスターデータを解決する共通入口 | `Resolve()`, `GetSelectionSprite()`, `GetInGameSprite()`, `GetSilhouetteSprite()`, `GetDisplayName()` | UI ごとのハードコードを減らすための要。`Resources/MasterData/Weapon` を読む |
| `GrenadeVisualResolver` | `Assets/Scripts/UI/GrenadeVisualResolver.cs` | グレネード ID から HUD スプライトや prefab を解決する共通入口 | `GetDisplayName()`, `GetInternalName()`, `GetHudSprite()`, `GetProjectilePrefab()`, `GetExplosionEffect()`, `GetPackHudSprite()` | レガシー master data と新しい HUD master data の両方に対応する |
| `GrenadeHudMasterData` | `Assets/Scripts/MasterData/GrenadeHudMasterData.cs` | グレネード HUD 用スプライトをまとめた ScriptableObject | 参照フィールドのみ | `normal / power / magnetic / mine / cluster / fire` を保持する |
| `SoundService` | `Assets/Scripts/Audio/SoundService.cs` | BGM / SE / 武器音 / プレイヤー音をまとめて再生するサービス | `PlayBGM(...)`, `StopBGM()`, `PlaySystemSound(...)`, `PlayMatchSound(...)`, `PlaySoundEffect(...)`, `PlayTakeItemSound(...)`, `PlayWeaponShot(...)`, `PlayWeaponReload(...)`, `PlayWeaponHit(...)`, `PlayGrenadeThrow(...)`, `PlayGrenadeExplosion(...)`, `PlayPlayerSound(...)`, `PlayOneShot(...)`, `ValidateSoundSetup(...)` | `SoundMasterData` と `BGMMasterData` を優先し、足りなければ `Resources` にフォールバックする |
| `DependencyInjectionConfig` | `Assets/Scripts/DI/DependencyInjectionConfig.cs` | DI の解決入口 | `Resolve<T>()` 系 | ここを通すと、シーン横断で依存解決しやすい |
| `GameInstaller` | `Assets/Scripts/DI/GameInstaller.cs` | DI のバインド設定をまとめる | インストール処理 | 起動時の依存関係の組み立てに関与する |

## Function Dictionary

| Function / Method | Belongs to | Role | Notes |
| --- | --- | --- | --- |
| `GameConfig.GetInstance()` | `GameConfig` | 設定シングルトンを返す | 音量設定の入口 |
| `GameConfig.MuteBGM()` | `GameConfig` | BGM 音量を 0 にする | `bgmVolume = 0.0f` |
| `GameConfig.MuteSE()` | `GameConfig` | SE 音量を 0 にする | `seVolume = 0.0f` |
| `UserSaveManager.EquipToSlot()` | `UserSaveManager` | 指定カテゴリを装備スロットへ入れる | `Character` / `Booster` / `InstantItem` / `Weapon` で挙動が違う |
| `UserSaveManager.EquipGrenadeToSlot()` | `UserSaveManager` | グレネード装備を保存する | 3スロット前提 |
| `PlayerEquipLoader.Load()` | `PlayerEquipLoader` | テスト用装備ファイルを読む | バージョン付き JSON なので古い形式も救済する |
| `PlayerStatus.FullRecovery()` | `PlayerStatus` | HP / Armor / Booster / Grenade を全回復する | スポーン時の基本処理 |
| `PlayerStatus.LoadGrenadeSlots()` | `PlayerStatus` | 装備済みグレネードを実行時状態へ反映する | 装備が空なら `Normal` を補充する |
| `PlayerStatus.RefillGrenade()` | `PlayerStatus` | 空きスロットへグレネードを補充する | `EGrenadeType` を指定可能 |
| `PlayerStatus.UseGrenade()` | `PlayerStatus` | 先頭の使用可能グレネードを1つ消費する | `out EGrenadeType` 版もある |
| `CharaController.OnSpawn()` | `CharaController` | スポーン時の初期化をする | 装備・グレネード・瞬間アイテムを再同期する |
| `CharaController.OnReSpawn()` | `CharaController` | リスポーン時の初期化をする | `OnSpawn()` とほぼ同系統 |
| `CharaController.UseItem(int)` | `CharaController` | 瞬間アイテムを使う | スロット番号は 1 始まり入力を想定している |
| `CharaController.ThrowGrenade()` | `CharaController` | 投擲入力を処理する | `PlayerGrenadeComponent` が優先、無ければ `Status` にフォールバックする |
| `CharaController.Shot()` | `CharaController` | 現在武器の射撃を実行する | `weaponSlots.mainWeaponSlot` から gun を引く |
| `CharaController.InitializeSpawnLoadout()` | `CharaController` | スポーン時の装備同期をまとめる | `Status.FullRecovery()` もここで呼ぶ |
| `CharaController.SyncGrenadeComponentTypeFromStatus()` | `CharaController` | 実行時グレネード種別をステータスから合わせる | 最初に見つかった非空スロットを使う |
| `PlayerGrenadeComponent.ThrowGrenade(float)` | `PlayerGrenadeComponent` | グレネードを生成して投げる | `Status.UseGrenade(grenadeType)` で残弾を消費してから生成する |
| `WeaponControllerBase.Update()` | `WeaponControllerBase` | マウス方向で左右反転する | `transform.localScale.x` を切り替える |
| `WeaponControllerBase.shot()` | `WeaponControllerBase` | 発射処理の入口 | 現状は空実装なので派生先で確認が必要 |
| `WeaponSelectDialog.RefreshData()` | `WeaponSelectDialog` | 左右の候補リストを埋め直す | お気に入り武器とバン候補の再構成を行う |
| `WeaponSelectDialog.UpdateStatusText()` | `WeaponSelectDialog` | 選択数の表示を更新する | `LEFT x/5 RIGHT y/5` 形式 |
| `WeaponSelectDialogSlot.SetVisualState()` | `WeaponSelectDialogSlot` | 1スロットの見た目をまとめて更新する | 選択 / バン / 装備 / 空状態をここで表現する |
| `PlayerStatusUIManager.UpdateGrenadeDisplay()` | `PlayerStatusUIManager` | HUD のグレネード表示を更新する | `GrenadeVisualResolver` を使って名前とアイコンを解決する |
| `SoundService.PlayBGM(EBgm, float)` | `SoundService` | enum 指定で BGM を再生する | `BGMMasterData` を優先する |
| `SoundService.PlayBGM(EMap)` | `SoundService` | マップに応じて BGM を決める | `ResolveMapBgm()` を経由する |
| `SoundService.PlayBGM(string, float)` | `SoundService` | 文字列名で BGM を再生する | マスターデータになければ直接ロードする |
| `SoundService.PlayBGM(AudioClip, float)` | `SoundService` | クリップを直接再生する | 再生後に現在 BGM 名も記録する |
| `SoundService.StopBGM(float)` | `SoundService` | BGM を停止する | `SimpleAudioManager` に委譲する |
| `SoundService.PlayOneShot(AudioClip, float, float)` | `SoundService` | 汎用 SE 再生 | null の場合は何もしない |
| `SoundService.ValidateSoundSetup(bool)` | `SoundService` | 音声設定の妥当性確認 | ログ出力付きで検証できる |

## 読み取りメモ

- `UserSaveManager` は「保存データ」、`PlayerStatus` は「実行時データ」と考えると整理しやすい
- `WeaponVisualResolver` と `GrenadeVisualResolver` は、UI 側のハードコード削減に効く
- `CharaController` は大きいが、装備の同期点として見れば追いやすい
- `PlayerStatusUIManager` は表示専用に近いので、ゲームロジックを入れすぎない方が保守しやすい

## 追記ルール

- まずは名前だけでも追加する
- 次に「何の責任を持つか」を1行で書く
- 迷ったら実装ファイルへのリンクを優先し、説明は短くする

## まず追記したい候補

- `Assets/Scripts/Weapon/`
- `Assets/Scripts/Player/`
- `Assets/Scripts/Network/`
- `Assets/Editor/`
- `Assets/Scripts/BaseLib/`

この辞典は「検索しやすさ」を優先しているので、最初は薄くて大丈夫です。
