# 📚 HyperManager ドキュメント

使い方や導入手順は [README](../README.md) にまとめています。ここでは管理者・開発者向けの詳しい情報を扱います。

## 🧑‍💼 管理者向け

| ドキュメント | 内容 |
|---|---|
| ⚙️ [設定リファレンス](configuration.md) | HyperManager と Agent のすべての設定項目・既定値・設定の書き方 |
| 🩺 [運用ガイド](operations.md) | サービス登録、HTTPS、ログ、ヘルスチェック、メトリクス、バックアップ、更新、トラブル対応 |
| 🔄 [hv-monitor からの移行](migration.md) | 旧 hv-monitor との違いと移行手順 |

## 🧑‍💻 開発者向け

| ドキュメント | 内容 |
|---|---|
| 🏗️ [アーキテクチャ](architecture.md) | 全体構成、処理の流れ、データ、設計方針 |
| 🔌 [API リファレンス](api.md) | Agent と HyperManager の API |
| 🧬 [Hyper-V 連携](hyperv.md) | CIM による情報取得・操作、状態コード、ホストメトリクス |
| 🛠️ [開発ガイド](development.md) | プロジェクト構成、起動、テスト、コーディング規約、よくある変更 |

## 📖 用語

| 用語 | 意味 |
|---|---|
| 🖥️ HyperManager | 中央の Web 画面。プロジェクト名は `HyperManager.Host` |
| 🤖 Agent | 各 Hyper-V ホストで動く API サーバー。プロジェクト名は `HyperManager.Agent` |
| 🏠 ホスト | Hyper-V が動いている物理サーバー |
| 💻 仮想マシン（VM） | Hyper-V 上のゲスト |
| 📸 スナップショット | Agent から 1 回で取得する「ホスト情報 + メトリクス + VM 一覧」（Hyper-V のチェックポイントとは別物） |
| 🔑 API キー | HyperManager が Agent を呼び出すときに使う合言葉（`X-Api-Key` ヘッダー） |
