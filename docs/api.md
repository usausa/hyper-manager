# 🔌 API リファレンス

Agent と HyperManager が公開する API をまとめます。

---

## 🤖 Agent API

HyperManager が各 Hyper-V ホストの Agent を呼び出すための API です。

### 📏 共通ルール

| 項目 | 内容 |
|---|---|
| 🌐 ベース URL | `http://<ホスト>:5100` |
| 🔑 認証 | `/health`・`/alive` 以外はヘッダー `X-Api-Key: <API キー>` が必要 |
| 📦 形式 | JSON（UTF-8、プロパティは camelCase、`state` は文字列、値のないプロパティは省略） |
| ⏱️ 時間 | `uptime` は `[d.]hh:mm:ss[.fffffff]` 形式の文字列（例: `1.02:03:04`） |
| ⏳ 操作 | 完了まで待ってから応答します（ジョブ待ちは既定で最大 5 分） |
| ❌ エラー | [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457) の ProblemDetails（`application/problem+json`、`traceId` 付き） |

### 📋 エンドポイント一覧

| メソッド | パス | 説明 | 成功 |
|---|---|---|---|
| GET | `/health` | ヘルスチェック（Hyper-V への接続を含む、認証不要） | 200 `Healthy` |
| GET | `/alive` | 生存確認（認証不要） | 200 `Healthy` |
| GET | `/api/host` | ホスト情報 | 200 `HostInfo` |
| GET | `/api/host/metrics` | ホストのメトリクス | 200 `HostMetrics` |
| GET | `/api/snapshot` | ホスト情報・メトリクス・VM 一覧をまとめて取得 | 200 `HostSnapshot` |
| GET | `/api/vms` | VM 一覧 | 200 `VmInfo[]` |
| POST | `/api/vms/{id}/start` | ▶️ 開始 | 204 |
| POST | `/api/vms/{id}/resume` | ⏯️ 再開 | 204 |
| POST | `/api/vms/{id}/pause` | ⏸️ 一時停止 | 204 |
| POST | `/api/vms/{id}/save` | 💾 状態保存 | 204 |
| POST | `/api/vms/{id}/shutdown` | 📴 シャットダウン（本文は省略可） | 204 |
| POST | `/api/vms/{id}/turnoff` | ⛔ 強制停止 | 204 |

`{id}` は VM の ID（GUID）です。`/api/vms` や `/api/snapshot` の `id` をそのまま使います。

### 🧪 呼び出し例

```powershell
# スナップショットを取得
curl.exe -H "X-Api-Key: <API キー>" http://192.168.1.10:5100/api/snapshot

# 開始
curl.exe -X POST -H "X-Api-Key: <API キー>" http://192.168.1.10:5100/api/vms/6F1B0B9E-3A55-4B79-9C63-2B4E5B8A0C11/start

# 本文付きの呼び出し（強制シャットダウン）
Invoke-RestMethod -Method Post -Uri http://192.168.1.10:5100/api/vms/6F1B0B9E-3A55-4B79-9C63-2B4E5B8A0C11/shutdown -Headers @{ "X-Api-Key" = "<API キー>" } -ContentType application/json -Body '{"force":true}'
```

### 📸 `GET /api/snapshot`

```json
{
  "host": { "name": "HV-HOST-A", "isElevated": true },
  "metrics": {
    "cpuUsagePercent": 13,
    "totalMemoryMb": 65459,
    "usedMemoryMb": 32768,
    "disks": [
      { "name": "C:", "totalBytes": 999043416064, "freeBytes": 158413393920 }
    ]
  },
  "vms": [
    { "id": "6F1B0B9E-3A55-4B79-9C63-2B4E5B8A0C11", "name": "web-01", "state": "Running", "cpuUsagePercent": 5, "memoryUsageMb": 4096, "uptime": "1.02:03:04", "version": "12.0" },
    { "id": "0C2D8F51-7B6A-4E39-A1F4-3D9E2C7B5A60", "name": "db-01", "state": "Off", "version": "12.0" }
  ]
}
```

> 💡 ホストのメトリクスだけ取得できなかった場合は、エラーにせず `metrics` を省略して VM 一覧を返します。

### 📴 `POST /api/vms/{id}/shutdown`

| プロパティ | 型 | 既定 | 説明 |
|---|---|---|---|
| `force` | bool | `false` | `true` にすると、アプリケーションが終了を拒んでもシャットダウンします |

