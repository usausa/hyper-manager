# 🧬 Hyper-V 連携

Agent（`HyperManager.Agent`）が Hyper-V から情報を取り出し、操作する仕組みをまとめます。実装は `src/HyperManager.Agent/HyperV/` にあります。

---

## 🔌 接続

| 項目 | 内容 |
|---|---|
| 📚 ライブラリ | CIM（`Microsoft.Management.Infrastructure`）。取得も操作も同じ仕組みを使います |
| 🖥️ 接続先 | ローカルの CIM（`CimSession.Create(null)`）。リクエストごとにセッションを作ります |
| 🧬 Hyper-V | 名前空間 `root\virtualization\v2` |
| 📊 ホストメトリクス | 名前空間 `root\cimv2` |

---

## 📋 VM 一覧の取得

`Msvm_SummaryInformation` を 1 回の問い合わせで取得します。

| プロパティ | 型 | 使い道 |
|---|---|---|
| `Name` | string | VM の ID（GUID）→ `id` |
| `ElementName` | string | VM の名前 → `name` |
| `EnabledState` | uint16 | 状態 → `state`（[🚦 状態の対応](#-状態の対応)） |
| `ProcessorLoad` | uint16 | CPU 使用率（%）→ `cpuUsagePercent` |
| `MemoryUsage` | uint64 | メモリ使用量（MB）→ `memoryUsageMb` |
| `UpTime` | uint64 | 稼働時間（ミリ秒）→ `uptime` |
| `Version` | string | 構成バージョン → `version` |

> 💡 CPU・メモリ・稼働時間は、実行中（`EnabledState = 2`）の VM だけ値を返します。

---

## 🚦 状態の対応

`EnabledState` の値は `root\virtualization\v2` のもので、`Microsoft.HyperV.PowerShell.VMState`（`Get-VM` の `State`）と同じです。変換は `VmStateMapper` で行います。

| `EnabledState` | Hyper-V での意味 | `VmState` |
|---|---|---|
| 2 | Running | 🟢 `Running` |
| 3 | Off | ⚪ `Off` |
| 4 | Stopping | ⏳ `Stopping` |
| 6 | Saved | 🔵 `Saved` |
| 9 | Paused | 🟡 `Paused` |
| 10 | Starting | ⏳ `Starting` |
| 11 | Reset | ⏳ `Starting` |
| 32773 | Saving | ⏳ `Saving` |
| 32776 | Pausing | ⏳ `Pausing` |
| 32777 | Resuming | ⏳ `Resuming` |
| 32779 | FastSaved | 🔵 `Saved` |
| 32780 | FastSaving | ⏳ `Saving` |
| 32781 | ForceShutdown | ⏳ `Stopping` |
| 32782 | ForceReboot | ⏳ `Starting` |
| 上記以外（`*Critical` など） | | ❔ `Other`（操作ボタンを出さない） |

> ⚠️ 旧 `root\virtualization`（v1）の値（一時停止 32768・保存済み 32769 など）とは異なります。

---

## 🎛️ 操作

### 🔄 状態の変更（`Msvm_ComputerSystem.RequestStateChange`）

| 操作 | `RequestedState` | Hyper-V での意味 |
|---|---|---|
| ▶️ 開始 | 2 | Enabled |
| ⏯️ 再開 | 2 | Enabled |
| ⏸️ 一時停止 | 9 | Quiesce |
| 💾 状態保存 | 6 | Offline |
| ⛔ 強制停止 | 3 | Disabled |

1. 🔍 VM の ID を GUID として検証・正規化してから `Msvm_ComputerSystem`（`Name = '<GUID>'`）を探します。見つからなければ 404 です
2. 📨 `RequestStateChange` を呼び出します
3. ⏳ 戻り値が 4096（ジョブ開始）なら、`Msvm_ConcreteJob` の `JobState` を一定間隔（既定 200 ミリ秒）で確認し、7（Completed）になるまで待ちます（既定で最大 300 秒）。7 より大きい値は失敗です

### 📴 シャットダウン（`Msvm_ShutdownComponent.InitiateShutdown`）

1. 🔍 `Msvm_SystemDevice` で VM に関連付いた `Msvm_ShutdownComponent` を探します。ない場合は「統合サービスが無効の可能性があります」として失敗します
2. 📨 `InitiateShutdown(Force, Reason = "HyperManager")` を呼び出します。ゲスト OS の終了は待ちません

### 🔢 戻り値

`RequestStateChange` と `InitiateShutdown` の戻り値は次のように扱います。

| 値 | 意味 | 扱い |
|---|---|---|
| 0 | 完了 | ✅ 成功 |
| 4096 | ジョブを開始した | ✅ 成功（状態変更はジョブの完了を待つ） |
| 32768 | 失敗しました | ❌ |
| 32769 | アクセスが拒否されました | ❌ |
| 32770 | サポートされていません | ❌ |
| 32771 | 状態が不明です | ❌ |
| 32772 | タイムアウトしました | ❌ |
| 32773 | パラメーターが不正です | ❌ |
| 32774 | システムが使用中です | ❌ |
| 32775 | 現在の状態では実行できません | ❌ |
| 32776 | データ型が不正です | ❌ |
| 32777 | システムを利用できません | ❌ |
| 32778 | メモリが不足しています | ❌ |

失敗はメッセージ（例: `状態の変更に失敗しました: 現在の状態では実行できません (戻り値=32775)`）として HyperManager に返り、画面と操作履歴に残ります。

---

## 📊 ホストメトリクス

| 指標 | クラス | プロパティ | 備考 |
|---|---|---|---|
| 🧮 CPU 使用率 ① | `Win32_PerfFormattedData_Counters_HyperVHypervisorLogicalProcessor`（`Name = '_Total'`） | `PercentTotalRunTime` | 最優先。Hyper-V ホストの実際の負荷に近い値です |
| 🧮 CPU 使用率 ② | `Win32_PerfFormattedData_PerfOS_Processor`（`Name = '_Total'`） | `PercentProcessorTime` | ① が取れないとき |
| 🧮 CPU 使用率 ③ | `Win32_Processor` | `LoadPercentage` | ①② が取れないとき。すべて失敗すると 0% |
| 🧠 メモリ | `Win32_OperatingSystem` | `TotalVisibleMemorySize` / `FreePhysicalMemory` | KB 単位。使用量 = 総量 − 空き |
| 💽 ディスク | `Win32_LogicalDisk`（`DriveType = 3`） | `DeviceID` / `Size` / `FreeSpace` | ローカルの固定ディスクだけ |

> 💡 メトリクスと VM 一覧は並行して取得します。メトリクスだけ失敗したときは、メトリクスを省略してスナップショットを返します。

---

## 💓 ヘルスチェック

Agent の `/health` は `Msvm_VirtualSystemManagementService` を問い合わせ、Hyper-V の管理サービスにアクセスできるかを確認します。

| 状況 | 結果 |
|---|---|
| ✅ アクセスできる | `Healthy` |
| ❌ Hyper-V が無効、または名前空間がない | `Unhealthy`（接続エラー） |
| ❌ 管理者権限がない | `Unhealthy`（管理サービスが見つからない） |

---

## 🔐 権限

- 🛡️ Hyper-V の情報は、権限のない利用者には返されません（エラーではなく「0 件」になります）。そのため Agent は**管理者権限**で動かします。
- 🔎 Agent は起動時と `/api/host` で、自分が管理者として動いているか（`isElevated`）を確認します。画面のホストカードの「管理者／非管理者」はこの値です。

---

## 🧪 手元で確かめる

管理者の PowerShell で次のコマンドを実行すると、Agent が使う値を直接確認できます（どれも読み取りだけです）。

```powershell
# VM の状態（PowerShell の VMState と数値）
Get-VM | Select-Object Name, State, @{ n = 'Value'; e = { [int]$_.State } }

# Agent が読む値そのもの
Get-CimInstance -Namespace root\virtualization\v2 -ClassName Msvm_SummaryInformation |
    Select-Object ElementName, Name, EnabledState, ProcessorLoad, MemoryUsage, UpTime, Version

# RequestStateChange で指定できる値の一覧
(Get-CimClass -Namespace root\virtualization\v2 -ClassName Msvm_ComputerSystem).CimClassMethods['RequestStateChange'].Parameters['RequestedState'].Qualifiers['ValueMap'].Value
```
