# 🔄 hv-monitor からの移行

HyperManager は hv-monitor の後継です。hv-monitor の機能をすべて引き継ぎ、ログイン・操作履歴などを加えて作り直しました。

---

## 🗺️ 何が変わったか

| 項目 | hv-monitor | HyperManager |
|---|---|---|
| 🖥️ 中央の画面 | `Monitor.Web` | `HyperManager.Host` |
| 🤖 ホストの API | `Monitor.Agent` + `Monitor.Core` | `HyperManager.Agent` |
| 📜 API の契約 | `Monitor.Contracts` | `HyperManager.Contracts` |
| 🔐 ログイン | なし（誰でも操作できた） | あり（VM の操作は管理者だけ） |
| 📜 操作の記録 | ログのみ | データベースに記録し、画面で検索・CSV 出力 |
| 🪟 Agent の動かし方 | 管理者で `dotnet run` | Windows サービス |
| 🔒 画面の通信 | HTTP | 本番は既定で HTTPS（`Auth:AllowHttp` で HTTP も可） |
| 🔍 一覧の絞り込み | ホストのみ | ホストと名前、列での並べ替え |
| 🩺 運用 | — | ヘルスチェック、Prometheus / OTLP のメトリクス、ファイルログ |

---

## 🐛 直った問題

| 問題 | hv-monitor での症状 | HyperManager |
|---|---|---|
| 🚦 状態コードの取り違え | 旧 `root\virtualization`（v1）の値を使っていたため、一時停止・保存済み・起動中・停止中の VM が「不明」と表示され、操作ボタンが出なかった。一時停止・状態保存の操作も正しい値を送れていなかった | v2 の値（一時停止 9・保存済み 6 など）に修正（[🧬 Hyper-V 連携](hyperv.md#-状態の対応)） |
| 📴 シャットダウンの判定 | `InitiateShutdown` が 4096（開始した）を返すと失敗と表示した | 成功として扱う |
| 🔍 VM の ID | ID をそのまま問い合わせに埋め込んでいた | GUID として検証してから使う |
| ⏱️ 応答しないホスト | 1 台が応答しないと、全体の更新が遅れた（タイムアウト 120 秒） | 取得のタイムアウトを 10 秒に分離 |
| 🔑 API キーの比較 | 通常の文字列比較 | 定数時間で比較 |

---

## ⚙️ 設定の読み替え

### 🖥️ HyperManager（旧 Monitor.Web）

| hv-monitor | HyperManager | 備考 |
|---|---|---|
| `HyperVHosts[].Name` | `Agent:Hosts[].Name` | |
| `HyperVHosts[].BaseUrl` | `Agent:Hosts[].Url` | パス付きの URL も可 |
| `HyperVHosts[].ApiKey` | `Agent:Hosts[].ApiKey` | |
| `Monitoring:Enabled` | `Monitor:Enable` | |
| `Monitoring:RefreshIntervalSeconds` | `Monitor:IntervalSeconds` | |
| （固定 120 秒） | `Agent:QueryTimeoutSeconds`（10）・`Agent:OperationTimeoutSeconds`（330） | 取得と操作で分離 |
| — | `Auth:InitialAccount` | 🆕 最初の管理者アカウント |
| — | `ConnectionStrings:Default` | 🆕 操作履歴などのデータベース |

**変更前（hv-monitor）**

```json
{
  "Monitoring": { "Enabled": true, "RefreshIntervalSeconds": 10 },
  "HyperVHosts": [
    { "Name": "Host A", "BaseUrl": "http://192.168.1.10:5100", "ApiKey": "key-a" }
  ]
}
```

**変更後（HyperManager）**

```json
{
  "Monitor": { "Enable": true, "IntervalSeconds": 10 },
  "Agent": {
    "Hosts": [
      { "Name": "Host A", "Url": "http://192.168.1.10:5100", "ApiKey": "key-a" }
    ]
  }
}
```

### 🤖 Agent

| hv-monitor | HyperManager | 備考 |
|---|---|---|
| `Agent:ApiKey` | `Agent:ApiKey` | **必須**になりました（未設定では起動しません） |
| `Kestrel:Endpoints:Http:Url`（`http://0.0.0.0:5100`） | `http_ports`（`5100`） | `Kestrel` の設定もそのまま使えます |

---

## 🔌 API の変更（Agent）

| 項目 | hv-monitor | HyperManager |
|---|---|---|
| 💓 疎通確認 | `GET /api/health` | `GET /health`（Hyper-V の確認を含む）・`GET /alive` |
| 🎛️ 操作の応答 | 200 | 204 |
| 📴 シャットダウンの本文 | 必須 | 省略可（`force: false`） |
| 🆔 VM の ID | 任意の文字列 | GUID のみ（それ以外は 404） |
| 🚦 `state` | 数値 | 文字列（`"Running"` など） |
| ∅ 値のないプロパティ | `null` を出力 | 省略 |
| 🔑 認証の失敗 | 本文なしの 401 | ProblemDetails の 401 |

> ⚠️ 旧 Agent と新しい HyperManager（またはその逆）を組み合わせて使うことは想定していません。両方を入れ替えてください。

---

## 🚚 移行の手順

1. 🛑 hv-monitor（`Monitor.Web`・`Monitor.Agent`）を止める
2. 🤖 各 Hyper-V ホストに新しい Agent を入れる（[README の導入手順](../README.md#-導入手順) の 2️⃣）
   - API キーは、十分に長いランダム値なら旧設定の値を引き継げます
3. 🖥️ HyperManager を入れる（[README の導入手順](../README.md#-導入手順) の 3️⃣）
   - `HyperVHosts` を `Agent:Hosts` に書き換えます（上の表）
4. 🔒 HTTPS を用意する。hv-monitor と同じように HTTP で使う場合は `Auth:AllowHttp` を `true` にする（[🩺 運用ガイド](operations.md#-https-と-http)）
5. ✅ 画面で状態の表示と、一時停止・状態保存などの操作を確認する
6. 🧹 hv-monitor のファイルを削除する
