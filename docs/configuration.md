# ⚙️ 設定リファレンス

HyperManager と Agent のすべての設定項目をまとめます。

---

## 📝 設定の書き方

### 📂 どこに書くか

設定は次の順に読み込まれ、後のものが前のものを上書きします。

| 順 | 場所 | 用途 |
|---|---|---|
| 1 | `appsettings.json` | 既定値（配布ファイルに含まれます） |
| 2 | `appsettings.{環境名}.json` | 環境ごとの設定。サービスとして動かすと環境名は `Production` です |
| 3 | 環境変数 | `:` を `__`（アンダースコア 2 つ）に置き換えて指定します |
| 4 | コマンドライン引数 | `--Monitor:IntervalSeconds=30` のように指定します |

> 💡 **運用環境の設定は `appsettings.Production.json` に書くのがおすすめです。** `appsettings.*.json` は配布ファイルに含まれないので、更新時に上書きされません。

### 🌱 環境変数の例

```powershell
# その場で起動するとき（現在の PowerShell だけ）
$env:Agent__ApiKey = "<API キー>"
$env:Agent__Hosts__0__Url = "http://192.168.1.10:5100"

# Windows サービスに渡すとき（サービスごとの環境変数。設定後にサービスを再起動）
Set-ItemProperty -Path HKLM:\SYSTEM\CurrentControlSet\Services\HyperManagerAgent -Name Environment -Type MultiString -Value @("Agent__ApiKey=<API キー>")
```

> ⚠️ 配列（`Agent:Hosts` など）は**番号ごとに**上書きされます。ファイルによって台数が違うと、混ざった結果になるので注意してください。

### ✅ 起動時のチェック

主な設定は起動時に検証され、誤りがあると起動を中止します（内容はログとイベント ビューアーに出ます）。

```
OptionsValidationException: DataAnnotation validation failed for 'AgentSetting' members: 'ApiKey' with the error: 'The ApiKey field is required.'.
```

---

## 🖥️ HyperManager（`HyperManager.Host`）

### 🌐 待ち受け

| 設定 | 既定値 | 説明 |
|---|---|---|
| `http_ports` | `8080` | HTTP で待ち受けるポート |
| `Kestrel:Endpoints` | なし | 待ち受ける URL と証明書（ASP.NET Core 標準）。設定すると `http_ports` より優先されます |
| `AllowedHosts` | `*` | 受け付けるホスト名 |

HTTPS の例（PFX ファイル）:

```json
"Kestrel": {
  "Endpoints": {
    "Https": {
      "Url": "https://*:8443",
      "Certificate": { "Path": "C:\\HyperManager\\cert\\hypermanager.pfx", "Password": "<PFX のパスワード>" }
    }
  }
}
```

