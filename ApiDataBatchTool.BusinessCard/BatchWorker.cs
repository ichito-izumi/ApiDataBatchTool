using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ApiDataBatchTool.BusinessCard.Configuration;
using ApiDataBatchTool.BusinessCard.Models;
using ApiDataBatchTool.Common.Configuration;
using ApiDataBatchTool.Common.Data;
using ApiDataBatchTool.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;

namespace ApiDataBatchTool.BusinessCard;

/// <summary>
/// 名刺バッチワーカー
/// </summary>
public class BatchWorker : BackgroundService
{
    // 終了コード定義
    private const int ExitCodeSuccess = 0;
    private const int ExitCodeApiError = 1;
    private const int ExitCodeCancelled = 2;
    private const int ExitCodeDatabaseError = 3;
    private const int ExitCodeParameterError = 4;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppDbContext _dbContext;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger<BatchWorker> _logger;

    // 設定
    private readonly ApiSettings _apiSettings;
    private readonly DatabaseSettings _dbSettings;
    private readonly BatchSettings _batchSettings;
    private readonly CidSettings _cidSettings;
    private readonly MailNotificationSettings _mailNotificationSettings;
    private readonly ExecutionHistorySettings _executionHistorySettings;

    private int _exitCode;

    public BatchWorker(
        IHttpClientFactory httpClientFactory,
        AppDbContext dbContext,
        IHostApplicationLifetime applicationLifetime,
        ILogger<BatchWorker> logger,
        IOptions<ApiSettings> apiSettings,
        IOptions<DatabaseSettings> dbSettings,
        IOptions<BatchSettings> batchSettings,
        IOptions<CidSettings> cidSettings,
        IOptions<MailNotificationSettings> mailNotificationSettings,
        IOptions<ExecutionHistorySettings> executionHistorySettings)
    {
        _httpClientFactory = httpClientFactory;
        _dbContext = dbContext;
        _applicationLifetime = applicationLifetime;
        _logger = logger;
        _apiSettings = apiSettings.Value;
        _dbSettings = dbSettings.Value;
        _batchSettings = batchSettings.Value;
        _cidSettings = cidSettings.Value;
        _mailNotificationSettings = mailNotificationSettings.Value;
        _executionHistorySettings = executionHistorySettings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("========================================");
        _logger.LogInformation("BatchWorker を開始しました: {BatchName}", _batchSettings.BatchName);
        _logger.LogInformation("実行開始時刻: {StartTime}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        _logger.LogInformation("========================================");

        try
        {
            _exitCode = await RunBatchAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "BatchWorker で予期しないエラーが発生しました");
            _exitCode = 99;
        }
        finally
        {
            // 失敗時のメール通知処理
            await HandleFailureNotificationAsync(stoppingToken);

            _logger.LogInformation("BatchWorker を終了します: ExitCode={ExitCode}", _exitCode);
            Environment.ExitCode = _exitCode;
            _applicationLifetime.StopApplication();
        }
    }

    /// <summary>
    /// バッチ処理のメイン処理
    /// </summary>
    private async Task<int> RunBatchAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("バッチ処理を開始します: {BatchName}", _batchSettings.BatchName);

        var currentStep = 0;

