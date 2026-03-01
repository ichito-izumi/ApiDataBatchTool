namespace ApiDataBatchTool.Common.Services;

/// <summary>
/// CID取得プロバイダーインターフェース
/// </summary>
public interface ICidProvider
{
    /// <summary>
    /// batファイルを実行してCIDを取得する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>CID文字列</returns>
    Task<string> GetCidAsync(CancellationToken cancellationToken = default);
}
