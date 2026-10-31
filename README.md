# HyperManager

複数の Hyper-V ホストをまたいで仮想マシン（VM）を監視・操作する Web アプリケーションです。
[hv-monitor](https://github.com/usausa/hv-monitor) の機能を、[template-blazor-server](https://github.com/usausa/template-blazor-server) の構成で再構築したものです。

各 Hyper-V ホスト上で動作する API サーバ（**HyperManager.Agent**）と、それらを 1 画面に統合表示する中央の Web UI（**HyperManager.Host**）の 2 種類のプロセスで構成します。

## アーキテクチャ

```
 Browser
    |
    v
+-------------------------------------+
| HyperManager.Host (Blazor Server)   |   central UI / login / operation history (SQLite)
+-------------------------------------+
    |  HTTP/REST + X-Api-Key            (parallel, one request per host)
    +-------------------------+
    v                         v
+-----------------------+ +-----------------------+
| HyperManager.Agent    | | HyperManager.Agent    |   Windows service on each Hyper-V host
| (Minimal API)         | | (Minimal API)         |
+-----------------------+ +-----------------------+
    |  CIM                    |  CIM
 Hyper-V                   Hyper-V
```

Host は全ホストへ並列に問い合わせ、結果を 1 つの表に統合表示します。1 ホストが到達不可でも他ホストは表示し、失敗したホストはカード上にエラーとして提示します。

## 機能

- 複数ホストの VM 一覧を 1 表に統合表示（ホスト・名前・状態・CPU・メモリ・稼働時間・バージョン）、ホスト／名前による絞り込みと列ソート
- ホストメトリクスの表示（ホストごとの CPU 使用率・メモリ使用量・各固定ディスクの空き容量、管理者権限／到達可否のバッジ）
- 自動更新（既定 10 秒間隔。Host 側で集約するため Agent への負荷はブラウザ数に依存しない）と手動更新
- VM 操作: 開始 / 再開 / 一時停止 / 状態保存 / シャットダウン（正常終了）/ 強制停止（電源オフ）
  - 状態に応じた操作ボタンの出し分け、操作前の確認ダイアログ、進捗表示、成否のスナックバー通知
  - 操作は Administrator ロールのユーザーのみ実行可能
- 操作履歴（実行日時・ユーザー・ホスト・VM・操作・結果）を SQLite に記録し、画面で検索・CSV 出力
- ログイン認証（Cookie 認証、初期アカウントは設定ファイルから作成）
- 運用機能（テンプレート由来）: Serilog によるログ、OpenTelemetry / Prometheus メトリクス、ヘルスチェック、CSP などのセキュリティヘッダー、Windows サービス／systemd 対応、Aspire AppHost による開発時の一括起動

## プロジェクト構成

| プロジェクト | TFM | 役割 | 参照 |
|---|---|---|---|
| `src/HyperManager.Contracts` | net10.0 | Agent API の契約（DTO・ルート・ヘッダー名） | なし |
| `src/HyperManager.Core` | net10.0 | Host 側の業務ロジック（Agent クライアント、監視、VM 操作、操作履歴、アカウント、DB アクセス） | Contracts |
| `src/HyperManager.Host` | net10.0 | 中央 Web UI（Blazor Server / MudBlazor）と Web API | Core |
| `src/HyperManager.Agent` | net10.0-windows | 各 Hyper-V ホストの API サーバ（Minimal API、X-Api-Key 認証、CIM による Hyper-V アクセス） | Contracts |
| `src/HyperManager.AppHost` | net10.0 | 開発用の Aspire AppHost（Agent と Host を同時に起動） | Agent, Host |
| `tests/HyperManager.UnitTests` | net10.0 | Core / Host の単体テスト（bUnit を含む） | Core, Host |
| `tests/HyperManager.IntegrationTests` | net10.0 | Host の結合テスト（認証・API・操作履歴） | Host |
| `tests/HyperManager.AgentTests` | net10.0-windows | Agent の API テストと状態コード変換のテスト | Agent |
| `tests/HyperManager.E2ETests` | net10.0 | Playwright による E2E テスト | Host |

## 必要要件

- **HyperManager.Agent を動かすホスト**: Windows 10/11 または Windows Server（Hyper-V の役割が有効）/ .NET 10 / VM 操作のため **管理者権限**（Windows サービスとして LocalSystem で動かすのが簡単です）
- **HyperManager.Host を動かすホスト**: .NET 10（Windows 非依存）/ 管理者権限は不要

## セットアップ

### 1. HyperManager.Agent（各 Hyper-V ホストに配置）

1. 発行します。

   ```powershell
   dotnet publish src/HyperManager.Agent -c Release -o C:\HyperManager\Agent
   ```

2. `appsettings.json` の `Agent:ApiKey` に、推測されない強いランダム値を設定します（ホストごとに別の値を推奨）。
   **未設定のままでは起動しません。** 環境変数 `Agent__ApiKey` でも設定できます。
3. 待ち受けポート（既定 `5100`）を必要に応じて変更し（`http_ports`）、ファイアウォールで開放します。
4. Windows サービスとして登録・開始します（管理者の PowerShell）。

   ```powershell
   sc.exe create HyperManagerAgent binPath= "C:\HyperManager\Agent\HyperManager.Agent.exe" start= auto
   sc.exe start HyperManagerAgent
   ```

疎通確認:

```powershell
curl http://localhost:5100/health                              # ヘルスチェック（認証不要、Hyper-V に接続できないと 503）
curl -H "X-Api-Key: <KEY>" http://localhost:5100/api/snapshot  # ホスト情報・メトリクス・VM 一覧（JSON）
```

### 2. HyperManager.Host（中央に 1 つ配置）

1. 発行します。

   ```powershell
   dotnet publish src/HyperManager.Host -c Release -o C:\HyperManager\Host
   ```

2. `appsettings.json` の `Agent:Hosts` に、各 Agent の `Name` / `Url` / `ApiKey` を登録します。

   ```json
   "Agent": {
     "QueryTimeoutSeconds": 10,
     "OperationTimeoutSeconds": 330,
     "Hosts": [
       { "Name": "Host A", "Url": "http://192.168.1.10:5100", "ApiKey": "key-a" },
       { "Name": "Host B", "Url": "http://192.168.1.11:5100", "ApiKey": "key-b" }
     ]
   }
   ```

3. `Auth:InitialAccount` の初期アカウント（既定 `admin` / `admin`）を変更します。アカウントはデータベースが空のときだけ作成されます。
4. 必要に応じて `Monitor` で自動更新を調整します（`Enable`: 既定 true、`IntervalSeconds`: 既定 10）。`Enable` を false にすると手動更新のみになります。
5. 起動します（Windows サービス／systemd としても動作します）。既定のポートは `8080` です。

## 開発時の起動

### Aspire AppHost（推奨）

`src/HyperManager.AppHost` をスタートアッププロジェクトにして起動すると、Agent と Host が同時に起動し、Aspire ダッシュボードでログ・トレース・メトリクスを確認できます。
API キーは起動のたびに生成され、Agent と Host の両方に設定されます。VM を操作する場合は Visual Studio を管理者として実行してください。

```powershell
dotnet run --project src/HyperManager.AppHost
```

### 個別に起動

```powershell
# ターミナル A: Agent（http://localhost:5100、Swagger UI: /swagger）
dotnet run --project src/HyperManager.Agent

# ターミナル B: Host（http://localhost:8080）
dotnet run --project src/HyperManager.Host
```

Development 環境では、Host に Local Agent（`http://localhost:5100` / API キー `change-me`）が登録済みです。

## API

### HyperManager.Agent

`/health` と `/alive` を除く全エンドポイントで HTTP ヘッダー `X-Api-Key: <key>` を検証します。

| メソッド | パス | 説明 | リクエスト |
|---|---|---|---|
| GET | `/health` | ヘルスチェック（Hyper-V への接続を含む、認証不要） | - |
| GET | `/alive` | 生存確認（認証不要） | - |
| GET | `/api/host` | ホスト情報 `{ name, isElevated }` | - |
| GET | `/api/host/metrics` | ホストメトリクス `{ cpuUsagePercent, totalMemoryMb, usedMemoryMb, disks }` | - |
| GET | `/api/snapshot` | ホスト情報＋メトリクス＋VM 一覧を一括取得（Host の定常取得はこれを使用） | - |
| GET | `/api/vms` | VM 一覧 | - |
| POST | `/api/vms/{id}/start` | 開始 | - |
| POST | `/api/vms/{id}/resume` | 再開 | - |
| POST | `/api/vms/{id}/pause` | 一時停止 | - |
| POST | `/api/vms/{id}/save` | 状態保存 | - |
| POST | `/api/vms/{id}/shutdown` | 正常シャットダウン | `{ "force": bool }`（省略時 false） |
| POST | `/api/vms/{id}/turnoff` | 強制電源オフ | - |

- 操作は完了まで待って 204 を返します。エラーは `ProblemDetails` で返します（認証失敗 401、VM 未検出 404、操作失敗 409、取得失敗 500）。
- `/api/snapshot` はホストメトリクスのみ取得できなかった場合、エラーにせず `metrics` を省略（null）して VM 一覧を返します。
- Development 環境では OpenAPI（`/openapi/v1.json`）、Swagger UI（`/swagger`）、ReDoc（`/redoc`）を公開します。

### HyperManager.Host

ログイン（Cookie）が必要です。

| メソッド | パス | 説明 |
|---|---|---|
| GET | `/api/operations?keyword=&page=&size=` | 操作履歴（新しい順、ホスト名・VM 名で部分一致検索） |
| GET | `/api/operations/csv` | 操作履歴の CSV |

## セキュリティ

- Host はログイン認証（Cookie）を行い、VM 操作は Administrator ロールに限定します。
- Host ↔ Agent は **API キー**（`X-Api-Key`、定数時間比較）で認証します。キーはホストごとに別の強いランダム値を設定してください。
- 通信は HTTP を前提にしています。API キーは平文で流れるため、**信頼できるネットワーク内での利用**を前提とし、必要に応じてリバースプロキシ等で HTTPS 化してください（Agent の URL はパス付きでも登録できます）。
- Agent は VM 操作のため管理者権限で動作します。Host は管理者権限不要です。

## テスト

コマンドはリポジトリ直下で実行します。

```powershell
dotnet run --project tests/HyperManager.UnitTests
dotnet run --project tests/HyperManager.IntegrationTests
dotnet run --project tests/HyperManager.AgentTests
```

E2E テスト（`tests/HyperManager.E2ETests`）は Playwright で動かします。Agent はスタブに置き換えるため Hyper-V は不要です。

```powershell
# ブラウザの入手(初回。ビルドで出力されるスクリプトを使う)
dotnet build tests/HyperManager.E2ETests
pwsh tests/HyperManager.E2ETests/bin/Debug/net10.0/playwright.ps1 install

# 実行(既定は Chromium・画面なし)
dotnet run --project tests/HyperManager.E2ETests

# 画面を出して実行
$env:HEADED = "1"; dotnet run --project tests/HyperManager.E2ETests; Remove-Item Env:HEADED
```

- 失敗したテストだけ、操作ごとの画面・DOM・通信・コンソールを記録したトレースを `tests/HyperManager.E2ETests/bin/Debug/net10.0/playwright-traces/<テストの表示名>.zip` に残します。`pwsh tests/HyperManager.E2ETests/bin/Debug/net10.0/playwright.ps1 show-trace <zip>` か https://trace.playwright.dev で開きます。

## ビルド

```powershell
dotnet build HyperManager.slnx
```

## ドキュメント

- 仕様: [docs/architecture.md](docs/architecture.md)

## 注意

- 「強制停止」はゲスト OS の電源を即座に切るため、未保存データが失われる可能性があります。
- Agent が管理者権限なしで動作している場合、VM は一覧に表示されず（Hyper-V が返さないため）、ホストカードに「非管理者」と表示されます。