        try
        {
            // ========================================
            // Step 1: パラメータ取得
            // ========================================
            currentStep = 1;
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogInformation("[Step 1/4] パラメータを取得します");

            var queryParams = await GetQueryParametersAsync(cancellationToken);

            // ========================================
            // Step 2: API全ページ取得
            // ========================================
            currentStep = 2;
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogInformation("[Step 2/4] APIからデータを取得します");

            var items = await GetAllPagesFromApiAsync(queryParams, cancellationToken);

            if (items.Count == 0)
            {
                _logger.LogWarning("取得データが0件のため、処理を終了します");
                return ExitCodeSuccess;
            }

            _logger.LogInformation("APIデータ取得完了: {Count}件", items.Count);

            // ========================================
            // Step 3: MERGE実行
            // ========================================
            currentStep = 3;
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogInformation("[Step 3/4] データベースへMERGE処理を実行します");

            var mergedCount = await MergeToDbAsync(items, cancellationToken);
            _logger.LogInformation("MERGE処理完了: {Count}件", mergedCount);

            // ========================================
            // Step 4: ストアドプロシージャ実行
            // ========================================
            currentStep = 4;
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogInformation("[Step 4/4] ストアドプロシージャを実行します");

            await ExecutePostMergeProcedureAsync(cancellationToken);
            _logger.LogInformation("ストアドプロシージャ実行完了");

            stopwatch.Stop();
            _logger.LogInformation("========================================");
            _logger.LogInformation("バッチ処理が正常に完了しました");
            _logger.LogInformation("処理時間: {Elapsed}", stopwatch.Elapsed);
            _logger.LogInformation("========================================");

            return ExitCodeSuccess;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("バッチ処理がキャンセルされました: 処理時間={Elapsed}", stopwatch.Elapsed);
            return ExitCodeCancelled;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            // Step 1: パラメータエラー, Step 2: APIエラー, Step 3-4: DBエラー
            var exitCode = currentStep switch
            {
                1 => ExitCodeParameterError,
                2 => ExitCodeApiError,
                _ => ExitCodeDatabaseError
            };
            _logger.LogError(
                ex,
                "バッチ処理でエラーが発生しました: Step={Step}, ExitCode={ExitCode}, 処理時間={Elapsed}",
                currentStep,
                exitCode,
                stopwatch.Elapsed);
            return exitCode;
        }
    }

    // ========================================
    // パラメータ取得処理
    // ========================================

    private async Task<BusinessCardQueryParameters> GetQueryParametersAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("バッチパラメータを取得します");

        // CIDを取得
        var cid = await GetCidAsync(cancellationToken);

        // 連携制御テーブルからカテゴリコードを取得
        var linkageControl = await _dbContext.LinkageControls
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        var categoryCode = linkageControl?.CategoryCode;

        var queryParams = new BusinessCardQueryParameters(
            Cid: cid,
            CategoryCode: categoryCode,
            IsOverseas: _apiSettings.IsOverseas
        );

        _logger.LogInformation(
            "バッチパラメータを取得しました: Cid={Cid}, CategoryCode={CategoryCode}, IsOverseas={IsOverseas}",
            queryParams.Cid,
            queryParams.CategoryCode ?? "(なし)",
            queryParams.IsOverseas);

        return queryParams;
    }

    private async Task<string> GetCidAsync(CancellationToken cancellationToken)
    {
        var batFilePath = ResolveBatFilePath(_cidSettings.BatFilePath);

        if (!File.Exists(batFilePath))
        {
            throw new FileNotFoundException($"CID取得用batファイルが見つかりません: {batFilePath}");
        }

        _logger.LogInformation(
            "CID取得batファイルを実行します: {BatFilePath}, タイムアウト={Timeout}秒",
            batFilePath,
            _cidSettings.TimeoutSeconds);

        var cmdArguments = string.IsNullOrEmpty(_cidSettings.BatArguments)
            ? $"/c \"{batFilePath}\""
            : $"/c \"{batFilePath}\" {_cidSettings.BatArguments}";

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = cmdArguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_cidSettings.TimeoutSeconds));

        try
        {
            var output = await process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
            var error = await process.StandardError.ReadToEndAsync(timeoutCts.Token);
            await process.WaitForExitAsync(timeoutCts.Token);

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"CID取得batファイルの実行に失敗しました: {error}");
            }

            var cid = output.Trim();
            if (string.IsNullOrEmpty(cid))
            {
                throw new InvalidOperationException("CID取得batファイルの出力が空です");
            }

            _logger.LogInformation("CID取得完了: CID={Cid}", cid);
            return cid;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            throw new TimeoutException($"CID取得batファイルの実行がタイムアウトしました（{_cidSettings.TimeoutSeconds}秒）");
        }
    }

    private static string ResolveBatFilePath(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }
        return Path.Combine(AppContext.BaseDirectory, configuredPath);
    }

    // ========================================
    // API呼び出し処理
    // ========================================

    private async Task<List<BusinessCardDto>> GetAllPagesFromApiAsync(
        BusinessCardQueryParameters queryParams,
        CancellationToken cancellationToken)
    {
        var allItems = new List<BusinessCardDto>();
        var currentPage = 1;

        _logger.LogInformation("API全ページ取得を開始します: Endpoint={Endpoint}", _apiSettings.BaseUrl);

        while (true)
        {
            var pageItems = await GetPageFromApiAsync(currentPage, queryParams, cancellationToken);

            allItems.AddRange(pageItems);

            _logger.LogInformation(
                "ページ取得完了: Page={Page}, 取得件数={Count}, 累計件数={Total}",
                currentPage,
                pageItems.Count,
                allItems.Count);

            // 取得件数がPageSize未満なら最終ページ
            if (pageItems.Count < _apiSettings.PageSize)
            {
                break;
            }

            currentPage++;
        }

        _logger.LogInformation("API全ページ取得が完了しました: 総件数={Total}", allItems.Count);
        return allItems;
    }

    private async Task<List<BusinessCardDto>> GetPageFromApiAsync(
        int page,
        BusinessCardQueryParameters queryParams,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(_apiSettings.HttpClientName);

        var url = $"{_apiSettings.BaseUrl}?page={page}&pageSize={_apiSettings.PageSize}{queryParams.ToQueryString()}";

        _logger.LogDebug("APIリクエスト: {Url}", url);

        var stopwatch = Stopwatch.StartNew();

        var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var apiResponse = await response.Content.ReadFromJsonAsync<BusinessCardApiResponse>(cancellationToken: cancellationToken);

        stopwatch.Stop();
        _logger.LogInformation(
            "APIリクエスト完了: Page={Page}, 処理時間={Elapsed}ms",
            page,
            stopwatch.ElapsedMilliseconds);

        return apiResponse?.SafeItems ?? [];
    }

    // ========================================
    // データベース処理
    // ========================================

    private async Task<int> MergeToDbAsync(List<BusinessCardDto> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            _logger.LogWarning("MERGEするデータが0件です");
            return 0;
        }

        var stopwatch = Stopwatch.StartNew();
        var totalMerged = 0;
        var batchSize = _batchSettings.MergeBatchSize;
        var tableName = _dbSettings.TargetTableName;

        _logger.LogInformation("MERGE処理開始: 総件数={TotalCount}, バッチサイズ={BatchSize}", items.Count, batchSize);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            for (var i = 0; i < items.Count; i += batchSize)
            {
                var batch = items.Skip(i).Take(batchSize).ToList();
                var batchNumber = (i / batchSize) + 1;

                var mergedCount = await ExecuteMergeBatchAsync(batch, tableName, cancellationToken);
                totalMerged += mergedCount;

                _logger.LogDebug("バッチ {BatchNumber} 完了: {Count}件", batchNumber, mergedCount);
            }

            await transaction.CommitAsync(cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "MERGE処理完了: 総件数={TotalMerged}, 処理時間={Elapsed}",
                totalMerged,
                stopwatch.Elapsed);

            return totalMerged;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "MERGE処理でエラーが発生しました");
            throw;
        }
    }

    private async Task<int> ExecuteMergeBatchAsync(
        List<BusinessCardDto> batch,
        string tableName,
        CancellationToken cancellationToken)
    {
        var sql = new StringBuilder();
        sql.AppendLine($"MERGE INTO {tableName} t");
        sql.AppendLine("USING (");

        var parameters = new List<OracleParameter>();
        var unions = new List<string>();

        for (var i = 0; i < batch.Count; i++)
        {
            var item = batch[i];
            unions.Add($@"
                SELECT
                    :p_card_id_{i} AS CARD_ID,
                    :p_person_name_{i} AS PERSON_NAME,
                    :p_company_name_{i} AS COMPANY_NAME,
                    :p_department_{i} AS DEPARTMENT,
                    :p_position_{i} AS POSITION,
                    :p_email_{i} AS EMAIL,
                    :p_phone_{i} AS PHONE,
                    :p_is_overseas_{i} AS IS_OVERSEAS,
                    :p_api_updated_at_{i} AS API_UPDATED_AT
                FROM DUAL");

            parameters.Add(new OracleParameter($":p_card_id_{i}", item.CardId));
            parameters.Add(new OracleParameter($":p_person_name_{i}", (object?)item.PersonName ?? DBNull.Value));
            parameters.Add(new OracleParameter($":p_company_name_{i}", (object?)item.CompanyName ?? DBNull.Value));
            parameters.Add(new OracleParameter($":p_department_{i}", (object?)item.Department ?? DBNull.Value));
            parameters.Add(new OracleParameter($":p_position_{i}", (object?)item.Position ?? DBNull.Value));
            parameters.Add(new OracleParameter($":p_email_{i}", (object?)item.Email ?? DBNull.Value));
            parameters.Add(new OracleParameter($":p_phone_{i}", (object?)item.Phone ?? DBNull.Value));
            parameters.Add(new OracleParameter($":p_is_overseas_{i}", item.IsOverseas ? 1 : 0));
            parameters.Add(new OracleParameter($":p_api_updated_at_{i}", (object?)item.UpdatedAt ?? DBNull.Value));
        }

        sql.AppendLine(string.Join(" UNION ALL ", unions));
        sql.AppendLine(") s");
        sql.AppendLine("ON (t.CARD_ID = s.CARD_ID)");
        sql.AppendLine("WHEN MATCHED THEN UPDATE SET");
        sql.AppendLine("    t.PERSON_NAME = s.PERSON_NAME,");
        sql.AppendLine("    t.COMPANY_NAME = s.COMPANY_NAME,");
        sql.AppendLine("    t.DEPARTMENT = s.DEPARTMENT,");
        sql.AppendLine("    t.POSITION = s.POSITION,");
        sql.AppendLine("    t.EMAIL = s.EMAIL,");
        sql.AppendLine("    t.PHONE = s.PHONE,");
        sql.AppendLine("    t.IS_OVERSEAS = s.IS_OVERSEAS,");
        sql.AppendLine("    t.API_UPDATED_AT = s.API_UPDATED_AT,");
        sql.AppendLine("    t.UPDATED_AT = SYSDATE");
        sql.AppendLine("WHEN NOT MATCHED THEN INSERT (");
        sql.AppendLine("    CARD_ID, PERSON_NAME, COMPANY_NAME, DEPARTMENT, POSITION,");
        sql.AppendLine("    EMAIL, PHONE, IS_OVERSEAS, API_UPDATED_AT, CREATED_AT, UPDATED_AT");
        sql.AppendLine(") VALUES (");
        sql.AppendLine("    s.CARD_ID, s.PERSON_NAME, s.COMPANY_NAME, s.DEPARTMENT, s.POSITION,");
        sql.AppendLine("    s.EMAIL, s.PHONE, s.IS_OVERSEAS, s.API_UPDATED_AT, SYSDATE, SYSDATE");
        sql.AppendLine(")");

        var result = await _dbContext.Database.ExecuteSqlRawAsync(
            sql.ToString(),
            parameters,
            cancellationToken);

        return result;
    }

    private async Task ExecutePostMergeProcedureAsync(CancellationToken cancellationToken)
    {
        var procedureName = _dbSettings.PostMergeProcedureName;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("ストアドプロシージャ実行開始: {ProcedureName}", procedureName);

        var connection = _dbContext.Database.GetDbConnection();

        var needsClose = connection.State != System.Data.ConnectionState.Open;
        if (needsClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = procedureName;
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.CommandTimeout = _dbSettings.CommandTimeoutSeconds;

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            if (needsClose)
            {
                await connection.CloseAsync();
            }
        }

        stopwatch.Stop();
        _logger.LogInformation(
            "ストアドプロシージャ実行完了: {ProcedureName}, 処理時間={Elapsed}",
            procedureName,
            stopwatch.Elapsed);
    }

    // ========================================
    // 失敗通知処理
    // ========================================

    private async Task HandleFailureNotificationAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_exitCode == ExitCodeSuccess)
            {
                // 成功時は連続失敗カウントをリセット
                await RecordExecutionResultAsync(isSuccess: true, cancellationToken);
                return;
            }

            if (!_mailNotificationSettings.Enabled)
            {
                return;
            }

            // APIエラーの場合のみ連続失敗をカウント
            var consecutiveApiFailureCount = 0;
            if (_exitCode == ExitCodeApiError)
            {
                consecutiveApiFailureCount = await RecordExecutionResultAsync(isSuccess: false, cancellationToken);
            }

            // 開発者向け通知: 全てのエラーで送信
            SendMailToDevelopment(consecutiveApiFailureCount);

            // 運用SE向け通知: APIエラーが連続2回以上の場合のみ送信
            if (_exitCode == ExitCodeApiError &&
                consecutiveApiFailureCount >= _executionHistorySettings.ConsecutiveFailureThreshold)
            {
                SendMailToOperation(consecutiveApiFailureCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "失敗通知処理でエラーが発生しました");
        }
    }

    private void SendMailToDevelopment(int consecutiveApiFailureCount)
    {
        var settings = _mailNotificationSettings.Development;
        var body = BuildMailBody(settings.BodyTemplate, consecutiveApiFailureCount);

        MailSender.Send(settings.Recipient, settings.Subject, body);

        _logger.LogWarning(
            "開発者向けエラー通知メールを送信しました: ExitCode={ExitCode}",
            _exitCode);
    }

    private void SendMailToOperation(int consecutiveApiFailureCount)
    {
        var settings = _mailNotificationSettings.Operation;
        var body = BuildMailBody(settings.BodyTemplate, consecutiveApiFailureCount);

        MailSender.Send(settings.Recipient, settings.Subject, body);

        _logger.LogWarning(
            "運用SE向けエラー通知メールを送信しました: ExitCode={ExitCode}, 連続失敗回数={FailureCount}",
            _exitCode,
            consecutiveApiFailureCount);
    }

    private string BuildMailBody(string template, int consecutiveFailureCount)
    {
        return template
            .Replace("{BatchName}", _batchSettings.BatchName)
            .Replace("{ExitCode}", _exitCode.ToString())
            .Replace("{FailureCount}", consecutiveFailureCount.ToString())
            .Replace("{DateTime}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    private async Task<int> RecordExecutionResultAsync(bool isSuccess, CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, _executionHistorySettings.FilePath);
        var history = await LoadExecutionHistoryAsync(filePath, cancellationToken);

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

        await SaveExecutionHistoryAsync(filePath, history, cancellationToken);

        _logger.LogInformation(
            "実行履歴を更新しました: 結果={Result}, 連続失敗回数={FailureCount}",
            history.LastResult,
            history.ConsecutiveFailureCount);

        return history.ConsecutiveFailureCount;
    }

    private async Task<ExecutionHistory> LoadExecutionHistoryAsync(string filePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            return new ExecutionHistory();
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var history = JsonSerializer.Deserialize<ExecutionHistory>(json);
            return history ?? new ExecutionHistory();
        }
        catch (JsonException)
        {
            _logger.LogWarning("実行履歴ファイルの読み込みに失敗しました。新規作成します");
            return new ExecutionHistory();
        }
    }

    private async Task SaveExecutionHistoryAsync(string filePath, ExecutionHistory history, CancellationToken cancellationToken)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(history, options);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }

    // ========================================
    // 内部クラス
    // ========================================

    private class ExecutionHistory
    {
        public DateTime? LastExecutionTime { get; set; }
        public string? LastResult { get; set; }
        public int ConsecutiveFailureCount { get; set; }
    }
}