本文を省略すると `force: false` として扱います。

### ❌ エラー

| ステータス | 状況 |
|---|---|
| 400 | リクエストの本文が不正 |
| 401 | API キーがない、または違う |
| 404 | VM が見つからない（`{id}` が GUID でない場合を含む） |
| 409 | 操作に失敗した（現在の状態では実行できない、ジョブの失敗など） |
| 500 | 一覧・メトリクスの取得に失敗した |

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflict",
  "status": 409,
  "detail": "状態の変更に失敗しました: 現在の状態では実行できません (戻り値=32775)",
  "traceId": "00-81458c3c60b9d24c9357c87698ac2489-60d3380a362cc2b4-00"
}
```

### 🧱 データ型

| 型 | プロパティ |
|---|---|
| `HostInfo` | `name`（マシン名）、`isElevated`（Agent が管理者権限で動いているか） |
| `HostMetrics` | `cpuUsagePercent`、`totalMemoryMb`、`usedMemoryMb`、`disks`（`DiskInfo[]`） |
| `DiskInfo` | `name`（`C:` など）、`totalBytes`、`freeBytes` |
| `HostSnapshot` | `host`（`HostInfo`）、`metrics`（`HostMetrics`、省略あり）、`vms`（`VmInfo[]`） |
| `VmInfo` | `id`、`name`、`state`（`VmState`）、`cpuUsagePercent`・`memoryUsageMb`・`uptime`（実行中のみ）、`version` |
| `ShutdownRequest` | `force` |

**`VmState`**

| 値 | 意味 |
|---|---|
| `Running` | 🟢 実行中 |
| `Off` | ⚪ 停止 |
| `Saved` | 🔵 保存済み |
| `Paused` | 🟡 一時停止 |
| `Starting` / `Stopping` / `Saving` / `Pausing` / `Resuming` | ⏳ 切り替え中 |
| `Other` | ❔ 上記以外 |
| `Unknown` | 予約（現在は返しません） |

Hyper-V の状態コードとの対応は [🧬 Hyper-V 連携](hyperv.md#-状態の対応) を参照してください。

---

## 🖥️ HyperManager API

HyperManager の画面が使う API です。**ログイン（Cookie）が必要**で、ログインしていないと 401 を返します。

| メソッド | パス | 説明 |
|---|---|---|
| GET | `/api/operations` | 📜 操作履歴（新しい順） |
| GET | `/api/operations/csv` | 📥 操作履歴の CSV（全件） |
| POST | `/auth/login` | 🔑 ログイン（ログイン画面のフォームから送信） |
| POST | `/auth/logout` | 🚪 ログアウト |
| GET | `/health` | 💓 ヘルスチェック（認証不要） |
| GET | `/alive` | 💓 生存確認（認証不要） |

### 📜 `GET /api/operations`

| パラメーター | 既定 | 範囲 | 説明 |
|---|---|---|---|
| `keyword` | なし | 100 文字まで | ホスト名・VM 名の部分一致 |
| `page` | `0` | 0 以上 | ページ番号（0 から） |
| `size` | `20` | 1〜100 | 1 ページの件数 |

```json
{
  "total": 1,
  "page": 0,
  "size": 20,
  "items": [
    {
      "id": 42,
      "executedAt": "2026-10-31T10:15:30.123",
      "userId": "admin",
      "hostName": "Host A",
      "vmId": "6F1B0B9E-3A55-4B79-9C63-2B4E5B8A0C11",
      "vmName": "web-01",
      "operation": "Start",
      "success": true
    }
  ]
}
```

失敗した操作には `message`（理由）が付きます。

### 📥 `GET /api/operations/csv`

`operations.csv`（UTF-8・BOM 付き）をダウンロードします。列は `Id, ExecutedAt, UserId, HostName, VmId, VmName, Operation, Success, Message` です。

---

## 🧪 OpenAPI / Swagger UI

開発環境（`Development`）では、Agent と HyperManager の両方で次を公開します。

| パス | 内容 |
|---|---|
| `/openapi/v1.json` | OpenAPI ドキュメント（JSON） |
| `/openapi/v1.yaml` | OpenAPI ドキュメント（YAML） |
| `/swagger` | Swagger UI |
| `/redoc` | ReDoc |

> 💡 Agent の Swagger UI では、［Authorize］から API キーを入力すると認証が必要な API も試せます。
