namespace ApiDataBatchTool.Configuration;

/// <summary>
/// バッチ処理設定
/// </summary>
public class BatchSettings
{
    public const string SectionName = "Batch";

    /// <summary>
    /// バッチ処理名（ログ出力用）
    /// </summary>
    public string BatchName { get; set; } = "ApiDataBatchTool";

    /// <summary>
    /// パラメータ取得用テーブル名
    /// TODO: 実際のテーブル名に置き換えてください
    /// </summary>
    public string ParameterTableName { get; set; } = "BATCH_PARAMETERS";

    /// <summary>
    /// バッチ処理のバッチサイズ（MERGE時のコミット単位）
    /// </summary>
    public int MergeBatchSize { get; set; } = 1000;
}
