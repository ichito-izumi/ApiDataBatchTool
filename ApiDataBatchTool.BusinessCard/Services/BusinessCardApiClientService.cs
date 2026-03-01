using ApiDataBatchTool.BusinessCard.Configuration;
using ApiDataBatchTool.BusinessCard.Models;
using ApiDataBatchTool.Common.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiDataBatchTool.BusinessCard.Services;

/// <summary>
/// 名刺APIクライアントサービス
/// </summary>
public class BusinessCardApiClientService : ApiClientServiceBase<BusinessCardQueryParameters>
{
    private const string HttpClientName = "BusinessCardApi";

    public BusinessCardApiClientService(
        IHttpClientFactory httpClientFactory,
        ILogger<BusinessCardApiClientService> logger,
        IOptions<BusinessCardApiSettings> apiSettings)
        : base(httpClientFactory, logger, apiSettings.Value, HttpClientName)
    {
    }
}
