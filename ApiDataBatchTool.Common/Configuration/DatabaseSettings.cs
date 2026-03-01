using System.ComponentModel.DataAnnotations;

namespace ApiDataBatchTool.Common.Configuration;

/// <summary>
/// データベース接続設定
/// </summary>
public class DatabaseSettings
{
    public const string SectionName = "Database";

    private string? _connectionString;

    /// <summary>
    /// 接続文字列取得用キー（appsettings.jsonから設定）
    /// </summary>
    [Required(ErrorMessage = "Database:ConnectionStringKey は必須です")]
    public required string ConnectionStringKey { get; set; }

    /// <summary>
    /// Oracle接続文字列（ConnectionStringKeyを使用して外部DLLから取得）
    /// </summary>
    public string ConnectionString
    {
        get => _connectionString ??= ResolveConnectionString(ConnectionStringKey);
        set => _connectionString = value;
    }

    /// <summary>
    /// コマンドタイムアウト（秒）
    /// </summary>
    [Range(1, 3600, ErrorMessage = "Database:CommandTimeoutSeconds は1から3600の間で指定してください")]
    public int CommandTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// MERGE対象テーブル名
    /// </summary>
    [Required(ErrorMessage = "Database:TargetTableName は必須です")]
    public required string TargetTableName { get; set; }

    /// <summary>
    /// MERGE後に実行するストアドプロシージャ名
    /// </summary>
    [Required(ErrorMessage = "Database:PostMergeProcedureName は必須です")]
    public required string PostMergeProcedureName { get; set; }

    /// <summary>
    /// 外部DLLから接続文字列を取得する
    /// </summary>
    private static string ResolveConnectionString(string key)
    {
        // TODO: 実際の接続文字列DLLを呼び出すように変更してください
        // 例: return YourSecurityLibrary.ConnectionStringProvider.GetConnectionString(key);

        // 開発用: キーをそのまま返す（本番導入前に上記に置き換え）
        return key;
    }
}
