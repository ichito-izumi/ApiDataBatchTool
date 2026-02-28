# ApiDataBatchTool

## プロジェクト概要
REST APIからデータを取得し、Oracleデータベースへ登録・更新するバッチツール。

## 技術スタック
- .NET 10 / C# 14
- Generic Host (Microsoft.Extensions.Hosting)
- Oracle EF Core + ODP.NET Managed
- Microsoft.Extensions.Http.Resilience（リトライ）
- Microsoft.Extensions.Logging

## アーキテクチャ
- DI ベース、インターフェースで疎結合
- レイヤー: Worker → Service → Repository

## コーディング規約
- async/await を一貫して使用
- ログは ILogger<T> で構造化ログ出力
- SQL はパラメータ化クエリ必須
```

### 2-3. 開発の推奨フロー（ステップバイステップ）

Claude Code への指示は、小さな単位で段階的に行うのがコツです。
以下の順序で進めると効率的です。

---

#### Step 1: 設定モデル & appsettings.json

```
> 仕様書の appsettings.json 構成例を基に、以下を作成して:
  1. Configuration/ApiSettings.cs
  2. Configuration/DatabaseSettings.cs
  3. Configuration/BatchSettings.cs
  4. appsettings.json
  5. appsettings.Development.json
```

#### Step 2: Program.cs（Generic Host 構成）

```
> Program.cs を Generic Host パターンで構成して。
  以下をDI登録する骨組みだけ作って:
  - appsettings.json の読み込み
  - ILogger
  - HttpClientFactory（リトライポリシー付き）
  - DbContext
  - 各サービスのインターフェースと実装（空でOK）
  - BatchWorker を BackgroundService として登録
```

#### Step 3: API クライアント（ページネーション対応）

```
> Services/IApiClientService.cs と ApiClientService.cs を作成して。
  要件:
  - IHttpClientFactory を使う
  - ページネーション対応（1ページ最大10,000件）
  - 取得件数 == 10,000 なら次ページをリクエスト
  - 全ページ結果を List<T> に統合して返す
  - ページごとに Info ログ（ページ番号、取得件数、累計件数）
  - 最大ページ数の安全制限（appsettings.json の MaxPages）
```

#### Step 4: データベース処理

```
> Data/AppDbContext.cs と Data/Repositories/ を作成して。
  要件:
  - Oracle EF Core で DbContext 構成
  - MERGE文の実行メソッド（ExecuteSqlRawAsync、パラメータ化クエリ）
  - ストアドプロシージャ実行メソッド（ODP.NET、引数なし）
  - トランザクション管理
```

#### Step 5: バッチオーケストレーション

```
> Services/BatchService.cs を作成して。
  処理フロー:
  1. ParameterService でパラメータ取得
  2. ApiClientService で全ページ取得
  3. DataRepository で MERGE 実行
  4. DataRepository でプロシージャ実行
  各ステップの前後にログ出力。エラー時は適切な終了コードを返す。
```