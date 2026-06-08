# OpenGSR Milestones — Issue-Based Roadmap

このマイルストーンはコードベースの直接調査で発見された**実際の問題**に基づいている。
優先順位は「壊れてるものほど先に直す」基準。

---

## 凡例

| 印 | 意味 |
|---|---|
| 🔴 CRITICAL | クラッシュ・データ欠損・ゲーム進行不可 |
| 🟡 HIGH | 機能が正しく動かない・状態矛盾 |
| ⚪ MEDIUM | デッドコード・パフォーマンス・エッジケース |
| 🟢 LOW | リファクタ・リネーム・微修正 |

---

## Phase 1: ゲームがクラッシュする問題 🔴

**先に直すべきもの。プレイ中に落ちる。**

### P1-1. CharaController.Shot() 三重NRE
**🔴 CRITICAL** — `Assets/Scripts/Player/CharaController.cs:194-199`

`weaponSlots.mainWeaponSlot` → `weapon.transform` → `GetComponentInChildren<AbstractGunController>()` → `.CanShot()` の連鎖でどの段階もnull非チェック。武器を所持していない状態で発砲するとNREクラッシュ。

**やること:** 各段階でnullガードを入れる。`TryGetComponent` パターンに変更。

### P1-2. CharaController.Update() Camera.main NRE
**🔴 CRITICAL** — `Assets/Scripts/Player/CharaController.cs:88,93`

シーン遷移中やカメラ未設定時に `Camera.main.WorldToScreenPoint` でNRE。

**やること:** `Camera.main` をキャッシュ＋nullチェック。なければ早期return。

### P1-3. PlayerAgent.damageVoices[0] 配列範囲外
**🔴 CRITICAL** — `Assets/Scripts/Player/PlayerAgent.cs:291`

`playerMasterData.damageVoices` が空またはnullの場合に `damageVoices[0]` で IndexOutOfRange。

**やること:** null/emptyチェックを追加。なければデフォルト値を返す。

### P1-4. グレネード爆発エフェクト Instantiate(null)
**🔴 CRITICAL** — `ClusterGrenadeController.cs:41`, `ChildClusterGrenadeController.cs:81-86`, `AbstractGrenadeController.cs:44`

`effectService` がnullかつ `expEffect` プレハブ未設定の場合に `Instantiate(null)` でNRE。

**やること:** `Instantiate` 前に `expEffect` のnullチェック。なければエフェクト無しで爆発だけ処理する。

### P1-5. AbstractFieldItem.DelayCoroutine の transform.position = Vector3.one
**🔴 CRITICAL** — `Assets/Scripts/Core/Base/Item/AbstractFieldItem.cs:22`

アイテム生成時に `transform.position = Vector3.one` で位置が (1,1,1) に固定される。さらにピックアップ後もコルーチンが走り続け、破棄済みオブジェクトにアクセスする。

**やること:** この行を削除。コルーチン停止機構を追加（`Coroutine`変数を保持し、`OnDisable`またはピックアップ時に `StopCoroutine`）。

### P1-6. CharaController.OnSpawn/OnReSpawn が base を呼んでいない
**🔴 CRITICAL** — `Assets/Scripts/Player/CharaController.cs:80-97`

`base.OnSpawn()` / `base.OnReSpawn()` を呼んでいないため、HP回復、パワーアップリセット、isDeadフラグ解除、観戦モード終了が行われない。**リスポーン後に実質的に死亡状態のまま。**

**やること:** 両メソッドの先頭に `base.OnSpawn()` / `base.OnReSpawn()` を追加。

### P1-7. CharaController と AbstractPlayer で2つの異なる PlayerStatus インスタンス
**🔴 CRITICAL** — `CharaController.cs:63,66-68` vs `AbstractPlayer.cs:102`

`CharaController` 側は `GamePlayerManager.Instance.Status` (シングルトン) を使うが、`AbstractPlayer.Status` は `new PlayerStatus()` (別インスタンス)。ダメージ計算とHP管理が異なるオブジェクトで行われている。

