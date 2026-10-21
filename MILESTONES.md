# OpenGSR Milestones — Issue-Based Roadmap

このマイルストーンはコードベースの直接調査で発見された**実際の問題**に基づいている。
優先順位は「壊れてるものほど先に直す」基準。

> **2026-10-14 改訂**: 全項目を現在のソースに対して再検証した。このドキュメントに
> 未修正と記載されていた項目のうち大半は既に修正済み（✅）であることが確認できたので、
> 完了実績として移動させた。行番号も現行ツリーに合わせて更新している。

---

## 凡例

| 印 | 意味 |
|---|---|
| ✅ | 修正済み（実コードで修正を確認） |
| 🔴 CRITICAL | クラッシュ・データ欠損・ゲーム進行不可 |
| 🟡 HIGH | 機能が正しく動かない・状態矛盾 |
| ⚪ MEDIUM | デッドコード・パフォーマンス・エッジケース |
| 🟢 LOW | リファクタ・リネーム・微修正 |

**残件サマリ（2026-10-14 時点）**

ロードマップに挙がっていた未修正項目は**すべて解消**した。実測の結果、
「足すべきだった」項目の一部は**足してはいけない**ものだった。

| ID | 内容 | 状態 |
|---|---|---|
| P3-7 | MatchManager の単一サブスクライバ | ✅ List 化＋例外分離 |
| P3-5 | DM のオフライン結果保存漏れ | ✅ TDM/CTF/TSUV と同一形に |
| P4-6 | AIPlayerController の pass-through override | ✅ 5件削除（基底は public virtual） |
| P6-3 | デシリアライザ未処理＋ログ洪水 | ✅ ログを型名のみに格下げ |
| P5-2 | Mission シーンの RespawnPoints | ✅ Mission1/2/5 を配線。他は**原点スポーンが正当**なので追加せず |
| P5-2 | LifeSlot UI / SoundManager が無い | 🚫 **追加しない**（不要。理由は下記） |
| P5-3 | MissionClearGate の完了経路 | ✅ Component 修復＋オフライン完了経路＋オンライン報告経路 |

---

## 完了済み ✅ — Phase 1: クラッシュ問題

いずれも修正済み。記録として残す。

| ID | 内容 | 修正箇所 |
|---|---|---|
| P1-1 | `CharaController.Shot()` の三重NRE | `CharaController.cs:200` `weaponSlots?.mainWeaponSlot` |
| P1-2 | `Camera.main` NRE | `CharaController.cs:31,86,98-111` キャッシュ＋null 早期return |
| P1-3 | `damageVoices[0]` 範囲外 | `PlayerAgent.cs:1431-1443` `TryGetDeathVoiceClip()` |
| P1-4 | グレネード爆発 `Instantiate(null)` | `AbstractGrenadeController.cs:53` `ClusterGrenadeController.cs:50` `ChildClusterGrenadeController.cs:89` |
| P1-5 | `AbstractFieldItem` の位置固定/コルーチン | `AbstractFieldItem.cs:37,83` `lifetimeCoroutine` を保持 |
| P1-6 | `OnSpawn`/`OnReSpawn` が base 未呼び出し | `CharaController.cs:45,55` |
| P1-7 | PlayerStatus の2インスタンス | `CharaController` は partial、`GamePlayerManager` 参照を廃止 |
| P1-8 | `up()` デリゲート未代入 | `ConnectToMatchServerScene.cs:98-115,155` null チェック＋try/catch |
| P1-9 | `OfflineQuestLoadingScene` が遷移しない | `OfflineQuestLoadingScene.cs:57-83` `LoadSceneAsync` 実行 |
| P1-10 | `BacktoWaitRoom()` が遷移しない | `MetalBreakerResultScene.cs:34-58` |

## 完了済み ✅ — Phase 2: ゲーム進行の問題

