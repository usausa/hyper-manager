# HyperManager 仕様

> 最終更新: 2026-10-31
> hv-monitor の仕様（確定版）を引き継ぎ、template-blazor-server の構成で再構築した内容を記述します。

---

## 1. 概要・目的

複数の Hyper-V ホスト上の仮想マシン（VM / ゲスト）を、Web ブラウザから一覧表示・監視し、
開始／停止などの基本操作を行うアプリケーション。

- 対象ユーザー: ホストを管理する運用者・開発者（信頼できるネットワーク内での利用を想定）
- 提供形態: 各ホストで動作する API サーバ（`HyperManager.Agent`）と、中央の Web UI（`HyperManager.Host`）の 2 種類のプロセス構成

### スコープ

| 区分 | 内容 |
|---|---|
| ✅ 対応済み | 複数ホストの VM 一覧統合表示、状態・CPU・メモリ・稼働時間の表示、開始／再開／一時停止／状態保存／シャットダウン／強制停止、ホストメトリクス（CPU・メモリ・ディスク）、自動更新（定期ポーリング）、手動更新、ログイン認証、操作履歴（検索・CSV 出力）、メトリクス出力（Prometheus / OTLP） |
| 🔜 将来候補 | HTTPS / 正規証明書、mTLS、スナップショット（チェックポイント）操作、コンソール接続、アカウント管理画面、操作履歴の保持期間管理、メトリクスの時系列グラフ |
| ❌ 非対象 | VM の新規作成・削除、仮想ディスク／ネットワークの構成変更 |

---

## 2. 全体構成

画面（UI）と Hyper-V 操作を別プロセスに分離し、Hyper-V 操作を **API サーバ（Agent）**として各ホスト上で動作させる。
Host は設定ファイルに複数の Agent を登録し、**1 つの画面で複数ホストをまとめて管理**する。

```
 Browser
    |  Blazor Server (SignalR) / Cookie authentication
    v
+--------------------------------------------------------------+
| HyperManager.Host                                            |  net10.0 / no administrator rights
|  - MonitorWorker refreshes all hosts periodically            |
|  - MonitorService holds the latest snapshot for all pages    |
|  - OperationService executes operations and records history |
|  - AgentClient (HttpClient + X-Api-Key)                      |
|  - SQLite: Account / OperationLog                            |
+--------------------------------------------------------------+
    |  HTTP/REST (parallel, one request per host)
    v
+--------------------------------------------------------------+
| HyperManager.Agent  (one per Hyper-V host)                   |  net10.0-windows / administrator (Windows service)
|  - Minimal API + API key authentication                      |
|  - HyperVService (CIM: root\virtualization\v2, root\cimv2)   |
+--------------------------------------------------------------+
```

---

## 3. プロジェクト構成

```
HyperManager.slnx
├─ src/
│  ├─ HyperManager.Contracts/    Agent API の契約（DTO・ルート・ヘッダー名）           net10.0
│  ├─ HyperManager.Core/         Host 側の業務ロジック（RootNamespace: HyperManager）    net10.0
│  │   ├─ Accessors/             SQL（2-way SQL / Smart.Data.Accessor）
│  │   ├─ Domain/                VmOperationPolicy など
│  │   ├─ Infrastructure/Agents/ AgentClient（Agent API のクライアント）
│  │   ├─ Models/                HostStatus / MonitorSnapshot / Entity
│  │   └─ Services/              MonitorService / OperationService / AccountService
│  ├─ HyperManager.Host/         中央 Web UI（Blazor Server / MudBlazor）と Web API      net10.0
│  ├─ HyperManager.Agent/        Hyper-V ホストの API サーバ                             net10.0-windows
│  └─ HyperManager.AppHost/      開発用 Aspire AppHost                                   net10.0
└─ tests/
   ├─ HyperManager.UnitTests/         Core / Host の単体テスト（bUnit）
   ├─ HyperManager.IntegrationTests/  Host の結合テスト（WebApplicationFactory）
   ├─ HyperManager.AgentTests/        Agent の API テスト（net10.0-windows）
   └─ HyperManager.E2ETests/          Playwright による E2E テスト
```

