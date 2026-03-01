using System.ComponentModel.DataAnnotations;

namespace ApiDataBatchTool.Common.Configuration;

/// <summary>
/// API接続設定（基底クラス）
/// </summary>
public class ApiSettingsBase
{
    public const string SectionName = "Api";

    /// <summary>
    /// APIのベースURL
    /// </summary>
    [Required(ErrorMessage = "Api:BaseUrl は必須です")]
    [Url(ErrorMessage = "Api:BaseUrl は有効なURLを指定してください")]
    public required string BaseUrl { get; set; }

    /// <summary>
    /// APIエンドポイント（相対パス）
    /// </summary>
    [Required(ErrorMessage = "Api:Endpoint は必須です")]
    public required string Endpoint { get; set; }

    /// <summary>
    /// 1ページあたりの最大取得件数
    /// </summary>
    [Range(1, 100000, ErrorMessage = "Api:PageSize は1から100000の間で指定してください")]
    public int PageSize { get; set; } = 10000;

    /// <summary>
    /// タイムアウト（秒）
    /// </summary>
    [Range(1, 600, ErrorMessage = "Api:TimeoutSeconds は1から600の間で指定してください")]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// リトライ回数
    /// </summary>
    [Range(0, 10, ErrorMessage = "Api:RetryCount は0から10の間で指定してください")]
    public int RetryCount { get; set; } = 3;
}