**やること:** 統一する。AbstractPlayer.Status もシングルトンを指すようにするか、CharaController が base.Status を使うように修正。

### P1-8. ConnectToMatchServerScene.cs: up() デリゲート未代入でNRE
**🔴 CRITICAL** — `Assets/Scripts/Scene/ConnectToMatchServerScene.cs:68`

`Update()` 内で `up()` が呼ばれるが、このデリゲートはどこからも代入されていない。初回フレームでNRE。

**やること:** `up` のnullチェック追加、または未使用なら削除。

### P1-9. OfflineQuestLoadingScene のローディングコルーチンがシーン遷移しない
**🔴 CRITICAL** — `Assets/Scripts/Scene/OfflineQuestLoadingScene.cs:64-67`

`LoadingCorutine()` が1秒待つだけで何もロードせず終了。プレイヤーはローディング画面で止まる。

**やること:** 実際のミッションシーンをロードする処理を追加。

### P1-10. MetalBreakerResultScene.BacktoWaitRoom() が遷移しない
**🔴 CRITICAL** — `Assets/Scripts/Scene/MetalBreakerResultScene.cs:33-36`

メソッド名は「待機部屋に戻る」だが `SceneManager.LoadScene` がない。プレイヤーが結果画面から進めない。

**やること:** 適切なシーン遷移コードを追加。

---

## Phase 2: ゲームの進行が止まる問題 🟡

**クラッシュはしないが、ゲームが前に進まない。**

### P2-1. TDMオフラインモードで試合が永遠に終わらない
**🟡 HIGH** — `TDMMatchMainScript.cs:138` + 全体

`Update()` が空。キル数チェックも時間制限もなく、`MatchEndNotification` のネットワークメッセージのみが終了条件。オフラインでは絶対に試合が終わらない。

**やること:** キルリミットまたはタイムリミットのオフライン終了条件を追加。他のマッチスクリプトと同様のパターンで `testGameTime` を使用するか、MatchRoom から制限時間を取得。

### P2-2. DM Survivalモードの観戦移行が未実装
**🟡 HIGH** — `DMMatchMainScript.cs:118-130`

`OnMyPlayerDead()` でSurvival判定時に `return;` しているだけ。プレイヤーは死んでも操作可能なまま。

**やること:** TSUVに合わせて `player.SetActive(false)` + `EnterSpectatorMode()` を実装。

### P2-3. オフライン全マッチでタイマーが機能していない
**🟡 HIGH** — `AbstractMatchMainScript.cs:17`, `MatchTimer.cs:33-36`

`MatchTimer` は `[SerializeField]` で宣言されているが、どのマッチスクリプトも `timer.StartTimer()` を呼んでいない。内部の `isStart` が常にfalseで完全に死んでいる。DMのみ `testGameTime` デバッグフィールドで時間制限を模擬。

**やること:** `AbstractMatchMainScript.Start()` で `timer.StartTimer()` を呼び、`timer.timeupEvent` に `OnTimeUp()` を購読。各モードの `OnTimeUp` で適切な終了処理。

### P2-4. MatchPauseEvent / MatchResumeEvent のシリアライズキー不一致
**🟡 HIGH** — `NetworkEventSerializer.cs:429,437` vs `NetworkEventDeserializer.cs:219,223`

Serializerは `PausedByPlayerId`/`ResumedByPlayerId` を書き込むが、Deserializerは `PausedBy`/`ResumedBy` を読む。ポーズ/レジュームイベントがネットワーク越しに完全に欠落する。

**やること:** キー名を統一。`RUDPMessageBuilder.CreateMatchPause`/`CreateMatchResume` も合わせて修正。

### P2-5. ClientNetworkManager.SendShootRequest / SendGrenadeThrow のJSON形式不一致
**🟡 HIGH** — `ClientNetworkManager.cs:665-678` vs `NetworkEventDeserializer.cs:対応箇所`

送信側はネストされた `Position { X, Y }` / `Direction { X, Y }` を使うが、受信側はフラットな `PosX, PosY, DirX, DirY` を期待する。射撃とグレネード投擲のメッセージがサーバーに正しく届かない。

