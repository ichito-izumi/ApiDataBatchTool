using System.ComponentModel.DataAnnotations;

namespace ApiDataBatchTool.Common.Configuration;

/// <summary>
/// 実行履歴設定
/// </summary>
public class ExecutionHistorySettings
{
    public const string SectionName = "ExecutionHistory";

    /// <summary>
    /// 実行履歴ファイルのパス
    /// </summary>
    [Required(ErrorMessage = "ExecutionHistory:FilePath は必須です")]
    public required string FilePath { get; set; }

    /// <summary>
    /// メール送信をトリガーする連続失敗回数の閾値
    /// </summary>
    [Range(1, 100, ErrorMessage = "ExecutionHistory:ConsecutiveFailureThreshold は1から100の間で指定してください")]
    public int ConsecutiveFailureThreshold { get; set; } = 2;
}
