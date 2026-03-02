using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ApiDataBatchTool.Common.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiDataBatchTool.Common.Services;

/// <summary>
/// 実行履歴サービス実装
/// </summary>
public class ExecutionHistoryService : IExecutionHistoryService
{
    private readonly ILogger<ExecutionHistoryService> _logger;
    private readonly ExecutionHistorySettings _settings;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _filePath;

    public ExecutionHistoryService(
        ILogger<ExecutionHistoryService> logger,
        IOptions<ExecutionHistorySettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        // exeと同じディレクトリにファイルを配置
        _filePath = Path.Combine(AppContext.BaseDirectory, _settings.FilePath);
    }

    /// <inheritdoc />
    public async Task<int> GetConsecutiveFailureCountAsync(CancellationToken cancellationToken = default)
    {
        var history = await LoadHistoryAsync(cancellationToken);
        return history.ConsecutiveFailureCount;
    }

    /// <inheritdoc />
    public async Task<int> RecordExecutionResultAsync(bool isSuccess, CancellationToken cancellationToken = default)
    {
        var history = await LoadHistoryAsync(cancellationToken);

        history.LastExecutionTime = DateTime.Now;
        history.LastResult = isSuccess ? "Success" : "Failed";

        if (isSuccess)
        {
            history.ConsecutiveFailureCount = 0;
        }
        else
        {
            history.ConsecutiveFailureCount++;
        }

        await SaveHistoryAsync(history, cancellationToken);

        _logger.LogInformation(
            "実行履歴を更新しました: 結果={Result}, 連続失敗回数={FailureCount}",
            history.LastResult,
            history.ConsecutiveFailureCount);

        return history.ConsecutiveFailureCount;
    }

    private async Task<ExecutionHistory> LoadHistoryAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            _logger.LogDebug("実行履歴ファイルが存在しません: {FilePath}", _filePath);
            return new ExecutionHistory();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_filePath, cancellationToken);
            var history = JsonSerializer.Deserialize<ExecutionHistory>(json);
            return history ?? new ExecutionHistory();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "実行履歴ファイルの読み込みに失敗しました。新規作成します: {FilePath}", _filePath);
            return new ExecutionHistory();
        }
    }

    private async Task SaveHistoryAsync(ExecutionHistory history, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(history, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json, cancellationToken);
    }

    /// <summary>
    /// 実行履歴データ
    /// </summary>
    private class ExecutionHistory
    {
        public DateTime? LastExecutionTime { get; set; }
        public string? LastResult { get; set; }
        public int ConsecutiveFailureCount { get; set; }
    }
}
