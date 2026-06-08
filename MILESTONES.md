# OpenGSR Milestones

このドキュメントはリポジトリの現状に基づいた優先順位を追跡する。
各マイルストーンは実際のコードベース調査に基づいてステータスを評価している。

凡例:
- ✅ **完了** — ほぼ手が入っていて実用レベル。フォローアップがある場合は注釈。
- 🟡 **途中** — コア機能は動くが、スコープ内に未完了の項目あり。
- 🔴 **未着手** — 設計/計画のみ、コードなし。
- ⛔ **凍結** — 優先度の関係で後回し。

---

## 現状サマリー

| Milestone | ステータス | 注釈 |
|---|---|---|
| **U0** — Unity境界削減 | 🔴 未着手 | 唯一の本当の未着手エリア |
| **M0** — シーン名整理 | ✅ 完了 | GeneralSceneMasterData で一元管理 |
| **M1** — オンラインロビー | 🟡 完了 | ローカルサーバで全機能動作。本番バックエンド不足 |
| **M2** — オフライン待機部屋 | ✅ 完了 | マップ選択、Bot追加、ゲーム開始 |
| **M3** — マッチフロー | 🟡 DM/TDM/CTF完了 | TSUV(TeamSurvival)はスタブ |
| **M4** — アカウント/セーブ/ショップ | 🟡 完了 | プロトコル・ローカル永続化済み。本番サーバ不足 |
| **M5** — 戦闘フィードバック | ✅ 完了 | 全システム本番実装済み（LagCompensation含む） |
| **M6** — アイテムループ | ✅ 完了 | UseItem含め完全実装。enum重複が微量 |
| **S0** — プロトコル整理 | ✅ 完了 | MessageType.Normalize() 全適用済み |
| **S1** — サーバサイドロビー | 🟡 ローカル | インメモリ＋JSON永続化。本番品質ではない |
| **S2** — マッチサーバ | 🟡 ローカル | メッセージ処理できるがループバックのみ |
| **S3** — ローディングHS | ✅ 完了 | エンドツーエンド動作確認済み |
| **S4** — ミッション系 | 🟡 途中 | シーンは8つ存在。Mission2-4はMainScript未アタッチ |
| **S5** — テスト | 🟡 インフラのみ | Coreに1テスト。Unity側の自動テストなし |

---

## 現在の実装優先順位

```
1. U0   — Unity境界削減（戦略的最重要）
2. M3-α — TSUV (TeamSurvival) マッチスクリプト完成
3. S4-α — Mission2-4 の MainScript アタッチと動作確認
4. M1   — オンラインロビー: ローカルサーバ→実サーバ接続
5. S1   — サーバの本番化
```

---

## U0. Unity境界削減

**ステータス: 🔴 未着手**

