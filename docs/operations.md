# 🩺 運用ガイド

HyperManager と Agent を安定して動かすための情報をまとめます。導入の基本手順は [README](../README.md#-導入手順) を参照してください。

---

## 📁 ファイルの配置

```
C:\HyperManager\
├─ Host\     🖥️ HyperManager（配布ファイル + appsettings.Production.json + data.db）
├─ Agent\    🤖 Agent（配布ファイル + appsettings.Production.json）※ 各 Hyper-V ホスト
├─ cert\     🔒 証明書（PFX）
└─ log\      📝 ログ（Host・Agent とも「実行フォルダ\..\log」に出力）
```

> 💡 HyperManager を Hyper-V ホストの 1 台に同居させても動きます。ポート（8443・5100）と Prometheus（Host のみ既定で有効）は重なりません。

---

## 🪟 Windows サービス

### ⚙️ 登録・操作

```powershell
# 登録（管理者の PowerShell）
New-Service -Name HyperManagerAgent -DisplayName "HyperManager Agent" -BinaryPathName "C:\HyperManager\Agent\HyperManager.Agent.exe" -StartupType Automatic

# 開始・停止・再起動・状態
Start-Service HyperManagerAgent
Stop-Service HyperManagerAgent
Restart-Service HyperManagerAgent
Get-Service HyperManagerAgent

# 異常終了したら 1 分後に自動で再起動
sc.exe failure HyperManagerAgent reset= 86400 actions= restart/60000/restart/60000/restart/60000

# 削除
Stop-Service HyperManagerAgent
sc.exe delete HyperManagerAgent
```

### 👤 実行アカウント

| | 推奨アカウント | 理由 |
|---|---|---|
| 🤖 Agent | LocalSystem（`New-Service` の既定） | 仮想マシンの取得・操作に管理者権限が必要です |
| 🖥️ HyperManager | LocalSystem、または専用アカウント | 管理者権限は不要です。専用アカウントにする場合は、実行フォルダ（`data.db`）と `log` への書き込み、証明書の読み取りを許可してください |

---

## 🐧 Linux で HyperManager を動かす

HyperManager（`HyperManager.Host`）は Linux でも動きます（Agent は Windows 専用です）。systemd のユニットファイルの例です。

```ini
# /etc/systemd/system/hypermanager.service
[Unit]
Description=HyperManager
After=network.target

[Service]
Type=notify
WorkingDirectory=/opt/hypermanager
ExecStart=/usr/bin/dotnet /opt/hypermanager/HyperManager.Host.dll
User=hypermanager
Restart=always

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now hypermanager
```

> ⚠️ 動作確認は Windows で行っています。ログの出力先（`Serilog` の `path`）は環境に合わせて変更してください。

---

## 🔒 HTTPS と HTTP

### 🤔 なぜ HTTPS が既定なのか

本番（`Production`）ではログイン状態を守るため、既定でログイン Cookie に `Secure` 属性が付きます。ブラウザは HTTPS（またはサーバー自身の `localhost`）でしかこの Cookie を送らないため、**そのまま HTTP で開くとログインしても画面に入れません**。
HTTP で使いたい場合は、下の「🅱️ HTTP で使う」の設定をします。

| | 方法 | おすすめ度 |
|---|---|---|
| 🅰️ | HyperManager 自体で HTTPS にする | ⭐⭐⭐ |
| 🔁 | リバースプロキシで HTTPS にする | ⭐⭐⭐ |
| 🅱️ | HTTP で使う（`Auth:AllowHttp`） | ⭐（信頼できるネットワーク内だけ） |

### 🅰️ HyperManager 自体で HTTPS にする

`appsettings.Production.json` に `Kestrel` の設定を追加します。設定すると `http_ports`（8080）は使われなくなります。

**PFX ファイルを使う**

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

**証明書ストアの証明書を使う**

```json
"Kestrel": {
  "Endpoints": {
    "Https": {
      "Url": "https://*:8443",
      "Certificate": { "Subject": "hypermanager.example.local", "Store": "My", "Location": "LocalMachine" }
    }
  }
}
```

> 💡 自己署名証明書など、信頼されていない証明書をストアから使う場合は `"AllowInvalid": true` も指定します。

### 🔁 リバースプロキシで HTTPS にする

IIS や nginx で HTTPS を受け、HyperManager の HTTP（8080）へ転送します。Blazor Server は **WebSocket** を使うので、プロキシで WebSocket を有効にしてください。nginx の例です。

```nginx
location / {
    proxy_pass         http://127.0.0.1:8080;
    proxy_http_version 1.1;
    proxy_set_header   Upgrade $http_upgrade;
    proxy_set_header   Connection "upgrade";
    proxy_set_header   Host $host;
    proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header   X-Forwarded-Proto $scheme;
}
```

> ⚠️ HyperManager は `X-Forwarded-For` / `X-Forwarded-Proto` をどこからでも受け付けます。プロキシ配下では `"Kestrel": { "Endpoints": { "Http": { "Url": "http://127.0.0.1:8080" } } }` のようにして、プロキシ以外から直接つながらないようにしてください。

### 🅱️ HTTP で使う（`Auth:AllowHttp`）

証明書を用意できない環境では、`appsettings.Production.json` の `Auth:AllowHttp` を `true` にすると、本番でも HTTP（8080 番）のままログインできます。

```json
"Auth": {
  "AllowHttp": true
}
```

- 🍪 ログイン Cookie に `Secure` 属性を付けなくなります（HTTPS で開いたときは付きます）
- ⚠️ ID・パスワード・ログイン状態が暗号化されずに流れます。盗み見られる心配のない、信頼できるネットワーク内だけで使ってください
- 💡 あとから HTTPS に切り替えるときは、`Kestrel` を設定して `AllowHttp` を `false` に戻します

### 🤖 Agent を HTTPS にする

Agent も同じ `Kestrel` の設定で HTTPS にできます。その場合は HyperManager の `Agent:Hosts:N:Url` を `https://` にします。HyperManager は証明書を検証するので、**HyperManager のサーバーが信頼する証明書**を使ってください。

---

## 📝 ログ

### 📂 出力先

| | ファイル | 備考 |
|---|---|---|
| 🖥️ | `C:\HyperManager\log\HyperManager.Host_YYYYMMDD.log` | 1 日 1 ファイル、既定で 31 日分を保持 |
| 🤖 | `C:\HyperManager\log\HyperManager.Agent_YYYYMMDD.log` | 同上 |
| 🪟 | イベント ビューアー（Windows ログ → Application） | 起動に失敗したときの例外 |

### 🔍 主な記録

| | レベル | メッセージ | いつ |
|---|---|---|---|
| 🖥️🤖 | INFO | `Service start.` と各種の起動情報 | 起動時 |
| 🖥️ | INFO | `Agent: hosts=[...]` | 起動時（監視するホストの一覧） |
| 🖥️ | WARN | `Host query failed. host=[...], url=[...], message=[...]` | ホストの状態を取得できなかった |
| 🖥️ | INFO | `VM operation succeeded. user=[...], host=[...], vm=[...], operation=[...]` | 操作が成功した（監査） |
| 🖥️ | WARN | `VM operation failed. user=[...], ..., message=[...]` | 操作が失敗した（監査） |
| 🤖 | INFO / WARN | `Privilege: machine=[...], elevated=[...]` / `Agent is not running as administrator.` | 起動時の権限 |
| 🤖 | WARN | `Invalid API key. remoteIpAddress=[...]` | 間違った API キーで呼ばれた |
| 🤖 | INFO / WARN | `VM operation succeeded.` / `VM operation failed.` | 操作の結果 |
| 🤖 | WARN | `Host metrics unavailable.` | ホストのメトリクスを取得できなかった（VM 一覧は返します） |

### 🎚️ 詳しく出す

`Serilog:MinimumLevel:Default` を `Debug` にすると詳細なログが出ます。HTTP の記録は `Log:HttpLog`、SQL の記録は `Profiler:SqlLog:Enable` です（[⚙️ 設定リファレンス](configuration.md#-ログlogserilog)）。

---

## 💓 ヘルスチェック

| | URL | 含まれる確認 | 結果 |
|---|---|---|---|
| 🖥️ | `/health` | 自身・データベース・Agent | 🟢 `Healthy`（200）/ 🟡 `Degraded`（200、到達不可のホストがある）/ 🔴 `Unhealthy`（503） |
| 🖥️ | `/alive` | 自身 | 🟢 `Healthy`（200） |
| 🤖 | `/health` | 自身・Hyper-V | 🟢 `Healthy`（200）/ 🔴 `Unhealthy`（503、Hyper-V に接続できない、または管理者権限がない） |
| 🤖 | `/alive` | 自身 | 🟢 `Healthy`（200） |

> 💡 HyperManager の「Agent」の確認は、最後に取得した結果を使います（確認のたびに Agent を呼び出しません）。

---

## 📈 メトリクス

### 🔥 Prometheus

| | 既定 | 収集する URL |
|---|---|---|
| 🖥️ | 有効（`http://localhost:9464`） | `http://localhost:9464/metrics` |
| 🤖 | 無効 | `Prometheus:Uri` を設定すると有効 |

ほかのサーバーの Prometheus から収集するときは、`Prometheus:Uri` にサーバーの名前か IP を指定します（例: `http://192.168.1.5:9464`）。

- 🪟 Windows では、その URL の予約に管理者権限が必要です。LocalSystem のサービスなら追加の作業は不要です。それ以外のアカウントでは URL を予約します: `netsh http add urlacl url=http://192.168.1.5:9464/ user=<アカウント>`
- 🚫 `0.0.0.0`・`+`・`*` は使えません（起動に失敗します）
- 🏷️ Prometheus 側も同じ名前・IP で収集してください

### 📊 HyperManager 独自のメトリクス

| 名前 | タグ | 内容 |
|---|---|---|
| `monitor.host.count` | `status`（`reachable` / `unreachable`） | 監視しているホストの数 |
| `monitor.vm.count` | `host`・`state` | 仮想マシンの数（最後に取得した結果） |
| `api.request.execution` | `http.request.method`・`http.route` | `/api` へのリクエスト数 |
| `api.request.long.execution` | 同上 | `Telemetry:LongExecutionThreshold` を超えたリクエスト数 |
| `application.uptime` | | 起動してからの秒数 |

このほか、.NET ランタイム・ASP.NET Core・HttpClient の標準メトリクスも出力します。Prometheus では名前の `.` が `_` になり、カウンターには `_total` が付きます（例: `monitor_host_count`）。

**PromQL の例**

```promql
# 到達不可のホストがある（アラート向け）
monitor_host_count{status="unreachable"} > 0

# 実行中の仮想マシンの数
sum(monitor_vm_count{state="Running"})
```

### 🛰️ OpenTelemetry（OTLP）

環境変数 `OTEL_EXPORTER_OTLP_ENDPOINT` を設定すると、ログ・メトリクス・トレースを OTLP で送ります。HyperManager から Agent への呼び出しも 1 本のトレースとしてつながります。

---

## 🗄️ データベース

HyperManager は SQLite のファイル `C:\HyperManager\Host\data.db` を使います。テーブルは起動時に自動で作られます。

| テーブル | 内容 |
|---|---|
| `Account` | ログインアカウント（パスワードはハッシュ化して保存） |
| `OperationLog` | 操作履歴 |

### 💾 バックアップ

```powershell
Stop-Service HyperManager
Copy-Item C:\HyperManager\Host\data.db "D:\Backup\data_$(Get-Date -Format yyyyMMdd).db"
Start-Service HyperManager
```

### 🧹 操作履歴を整理する

操作履歴は自動では削除されません。必要に応じて SQLite のツール（`sqlite3`、DB Browser for SQLite など）で削除します。サービスを止めてから実行してください。

```sql
DELETE FROM OperationLog WHERE ExecutedAt < '2026-01-01';
VACUUM;
```

### 🔑 パスワードを忘れたとき

アカウントは「1 件もないとき」に `Auth:InitialAccount` から作り直されます。

1. 🛑 サービスを止める: `Stop-Service HyperManager`
2. 🗑️ アカウントを削除する（SQLite のツールで）: `DELETE FROM Account;`
3. ✏️ `appsettings.Production.json` の `Auth:InitialAccount` に新しいパスワードを書く
4. ▶️ サービスを開始する: `Start-Service HyperManager`

> ⚠️ すべてのアカウントが削除されます。操作履歴は残ります。

---

## ⬆️ 更新

1. 📦 新しい配布ファイルを作る（`dotnet publish`）
2. 🛑 サービスを止める
3. 💾 `data.db` と `appsettings.Production.json` をバックアップする
4. 📂 配布ファイルを上書きコピーする（例: `robocopy publish\Host C:\HyperManager\Host /E`）
   - `appsettings.Production.json` と `data.db` は配布ファイルに含まれないので、そのまま残ります
5. ▶️ サービスを開始する
6. ✅ `/health` と画面で動作を確認する

> ⚠️ HyperManager と Agent は同じ版にそろえてください。

---

## 🧯 トラブル対応

| 症状 | 考えられる原因 | 対処 |
|---|---|---|
| 🛑 サービスが起動しない | 設定の誤り（API キーがない、ホストの URL が不正など）、ポートの重複、証明書のパス・パスワードの誤り | イベント ビューアーとログで例外を確認します |
| 🛑 `PrometheusExporter HttpListener could not be started` | `Prometheus:Uri` に使えない値、または URL の予約がない | [📈 メトリクス](#-メトリクス) のとおり直すか、空にして無効にします |
| 🔐 ログインしてもログイン画面に戻る | HTTP で開いている | HTTPS にするか、`Auth:AllowHttp` を `true` にします（[🔒 HTTPS と HTTP](#-https-と-http)） |
| 🔌 「サーバーへ再接続しています...」が消えない | ネットワークの切断、プロキシが WebSocket を通していない、サービスの再起動 | ネットワークとプロキシを確認し、ページを再読み込みします |
| 🔴 すべてのホストが到達不可 | HyperManager から Agent へつながらない | ファイアウォール、`Agent:Hosts:N:Url`、API キーを確認します |
| 🔴 Agent の `/health` が 503 | 管理者権限がない、Hyper-V が無効 | LocalSystem のサービスで動かし、Hyper-V の役割を確認します |
| 🟡 仮想マシンが一覧に出ない | Agent に管理者権限がない | 同上 |
| ⏳ 操作がタイムアウトする | ジョブが長い（大きな VM の状態保存など） | `HyperV:JobTimeoutSeconds` と `Agent:OperationTimeoutSeconds` を延ばします |

---

## 🛡️ セキュリティ チェックリスト

- [ ] 🔑 最初の管理者パスワードを変更した（初回起動の前に `Auth:InitialAccount` を設定）
- [ ] 🔑 API キーをホストごとに長いランダム値にした
- [ ] 🧱 Agent のポートを HyperManager のサーバーからだけ許可した
- [ ] 🔒 HyperManager を HTTPS にした（`Auth:AllowHttp` で HTTP を許可する場合は、信頼できるネットワーク内に限定した）
- [ ] 📁 `appsettings.Production.json`（API キー・証明書のパスワードを含む）と `data.db` を、管理者とサービスのアカウントだけが読めるようにした
- [ ] 🏠 Agent との通信を信頼できるネットワーク内に限定した