| ID | 内容 | 修正箇所 |
|---|---|---|
| P2-1 | TDMオフラインモードで試合が終わらない | `TDMMatchMainScrip.cs:279-304` キル上限＋`OnLocalTimeUp` |
| P2-2 | DM Survival 観戦移行未実装 | `DMMatchMainScript.cs:111-122` `SetActive(false)`＋`EnterSpectatorMode` |
| P2-3 | `MatchTimer` が起動されない | `AbstractMatchMainScript.cs:468-476` `StartTimer()`＋`timeupEvent` 購読 |
| P2-4 | Pause/Resume のキー不一致 | クライアントは `PausedBy`/`ResumedBy` で一致（サーバ/Coreは参照しない） |
| P2-5 | ShootRequest のJSONフォーマット不一致 | `ClientNetworkManager.cs:2090-2099` フラット形式に統一 |
| P2-6 | `MissionClear()` が何もしない | `MissionClearGate.cs:27-40` `SendMessage` 発火 |
| P2-7 | `OfflineLoadingScene` のシーン名ハードコード | `OfflineLoadingScene.cs:177-214` マスターデータ経由に解決 |
| P2-8 | `TitleScene` のシーン名ハードコード | `TitleScene.cs:154-160` `LoadConfiguredScene` 経由 |

## 完了済み ✅ — Phase 3: 状態矛盾・データ欠損（一部）

| ID | 内容 | 修正箇所 |
|---|---|---|
| P3-1 | `Sit()`/`StandUp()` のポリモーフィズム破壊 | `CharaController.cs:245-253,265-272` `base.Sit()`＋アニメーター復帰 |
| P3-2 | `PlayerAgent.Heal()` が回復しない | `PlayerAgent.cs:1681-1687` `Status.AddHp()` |
| P3-3 | `PoisonBullet`/`FireBullet` がログのみ | `AbstractPlayer.cs:644-651` `StartElementalDamage()` |
| P3-4 | `InvincibleCounter` が無敵にしない | `AbstractPlayer.cs:851-867` `isInvincible`＋versionガード |
| P3-6 | CTF に `OnMyPlayerDead` が無い | `CTFMatchMainScript.cs:443-461` リスポーン遅延実装 |

---

## Phase 3（状態矛盾・データ欠損）

### P3-7. MatchManager が単一サブスクライバしか保持しない ✅ — **修正済み**
`Assets/Scripts/Core/Base/MatchManager.cs`

`SubscribeEvent`/`UnSubscribeEvent` が1つの `mainScriptSubscriber` しか持たず、
2つ目のマッチスクリプトが有効なと上書きされ、`UnSubscribeEvent` は
渡されたスクリプトに関わらず他人の枠も null にしていた。
`AbstractMatchMainScript` と `CTFMatchMainScript` が両方購読する
（`AbstractMatchMainScript.cs:833,838` と `CTFMatchMainScript.cs:88,674`）ため、
これが実際に起きていた。

- フィールドを `List<IAbstractMatchMainScript>` に変更
- `SubscribeEvent` は重複追加しない、`UnSubscribeEvent` は**渡された1つだけ**を外す
- `SendEvent` はスナップショット نسخةを各購読へ配信し、1つの購読が例外を投げても
  記録して残りの購読へ届ける（これらのハンドラはモードの試合状態の総経路なので、
  1つが例外を投げると後続のイベントが失われていた）
- `ClearAllSubscribe` / `Clear` は List をクリア

### P3-5. DM のサーバ終了経路がオフライン結果を保存しない ✅ — **修正済み**
`Assets/Scripts/Match/DMMatchMainScript.cs`（`OnServerMatchEnd`）

GodMode は `StoreOfflineMatchResult()` を呼ぶようになった（`GodModeMainScript.cs:28-52`）が、
DM のサーバ通知経路は guard なしで結果シーンへ直行していた。
`StoreOfflineMatchResult()` は `GameEnd()`（ローカル時計経路）からのみ呼ばれていた。

他のモードと同じ `if (IsOfflineMatch()) StoreOfflineMatchResult();` を
`OnServerMatchEnd` に追加した。TDM `:598-601` / CTF `:1377-1380` / TSUV `:445` と揃えた形。

---

## 完了済み ✅ — Phase 4（デッドコード）

### P4-6. AIPlayerController の空 pass-through override ✅ — **修正済み**
`Assets/Scripts/Player/AIPlayerController.cs`

