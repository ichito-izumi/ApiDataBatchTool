using ApiDataBatchTool.Common.Models;

namespace ApiDataBatchTool.Common.Services;

/// <summary>
/// APIクライアントサービスインターフェース
/// </summary>
public interface IApiClientService<TQueryParams> where TQueryParams : ApiQueryParametersBase
{
    /// <summary>
    /// 全ページのデータを取得する
    /// </summary>
    /// <typeparam name="T">データ項目の型</typeparam>
    /// <param name="queryParameters">クエリパラメータ</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>全ページのデータを統合したリスト</returns>
    Task<List<T>> GetAllPagesAsync<T>(TQueryParams queryParameters, CancellationToken cancellationToken = default);
}
