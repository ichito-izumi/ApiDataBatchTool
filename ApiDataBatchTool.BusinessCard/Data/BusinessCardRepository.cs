using System.Text;
using ApiDataBatchTool.BusinessCard.Models;
using ApiDataBatchTool.Common.Configuration;
using ApiDataBatchTool.Common.Data;
using ApiDataBatchTool.Common.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;

namespace ApiDataBatchTool.BusinessCard.Data;

/// <summary>
/// 名刺リポジトリ
/// </summary>
public class BusinessCardRepository : DataRepositoryBase<BusinessCardDto>
{
    public BusinessCardRepository(
        AppDbContext context,
        ILogger<BusinessCardRepository> logger,
        IOptions<DatabaseSettings> dbSettings,
        IOptions<BatchSettings> batchSettings)
        : base(context, logger, dbSettings.Value, batchSettings.Value)
    {
    }

    /// <inheritdoc/>
    public override async Task<int> MergeAsync(IEnumerable<BusinessCardDto> items, CancellationToken cancellationToken = default)
    {
        var itemList = items as IList<BusinessCardDto> ?? items.ToList();
        return await ExecuteMergeWithTransactionAsync(itemList, ExecuteMergeBatchAsync, cancellationToken);
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

        var result = await Context.Database.ExecuteSqlRawAsync(
            sql.ToString(),
            parameters,
            cancellationToken);

        return result;
    }
}
