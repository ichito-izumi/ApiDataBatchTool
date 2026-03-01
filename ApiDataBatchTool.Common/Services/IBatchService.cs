namespace ApiDataBatchTool.Common.Services;

/// <summary>
/// バッチ処理サービスインターフェース
/// </summary>
public interface IBatchService
{
    /// <summary>
    /// バッチ処理を実行する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>処理結果（0: 成功, 1以上: エラー）</returns>
    Task<int> ExecuteAsync(CancellationToken cancellationToken = default);
}
