using ApiDataBatchTool.Common.Data;
using ApiDataBatchTool.Common.Data.Entities;
using ApiDataBatchTool.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ApiDataBatchTool.Common.Services;

/// <summary>
/// パラメータサービス基底クラス
/// </summary>
public abstract class ParameterServiceBase<TQueryParams> : IParameterService<TQueryParams>
    where TQueryParams : ApiQueryParametersBase
{
    protected readonly AppDbContext Context;
    protected readonly ICidProvider CidProvider;
    protected readonly ILogger Logger;

    protected ParameterServiceBase(
        AppDbContext context,
        ICidProvider cidProvider,
        ILogger logger)
    {
        Context = context;
        CidProvider = cidProvider;
        Logger = logger;
    }

    /// <inheritdoc/>
    public async Task<TQueryParams> GetApiQueryParametersAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("バッチパラメータを取得します");

        // batファイルを実行してCIDを取得
        var cid = await CidProvider.GetCidAsync(cancellationToken);

        // 連携制御テーブルからデータを取得
        var linkageControl = await GetLinkageControlAsync(cancellationToken);

        // 派生クラスでクエリパラメータを組み立て
        var queryParameters = BuildQueryParameters(cid, linkageControl);

        LogQueryParameters(queryParameters);

        return queryParameters;
    }

    /// <summary>
    /// 連携制御テーブルからデータを取得
    /// </summary>
    protected virtual async Task<LinkageControlEntity?> GetLinkageControlAsync(CancellationToken cancellationToken)
    {
        return await Context.LinkageControls
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// クエリパラメータを組み立てる（派生クラスで実装）
    /// </summary>
    protected abstract TQueryParams BuildQueryParameters(string cid, LinkageControlEntity? linkageControl);

    /// <summary>
    /// クエリパラメータをログ出力（派生クラスでオーバーライド可能）
    /// </summary>
    protected virtual void LogQueryParameters(TQueryParams queryParameters)
    {
        Logger.LogInformation(
            "バッチパラメータを取得しました: Cid={Cid}, CategoryCode={CategoryCode}",
            queryParameters.Cid,
            queryParameters.CategoryCode ?? "(なし)");
    }
}