**やること:** フォーマットを統一（フラット形式に合わせる）。

### P2-6. MissionClearGate.MissionClear() が何もしていない
**🟡 HIGH** — `Assets/Scripts/BaseLib/Map/MissionClearGate.cs:14-17`

`GameObject.Find("MissionMainScript")` で見つけたオブジェクトに対して何もしていない。クリアゲートが完全に機能しない。

**やること:** 見つけた `MissionMainScript` に対して `MissionClear()` または適切なイベント発火を行う。

### P2-7. OfflineLoadingScene のシーン名がハードコード
**🟡 HIGH** — `OfflineLoadingScene.cs:132-139`

`"DryDays(Stage)(CTF)"` など生文字列でシーン名が書かれている。シーンファイルをリネームするとロード失敗。

**やること:** `GeneralSceneMasterData` または `MapSceneMasterData` 経由で解決するように変更。

### P2-8. TitleScene にハードコードされたシーン名
**🟡 HIGH** — `TitleScene.cs:107`

`SceneManager.LoadScene("ExportAssetScene")` — マスターデータを通さず直接ロード。

**やること:** `GeneralSceneMasterData.Instance().ExportAssetScene()` を使う。

---

## Phase 3: 状態矛盾とデータ欠損 🟡

### P3-1. CharaController.Sit() / StandUp() のポリモーフィズム破壊
**🟡 HIGH** — `CharaController.cs:201-217`

`Sit()` はアニメーターコードがコメントアウトされていて `base.Sit()` も未呼び出し。`StandUp()` は `new` で隠蔽していてベースの pose state が適用されない。

**やること:** `Sit()` でアニメーター設定＋ `base.Sit()` 呼び出し。`StandUp()` にも `base.StandUp()` 追加。

### P3-2. PlayerAgent.Heal() が実際に回復しない
**🟡 HIGH** — `PlayerAgent.cs:395`

`public void Heal(float heal = 0)` がログ出力のみでHPを変更しない。フィールドアイテムの回復が効かない。

**やること:** `Status.AddHp(heal)` または同等のHP変更処理を追加。

### P3-3. PoisonBullet / FireBullet がログ出力のみ
**🟡 HIGH** — `AbstractPlayer.cs:348-353`

毒/炎上効果を適用するメソッドだが、実際には何の効果もなくログを出すだけ。

**やること:** DOTweenまたはコルーチンで継続ダメージ処理を実装。または当面はスタブのままにして明示的なTODOコメントを追加。

### P3-4. AbstractPlayer.InvincibleCounter が無敵状態にしない
**🟡 HIGH** — `AbstractPlayer.cs:434`

メソッド名は「無敵カウンター」だが、実際は指定時間待機するだけで無敵フラグを一切設定しない。

**やること:** `isInvincible` フラグを追加し、カウンター中はダメージを無効化。終了時に解除。

### P3-5. オフラインマッチ結果の保存漏れ
**🟡 HIGH** — `DMMatchMainScript.cs:330-340`, `GodModeMainScript.cs:25-28`

DMの `HandleMatchEnd`（ネットワーク経由の終了）が `StoreOfflineMatchResult()` を呼ばずに `GoToResult()` する。GodModeは結果保存機能自体がない。

**やること:** すべての終了パスで結果保存を確認。GodModeにも保存処理を追加。

### P3-6. CTFMatchMainScript に OnMyPlayerDead のオーバーライドがない
**🟡 HIGH** — `CTFMatchMainScript.cs` 全体

プレイヤー死亡後のリスポーン/観戦処理が未実装。CTFで死ぬとそのまま地面に倒れたまま。

**やること:** TDM/TSUVと同様の `OnMyPlayerDead` オーバーライドを追加。

### P3-7. MatchManager が単一サブスクライバしか保持しない
**🟡 MEDIUM** — `MatchManager.cs`, `AbstractMatchMainScript.cs:494-497`

