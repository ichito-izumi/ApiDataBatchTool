using ApiDataBatchTool.Common.Configuration;
using ApiDataBatchTool.Common.Services;
using ApiDataBatchTool.Office.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiDataBatchTool.Office.Services;

/// <summary>
/// 事業所バッチサービス
/// </summary>
public class OfficeBatchService : BatchServiceBase<OfficeQueryParameters, OfficeDto>
{
    public OfficeBatchService(
        IParameterService<OfficeQueryParameters> parameterService,
        IApiClientService<OfficeQueryParameters> apiClientService,
        IDataRepository<OfficeDto> repository,
        ILogger<OfficeBatchService> logger,
        IOptions<BatchSettings> batchSettings)
        : base(parameterService, apiClientService, repository, logger, batchSettings)
    {
    }
}
