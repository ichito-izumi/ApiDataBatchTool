namespace ApiDataBatchTool.Configuration;

/// <summary>
/// データベース接続設定
/// </summary>
public class DatabaseSettings
{
    public const string SectionName = "Database";

    /// <summary>
    /// Oracle接続文字列
    /// TODO: 本番環境では適切な接続文字列に置き換えてください
    /// 例: "User Id=user;Password=pass;Data Source=host:port/service"
    /// </summary>
    public required string ConnectionString { get; set; }

    /// <summary>
    /// コマンドタイムアウト（秒）
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// MERGE対象テーブル名
    /// TODO: 実際のテーブル名に置き換えてください
    /// </summary>
    public string TargetTableName { get; set; } = "PRODUCT_MASTER";

    /// <summary>
    /// MERGE後に実行するストアドプロシージャ名
    /// TODO: 実際のプロシージャ名に置き換えてください
    /// </summary>
    public string PostMergeProcedureName { get; set; } = "PKG_PRODUCT.PROC_POST_MERGE";
}
