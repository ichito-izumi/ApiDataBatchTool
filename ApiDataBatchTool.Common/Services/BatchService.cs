using System.Diagnostics;
using ApiDataBatchTool.Common.Configuration;
using ApiDataBatchTool.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiDataBatchTool.Common.Services;

/// <summary>
/// バッチサービス
/// </summary>
/// <typeparam name="TQueryParams">クエリパラメータの型</typeparam>
/// <typeparam name="TDto">DTOの型</typeparam>
public class BatchService<TQueryParams, TDto> : IBatchService
    where TQueryParams : ApiQueryParametersBase
{
    private readonly IParameterService<TQueryParams> _parameterService;
    private readonly IApiClientService<TQueryParams, TDto> _apiClientService;
    private readonly IDataRepository<TDto> _repository;
    private readonly ILogger _logger;
    private readonly BatchSettings _batchSettings;

    public BatchService(
        IParameterService<TQueryParams> parameterService,
        IApiClientService<TQueryParams, TDto> apiClientService,
        IDataRepository<TDto> repository,
        ILogger logger,
        IOptions<BatchSettings> batchSettings)
    {
        _parameterService = parameterService;
        _apiClientService = apiClientService;
        _repository = repository;
        _logger = logger;
        _batchSettings = batchSettings.Value;
    }

    /// <inheritdoc/>
    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("========================================");
        _logger.LogInformation("バッチ処理を開始します: {BatchName}", _batchSettings.BatchName);
        _logger.LogInformation("========================================");

        try
        {
            // Step 1: パラメータ取得
            _logger.LogInformation("[Step 1/4] パラメータを取得します");
            var queryParameters = await _parameterService.GetApiQueryParametersAsync(cancellationToken);

            // Step 2: API全ページ取得
            _logger.LogInformation("[Step 2/4] APIからデータを取得します");
            var items = await _apiClientService.GetAllPagesAsync(queryParameters, cancellationToken);

            if (items.Count == 0)
            {
                _logger.LogWarning("取得データが0件のため、処理を終了します");
                return 0;
            }

            _logger.LogInformation("APIデータ取得完了: {Count}件", items.Count);

            // Step 3: MERGE実行
            _logger.LogInformation("[Step 3/4] データベースへMERGE処理を実行します");
            var mergedCount = await _repository.MergeAsync(items, cancellationToken);
            _logger.LogInformation("MERGE処理完了: {Count}件", mergedCount);

            // Step 4: ストアドプロシージャ実行
            _logger.LogInformation("[Step 4/4] ストアドプロシージャを実行します");
            await _repository.ExecutePostMergeProcedureAsync(cancellationToken);
            _logger.LogInformation("ストアドプロシージャ実行完了");

            stopwatch.Stop();
            _logger.LogInformation("========================================");
            _logger.LogInformation("バッチ処理が正常に完了しました");
            _logger.LogInformation("処理時間: {Elapsed}", stopwatch.Elapsed);
            _logger.LogInformation("========================================");

            return 0;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("バッチ処理がキャンセルされました: 処理時間={Elapsed}", stopwatch.Elapsed);
            return 2;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "バッチ処理でエラーが発生しました: 処理時間={Elapsed}", stopwatch.Elapsed);
            return 1;
        }
    }
}
