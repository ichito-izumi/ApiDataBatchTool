using ApiDataBatchTool.BusinessCard.Models;
using ApiDataBatchTool.Common.Configuration;
using ApiDataBatchTool.Common.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiDataBatchTool.BusinessCard.Services;

/// <summary>
/// 名刺バッチサービス
/// </summary>
public class BusinessCardBatchService : BatchServiceBase<BusinessCardQueryParameters, BusinessCardDto>
{
    public BusinessCardBatchService(
        IParameterService<BusinessCardQueryParameters> parameterService,
        IApiClientService<BusinessCardQueryParameters> apiClientService,
        IDataRepository<BusinessCardDto> repository,
        ILogger<BusinessCardBatchService> logger,
        IOptions<BatchSettings> batchSettings)
        : base(parameterService, apiClientService, repository, logger, batchSettings)
    {
    }
}
