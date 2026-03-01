using ApiDataBatchTool.Common.Models;

namespace ApiDataBatchTool.Common.Services;

/// <summary>
/// パラメータサービスインターフェース
/// </summary>
public interface IParameterService<TQueryParams> where TQueryParams : ApiQueryParametersBase
{
    /// <summary>
    /// DBからAPIクエリパラメータを取得する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>APIクエリパラメータ</returns>
    Task<TQueryParams> GetApiQueryParametersAsync(CancellationToken cancellationToken = default);
}
