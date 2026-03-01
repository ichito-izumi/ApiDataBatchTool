using ApiDataBatchTool.Common.Data;
using ApiDataBatchTool.Common.Data.Entities;
using ApiDataBatchTool.Common.Services;
using ApiDataBatchTool.Office.Models;
using Microsoft.Extensions.Logging;

namespace ApiDataBatchTool.Office.Services;

/// <summary>
/// 事業所パラメータサービス
/// </summary>
public class OfficeParameterService : ParameterServiceBase<OfficeQueryParameters>
{
    public OfficeParameterService(
        AppDbContext context,
        ICidProvider cidProvider,
        ILogger<OfficeParameterService> logger)
        : base(context, cidProvider, logger)
    {
    }

    /// <inheritdoc/>
    protected override OfficeQueryParameters BuildQueryParameters(string cid, LinkageControlEntity? linkageControl)
    {
        var categoryCode = linkageControl?.CategoryCode
            ?? throw new InvalidOperationException("連携制御テーブルからカテゴリコードを取得できませんでした");

        return new OfficeQueryParameters(
            Cid: cid,
            CategoryCode: categoryCode
        );
    }
}
