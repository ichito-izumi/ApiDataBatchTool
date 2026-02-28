using ApiDataBatchTool.Configuration;
using ApiDataBatchTool.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiDataBatchTool.Workers;

/// <summary>
/// バッチ処理ワーカー
/// BackgroundServiceとして1回実行して終了する
/// </summary>
public class BatchWorker : BackgroundService
{
    private readonly IBatchService _batchService;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger<BatchWorker> _logger;
    private readonly BatchSettings _batchSettings;

    private int _exitCode;

    public BatchWorker(
        IBatchService batchService,
        IHostApplicationLifetime applicationLifetime,
        ILogger<BatchWorker> logger,
        IOptions<BatchSettings> batchSettings)
    {
        _batchService = batchService;
        _applicationLifetime = applicationLifetime;
        _logger = logger;
        _batchSettings = batchSettings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BatchWorker を開始しました: {BatchName}", _batchSettings.BatchName);

        try
        {
            _exitCode = await _batchService.ExecuteAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "BatchWorker で予期しないエラーが発生しました");
            _exitCode = 99;
        }
        finally
        {
            _logger.LogInformation("BatchWorker を終了します: ExitCode={ExitCode}", _exitCode);

            // 終了コードを設定してアプリケーションを終了
            Environment.ExitCode = _exitCode;
            _applicationLifetime.StopApplication();
        }
    }
}