| プロジェクト | 主な依存パッケージ |
|---|---|
| Contracts | （なし） |
| Core | Usa.Smart.Data.Accessor, BunnyTail.ServiceRegistration, Microsoft.Extensions.Http |
| Host | MudBlazor, Usa.Smart.Blazor, Microsoft.Data.Sqlite, MiniDataProfiler, CsvHelper, Serilog, OpenTelemetry, NSwag |
| Agent | Microsoft.Management.Infrastructure, Serilog, OpenTelemetry, NSwag |

- DTO と API のパスは `HyperManager.Contracts` に集約し、Agent と Host（AgentClient）で共有する。
- Host は CIM を参照しないため `net10.0`（Windows 非依存）で動作する。
- テスト用に、Agent の Hyper-V アクセスは `IHyperVService`、Host の Agent 呼び出しは `IAgentClient` で差し替え可能にする。

---

## 4. 機能仕様

### 4.1 VM 一覧表示（`/`）

- 設定された全ホストの VM を 1 つの表（`MudDataGrid`）に統合表示する。
- 表示項目: ホスト / 名前 / 状態 / CPU 使用率(%) / メモリ使用量(MB) / 稼働時間 / バージョン / 操作
- 状態は `MudChip` で色分けする（実行中=緑、停止=既定、一時停止=黄、保存=青 など）。
- ホスト（複数ホスト登録時）と名前で絞り込み、列でソートできる。
- 最終更新時刻を表示する。

### 4.2 ホストメトリクス

ホストごとのカード（`HostCard`）に、ホスト自身の稼働状況を表示する。

- 表示名とマシン名、管理者権限（`isElevated`）と到達可否のバッジ
- CPU 使用率、メモリ 使用量 / 総量、各固定ディスクの空き / 総量（ゲージ）
- 使用率に応じてゲージの色を変える（90% 以上=エラー、75% 以上=警告、それ未満=通常）。
- 到達不可のホストはカードにエラー内容を表示し、他ホストの表示は継続する。
- メトリクスを取得できないホストは、メトリクス部分のみ非表示とし VM 一覧の表示は継続する。

### 4.3 VM 操作

各行の操作ボタンから実行する。実行前に確認ダイアログ（`VmOperationDialog`）を表示する。

| 操作 | 概要 | 実行可能な状態 |
|---|---|---|
| 開始 (Start) | VM を起動する | 停止 / 保存済み |
| 再開 (Resume) | 一時停止から復帰する | 一時停止 |
| 一時停止 (Pause) | 一時停止する | 実行中 |
| 状態保存 (Save) | メモリ状態を保存して停止する | 実行中 |
| シャットダウン (Shutdown) | ゲスト OS に正常終了を要求する（統合サービス必須） | 実行中 |
| 強制停止 (Turn Off) | 電源を即時オフする（データ損失の警告を表示） | 実行中 / 一時停止 |

- 実行可否は `VmOperationPolicy` が判定し、実行できない操作のボタンは表示しない。
- 遷移中の状態（起動中・停止中・保存中・一時停止中・再開中）ではすべての操作ボタンを表示しない。
- 操作ボタンは Administrator ロールのユーザーにだけ表示する。

### 4.4 操作のフィードバック

- 操作中は全ボタンを無効化し、`ProgressOverlay` で進行中を表示する。
- 完了／失敗をスナックバーで通知し、失敗時はエラーメッセージを画面にも表示する。
- 操作完了後に全ホストを再取得する（結果は開いている全画面に反映される）。
- 状態遷移は Agent 側で完了まで待機する（`Msvm_ConcreteJob` 監視、既定タイムアウト 5 分）。

### 4.5 操作履歴（`/operations`）

- `OperationService` が操作の成否を `OperationLog` テーブルに記録する（実行日時・ユーザー・ホスト・VM ID・VM 名・操作・成否・メッセージ）。
- 実行ユーザーと日時はテンプレートの `ServiceContext`（画面のイベント境界で開始）から取得する。
- 画面では新しい順にページング表示し、ホスト名・VM 名で部分一致検索できる。CSV で出力できる。
- アプリログにも監査用に記録する。

### 4.6 自動更新

- Host の `MonitorWorker`（`BackgroundService`）が `PeriodicTimer` で設定間隔（既定 10 秒）ごとに `MonitorService.RefreshAsync` を呼ぶ。
  起動直後に 1 回即時取得し、以降はティックごとに取得する。