`CharaController` 側は削除済みだったが、同一のデッドコードが `AIPlayerController` に
残っていた。ロードマップは `CharaController` の話として記載されていたが、実体は
別クラス（`AIPlayerController : AbstractPlayer`）にあった。

`AddDamage` / `IncreaseAttack` / `IncreaseDefense` / `Invisible` / `SpeedUp` の
5つ（ロードマップ記載の4つに加え `AddDamage` もあり）が `base.X()` のみで、
`base` 側は全て `public virtual`（`AbstractPlayer.cs:419,597,605,613,619`）だったため
削除しても基底実装がそのまま使われる。挙動互換。

> P4-1〜P5, P4-7, P4-8 は完了済み（`GrenadeSlot.cs` / `MatchMainScript.cs` /
> `WeaponControllerBase.cs` は削除済みで参照残なし、`AutoSet()` は実装済み、
> `animetor` は typo ごと削除済み）。

---

## Phase 5（ミッションシーン）

### P5-2. Mission シーンの RespawnPoints 未配線 ⚠ — **一部修正済み**

当初の記載「Mission3/4 に LifeSlot / SoundManager が無い」は実測すると
**Mission3/4 だけが足りているわけではない**。Mission1 も含めて
`MissionReSpawnPoints` Component を持つシーンが1つもなかった。

| Scene | RespawnPoints オブジェクト | `MissionReSpawnPoints` | `respawnPoints` 配線 | LifeSlot UI | SoundManager |
|---|---|---|---|---|---|
| Mission1 | ○ | **○（追加）** | **○（追加）** | ○ | ○ |
| Mission2 | ○（空オブジェクトのみ） | **○（追加）** | **○（追加）** | × | × |
| Mission3 | × | × | × | × | × |
| Mission4 | × | × | × | × | × |
| Mission5 | ○（空オブジェクトのみ） | **○（追加）** | **○（追加）** | × | × |
| Quest1〜3 | × | × | × | × | Quest2 のみ ○ |

**済んだ対応:** Mission1 / 2 / 5 の既存 `RespawnPoints` オブジェクトに
`MissionReSpawnPoints` Component（GUID `f2527f44acf2f5b4ca7cf3f6183ae9e5`）を
追加し、`MissionMainScript.respawnPoints` から参照させた。
編集前にあった GameObject の位置をそのまま使うため、座標の推測は入っていない。

**実測結果（Unity Editor でシーンを開いて確認）:**

```
Mission1: respawnPoints -> (-1.07, 2.15)     ← 修正前は Vector3.zero
Mission2: respawnPoints -> (-1.07, 2.15)     ← 修正前は Vector3.zero
Mission5: respawnPoints -> (-1.07, 2.15)     ← 修正前は Vector3.zero
Mission3 / Mission4 / Quest1〜3: NONE
Quest1: MissionClearGate=yes（別項 P5-3 で確認）
```

3シーンとも座標が同一だが、これは3つの `RespawnPoints` オブジェクトが
同一の `m_LocalPosition`（`x:-1.0677723, y:2.1524441, z:-24.317114`）を
Duplication したデータであるため。シーン側の配置課題。

**残件:**

- **Mission3 / Mission4 / Quest1〜3 の RespawnPoints** — 実測の結果、
  **追加しない**。これらのシーンは原点中心で，原点スポーンが正当。
  Mission3 の Main Camera は `(-1.0677723, 2.1524441, -36.70681)` で
  Mission1 の RespawnPoints と x/y が一致し，Quest1 の地物は
  `x -8.0〜10.98 / y -2.6〜2.99` で原点はレベル内。座標を推測して
  オブジェクトを作ると，設定済みという誤解蕂生むだけでxelより悪くなる。
  なおカメラは `SetupPlayerCamera(playerObj.transform)` でプレイヤーを追従
  するため，カメラ位置はスポーン根拠にならない。
  未配線であることがログに出るよう `MissionMainScript.ResolveSpawnPosition()`
  に警告を入れてある。
- **LifeSlot UI / SoundManager** — **追加しない**。理由は下記。

### 🚫 P5-2 の LifeSlot / SoundManager は追加してはいけない