**目標:** ゲームルールと永続状態を Unity 依存のスクリプトから OpenGSCore (Pure C#) に移動する。

**スコープ:**

1. **Phase 1 — PlayerStatus 統合（最優先）**
   - `Assets/Scripts/Interface/PlayerStatus.cs` (Unity+UniRx) と
     `Packages/com.opengs.logic/Player/PlayerStatus.cs` (Pure C#) の重複解決
   - Unity側にある Armor, Kill/Deathトラッキング, ReactiveProperty を Core に取り込むか、
     アダプタ層に隔離するかの判断が必要
   - 現状: 両者は概念は同じだが機能セットが分岐しており、どちらも単独では置き換え不能

2. **Phase 2 — AbstractPlayer のドメインロジック抽出**
   - 1114行中 ~40-50% が純粋なドメインロジック（HP管理、ダメージ計算、パワーアップ状態、チーム、フラグ）
   - Coroutine主体のタイマー処理、MonoBehaviour依存は Unity 層に残す
   - 具体ターゲット:
     - `GetHP()`, `AddDamage()`, `Heal()`, `AttackMultiplier()` などの stat accessors
     - `OnDead()`, `OnSpawn()`, `OnReSpawn()` の state transition 定義
     - `Berserk()`, `IncreaseAttack()`, `SpeedUp()`, `Invisible()` などのパワーアップロジック
     - `EnemyFlagCaptured()`, `HasEnemyFlag()` などのフラグ状態

3. **Phase 3 — MatchMainScript のルール抽出**
   - AbstractMatchMainScript (948行) からタイマー制御、試合終了条件を Core の Rule evaluator に委譲

**完了条件:**
- Unity クラスは入力転送とプレゼンテーションに徹し、ルールを所有しない
- Core のクラスで gameplay state を Unity 型なしで表現可能
- `PlayerStatus` の二重管理が解消されている

---

## M3-α. TeamSurvival (TSUV) マッチスクリプト完成

**ステータス: 🟡 未完了 — TSUV がスタブ**

DM/TDM/CTF は完全に実装済み。**TSUVMainScript のみがスタブ**。
プレイヤー生成と `GoToResult()` しかできない。

**スコープ:**
- `Assets/Scripts/Match/TSUVMainScript.cs` — 現在ほぼ空。CreateMyPlayer + GoToResult のみ
- `Packages/com.opengs.logic/Match/Rule/Team/TSuvMatchRule.cs` — Core側のルール評価器は存在確認済み
- `Packages/com.opengs.logic/Match/Rule/TeamSurvivalResultEvaluator.cs` — Result evaluator も存在
- `Packages/com.opengs.logic/Match/Result/TSuvMatchResult.cs` — Result モデルも存在
- `Packages/com.opengs.logic/Match/Situation/TeamSurvivalMatchSituation.cs` — Situation も存在
- Core 側の型は揃っているので、Unity の TSUVMainScript に命を吹き込むだけ

**完了条件:**
- Survival ルール（復帰なし、制限時間/最終生き残り判定）が TSUV で動作
- チーム割り振り、リスポーン管理、試合終了→結果画面遷移
- DM/TDM/CTF と同じ品質で TeamSurvival がプレイ可能

---

## S4-α. ミッションシーンの動作確認と補完

**ステータス: 🟡 Mission1/5/Quest1-3 は実装済み。Mission2-4 が空**

**現状詳細:**
- ✅ Mission1: `MissionMainScript` 完全アタッチ済み（シーン300行以上）
- ❌ Mission2: MainScript が Transform のみ — スクリプト未アタッチ
- ❌ Mission3: MainScript が Transform のみ — スクリプト未アタッチ
- ❌ Mission4: MainScript が Transform のみ — スクリプト未アタッチ (169行)
- ✅ Mission5: MissionMainScript + MissionClearGate + RespawnPoints + ステージスプライト完備
- ✅ Quest1: フルレンダリングステージ + MissionMainScript + MissionClearGate
- ✅ Quest2: フルレンダリングステージ + QuestSceneStorageManager
- ✅ Quest3: フルレンダリングステージ（別GlobalVolume profile）

**スコープ:**
- Mission2-4 の MainScript GameObject に `MissionMainScript` コンポーネントをアタッチ
- MissionAndQuestLobbyScene → MissionLobbyScene → EnterMissionX → 読み込み → MissionMainScript の流れを通しで確認
- MissionMainScript 自体は完成しているので、シーン設定だけの問題

**完了条件:**
- Mission1-5 すべてがプレイヤー生成→ミッションクリア/失敗→結果画面までシームレスに動作
- クエストモードのゲームプレイループも同様

---

## M1. オンラインロビー: ローカル→実サーバ接続

**ステータス: 🟡 ローカルサーバで全機能動作。実バックエンド不足**

**現状:**
- `OnlineLobbyScene.cs`: `LocalTestTcpServer` を使用してルーム作成/参加/退出/フィルター/QuickJoin/チャット すべて動作
- `MatchRoomManager.cs`: WaitRoom のライフサイクル管理まで実装済み
- `GeneralServerNetworkManager.cs`: インメモリでアカウント/ルーム/ショップ/ギルド/フレンド管理
- 永続化は `Application.persistentDataPath` の JSON ファイル

**スコープ:**
- `GeneralServerNetworkManager` のモック/ローカルと実サーバモードの分離
- `LocalTestTcpServer` の責務を明確にし、実サーバクライアントとの差を最小化
- サーバ側の `OpenGSServer` リポジトリとの接続インタフェース整備

**完了条件:**
- ログイン → ロビー → ルーム作成/参加 → 待機部屋 → ローディング → マッチ の全フローが実サーバで動作
- アカウント作成、クレジット、所有権がサーバ経由で永続化される

---

## M4. アカウント/セーブ/ショップ

**ステータス: 🟡 完了。**

実装自体は完了している。S1 のサーバ本番化とセットで扱う。

- `CreateNewAccountScene.cs`: アカウント作成＋`GeneralServerNetworkManager` への送信まで実装
- `OnlineShopService.cs`: GetItems/Purchase/Equip/Unequip/GetCredits/IsPurchased/IsEquipped すべて実装。ローカルフォールバック完備
- `UserSaveManager.cs`: ローカル JSON 永続化
- `EquipmentSaveManager.cs`: 装備永続化

M1 のサーバ接続が完成したら自動的にこの領域もカバーされる。

---

## S1. サーバサイドロビー・アカウント状態の本番化

**ステータス: 🟡 ローカルインメモリのみ**

M1/M4 と連動する。`GeneralServerNetworkManager` のインメモリ実装を実際のサーバ通信に置き換える。

---

## S2. マッチサーバコア

**ステータス: 🟡 ループバックでメッセージ処理可能だが本番品質ではない**

`LocalTestMatchRUDPServer.cs` が以下を処理:
- PlayerInput, Shoot, Death, Kill, Grenade, Flag events, ItemUse, Buff/Debuff
- しきい値ベースの試合終了検出（3デスで終了などデバッグ用）

本番マッチサーバは `OpenGSServer` リポジトリの範囲。

---

## S5. テスト

**ステータス: 🟡 インフラは充実。自動テストは限定的**

- `Packages/com.opengs.logic/Tests/Editor/LoadingGaugeTests.cs` — Coreに1ユニットテスト確認済み
- `LocalTestTcpServer.cs` / `LocalTestMatchRUDPServer.cs` — 充実したローカルテストサーバ
- Unity Test Framework の本格活用は未確認
- 優先順位: テストは各機能実装と並行して追加。単独マイルストーンとしては低優先

---

## 将来/低優先

以下のマイルステーンは完了しているか、優先度が低いため凍結:

| Milestone | 判断理由 |
|---|---|
| ~~M0~~ ✅ | シーン名一元管理完了 |
| ~~M2~~ ✅ | オフライン待機部屋完全動作 |
| ~~M3 DM/TDM/CTF~~ ✅ | 完全実装 |
| ~~M5~~ ✅ | 全戦闘フィードバック実装済み |
| ~~M6~~ ✅ | アイテムループ完全実装（UseItem含む） |
| ~~S0~~ ✅ | プロトコル正規化完了 |
| ~~S3~~ ✅ | ローディングハンドシェイク完了 |
| ~~S4 (Mission5/Quest1-3)~~ ✅ | 該当シーンは実装済み |
| ⛔ 後継 | 完了済みマイルストーンは追跡対象外 |

---

## 補足ドキュメント

- サーバサイド詳細計画: `SERVER_ROADMAP.md`
- 並行開発計画: `PARALLEL_DEV_PLAN.md`
- アーキテクチャ境界: `ARCHITECTURE_BOUNDARY.md`
- AI開発メモ: `docs/codex-memo.md`