- `MonitorService`（Singleton）は全ホストへ並列に `/api/snapshot` を問い合わせ、結果を `MonitorSnapshot` として保持する。
  - `Current` に最新値を保持し、画面は開いた瞬間にこれを読んで即描画する。
  - `Updated` イベントで購読者へ通知する。画面は `InvokeAsync(StateHasChanged)` で UI スレッドへマーシャリングし、`Dispose` で購読解除する。
  - 更新は直列化し、古い結果が新しい結果を上書きしないようにする。
- ホスト単位の失敗は `HostStatus.Error` として保持し、ログに記録する。
- 手動の「更新」ボタン・操作後の再取得も同じ経路を使う（全画面に反映される）。

### 4.7 認証

- テンプレートの Cookie 認証を使う。`/login` でログインし、未認証のページアクセスはログイン画面へ、API は 401 を返す。
- 初期アカウントは `Auth:InitialAccount` から、アカウントが 1 件もないときに Administrator ロールで作成する。
- パスワードは PBKDF2（SHA256、310,000 回）でハッシュ化して保存する。

---

## 5. API 仕様（HyperManager.Agent）

- ベース: `http://<host>:<port>`（既定ポート `5100`）
- 認証: `/health`・`/alive` を除く全エンドポイントで HTTP ヘッダー `X-Api-Key: <key>` を検証する（定数時間比較）。
- 形式: JSON（camelCase、`VmState` は文字列、null のプロパティは省略）。操作は**完了まで待って**結果を返す。

| メソッド | パス | 説明 | リクエスト | レスポンス |
|---|---|---|---|---|
| GET | `/health` | ヘルスチェック（Hyper-V 管理サービスへの接続を含む） | - | 200 / 503 |
| GET | `/alive` | 生存確認 | - | 200 |
| GET | `/api/host` | ホスト情報 | - | `HostInfo` |
| GET | `/api/host/metrics` | ホストメトリクス（個別取得用） | - | `HostMetrics` |
| GET | `/api/snapshot` | ホスト情報＋メトリクス＋VM 一覧を一括取得 | - | `HostSnapshot` |
| GET | `/api/vms` | VM 一覧 | - | `VmInfo[]` |
| POST | `/api/vms/{id}/start` | 開始 | - | 204 / エラー |
| POST | `/api/vms/{id}/resume` | 再開 | - | 204 / エラー |
| POST | `/api/vms/{id}/pause` | 一時停止 | - | 204 / エラー |
| POST | `/api/vms/{id}/save` | 状態保存 | - | 204 / エラー |
| POST | `/api/vms/{id}/shutdown` | 正常シャットダウン | `ShutdownRequest`（省略可） | 204 / エラー |
| POST | `/api/vms/{id}/turnoff` | 強制電源オフ | - | 204 / エラー |

Host の定常取得は `/api/snapshot` のみを使う（ラウンドトリップ削減と取得時点の一貫性のため）。
Agent はメトリクスと VM 一覧を並行に取得する。

### エラー応答

`ProblemDetails`（ASP.NET Core 標準、`traceId` 付き）で返す。

| 状況 | ステータス |
|---|---|
| API キー不正・未指定 | 401 |
| VM 未検出（`VmNotFoundException`、ID が GUID でない場合を含む） | 404 |
| 操作失敗（`HyperVException`） | 409 |
| 一覧・メトリクス取得失敗 | 500 |

`/api/snapshot` はメトリクス取得のみが失敗した場合、500 にせず `metrics` を省略（null）して
VM 一覧の取得結果を返す（部分表示を許容する）。

### DTO（HyperManager.Contracts）

```csharp
VmInfo(string Id, string Name, VmState State, int? CpuUsagePercent, long? MemoryUsageMb, TimeSpan? Uptime, string? Version)
HostInfo(string Name, bool IsElevated)
HostMetrics(double CpuUsagePercent, long TotalMemoryMb, long UsedMemoryMb, IReadOnlyList<DiskInfo> Disks)
DiskInfo(string Name, long TotalBytes, long FreeBytes)
HostSnapshot(HostInfo Host, HostMetrics? Metrics, IReadOnlyList<VmInfo> Vms)
ShutdownRequest(bool Force)

enum VmState { Unknown, Running, Off, Stopping, Saved, Paused, Starting, Saving, Pausing, Resuming, Other }
enum VmOperation { Start, Resume, Pause, Save, Shutdown, TurnOff }
```

単位の整形（GB 表記など）は Host 側で行う。

### Host の API