`SubscribeEvent` / `UnSubscribeEvent` が1つの `mainScriptSubscriber` しか持たない。複数のマッチスクリプトが有効だと上書きされる。

**やること:** `List<IMatchEventSubscriber>` に変更。

---

## Phase 4: デッドコード削除とリファクタリング ⚪

### P4-1. GrenadeSlot.cs — 全体的にバグっている死にコード
**⚪ MEDIUM** — `Assets/Scripts/Item/GrenadeSlot.cs` (149行すべて)

- `IsEmpty()` が NRE（配列がnull参照で初期化される）
- `FillGrenade()` の条件が反転（空いてるスロットをスキップして埋まってるスロットを埋める）
- `Use()` がスロット内容を無視して新しい空アイテムを返す
- `Size()` と `Count()` の値が矛盾（2 vs 3）
- 実際のグレネード管理は `PlayerStatus.cs` で行われている

**やること:** ファイルごと削除。`PlayerStatus` が正しい実装。

### P4-2. MatchMainScript.cs — 空のクラス
**⚪ MEDIUM** — `Assets/Scripts/Match/MatchMainScript.cs`

`class MatchMainScript { }` — MonoBehaviourですらない空クラス。

**やること:** 削除。

### P4-3. WeaponControllerBase.cs — ほぼスタブ
**⚪ MEDIUM** — `Assets/Scripts/Weapon/WeaponControllerBase.cs`

64行で `Start()` 空、`Update()` のみ実装っぽい、`OnCollisionEnter2D` 空、`shot()` 空。`#pragma warning disable 0414` で未使用フィールド警告抑制。

**やること:** 使用実態を調査。不要なら削除。

### P4-4. MatchMainScript.cs — 空クラス
**⚪ MEDIUM** — `Assets/Scripts/Match/MatchMainScript.cs`

同上。死にコード。

### P4-5. PlayerGrenadeComponent.AutoSet() — 空
**⚪ LOW** — `PlayerGrenadeComponent.cs:104`

`[Button]` 属性でインスペクタにボタンが出るが何もしない。

**やること:** 実装するか属性を削除。

### P4-6. CharaController の pass-through オーバーライド4つ
**⚪ LOW** — `CharaController.cs:330-341`

`IncreaseAttack`, `IncreaseDefense`, `SpeedUp`, `Invisible` がただ `base.X()` を呼ぶだけ。オーバーライドする意味がない。

**やること:** 削除（継承先で必要なときに追加し直す）。

### P4-7. CharaController.animetor タイポフィールド
**⚪ LOW** — `CharaController.cs:69`

`[SerializeField] [Required] private Animator animetor` — `animator` のtypo。一度も使われていない。

**やること:** 削除。

### P4-8. MatchMainScript.cs (古い方) — 空クラス
**⚪ LOW** — 同上。

---

## Phase 5: ミッションシーン整備 ⚪

### P5-1. Mission2-4 に MissionMainScript 未アタッチ
**⚪ MEDIUM** — Mission2/3/4.unity

MainScript GameObject が Transform のみで `MissionMainScript` コンポーネントがない。シーンを開いてアタッチするだけの作業。

**やること:** Mission1 をテンプレートに、各シーンの MainScript に MissionMainScript を追加。必要なパラメータ設定。

### P5-2. Mission2-4 に RespawnPoints / UI / SoundManager 不足
**⚪ MEDIUM** — Mission2/3/4.unity

Mission1 には LifeSlot UI、SoundManager、RespawnPoints があるが、Mission2-4 にはない。

**やること:** 各シーンに不足コンポーネントを追加。

### P5-3. Quest1 の MissionClearGate が機能しない
**⚪ MEDIUM** — Quest1.unity

Collider2D 未設定で衝突検知が動作しない。さらに `MissionClear()` メソッド自体が何もしていない（P2-6と重複）。

**やること:** Collider2D を追加し、MissionClear の連携を実装。

### P5-4. Quest2/Quest3 に MissionMainScript 未アタッチ
**⚪ MEDIUM** — Quest2/Quest3.unity

Mission2-4 と同様。アートはあるがゲームプレイロジックがない。

