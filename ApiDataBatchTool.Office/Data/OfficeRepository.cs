using System.Text;
using ApiDataBatchTool.Common.Configuration;
using ApiDataBatchTool.Common.Data;
using ApiDataBatchTool.Common.Services;
using ApiDataBatchTool.Office.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;

namespace ApiDataBatchTool.Office.Data;

/// <summary>
/// 事業所リポジトリ
/// </summary>
public class OfficeRepository : DataRepositoryBase<OfficeDto>
{
    public OfficeRepository(
        AppDbContext context,
        ILogger<OfficeRepository> logger,
        IOptions<DatabaseSettings> dbSettings,
        IOptions<BatchSettings> batchSettings)
        : base(context, logger, dbSettings.Value, batchSettings.Value)
    {
    }

    /// <inheritdoc/>
    public override async Task<int> MergeAsync(IEnumerable<OfficeDto> items, CancellationToken cancellationToken = default)
    {
        var itemList = items as IList<OfficeDto> ?? items.ToList();
        return await ExecuteMergeWithTransactionAsync(itemList, ExecuteMergeBatchAsync, cancellationToken);
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

        var result = await Context.Database.ExecuteSqlRawAsync(
            sql.ToString(),
            parameters,
            cancellationToken);

        return result;
    }
}