ロードマップは「Mission1 には LifeSlot UI と SoundManager があり、
Mission2/3/4 にはない」としていた。実測すると両方とも Mission1 の
アーティファクトが壊れているか不要だった。

- **SoundManager** — `SimpleAudioManager.Instance`
  (`Assets/Scripts/Audio/SimpleAudioManager.cs:9-25`) は**自己修復する**。
  シーンに無ければ `new GameObject("SimpleAudioManager")` を作って
  `AddComponent` し `DontDestroyOnLoad` する。Prefab を4シーンに複製すると，
  既存のインスタンスと自動生成されたインスタンスが二重に存在する。
  なお `SoundManager.prefab` の中身は `SoundManager` ではなく
  `SimpleAudioManager`（`SoundManager` は MonoBehaviour でない静的クラス）
- **LifeSlot** — Mission1 の `LIfeSlot` は `Image`＋`CanvasRenderer` の
   alone なオブジェクトで**プロジェクトスクリプトを持たない**。親 RectTransform
  (`Mission1.unity:888-897`) は `m_LocalScale: {x: 0, y: 0, z: 0}` で
  **Mission1 でも不可視**。参照するコードもゼロ。

壊れた可視アーティファクトを4シーンに複製するのは改善ではなく破損の拡散である。
LifeSlot を使うなら作り直し（Lifecycle UI として）、使わないなら削除する。

### P5-3. MissionClearGate の完了経路 ✅ — **修正済み（オフライン範囲）**

**1. Component の吊り忘れを修正** — `Assets/Scenes/Map/Mission/Quest1.unity:383`

削除済みスクリプトの残骸 GUID `1e642fb360b526847aa32b071584ff42` を
`MissionClearGate` の GUID `64dbaa79085990543a20a27f3c8862aa` に差し替え、
残存していた死フィールド（`system` / `soundManager` / `generalSceneMasterData` /
`script`）も削除した。これで `Start()` の自己修復
（`MissionClearGate.cs:15-24` の `AddComponent<BoxCollider2D>()`）が動き、
接触判定が発火する（`PlayerPrefabBase.prefab:115,143` に Rigidbody2D と
BoxCollider2D があるため `OnCollisionEnter2D` が呼ばれる）。

**2. 完了経路を実装**

監査時点で以下が判明していた: ゲートは `GameObject.Find("MissionMainScript")` に
`SendMessage("MissionClear")` を送っていたが、(i) Mission1〜5 のオブジェクト名は
`MainScript` で該当しない、(ii) `MissionClear()` を持つのは
`SkyFighterMainScript`（`AbstractMatchMainScript` 派生・`MissionMainScript` の
基底ではない）で、かつどのシーンからも参照されない、
(iii) 全シーンが使う `MissionMainScript` に `MissionClear` は存在しなかった。
つまりゲートに触れても何も起きなかった。

- `MissionClearGate.cs:37-51` — `GameObject.Find`＋`SendMessage` をやめ、
  `FindAnyObjectByType<MissionMainScript>()` で型から取得して直接呼ぶ
  （`FindFirstObjectByType` は Unity 6000.6 で CS0618 廃止のため `FindAny` を使用）
- `MissionMainScript.cs:153-170` — `public virtual void MissionClear()` を追加。
  `endFlag` で多重呼び出しを防ぎ、**オンライン時はサーバが結果所有者なので拒否**
  （他のローカル終了経路と同じ方針）。オフライン時のみ
  `EndMatchLocally()` で終了する
- `MissionMainScript.cs:180-188` — `BuildOfflineClearResult()` は
  `Result="Clear"` / `ResultType="Mission"` / `Success=true` のみを返す。
  残機・スコア・キル数はサーバの管理値でこのクライアントは保持していないため
  **意図的に埋めない**（0 表示になる）

既存の `EndMatchLocally()` → `OnServerMatchEnd()` が `Success` を読む経路と
`MissionScore.FromMissionResult()` にそのまま乗るため、結果保存と結果シーンへの
遷移は既存機構が処理する。

**残件（機能未構築・不具合ではない）:** オンラインのミッション完了は
**この改修で実装済み**（下記 P5-4）。

---

