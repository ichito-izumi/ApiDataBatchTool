using System.Threading;
using System.Threading.Tasks;

namespace ApiDataBatchTool.Common.Services;

/// <summary>
/// 実行履歴サービスインターフェース
/// </summary>
public interface IExecutionHistoryService
{
    /// <summary>
    /// 連続失敗回数を取得する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>連続失敗回数</returns>
    Task<int> GetConsecutiveFailureCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 実行結果を記録する
    /// </summary>
    /// <param name="isSuccess">成功したかどうか</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>更新後の連続失敗回数</returns>
    Task<int> RecordExecutionResultAsync(bool isSuccess, CancellationToken cancellationToken = default);
}
