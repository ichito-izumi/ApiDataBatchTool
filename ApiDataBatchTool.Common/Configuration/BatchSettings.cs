using System.ComponentModel.DataAnnotations;

namespace ApiDataBatchTool.Common.Configuration;

/// <summary>
/// バッチ処理設定
/// </summary>
public class BatchSettings
{
    public const string SectionName = "Batch";

    /// <summary>
    /// バッチ処理名（ログ出力用）
    /// </summary>
    [Required(ErrorMessage = "Batch:BatchName は必須です")]
    public required string BatchName { get; set; }

    /// <summary>
    /// バッチ処理のバッチサイズ（MERGE時のコミット単位）
    /// </summary>
    [Range(1, 10000, ErrorMessage = "Batch:MergeBatchSize は1から10000の間で指定してください")]
    public int MergeBatchSize { get; set; } = 1000;

    /// <summary>
    /// シャットダウンタイムアウト（秒）
    /// </summary>
    [Range(5, 600, ErrorMessage = "Batch:ShutdownTimeoutSeconds は5から600の間で指定してください")]
    public int ShutdownTimeoutSeconds { get; set; } = 60;
}
