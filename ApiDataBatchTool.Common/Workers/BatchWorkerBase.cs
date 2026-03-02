using System;
using System.Threading;
using System.Threading.Tasks;
using ApiDataBatchTool.Common.Configuration;
using ApiDataBatchTool.Common.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiDataBatchTool.Common.Workers;

/// <summary>
/// バッチ処理ワーカー
/// </summary>
public class BatchWorkerBase : BackgroundService
{
    private readonly IBatchService _batchService;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger<BatchWorkerBase> _logger;
    private readonly BatchSettings _batchSettings;

    // オプション機能: 失敗通知
    private readonly IExecutionHistoryService? _executionHistoryService;
    private readonly IMailService? _mailService;
    private readonly MailSettings? _mailSettings;
    private readonly ExecutionHistorySettings? _executionHistorySettings;

    private int _exitCode;

    public BatchWorkerBase(
        IBatchService batchService,
        IHostApplicationLifetime applicationLifetime,
        ILogger<BatchWorkerBase> logger,
        IOptions<BatchSettings> batchSettings,
        IExecutionHistoryService? executionHistoryService = null,
        IMailService? mailService = null,
        IOptions<MailSettings>? mailSettings = null,
        IOptions<ExecutionHistorySettings>? executionHistorySettings = null)
    {
        _batchService = batchService;
        _applicationLifetime = applicationLifetime;
        _logger = logger;
        _batchSettings = batchSettings.Value;
        _executionHistoryService = executionHistoryService;
        _mailService = mailService;
        _mailSettings = mailSettings?.Value;
        _executionHistorySettings = executionHistorySettings?.Value;
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
            // 失敗通知機能（オプション）
            await ProcessFailureNotificationAsync(stoppingToken);

            _logger.LogInformation("BatchWorker を終了します: ExitCode={ExitCode}", _exitCode);

            Environment.ExitCode = _exitCode;
            _applicationLifetime.StopApplication();
        }
    }

    /// <summary>
    /// 失敗通知処理（オプション機能）
    /// </summary>
    private async Task ProcessFailureNotificationAsync(CancellationToken cancellationToken)
    {
        // 必要なサービスが登録されていない場合はスキップ
        if (_executionHistoryService == null ||
            _mailService == null ||
            _mailSettings == null ||
            _executionHistorySettings == null)
        {
            return;
        }

        try
        {
            var isSuccess = _exitCode == 0;
            var consecutiveFailureCount = await _executionHistoryService.RecordExecutionResultAsync(isSuccess, cancellationToken);

            if (!isSuccess && _mailSettings.Enabled)
            {
                // 即時通知が必要な終了コードかどうか判定
                var isImmediateNotification = Array.Exists(
                    _executionHistorySettings.ImmediateNotificationExitCodes,
                    code => code == _exitCode);

                // 即時通知コード、または連続失敗回数が閾値に達した場合にメール送信
                if (isImmediateNotification ||
                    consecutiveFailureCount >= _executionHistorySettings.ConsecutiveFailureThreshold)
                {
                    var body = BuildFailureMailBody(consecutiveFailureCount, isImmediateNotification);
                    await _mailService.SendAsync(
                        _mailSettings.Recipient,
                        _mailSettings.Subject,
                        body,
                        cancellationToken);

                    if (isImmediateNotification)
                    {
                        _logger.LogWarning(
                            "即時通知が必要なエラーが発生したためメールを送信しました: ExitCode={ExitCode}",
                            _exitCode);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "連続失敗回数が閾値に達したためメールを送信しました: 連続失敗回数={FailureCount}, 閾値={Threshold}",
                            consecutiveFailureCount,
                            _executionHistorySettings.ConsecutiveFailureThreshold);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "失敗通知処理でエラーが発生しました");
        }
    }

    /// <summary>
    /// 失敗通知メールの本文を生成する
    /// </summary>
    private string BuildFailureMailBody(int consecutiveFailureCount, bool isImmediateNotification)
    {
        var reason = isImmediateNotification
            ? "重大なエラーが発生しました。"
            : "バッチ処理が連続で失敗しました。";

        return $@"{reason}

バッチ名: {_batchSettings.BatchName}
連続失敗回数: {consecutiveFailureCount}
終了コード: {_exitCode}
実行日時: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

詳細はログを確認してください。";
    }
}
