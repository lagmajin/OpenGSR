# OpenGSR Rescue Modules Documentation

このファイルは、旧プロジェクト (OpenGS) から救出・改善された主要なモジュールとクラスの索引です。
AIがプロジェクトの構造を理解し、機能拡張を容易にすることを目的としています。

## 1. Core Services (抽象化レイヤー)

### `IInputService` / `UnityInputService` / `EnemyInputService`
- **概要**: 入力（マウス、キーボード、AIの意思）を抽象化。
- **場所**: `Assets/Scripts/Interface/IInputService.cs`, `Assets/Scripts/Systems/UnityInputService.cs`, `Assets/Scripts/Enemy/EnemyInputService.cs`
- **AIへのヒント**: プレイヤーもAIもこのインターフェースを介して操作されます。新しい操作（回避など）を追加する場合はこのインターフェースを拡張してください。

### `ISoundService` / `SoundService`
- **概要**: 音声再生の抽象化。`SoundMasterData` からのクリップ解決を担当。
- **場所**: `Assets/Scripts/Audio/ISoundService.cs`, `Assets/Scripts/Audio/SoundService.cs`
- **AIへのヒント**: `SoundManager.Instance` への直接参照は避け、このサービスを `[Inject]` して使用してください。

### `IEffectService` / `EffectService`
- **概要**: エフェクト（着弾火花、カメラシェイク等）の管理。
- **場所**: `Assets/Scripts/Systems/EffectService.cs`
- **機能**: `PlayImpactEffect`, `ShakeCamera` (DOTween使用)。

---

## 2. Player & Movement (プレイヤー制御)

### `PlayerController`
- **概要**: プレイヤーの具象クラス。物理移動、ブースター、武器使用を統合。
- **場所**: `Assets/Scripts/Player/AsmExport/PlayerController.cs`
- **改善点**: 旧プロジェクトの `Character.cs` を物理ベースにアップグレード。

### `GroundCheck`
- **概要**: 接地判定。`OverlapBox` を使用。
- **場所**: `Assets/Scripts/Player/AsmExport/GroundCheck.cs`

### `WeaponSlots`
- **概要**: 武器の3スロット管理 (Main, Secondary, Special)。
- **場所**: `Assets/Scripts/Player/AsmExport/WeaponSlots.cs`

---

## 3. Weapons (武器システム)

### `AbstractGunController`
- **概要**: 全ての銃の基底クラス。熱(Heat)、ブレ(Spread)、リロードを管理。
- **場所**: `Assets/Scripts/Weapon/AsmExport/AbstractGunController.cs`

### 具象武器クラス
- **`AssaultRifleController`**: 標準連射。
- **`ShotgunController`**: 散弾（複数ペレット）。
- **`MachineGunController`**: 高速連射＋熱による精度低下。
- **`GrenadeLauncherController`**: 物理弾（重力あり）。
- **`HandgunController`**: セミオート。

---

## 4. Enemy AI (AIシステム)

### `EnemyBrainBase` / `GunnerEnemyBrain` / `MonsterEnemyBrain`
- **概要**: 敵の思考ルーチン。
- **場所**: `Assets/Scripts/Enemy/`
- **仕組み**: 思考結果を `EnemyInputService` に書き込み、`PlayerController` や武器を操作させる。
