namespace ApiDataBatchTool.Configuration;

/// <summary>
/// API接続設定
/// </summary>
public class ApiSettings
{
    public const string SectionName = "Api";

    /// <summary>
    /// APIのベースURL
    /// </summary>
    public required string BaseUrl { get; set; }

    /// <summary>
    /// APIエンドポイント（相対パス）
    /// </summary>
    public required string Endpoint { get; set; }

    /// <summary>
    /// 1ページあたりの最大取得件数
    /// </summary>
    public int PageSize { get; set; } = 10000;

    /// <summary>
    /// タイムアウト（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// リトライ回数
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// 海外フラグ
    /// </summary>
    public bool IsOverseas { get; set; } = false;
}
