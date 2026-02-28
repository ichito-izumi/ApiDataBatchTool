using System.Diagnostics;
using ApiDataBatchTool.Configuration;
using ApiDataBatchTool.Data.Repositories;
using ApiDataBatchTool.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiDataBatchTool.Services;

/// <summary>
/// バッチ処理サービス実装
/// </summary>
public class BatchService : IBatchService
{
    private readonly IParameterService _parameterService;
    private readonly IApiClientService _apiClientService;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<BatchService> _logger;
    private readonly BatchSettings _batchSettings;

    public BatchService(
        IParameterService parameterService,
        IApiClientService apiClientService,
        IProductRepository productRepository,
        ILogger<BatchService> logger,
        IOptions<BatchSettings> batchSettings)
    {
        _parameterService = parameterService;
        _apiClientService = apiClientService;
        _productRepository = productRepository;
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
            var products = await _apiClientService.GetAllPagesAsync<ProductDto>(queryParameters, cancellationToken);

            if (products.Count == 0)
            {
                _logger.LogWarning("取得データが0件のため、処理を終了します");
                return 0;
            }

            _logger.LogInformation("APIデータ取得完了: {Count}件", products.Count);

            // Step 3: MERGE実行
            _logger.LogInformation("[Step 3/4] データベースへMERGE処理を実行します");
            var mergedCount = await _productRepository.MergeProductsAsync(products, cancellationToken);
            _logger.LogInformation("MERGE処理完了: {Count}件", mergedCount);

            // Step 4: ストアドプロシージャ実行
            _logger.LogInformation("[Step 4/4] ストアドプロシージャを実行します");
            await _productRepository.ExecutePostMergeProcedureAsync(cancellationToken);
            _logger.LogInformation("ストアドプロシージャ実行完了");

            stopwatch.Stop();
            _logger.LogInformation("========================================");
            _logger.LogInformation("バッチ処理が正常に完了しました");
            _logger.LogInformation("処理時間: {Elapsed}", stopwatch.Elapsed);
            _logger.LogInformation("========================================");

            return 0; // 成功
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("バッチ処理がキャンセルされました: 処理時間={Elapsed}", stopwatch.Elapsed);
            return 2; // キャンセル
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "バッチ処理でエラーが発生しました: 処理時間={Elapsed}", stopwatch.Elapsed);
            return 1; // エラー
        }
    }
}
