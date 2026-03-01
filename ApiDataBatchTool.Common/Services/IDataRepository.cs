namespace ApiDataBatchTool.Common.Services;

/// <summary>
/// データリポジトリインターフェース
/// </summary>
public interface IDataRepository<TDto>
{
    /// <summary>
    /// データをMERGE（UPSERT）する
    /// </summary>
    /// <param name="items">データリスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>処理件数</returns>
    Task<int> MergeAsync(IEnumerable<TDto> items, CancellationToken cancellationToken = default);

    /// <summary>
    /// MERGE後処理のストアドプロシージャを実行する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task ExecutePostMergeProcedureAsync(CancellationToken cancellationToken = default);
}
