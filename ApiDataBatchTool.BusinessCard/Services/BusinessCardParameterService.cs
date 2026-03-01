using ApiDataBatchTool.BusinessCard.Configuration;
using ApiDataBatchTool.BusinessCard.Models;
using ApiDataBatchTool.Common.Data;
using ApiDataBatchTool.Common.Data.Entities;
using ApiDataBatchTool.Common.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiDataBatchTool.BusinessCard.Services;

/// <summary>
/// 名刺パラメータサービス
/// </summary>
public class BusinessCardParameterService : ParameterServiceBase<BusinessCardQueryParameters>
{
    private readonly BusinessCardApiSettings _apiSettings;

    public BusinessCardParameterService(
        AppDbContext context,
        ICidProvider cidProvider,
        ILogger<BusinessCardParameterService> logger,
        IOptions<BusinessCardApiSettings> apiSettings)
        : base(context, cidProvider, logger)
    {
        _apiSettings = apiSettings.Value;
    }

    /// <inheritdoc/>
    protected override BusinessCardQueryParameters BuildQueryParameters(string cid, LinkageControlEntity? linkageControl)
    {
        return new BusinessCardQueryParameters(
            Cid: cid,
            CategoryCode: linkageControl?.CategoryCode,
            IsOverseas: _apiSettings.IsOverseas
        );
    }

    /// <inheritdoc/>
    protected override void LogQueryParameters(BusinessCardQueryParameters queryParameters)
    {
        Logger.LogInformation(
            "バッチパラメータを取得しました: Cid={Cid}, CategoryCode={CategoryCode}, IsOverseas={IsOverseas}",
            queryParameters.Cid,
            queryParameters.CategoryCode ?? "(なし)",
            queryParameters.IsOverseas);
    }
}