**やること:** MissionMainScript を追加。

### P5-5. MissionLobbyScene.FilterRoom() / CreateNewRoom() が空
**⚪ LOW** — `MissionLobbyScene.cs:52,65-67`

どちらも実装されていない。今のところEnterMission1-5のボタンで直接起動できるので優先度は低いが、本来のルーム管理機能が欠けている。

---

## Phase 6: ネットワークの堅牢化 ⚪

### P6-1. ClientNetworkManager で40+メッセージ中7つしか処理していない
**⚪ MEDIUM** — `ClientNetworkManager.cs:390-437`

`ProcessUdpMessage` で7メッセージのみ処理。PlayerDeath, Kill, Damage, RoundStart/End, Flag関係などが未処理でデフォルトブランチに落ちるだけ。

**やること:** 主要なゲームメッセージのハンドラを追加。

### P6-2. サーバーディスコネクト後の再接続/クリーンアップなし
**⚪ MEDIUM** — `ClientNetworkManager.cs:313-316`, `OnlineLoadingScene.cs:83-85`

切断検知後も `_serverPeer = null` にするだけで、マッチルーム状態のクリーンアップや再接続試行がない。ローディングタイムアウト後の戻り先でも特に後処理なし。

**やること:** `DisconnectAll()` にマッチルーム状態のリセット追加。再接続ロジックの検討。

### P6-3. NetworkEventDeserializer で多数のメッセージが null を返す
**⚪ MEDIUM** — `NetworkEventDeserializer.cs:142-148`

ClientConnect, TeamKill, ItemTypes, VoteTypes, Lobby/WaitRoom/Room系メッセージなど多数が未処理でnull返却。

**やること:** 必要なものから順にdeserializerを追加。

### P6-4. FieldItem の enum 二重管理
**⚪ LOW** — `eFieldItemType.cs` + `FieldItem.cs`

Unity側とCore側で同じ概念のenumが異なる値セットで存在。変換レイヤー (`FieldItemVisualResolver`) でマッピングロスが発生（None/Random→PowerUpItem になる）。

**やること:** Coreのenumに統一する。変換レイヤーを削除してCoreの型だけを使う。

### P6-5. OfflineQuestLoadingScene.BackToOfflineWaitRoom がタイトルに戻る
**⚪ MEDIUM** — `OfflineQuestLoadingScene.cs:80-82`

メソッド名は「オフライン待機部屋に戻る」だが実際は `TitleScene()` をロードしている。命名と実装が不一致。

**やること:** `OfflineWaitRoomScene()` をロードするように修正。

---

## 優先順位（推奨）

| 優先順位 | Phase | やること | 理由 |
|---|---|---|---|
| **1** | P1-1〜P1-10 | クラッシュする10問題 | ゲームが落ちる |
| **2** | P2-1〜P2-8 | ゲーム進行が止まる8問題 | 遊べない |
| **3** | P3-1〜P3-7 | 状態矛盾7問題 | 正しく動かない |
| **4** | P5-1〜P5-5 | ミッションシーン整備 | 完成度向上 |
| **5** | P4-1〜P4-7 | デッドコード削除 | コードベース健全化 |
| **6** | P6-1〜P6-5 | ネットワーク堅牢化 | オンライン品質向上 |

---

## 完了マイルストーン（追跡対象外）

以下の旧マイルストーンは調査の結果、完了と判断:

- ~~M0~~ ✅ シーン名一元管理
- ~~M1~~ ✅ オンラインロビー全機能（ローカルサーバ）
- ~~M2~~ ✅ オフライン待機部屋
- ~~M3 DM/TDM/CTF~~ ✅ マッチフロー完了
- ~~M3-alpha TSUV~~ ✅ 実装済み
- ~~M4~~ ✅ アカウント/セーブ/ショップ
- ~~M5~~ ✅ 戦闘フィードバック
- ~~M6~~ ✅ アイテムループ
- ~~S0~~ ✅ プロトコル整理
- ~~S3~~ ✅ ローディングハンドシェイク
