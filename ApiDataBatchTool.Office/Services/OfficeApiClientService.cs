using ApiDataBatchTool.Common.Services;
using ApiDataBatchTool.Office.Configuration;
using ApiDataBatchTool.Office.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiDataBatchTool.Office.Services;

/// <summary>
/// 事業所APIクライアントサービス
/// </summary>
public class OfficeApiClientService : ApiClientServiceBase<OfficeQueryParameters>
{
    public OfficeApiClientService(
        IHttpClientFactory httpClientFactory,
        ILogger<OfficeApiClientService> logger,
        IOptions<OfficeApiSettings> apiSettings)
        : base(httpClientFactory, logger, apiSettings.Value, "OfficeApi")
    {
    }
}
