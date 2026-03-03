using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ApiDataBatchTool.Common.Configuration;
using ApiDataBatchTool.Common.Data;
using ApiDataBatchTool.Mail;
using ApiDataBatchTool.Office.Configuration;
using ApiDataBatchTool.Office.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;

namespace ApiDataBatchTool.Office;

/// <summary>
/// 事業所バッチワーカー
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
    private readonly MailSettings _mailSettings;

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
        IOptions<MailSettings> mailSettings)
    {
        _httpClientFactory = httpClientFactory;
        _dbContext = dbContext;
        _applicationLifetime = applicationLifetime;
        _logger = logger;
        _apiSettings = apiSettings.Value;
        _dbSettings = dbSettings.Value;
        _batchSettings = batchSettings.Value;
        _cidSettings = cidSettings.Value;
        _mailSettings = mailSettings.Value;
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
            HandleFailureNotification();

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

    private async Task<OfficeQueryParameters> GetQueryParametersAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("バッチパラメータを取得します");

        // CIDを取得
        var cid = await GetCidAsync(cancellationToken);

        // 連携制御テーブルからカテゴリコードを取得
        var linkageControl = await _dbContext.LinkageControls
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        var categoryCode = linkageControl?.CategoryCode
            ?? throw new InvalidOperationException("連携制御テーブルからカテゴリコードを取得できませんでした");

        var queryParams = new OfficeQueryParameters(
            Cid: cid,
            CategoryCode: categoryCode
        );

        _logger.LogInformation(
            "バッチパラメータを取得しました: Cid={Cid}, CategoryCode={CategoryCode}",
            queryParams.Cid,
            queryParams.CategoryCode);

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

    private async Task<List<OfficeDto>> GetAllPagesFromApiAsync(
        OfficeQueryParameters queryParams,
        CancellationToken cancellationToken)
    {
        var allItems = new List<OfficeDto>();
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

    private async Task<List<OfficeDto>> GetPageFromApiAsync(
        int page,
        OfficeQueryParameters queryParams,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(_apiSettings.HttpClientName);

        var url = $"{_apiSettings.BaseUrl}?page={page}&pageSize={_apiSettings.PageSize}{queryParams.ToQueryString()}";

        _logger.LogDebug("APIリクエスト: {Url}", url);

        var stopwatch = Stopwatch.StartNew();

        var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var apiResponse = await response.Content.ReadFromJsonAsync<OfficeApiResponse>(cancellationToken: cancellationToken);

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

    private async Task<int> MergeToDbAsync(List<OfficeDto> items, CancellationToken cancellationToken)
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
        List<OfficeDto> batch,
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
                    :p_office_code_{i} AS OFFICE_CODE,
                    :p_office_name_{i} AS OFFICE_NAME,
                    :p_office_name_kana_{i} AS OFFICE_NAME_KANA,
                    :p_postal_code_{i} AS POSTAL_CODE,
                    :p_address_{i} AS ADDRESS,
                    :p_phone_number_{i} AS PHONE_NUMBER,
                    :p_fax_number_{i} AS FAX_NUMBER,
                    :p_established_date_{i} AS ESTABLISHED_DATE,
                    :p_closed_date_{i} AS CLOSED_DATE,
                    :p_is_active_{i} AS IS_ACTIVE,
                    :p_updated_at_{i} AS UPDATED_AT
                FROM DUAL");

            parameters.Add(new OracleParameter($":p_office_code_{i}", item.OfficeCode));
            parameters.Add(new OracleParameter($":p_office_name_{i}", item.OfficeName));
            parameters.Add(new OracleParameter($":p_office_name_kana_{i}", (object?)item.OfficeNameKana ?? DBNull.Value));
            parameters.Add(new OracleParameter($":p_postal_code_{i}", (object?)item.PostalCode ?? DBNull.Value));
            parameters.Add(new OracleParameter($":p_address_{i}", (object?)item.Address ?? DBNull.Value));
            parameters.Add(new OracleParameter($":p_phone_number_{i}", (object?)item.PhoneNumber ?? DBNull.Value));
            parameters.Add(new OracleParameter($":p_fax_number_{i}", (object?)item.FaxNumber ?? DBNull.Value));
            parameters.Add(new OracleParameter($":p_established_date_{i}", (object?)item.EstablishedDate ?? DBNull.Value));
            parameters.Add(new OracleParameter($":p_closed_date_{i}", (object?)item.ClosedDate ?? DBNull.Value));
            parameters.Add(new OracleParameter($":p_is_active_{i}", item.IsActive ? 1 : 0));
            parameters.Add(new OracleParameter($":p_updated_at_{i}", item.UpdatedAt));
        }

        sql.AppendLine(string.Join(" UNION ALL ", unions));
        sql.AppendLine(") s");
        sql.AppendLine("ON (t.OFFICE_CODE = s.OFFICE_CODE)");
        sql.AppendLine("WHEN MATCHED THEN UPDATE SET");
        sql.AppendLine("    t.OFFICE_NAME = s.OFFICE_NAME,");
        sql.AppendLine("    t.OFFICE_NAME_KANA = s.OFFICE_NAME_KANA,");
        sql.AppendLine("    t.POSTAL_CODE = s.POSTAL_CODE,");
        sql.AppendLine("    t.ADDRESS = s.ADDRESS,");
        sql.AppendLine("    t.PHONE_NUMBER = s.PHONE_NUMBER,");
        sql.AppendLine("    t.FAX_NUMBER = s.FAX_NUMBER,");
        sql.AppendLine("    t.ESTABLISHED_DATE = s.ESTABLISHED_DATE,");
        sql.AppendLine("    t.CLOSED_DATE = s.CLOSED_DATE,");
        sql.AppendLine("    t.IS_ACTIVE = s.IS_ACTIVE,");
        sql.AppendLine("    t.UPDATED_AT = s.UPDATED_AT,");
        sql.AppendLine("    t.SYS_UPDATED_AT = SYSDATE");
        sql.AppendLine("WHEN NOT MATCHED THEN INSERT (");
        sql.AppendLine("    OFFICE_CODE, OFFICE_NAME, OFFICE_NAME_KANA, POSTAL_CODE, ADDRESS,");
        sql.AppendLine("    PHONE_NUMBER, FAX_NUMBER, ESTABLISHED_DATE, CLOSED_DATE,");
        sql.AppendLine("    IS_ACTIVE, UPDATED_AT, CREATED_AT, SYS_UPDATED_AT");
        sql.AppendLine(") VALUES (");
        sql.AppendLine("    s.OFFICE_CODE, s.OFFICE_NAME, s.OFFICE_NAME_KANA, s.POSTAL_CODE, s.ADDRESS,");
        sql.AppendLine("    s.PHONE_NUMBER, s.FAX_NUMBER, s.ESTABLISHED_DATE, s.CLOSED_DATE,");
        sql.AppendLine("    s.IS_ACTIVE, s.UPDATED_AT, SYSDATE, SYSDATE");
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

    private void HandleFailureNotification()
    {
        try
        {
            // 事業所は1回失敗で即メール送信
            if (_exitCode != ExitCodeSuccess && _mailSettings.Enabled)
            {
                var body = BuildFailureMailBody();

                // モックDLLのメール送信を呼び出し
                MailSender.Send(_mailSettings.Recipient, _mailSettings.Subject, body);

                _logger.LogWarning("失敗が発生したためメールを送信しました: ExitCode={ExitCode}", _exitCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "失敗通知処理でエラーが発生しました");
        }
    }

    private string BuildFailureMailBody()
    {
        return $@"バッチ処理が失敗しました。

バッチ名: {_batchSettings.BatchName}
終了コード: {_exitCode}
実行日時: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

詳細はログを確認してください。";
    }
}
