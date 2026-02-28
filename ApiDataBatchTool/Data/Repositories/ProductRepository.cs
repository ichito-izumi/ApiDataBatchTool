using System.Diagnostics;
using ApiDataBatchTool.Configuration;
using ApiDataBatchTool.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;

namespace ApiDataBatchTool.Data.Repositories;

/// <summary>
/// 商品リポジトリ実装
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<ProductRepository> _logger;
    private readonly DatabaseSettings _dbSettings;
    private readonly BatchSettings _batchSettings;

    public ProductRepository(
        AppDbContext context,
        ILogger<ProductRepository> logger,
        IOptions<DatabaseSettings> dbSettings,
        IOptions<BatchSettings> batchSettings)
    {
        _context = context;
        _logger = logger;
        _dbSettings = dbSettings.Value;
        _batchSettings = batchSettings.Value;
    }

    /// <inheritdoc/>
    public async Task<int> MergeProductsAsync(IEnumerable<ProductDto> products, CancellationToken cancellationToken = default)
    {
        var productList = products.ToList();
        if (productList.Count == 0)
        {
            _logger.LogWarning("MERGEするデータが0件です");
            return 0;
        }

        _logger.LogInformation("MERGE処理を開始します: {Count}件", productList.Count);

        var totalProcessed = 0;
        var batches = productList.Chunk(_batchSettings.MergeBatchSize);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var batch in batches)
            {
                var processed = await MergeBatchAsync(batch, cancellationToken);
                totalProcessed += processed;
                _logger.LogDebug("バッチ処理完了: {Processed}/{Total}件", totalProcessed, productList.Count);
            }

            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("MERGE処理が完了しました: {Count}件", totalProcessed);
            return totalProcessed;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "MERGE処理でエラーが発生しました");
            throw;
        }
    }

    /// <summary>
    /// バッチ単位でMERGEを実行
    /// </summary>
    private async Task<int> MergeBatchAsync(ProductDto[] batch, CancellationToken cancellationToken)
    {
        // テーブル名は設定ファイルから取得
        var tableName = _dbSettings.TargetTableName;

        // TODO: 実際のテーブル構造に合わせてMERGE文を調整してください
        // 以下はサンプルのMERGE文です
        var mergeSql = $"""
            MERGE INTO {tableName} tgt
            USING (
                SELECT
                    :p_product_code AS PRODUCT_CODE,
                    :p_product_name AS PRODUCT_NAME,
                    :p_category_code AS CATEGORY_CODE,
                    :p_unit_price AS UNIT_PRICE,
                    :p_stock_quantity AS STOCK_QUANTITY,
                    :p_is_active AS IS_ACTIVE,
                    :p_api_updated_at AS API_UPDATED_AT
                FROM DUAL
            ) src
            ON (tgt.PRODUCT_CODE = src.PRODUCT_CODE)
            WHEN MATCHED THEN
                UPDATE SET
                    tgt.PRODUCT_NAME = src.PRODUCT_NAME,
                    tgt.CATEGORY_CODE = src.CATEGORY_CODE,
                    tgt.UNIT_PRICE = src.UNIT_PRICE,
                    tgt.STOCK_QUANTITY = src.STOCK_QUANTITY,
                    tgt.IS_ACTIVE = src.IS_ACTIVE,
                    tgt.API_UPDATED_AT = src.API_UPDATED_AT,
                    tgt.UPDATED_AT = SYSDATE
            WHEN NOT MATCHED THEN
                INSERT (
                    PRODUCT_CODE,
                    PRODUCT_NAME,
                    CATEGORY_CODE,
                    UNIT_PRICE,
                    STOCK_QUANTITY,
                    IS_ACTIVE,
                    API_UPDATED_AT,
                    CREATED_AT,
                    UPDATED_AT
                )
                VALUES (
                    src.PRODUCT_CODE,
                    src.PRODUCT_NAME,
                    src.CATEGORY_CODE,
                    src.UNIT_PRICE,
                    src.STOCK_QUANTITY,
                    src.IS_ACTIVE,
                    src.API_UPDATED_AT,
                    SYSDATE,
                    SYSDATE
                )
            """;

        var processedCount = 0;

        foreach (var product in batch)
        {
            var parameters = new[]
            {
                new OracleParameter("p_product_code", product.ProductCode),
                new OracleParameter("p_product_name", (object?)product.ProductName ?? DBNull.Value),
                new OracleParameter("p_category_code", (object?)product.CategoryCode ?? DBNull.Value),
                new OracleParameter("p_unit_price", (object?)product.UnitPrice ?? DBNull.Value),
                new OracleParameter("p_stock_quantity", (object?)product.StockQuantity ?? DBNull.Value),
                new OracleParameter("p_is_active", product.IsActive ? 1 : 0),
                new OracleParameter("p_api_updated_at", (object?)product.UpdatedAt ?? DBNull.Value)
            };

            await _context.Database.ExecuteSqlRawAsync(mergeSql, parameters, cancellationToken);
            processedCount++;
        }

        return processedCount;
    }

    /// <inheritdoc/>
    public async Task ExecutePostMergeProcedureAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ストアドプロシージャを実行します: {ProcedureName}", _dbSettings.PostMergeProcedureName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // TODO: 実際のプロシージャ名に置き換えてください
            // ODP.NET を使用してストアドプロシージャを実行
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = _dbSettings.PostMergeProcedureName;
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.CommandTimeout = _dbSettings.CommandTimeoutSeconds;

            await command.ExecuteNonQueryAsync(cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "ストアドプロシージャの実行が完了しました: 処理時間={Elapsed}ms",
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "ストアドプロシージャの実行でエラーが発生しました: {ProcedureName}, 処理時間={Elapsed}ms",
                _dbSettings.PostMergeProcedureName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
