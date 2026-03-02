using ApiDataBatchTool.Common.Models;

namespace ApiDataBatchTool.Common.Services;

/// <summary>
/// APIクライアントサービスインターフェース
/// </summary>
/// <typeparam name="TQueryParams">クエリパラメータの型</typeparam>
/// <typeparam name="TDto">DTOの型</typeparam>
public interface IApiClientService<TQueryParams, TDto>
    where TQueryParams : ApiQueryParametersBase
{
    /// <summary>
    /// 全ページのデータを取得する
    /// </summary>
    /// <param name="queryParameters">クエリパラメータ</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>全ページのデータを統合したリスト</returns>
    Task<List<TDto>> GetAllPagesAsync(TQueryParams queryParameters, CancellationToken cancellationToken = default);
}
