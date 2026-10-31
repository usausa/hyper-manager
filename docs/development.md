# 🛠️ 開発ガイド

HyperManager を開発するための情報をまとめます。

---

## 🧰 必要なもの

| | 用途 |
|---|---|
| 🟣 .NET 10 SDK | ビルド・実行 |
| 🪟 Windows | Agent と Agent のテスト（`net10.0-windows`）のビルド・実行 |
| ⚙️ Hyper-V（任意） | 実際の仮想マシンでの確認 |
| 🧑‍💻 Visual Studio / Rider（任意） | `HyperManager.AppHost` をスタートアップにして開発 |
| 🎭 Playwright のブラウザ | E2E テスト |

---

## 📦 プロジェクト構成

```
HyperManager.slnx
├─ src/
│  ├─ HyperManager.Contracts/   📜 Agent API の契約（DTO・パス・ヘッダー名）
│  ├─ HyperManager.Core/        🧠 HyperManager の業務ロジック（RootNamespace: HyperManager）
│  │  ├─ Accessors/             DB アクセス（SQL は Accessors/Sql/）
│  │  ├─ Domain/                VmOperationPolicy など
│  │  ├─ Infrastructure/        Agents（AgentClient）・Security（パスワード）
│  │  ├─ Models/                HostStatus・MonitorSnapshot・Entity
│  │  └─ Services/              MonitorService・OperationService・AccountService など
│  ├─ HyperManager.Host/        🖥️ 画面と Web API
│  │  ├─ Application/           起動構成・認証・ヘルスチェック・計測
│  │  ├─ Components/            Blazor（Pages・Shared・Dialogs・Layout）
│  │  ├─ Endpoints/             Web API（ログイン・操作履歴）
│  │  ├─ Infrastructure/        ログ・セキュリティヘッダー
│  │  ├─ Settings/              設定クラス
│  │  ├─ Workers/               MonitorWorker
│  │  └─ Assets/Data/           テーブル定義（Schema.sql）
│  ├─ HyperManager.Agent/       🤖 Agent
│  │  ├─ Application/           起動構成・API キー認証・ヘルスチェック
│  │  ├─ Endpoints/             Agent API
│  │  ├─ HyperV/                CIM による取得・操作
│  │  └─ Settings/              設定クラス
│  └─ HyperManager.AppHost/     🚀 開発用の Aspire AppHost
└─ tests/
   ├─ HyperManager.UnitTests/         🧪 単体テスト（Core・Host、bUnit）
   ├─ HyperManager.IntegrationTests/  🔗 HyperManager の結合テスト
   ├─ HyperManager.AgentTests/        🤖 Agent の API テスト（Windows）
   └─ HyperManager.E2ETests/          🎭 Playwright による E2E テスト
```

### 📚 主なライブラリ

| ライブラリ | 用途 |
|---|---|
| MudBlazor | 画面の部品 |
| Usa.Smart.Blazor | `<Condition>` など Blazor の補助 |
| Usa.Smart.Data.Accessor | SQL ファイルからデータアクセスのコードを生成（2-way SQL） |
| BunnyTail.ServiceRegistration | Core の `*Service` クラスを Singleton として自動登録 |
| Usa.Smart.Mapper | エンティティから API のモデルへの変換 |
| Microsoft.Data.Sqlite・MiniDataProfiler | SQLite と SQL のログ・トレース |
| Microsoft.Management.Infrastructure | CIM による Hyper-V へのアクセス（Agent） |
| Serilog・OpenTelemetry・NSwag | ログ・計測・OpenAPI の UI |
| CsvHelper | 操作履歴の CSV |

---

## ▶️ 起動する

### 🚀 Aspire でまとめて起動（おすすめ）

```powershell
dotnet run --project src/HyperManager.AppHost
```

- 🧭 Aspire のダッシュボード（`http://localhost:15000`）で、ログ・トレース・メトリクスを確認できます
- 🔑 API キーは起動のたびに生成され、Agent と HyperManager の両方に渡されます
- 🖥️ HyperManager は `http://localhost:8080`、🤖 Agent は `http://localhost:5100` です
- 🛡️ 仮想マシンを操作するときは、Visual Studio やターミナルを**管理者として**実行してください

### 🧩 別々に起動

```powershell
# Agent（http://localhost:5100、Swagger UI: /swagger）
dotnet run --project src/HyperManager.Agent

# HyperManager（http://localhost:8080）
dotnet run --project src/HyperManager.Host
```

開発環境では、HyperManager に `Local`（`http://localhost:5100`・API キー `change-me`）が登録済みです。ログインは `admin` / `admin` です。

---

## 🔨 ビルド

```powershell
dotnet build HyperManager.slnx
```

> ⚠️ **ビルド警告はゼロに保ちます。** アナライザーの警告もすべて対象です。

---

## 🧪 テスト

| プロジェクト | 内容 | Hyper-V |
|---|---|---|
| 🧪 `HyperManager.UnitTests` | ドメイン・サービス・AgentClient・画面部品（bUnit） | 不要 |
| 🔗 `HyperManager.IntegrationTests` | 認証・API・OpenAPI・操作履歴（Agent はスタブ） | 不要 |
| 🤖 `HyperManager.AgentTests` | API キー認証・各 API・状態コードの変換（Hyper-V はフェイク） | 不要 |
| 🎭 `HyperManager.E2ETests` | ログイン・一覧表示・操作・操作履歴（Agent はスタブ） | 不要 |