| メソッド | パス | 説明 |
|---|---|---|
| GET | `/api/operations?keyword=&page=&size=` | 操作履歴（新しい順、ホスト名・VM 名で部分一致検索） |
| GET | `/api/operations/csv` | 操作履歴の CSV |

---

## 6. Hyper-V アクセス（HyperManager.Agent）

CIM（`Microsoft.Management.Infrastructure`）に統一する。取得・操作とも同一の `CimSession` 基盤を使う。

### ゲスト情報（`root\virtualization\v2`）

| 用途 | クラス / メソッド |
|---|---|
| VM 一覧・メトリクス | `Msvm_SummaryInformation`（`ElementName` / `EnabledState` / `ProcessorLoad` / `MemoryUsage` / `UpTime` / `Version`） |
| 状態変更 | `Msvm_ComputerSystem.RequestStateChange` |
| 正常シャットダウン | `Msvm_ShutdownComponent.InitiateShutdown`（`Msvm_SystemDevice` 経由で取得） |
| 非同期ジョブの完了待ち | `Msvm_ConcreteJob`（既定 200ms 間隔でポーリング、既定タイムアウト 5 分） |
| ヘルスチェック | `Msvm_VirtualSystemManagementService` |

`EnabledState` → `VmState` の対応は `VmStateMapper` に定義する。値は v2 名前空間のもので、`Microsoft.HyperV.PowerShell.VMState` と同じ。

| EnabledState | VmState |
|---|---|
| 2 | Running |
| 3 | Off |
| 4 / 32781（ForceShutdown） | Stopping |
| 6 / 32779（FastSaved） | Saved |
| 9 | Paused |
| 10 / 11（Reset）/ 32782（ForceReboot） | Starting |
| 32773 / 32780（FastSaving） | Saving |
| 32776 | Pausing |
| 32777 | Resuming |
| それ以外（Critical 系を含む） | Other（操作ボタンを表示しない） |

`RequestStateChange` の `RequestedState` は 2（開始・再開）、3（強制停止）、6（状態保存）、9（一時停止）を使う。
CPU / メモリ / 稼働時間は実行中（`EnabledState == 2`）の VM のみ値を持ち、それ以外は null とする。

`RequestStateChange` / `InitiateShutdown` の戻り値は 0（完了）と 4096（ジョブ開始）を成功として扱い、
`RequestStateChange` が 4096 の場合は `Msvm_ConcreteJob` の `JobState` が 7（完了）になるまで待つ。7 を超える値は失敗として扱う。
それ以外の戻り値（32768〜32778）は内容を日本語で返す（例: 32775 = 現在の状態では実行できません）。

VM の ID（`Msvm_ComputerSystem.Name`）は GUID として検証・正規化してから WQL に埋め込む。

### ホストメトリクス（`root\cimv2`）

| 指標 | クラス | プロパティ | 備考 |
|---|---|---|---|
| CPU 使用率 | `Win32_PerfFormattedData_Counters_HyperVHypervisorLogicalProcessor`（`Name='_Total'`） | `PercentTotalRunTime` | 第 1 候補。Hyper-V ホストではルートパーティションの値より実負荷を正確に反映する |
| CPU 使用率（代替） | `Win32_PerfFormattedData_PerfOS_Processor`（`Name='_Total'`） | `PercentProcessorTime` | 第 2 候補 |
| CPU 使用率（最終手段） | `Win32_Processor` | `LoadPercentage` | 第 3 候補。粗いが可用性が高い |
| メモリ総量 / 空き | `Win32_OperatingSystem` | `TotalVisibleMemorySize` / `FreePhysicalMemory` | KB 単位。使用量 = 総量 − 空き |
| ディスク空き | `Win32_LogicalDisk`（`DriveType=3`） | `DeviceID` / `Size` / `FreeSpace` | バイト単位。固定ローカルディスクのみ |

---

## 7. 設定

### HyperManager.Agent（appsettings.json）

```json
{
  "http_ports": "5100",
  "Agent": { "ApiKey": "REPLACE_WITH_A_STRONG_RANDOM_KEY" },
  "HyperV": { "JobTimeoutSeconds": 300, "JobPollIntervalMilliseconds": 200 },
  "Log": { "HttpLog": false },
  "Prometheus": { "Uri": "" }
}
```

