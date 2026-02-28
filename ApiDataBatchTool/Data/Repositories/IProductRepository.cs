using ApiDataBatchTool.Models;

namespace ApiDataBatchTool.Data.Repositories;

/// <summary>
/// 商品リポジトリインターフェース
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// 商品データをMERGE（UPSERT）する
    /// </summary>
    /// <param name="products">商品データリスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>処理件数</returns>
    Task<int> MergeProductsAsync(IEnumerable<ProductDto> products, CancellationToken cancellationToken = default);

    /// <summary>
    /// MERGE後処理のストアドプロシージャを実行する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task ExecutePostMergeProcedureAsync(CancellationToken cancellationToken = default);
}