テストは Microsoft Testing Platform で動きます。リポジトリ直下で `dotnet run --project` を使って実行します。

```powershell
dotnet run --project tests/HyperManager.UnitTests
dotnet run --project tests/HyperManager.IntegrationTests
dotnet run --project tests/HyperManager.AgentTests
```

**📈 カバレッジ**

```powershell
dotnet run --project tests/HyperManager.UnitTests -- --coverage --coverage-output-format cobertura --coverage-settings CodeCoverage.runsettings
```

### 🎭 E2E テスト

日常の実行には含めず、必要なときやリリースの前に実行します。

```powershell
# ブラウザを入手する（初回のみ。ビルドで出力されるスクリプトを使う）
dotnet build tests/HyperManager.E2ETests
pwsh tests/HyperManager.E2ETests/bin/Debug/net10.0/playwright.ps1 install

# 実行する（既定は Chromium・画面なし）
dotnet run --project tests/HyperManager.E2ETests

# 画面を出して実行する
$env:HEADED = "1"; dotnet run --project tests/HyperManager.E2ETests; Remove-Item Env:HEADED

# ブラウザを切り替える（chromium / firefox / webkit）
$env:BROWSER = "firefox"; dotnet run --project tests/HyperManager.E2ETests; Remove-Item Env:BROWSER

# Playwright Inspector で 1 手ずつ実行する
$env:PWDEBUG = "1"; dotnet run --project tests/HyperManager.E2ETests; Remove-Item Env:PWDEBUG
```

- 🧾 失敗したテストだけ、操作ごとの画面・DOM・通信・コンソールを記録したトレースを `tests/HyperManager.E2ETests/bin/Debug/net10.0/playwright-traces/<テストの表示名>.zip` に残します
- 🔍 `pwsh tests/HyperManager.E2ETests/bin/Debug/net10.0/playwright.ps1 show-trace <zip>` か https://trace.playwright.dev で開けます

---

## 📏 コーディング規約

[AGENTS.md](../AGENTS.md) と `.editorconfig` に従います。

- 📐 `.editorconfig` のルールに従う
- 🚫 フィールド名に `_` を付けない
- ⚠️ ビルド警告を出さない
- 🙋 警告の抑制が必要なときは、適用する前に相談する
- ↩️ 既存ファイルの改行コードは変えない。新しいテキストファイルは CRLF にする

そのほかの慣習です。

- 📂 名前空間はフォルダと一致させる（ファイルスコープ）。`using` は名前空間の内側に、`System` を先頭にアルファベット順で書く
- 📝 ログは `Log.cs` に `[LoggerMessage]` で定義する
- ⚙️ 設定クラスはデータ注釈で検証し、`ValidateOnStart` で起動時にチェックする
- 💬 コメントの記号・英数字は半角にする（`Usa.Smart.Analyzers.JapaneseComment` が検査します）

---

## 🧩 よくある変更

### 🔌 Agent API を追加する

1. 📜 `HyperManager.Contracts` に DTO と `AgentApi` のパスを追加する
2. 🤖 `HyperManager.Agent` の `Endpoints/` にエンドポイント、`HyperV/` に Hyper-V の処理を追加する
3. 📡 `HyperManager.Core` の `IAgentClient` / `AgentClient` に呼び出しを追加する
4. 🧪 `AgentTests`（`FakeHyperVService` を使った API テスト）と `UnitTests`（`AgentClient`）にテストを追加する
5. 📖 [API リファレンス](api.md) を更新する

> 💡 `AgentTests` の `OperationPathOfClientIsRouted` は、クライアントのパスと Agent のルートがずれていないかを確認します。

### 📄 画面を追加する

1. `Components/Pages/` に `XxxPage.razor` と `XxxPage.razor.cs` を追加し、`@attribute [Authorize]` を付ける
2. `Components/Layout/NavMenu.razor` にリンクを追加し、`NavMenuTests` のリンク数を直す

### 🗄️ テーブルを変更する

1. `src/HyperManager.Host/Assets/Data/Schema.sql` とルートの `Database.sql` を変更する
2. `Accessors/` のアクセサーと `Accessors/Sql/<クラス名>.<メソッド名>.sql` を追加・変更する（`/*@ name */` でパラメーター、`/*% if (...) { */` で条件）

> ⚠️ マイグレーションの仕組みはありません。テーブルは `CREATE TABLE IF NOT EXISTS` で作るだけなので、既存のデータベースには手作業で `ALTER TABLE` が必要です。

### ⚙️ 設定を追加する

1. `Settings/`（または Core の該当フォルダ）に設定クラスを追加する
2. `ApplicationExtensions.ConfigureComponents` で `AddOptions<T>().BindConfiguration(...).ValidateDataAnnotations().ValidateOnStart()` を登録する
3. `appsettings.json` に既定値を書き、[設定リファレンス](configuration.md) を更新する

---

## 🐞 デバッグのヒント

- 🖥️ 開発環境ではログがコンソールにも出ます（Debug レベル）。HTTP の記録と SQL の記録も有効です
- 🔑 Agent の Swagger UI（`/swagger`）で［Authorize］に API キーを入れると、Agent API を直接試せます
- 🧭 Aspire のダッシュボードでは、HyperManager から Agent への呼び出しを 1 本のトレースとして追えます
- ⚙️ 実機の Hyper-V で確かめるときは、テスト用の仮想マシンを使ってください