- `Agent:ApiKey` は必須（未設定では起動しない）。Development 環境の既定値は `change-me`。
- `Prometheus:Uri` を設定すると Prometheus 形式のメトリクスを公開する。

### HyperManager.Host（appsettings.json）

```json
{
  "http_ports": "8080",
  "ConnectionStrings": { "Default": "Data Source=data.db;Cache=Shared;Pooling=True" },
  "Auth": { "ExpireMinutes": 1440, "InitialAccount": { "Id": "admin", "Password": "admin" } },
  "Monitor": { "Enable": true, "IntervalSeconds": 10 },
  "Agent": {
    "QueryTimeoutSeconds": 10,
    "OperationTimeoutSeconds": 330,
    "Hosts": [
      { "Name": "Host A", "Url": "http://192.168.1.10:5100", "ApiKey": "key-a" },
      { "Name": "Host B", "Url": "http://192.168.1.11:5100", "ApiKey": "key-b" }
    ]
  }
}
```

- `AgentClientOption` / `MonitorWorkerOption` として Options パターンで読み込み、起動時に検証する
  （ホスト名の必須・重複、URL が http/https の絶対 URL であること、API キーの必須）。
- `Monitor:Enable` を false にすると自動更新を停止し、手動更新のみになる。
- `QueryTimeoutSeconds` はスナップショット取得のタイムアウト。到達不可ホストが他ホストの更新を遅らせないよう短くする。
- `OperationTimeoutSeconds` は VM 操作のタイムアウト。Agent のジョブ待ち（既定 5 分）より長くし、Agent のエラーを受け取れるようにする。
- Agent の URL はパス付き（リバースプロキシ配下など）でも登録できる。
- そのほかのログ・CSP・圧縮・テレメトリの設定はテンプレートと同じ。

---

## 8. 非機能要件

| 区分 | 要件 |
|---|---|
| 動作環境 | Agent: Windows 10/11 または Windows Server（Hyper-V 役割が有効）。Host: Windows 非依存 |
| ランタイム | .NET 10 / C#（Directory.Build.props に準拠） |
| 権限 | Agent は管理者権限で実行（Windows サービス）。Host は管理者権限不要 |
| 性能 | VM 数十台規模で一覧取得が体感数秒以内。ポーリングは Host 集約のためクライアント数に依存しない |
| 品質 | ビルド警告ゼロ（StyleCop / CA / IDE / Nullable）。単体・結合・Agent・E2E テスト |
| 可用性 | 1 ホストの障害が全体を落とさない（部分失敗を許容、取得タイムアウト 10 秒） |
| 運用 | Serilog（ファイル）、OpenTelemetry（OTLP）、Prometheus、ヘルスチェック（`/health`・`/alive`） |

Host の Prometheus メトリクスには、テンプレートのものに加えて次を出力する。

| メトリクス | タグ | 内容 |
|---|---|---|
| `monitor.host.count` | `status`（reachable / unreachable） | 監視対象ホスト数 |
| `monitor.vm.count` | `host`, `state` | VM 数 |

---

## 9. セキュリティ・通信

- Host はログイン認証（Cookie）を行い、VM 操作は Administrator ロールに限定する。
- Host ↔ Agent は **API キー**（`X-Api-Key`）で認証する。キーはホストごとに別の強いランダム値を設定する。
  Agent は認証失敗を接続元 IP とともにログに記録する。
- **HTTPS は前提にしていない**。Host ↔ Agent を HTTP で運用する場合、API キーは平文で流れるため
  **信頼できるネットワーク内での利用を前提**とする。必要に応じてリバースプロキシ等で HTTPS 化する。
- Host はテンプレートのセキュリティヘッダー（CSP・X-Frame-Options など）を付与する。
- VM の操作履歴はデータベースとアプリログに記録する。

---

## 10. 設計判断の記録

