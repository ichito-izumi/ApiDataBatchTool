using ApiDataBatchTool.Models;

namespace ApiDataBatchTool.Services;

/// <summary>
/// パラメータサービスインターフェース
/// </summary>
public interface IParameterService
{
    /// <summary>
    /// DBからAPIクエリパラメータを取得する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>APIクエリパラメータ</returns>
    Task<ApiQueryParameters> GetApiQueryParametersAsync(CancellationToken cancellationToken = default);
}