### P5-4. オンラインのミッションが完了しない ✅ — **修正済み**

**実態:** ミッションを終わらせる部品は全部できていた。
`MissionMatchRule`（目標数・残機・`IsMatchFinished`）、
`MissionMatchSituation`（目標進捗）、`MissionResultEvaluator`（結果評価）、
`MatchRoom.GameUpdate()` の終了判定（`MatchRoom.cs:314-317`）、
`MatchRoom.RecordObjectiveProgress()`（`MatchRoom.cs:602-612`）、
そして `MatchEndNotification` のブロードキャストまで。

**欠けていたのは1本の配線だった。** ルールと結果評価が読む
`ObjectiveProgress` を**動かすものが、どこにもなかった。**

`MessageType.MissionCompleteNotification`
（`OpenGSCore/MessageType.cs:273`）は共有コントラクトに定義されていたが、
クライアントからもサーバからも**一度も参照されていなかった**。
そのためオンラインのミッションは、時計か残機切れでしか終わらなかった。

- `ClientNetworkManager.ReportMissionClear()`
  (`Assets/Scripts/Networking/ClientNetworkManager.cs`) — 完了を
  **報告**する。試合を終わらせない
- `MissionMainScript.MissionClear()` — オンライン時は報告へ、
  オフライン時のみ `EndMatchLocally()`。オフラインは報告先がないため
  クライアントで決める必要があり，オンラインはサーバのルールが決める
- `MatchRoomHandler.HandleMissionComplete()`
  (`OpenGSServer/Server/Event/MatchRoomHandler.cs`) — `ProcessSystemEvent`
  から呼ばれる。試合中か・ミッション situation か・部屋員かを検証してから
  `RecordObjectiveProgress()`。進行度はクライアントが決めず、
  **ルールが試合を終わらせる**
- `MatchUDPServer.TryDispatchMatchEvent()` — `MissionCompleteNotification` を
  システムイベントとして分類し，マッチ経路に流す
- `GameMessageTypes.MissionComplete` — 共有コントラクトの名前を
  サーバ側で名指せるようにした

**テスト:** `OpenGSServer/Tests/MissionObjectiveProgressTests.cs` に12件。
ルール→situation→tick→評価器の連鎖全体を固定している（個別メソッドの
テストではないのは，progress をルールに書いても situation や評価器が
追従しなければ，部屋は完了と宣言したミッションを握りつぶしたままになるから）。
既存の337件は回帰なし、349件全て成功。

---

### P6-3. NetworkEventDeserializer が約60メッセージを未処理 ✅ — **修正済み**
`Assets/Scripts/Network/NetworkEventDeserializer.cs`

52 種類を処理、`RUDPMessageTypes` の112 定数に対して約60が未処理。
未処理は `DeserializeUnsupported()` で警告＋null 返却になっていた。

**当時の記載は実態と違っていた。** 監査の結果:

- ロードマップ記載の `ItemTypes` / `VoteTypes` は**コードに存在しない**
- 未処理の多くは (a) クライアント→サーバのリクエスト（`ClientConnect`、
  `ShootRequest` 等）でそもそも受信されない、(b) `GeneralServerNetworkManager` を
  通る別経路で `WaitroomNetworkManager` が処理している
  （`WaitroomNetworkManager.cs:28,521-608` は `ClientNetworkManager` とは
  別のマネージャを resolve している）、のいずれか
- したがってこれに「デシリアライザを足す」のは重複配信の危険を生むだけ

**実際の欠陥はログだった。** 到達が正常系であるものを warning で、
さらにペイロード全量を出していたため、正常なロビー接続だけで
console が大きな警告で埋まっていた。

- `DeserializeUnsupported()` — warning → `Debug.Log` に格下げ。
  ペイロード全量ではなく**型名＋フィールド名のみ**を出力
- `MessageType` 欠落時の警告も同様にペイロード全量出力を削除
- `ClientNetworkManager` の TCP/UDP default 経路 — ペイロード全量出力を削除し、
  型名のみに。TCP 側は同じ内容を二重に出していた

comment に「なぜ約60件あるのか」を残してあるので、数が多いことが
未完了の backlog に見えない。

---

