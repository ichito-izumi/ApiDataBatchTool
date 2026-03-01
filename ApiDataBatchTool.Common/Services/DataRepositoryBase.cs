using System.Diagnostics;
using ApiDataBatchTool.Common.Configuration;
using ApiDataBatchTool.Common.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ApiDataBatchTool.Common.Services;

/// <summary>
/// データリポジトリ基底クラス
/// </summary>
public abstract class DataRepositoryBase<TDto> : IDataRepository<TDto>
{
    protected readonly AppDbContext Context;
    protected readonly ILogger Logger;
    protected readonly DatabaseSettings DbSettings;
    protected readonly BatchSettings BatchSettings;

    protected DataRepositoryBase(
        AppDbContext context,
        ILogger logger,
        DatabaseSettings dbSettings,
        BatchSettings batchSettings)
    {
        Context = context;
        Logger = logger;
        DbSettings = dbSettings;
        BatchSettings = batchSettings;
    }

    /// <inheritdoc/>
    public abstract Task<int> MergeAsync(IEnumerable<TDto> items, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public async Task ExecutePostMergeProcedureAsync(CancellationToken cancellationToken = default)
    {
        var procedureName = DbSettings.PostMergeProcedureName;
        var stopwatch = Stopwatch.StartNew();

        Logger.LogInformation("ストアドプロシージャ実行開始: {ProcedureName}", procedureName);

        try
        {
            var connection = Context.Database.GetDbConnection();

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
                command.CommandTimeout = DbSettings.CommandTimeoutSeconds;

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
            Logger.LogInformation(
                "ストアドプロシージャ実行完了: {ProcedureName}, 処理時間={Elapsed}",
                procedureName,
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex,
                "ストアドプロシージャの実行でエラーが発生しました: {ProcedureName}, 処理時間={Elapsed}ms",
                procedureName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// トランザクション管理付きのバッチMERGE処理を実行
    /// </summary>
    protected async Task<int> ExecuteMergeWithTransactionAsync(
        IList<TDto> items,
        Func<List<TDto>, string, CancellationToken, Task<int>> executeBatchFunc,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            Logger.LogWarning("MERGEするデータが0件です");
            return 0;
        }

        var stopwatch = Stopwatch.StartNew();
        var totalMerged = 0;
        var batchSize = BatchSettings.MergeBatchSize;
        var tableName = DbSettings.TargetTableName;

        Logger.LogInformation("MERGE処理開始: 総件数={TotalCount}, バッチサイズ={BatchSize}", items.Count, batchSize);

        await using var transaction = await Context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            for (var i = 0; i < items.Count; i += batchSize)
            {
                var batch = items.Skip(i).Take(batchSize).ToList();
                var batchNumber = (i / batchSize) + 1;
                var batchStopwatch = Stopwatch.StartNew();

                var mergedCount = await executeBatchFunc(batch, tableName, cancellationToken);
                totalMerged += mergedCount;

                batchStopwatch.Stop();
                Logger.LogDebug(
                    "バッチ {BatchNumber} 完了: {Count}件, 処理時間={Elapsed}ms",
                    batchNumber,
                    mergedCount,
                    batchStopwatch.ElapsedMilliseconds);
            }

            await transaction.CommitAsync(cancellationToken);

            stopwatch.Stop();
            Logger.LogInformation(
                "MERGE処理完了: 総件数={TotalMerged}, 処理時間={Elapsed}",
                totalMerged,
                stopwatch.Elapsed);

            return totalMerged;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Logger.LogError(ex, "MERGE処理でエラーが発生しました");
            throw;
        }
    }
}