| ID | 論点 | 決定 |
|---|---|---|
| A | Hyper-V アクセス方式 | **CIM に統一**（`Microsoft.Management.Infrastructure`）。取得・操作とも CIM |
| B | プロセス構成 | **画面と Hyper-V 操作を分離**。Agent を各ホストに配置し REST API 化 |
| C | 「停止」操作 | **両方提供**（正常シャットダウン＋強制電源オフ）。強制側は警告を表示する確認ダイアログ |
| D | 更新方式 | **Host 集約の定期ポーリング**＋手動更新。各ブラウザが個別に Agent を叩かない |
| E | 管理者権限 | **起動時に検知して UI 表示**（manifest による強制はしない） |
| F | 複数ホスト表示 | **1 表に統合**（ホスト列＋ホストフィルタ） |
| G | API 形式・認証 | **REST / Minimal API**、**API キー**認証（`AuthenticationHandler` として実装） |
| H | HTTPS | **後回し**。HTTP 運用とし、信頼ネットワーク前提 |
| I | メトリクス取得 | `/api/snapshot` に統合し 1 リクエストで VM 一覧とホストメトリクスを取得 |
| J | 基盤 | **template-blazor-server の構成を採用**（Core / Host / AppHost、Serilog、OpenTelemetry、Cookie 認証、CSP、テスト構成） |
| K | プロジェクト名 | テンプレートに合わせ中央 UI を **Host**、各ホストの API サーバを **Agent** とする |
| L | 契約の共有 | DTO と API パスを **Contracts** に置き、Agent と AgentClient で共有する（パスの不一致はテストで検出） |
| M | ユーザー認証 | **テンプレートの Cookie 認証を採用**し、操作は Administrator ロールに限定 |
| N | 操作の監査 | **データベースに操作履歴を記録**し、画面と CSV で参照できるようにする |
| O | Agent の運用 | **Windows サービス**として動作させる（LocalSystem で管理者権限を得る） |
| P | タイムアウト | 取得（10 秒）と操作（330 秒）を分け、到達不可ホストが更新全体を遅らせないようにする |

---

## 11. 既知の制約・リスク

| 項目 | 内容・対応 |
|---|---|
| 通信の平文化 | HTTPS 未対応のため信頼ネットワーク内に限定する。必要に応じてリバースプロキシで HTTPS 化 |
| 一部ホストの到達不可 | 並列取得で部分失敗を許容し、ホスト別にエラー表示する |
| CPU カウンタの可用性・精度 | 3 段フォールバックで取得。全滅時は 0% として表示する |
| 管理者権限不足 | Hyper-V は権限のないユーザーに VM を返さないため、一覧は空になる。ホストカードに「非管理者」を表示し、ヘルスチェックは Unhealthy になる |
| 長時間ジョブ | Agent 側で完了まで待機（既定 5 分でタイムアウト）。Host 側の操作タイムアウトは 330 秒 |
| Blazor Server の接続断 | 再接続後、`MonitorService.Current` から最新値を即時復元する |
| 操作履歴の増加 | 保持期間の管理は未対応（将来候補） |
| アカウント管理 | 初期アカウントのみ（追加・パスワード変更の画面は未対応） |

---

## 12. hv-monitor からの変更点

| 区分 | 変更内容 |
|---|---|
| 構成 | `Monitor.Web` → `HyperManager.Host`、`Monitor.Agent` + `Monitor.Core` → `HyperManager.Agent`、`Monitor.Contracts` → `HyperManager.Contracts`。Host 側の業務ロジックを `HyperManager.Core` に分離し、Aspire AppHost とテストプロジェクトを追加 |
| 状態コード | **一時停止・状態保存の値を v2 名前空間のものに修正**（RequestedState 一時停止 32768→9、状態保存 32769→6。EnabledState 一時停止 32768→9、保存済み 32769→6 ほか）。hv-monitor の値は v1 名前空間のもので、v2 では正しく動作しない |
| Agent | API キー認証を `AuthenticationHandler`（定数時間比較）に変更、`/api/health` → `/health`・`/alive`、操作の応答を 200 → 204、シャットダウンの本文を省略可能に、VM ID を GUID として検証、メトリクスと VM 一覧を並行取得、戻り値の日本語化、ジョブ待ちを非同期化、`InitiateShutdown` の 4096 を成功として扱う、Windows サービス対応、API キー未設定時は起動しない |
| Host | ログイン認証と操作権限、操作履歴（DB・画面・CSV）、取得と操作のタイムアウト分離、ホストカードのコンポーネント化、名前での絞り込み、最終更新時刻の表示、Agent のヘルスチェック、VM 数・ホスト数のメトリクス |
| 設定 | Host のホスト登録を `HyperVHosts`（ルート配列）から `Agent:Hosts` に、`BaseUrl` を `Url` に、`Monitoring:Enabled` / `RefreshIntervalSeconds` を `Monitor:Enable` / `IntervalSeconds` に変更 |