証明書ストアの証明書を使う場合は `"Certificate": { "Subject": "hypermanager.example.local", "Store": "My", "Location": "LocalMachine" }` のように指定します。HTTP のまま使う場合は `Auth:AllowHttp` を設定します（[🩺 運用ガイド](operations.md#-https-と-http)）。

### 🔐 ログイン（`Auth`）

| 設定 | 既定値 | 範囲 | 説明 |
|---|---|---|---|
| `Auth:ExpireMinutes` | `1440` | 1〜43200 | ログインの有効時間（分）。使っている間は延長されます |
| `Auth:AllowHttp` | `false` | | `true` にすると、本番でも HTTP でログインできます（ログイン Cookie に `Secure` を付けません）。信頼できるネットワーク内だけで使ってください |
| `Auth:InitialAccount:Id` | `admin` | 必須 | 最初のアカウントの ID |
| `Auth:InitialAccount:Password` | `admin` | 必須 | 最初のアカウントのパスワード |

> 🔑 最初のアカウントは**アカウントが 1 件もないときだけ**、管理者として作られます。作られた後に設定を変えてもパスワードは変わりません。

### 🔄 自動更新（`Monitor`）

| 設定 | 既定値 | 範囲 | 説明 |
|---|---|---|---|
| `Monitor:Enable` | `true` | | `false` で自動更新を止めます（画面の［更新］だけになります） |
| `Monitor:IntervalSeconds` | `10` | 1〜3600 | 自動更新の間隔（秒） |

### 🤖 監視するホスト（`Agent`）

| 設定 | 既定値 | 範囲 | 説明 |
|---|---|---|---|
| `Agent:QueryTimeoutSeconds` | `10` | 1〜300 | 状態を取得するときの待ち時間（秒）。応答しないホストがあっても、ほかのホストの更新が遅れないよう短めにします |
| `Agent:OperationTimeoutSeconds` | `330` | 1〜3600 | 操作の完了を待つ時間（秒）。Agent のジョブ待ち（既定 300 秒）より長くします |
| `Agent:Hosts` | なし | | 監視するホストの一覧 |
| `Agent:Hosts:N:Name` | | 必須・重複不可 | 画面での表示名 |
| `Agent:Hosts:N:Url` | | http/https の絶対 URL | Agent のアドレス。パス付き（リバースプロキシ配下など）も可 |
| `Agent:Hosts:N:ApiKey` | | 必須 | その Agent の API キー（制御文字は不可） |

```json
"Agent": {
  "Hosts": [
    { "Name": "Host A", "Url": "http://192.168.1.10:5100", "ApiKey": "<Host A の API キー>" },
    { "Name": "Host B", "Url": "https://proxy.example.local/host-b/", "ApiKey": "<Host B の API キー>" }
  ]
}
```

### 🗄️ データベース（`ConnectionStrings`）

| 設定 | 既定値 | 説明 |
|---|---|---|
| `ConnectionStrings:Default` | `Data Source=data.db;Cache=Shared;Pooling=True` | SQLite の接続文字列。ファイルは実行フォルダからの相対パスです |

### 📝 ログ（`Log`・`Serilog`）

| 設定 | 既定値 | 範囲 | 説明 |
|---|---|---|---|
| `Log:HttpLog` | `false` | | `/api` へのリクエストを記録します |
| `Log:HttpDump` | `false` | | リクエスト・レスポンスの本文も記録します |
| `Log:HttpDumpLimit` | `4096` | 1〜1048576 | 本文を記録する最大バイト数 |
| `Log:W3CLog:Enable` | `false` | | W3C 形式のアクセスログを出します |
| `Log:W3CLog:Directory` | `../log` | 必須 | アクセスログの出力先 |
| `Log:W3CLog:FileName` | `access-` | 必須 | アクセスログのファイル名の先頭 |
| `Log:W3CLog:RetainedFileCount` | `4` | 1〜1000 | 残すファイル数 |
| `Serilog` | ファイル出力 | | アプリのログ（[Serilog の設定形式](https://github.com/serilog/serilog-settings-configuration)）。既定は `../log/HyperManager.Host_YYYYMMDD.log` |

### 📈 監視・計測（`Prometheus`・`Telemetry`・`Profiler`）

| 設定 | 既定値 | 範囲 | 説明 |
|---|---|---|---|
| `Prometheus:Uri` | `http://localhost:9464` | | Prometheus 形式のメトリクスを公開する URL。空にすると無効です |
| `Telemetry:LongExecutionThreshold` | `5000` | 1〜3600000 | この時間（ミリ秒）を超えた API リクエストを警告ログに出し、件数を数えます |
| `Profiler:SqlLog:Enable` | `false` | | 実行した SQL をログに出します |
| `Profiler:SqlLog:OutputParameter` | `true` | | SQL のパラメーターも出します |
| `Profiler:SqlLog:ElapsedThresholdMilliseconds` | `0` | 0〜60000 | この時間以上かかった SQL だけ出します |
| `Profiler:SqlTelemetry:Enable` | `false` | | SQL をトレースに含めます |

> ⚠️ Windows では `Prometheus:Uri` に `0.0.0.0`・`+`・`*` は使えません（起動に失敗します）。ほかのサーバーから収集する場合はサーバーの名前か IP を指定してください（[🩺 運用ガイド](operations.md#-メトリクス)）。

### 🛡️ そのほか

| 設定 | 既定値 | 説明 |
|---|---|---|
| `Compression:Response` | `true` | `/api` のレスポンスを圧縮します |
| `Compression:Request` | `false` | `/api` への圧縮されたリクエストを受け付けます |
| `Csp:ReportOnly` | `false` | Content-Security-Policy を「報告のみ」にします（ブロックしません） |

---

## 🤖 Agent（`HyperManager.Agent`）

| 設定 | 既定値 | 範囲 | 説明 |
|---|---|---|---|
| `http_ports` | `5100` | | HTTP で待ち受けるポート |
| `Kestrel:Endpoints` | なし | | 待ち受ける URL と証明書（設定すると `http_ports` より優先） |
| `Agent:ApiKey` | なし | **必須** | API キー。未設定では起動しません |
| `HyperV:JobTimeoutSeconds` | `300` | 1〜3600 | 状態変更のジョブの完了を待つ最大時間（秒） |
| `HyperV:JobPollIntervalMilliseconds` | `200` | 50〜10000 | ジョブの状態を確認する間隔（ミリ秒） |
| `Log:HttpLog` | `false` | | `/api` へのリクエストを記録します |
| `Prometheus:Uri` | 空（無効） | | Prometheus 形式のメトリクスを公開する URL |
| `Serilog` | ファイル出力 | | 既定は `../log/HyperManager.Agent_YYYYMMDD.log` |

---

## 🧪 環境ごとの違い

### 開発（`Development`）

`appsettings.Development.json` で次の値に変わります。

| | 設定 | 値 |
|---|---|---|
| 🖥️ | `Agent:Hosts` | `Local`（`http://localhost:5100`、API キー `change-me`） |
| 🖥️ | `Csp:ReportOnly` | `true` |
| 🖥️ | `Profiler:SqlLog:Enable`・`SqlTelemetry:Enable` | `true` |
| 🖥️🤖 | `Log:HttpLog` | `true` |
| 🖥️🤖 | `Serilog` | Debug レベル、コンソールにも出力 |
| 🤖 | `Agent:ApiKey` | `change-me` |

そのほか、開発環境では OpenAPI・Swagger UI・ReDoc が公開され（[🔌 API リファレンス](api.md#-openapi--swagger-ui)）、ログイン Cookie が HTTP でも送られます。

### 本番（`Production`）

- 🔒 ログイン Cookie に `Secure` 属性が付き、HTTPS でしか送られません（`localhost` を除く）。`Auth:AllowHttp` を `true` にすると付きません
- 🛡️ HSTS ヘッダーを付けます

### 🌍 環境変数

| 環境変数 | 説明 |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | 環境名（既定 `Production`） |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | 設定すると、ログ・メトリクス・トレースを OTLP で送ります（Aspire では自動で設定されます） |