## 監査で判明した付随所見（未修正・任意）

| 内容 | 箇所 | 影響 |
|---|---|---|
| TDM の `OnPlayerDead(ETeam,ETeam)` への配線 | `TDMMatchMainScrip.cs:ProcessKillEvent` | オフライン時に集計するよう配線済み（下記） |
| `Quest2.unity` の削除済みPrefab参照 | `Quest2.unity`（旧`guid: 07ae2989…`） | 除去済み（下記）。参照元・クラス・他シーン全て不存在を確認 |
| Mission1/2/5 の `RespawnPoints` が同一座標 | 各シーン | 3シーンとも `(-1.07, 2.15)`。オブジェクトの複製由来の値 |
| TCP/UDP 再接続のバックオフ | `ClientNetworkManager.cs:86-100` | 指数バックオフ＋上限に改善済み（下記） |
| `SkyFighterMainScript` 等の削除 | 旧`Mission/SkyFighterMainScript.cs`等 | 削除済み（下記）。`Sound.cs` の同名enum値とは無関係 |

---

## ネットワークプロトコル制約

**UDPペイロード: JSON限定。LiteNetLibはクライアント側では不使用。**

- Unityクライアント (OpenGSR) のプラットフォーム制約により、クライアント側では
  LiteNetLibを採用しない。サーバー (OpenGSServer) はLiteNetLibをトランスポート
  層として使用してもよいが、ペイロードは必ずJSON文字列形式とする。
- クライアントの `ClientNetworkManager` はすでにJSON形式で送受信しているため、
  この制約はクライアント側の既存コードに合致する。

---

## 完了マイルストーン（追跡対象外）

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
- ~~Phase 1 (P1-1〜P1-10)~~ ✅ クラッシュ10件すべて修正済み
- ~~Phase 2 (P2-1〜P2-8)~~ ✅ 進行阻害8件すべて修正済み
- ~~P4-1〜P5, P4-7, P4-8~~ ✅ デッドコード削除完了
- ~~P6-1, P6-2, P6-4, P6-5~~ ✅ ネットワーク4件修正済み
- ~~P5-3~~ ✅ MissionClearGate の Component 吊り忘れを修復し、オフラインの完了経路を実装
- ~~P3-7~~ ✅ MatchManager を複数購読対応にし、購読ごとの例外分離を追加
- ~~P3-5~~ ✅ DM のサーバ終了経路でもオフライン結果を保存
- ~~P4-6~~ ✅ AIPlayerController の pass-through override 5件を削除
- ~~P6-3~~ ✅ 未処理メッセージのログを warning＋全量出力から型名のみの Debug.Log へ
- ~~P5-2 (Mission1/2/5)~~ ✅ MissionReSpawnPoints を配線（スポーンが `(0,0,0)` から実座標へ）
- ~~TDMオフラインキル集計~~ ✅ `ProcessKillEvent` から `OnPlayerDead` を呼ぶよう配線（オフライン時のみ。オンラインはサーバ集計のまま二重計上を回避）
- ~~TDMオフライン生死橋~~ ✅ kill/deathイベントがmatch scriptに届いていなかった問題を修正。`GameEventBroker` を直接購読し、オフライン時のみ集計・リスポーン起動（オンラインはサーバ駆動のまま）
- ~~デッドコード掃除~~ ✅ 無参照の `GamePlayerManager.Status` と未使用の `OfflineLoadingScene.mapMasterdata` を削除
- ~~Quest2のmissing参照~~ ✅ 削除済みPrefab（`QuestSceneStorageManager`）への `PrefabInstance` ブロック69行を除去。GUIDはワークスペース全体から消滅
- ~~SkyFighter削除~~ ✅ `SkyFighterMainScript.cs`・空の `SkyFighterManager.cs` を削除し、`MissionModeCollection` の対応フィールドを除去。`AutoCreateIfMissing` はエディタ用でありコレクション自体が未配置のため連鎖全体が不活性だった。`Sound.cs` の同名enum値は無関係のため温存
- ~~再接続バックオフ~~ ✅ TCP/UDP再接続を指数バックオフ（1s→最大30s＋ジッター）にし、成功時と切断時にリセット