using ApiDataBatchTool.Common.Models;

namespace ApiDataBatchTool.BusinessCard.Models;

/// <summary>
/// 名刺API用クエリパラメータ
/// </summary>
public record BusinessCardQueryParameters(
    string Cid,
    string? CategoryCode = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    bool IsOverseas = false
) : ApiQueryParametersBase(Cid, CategoryCode, FromDate, ToDate)
{
    /// <inheritdoc/>
    protected override List<string> GetQueryParameters()
    {
        var parameters = base.GetQueryParameters();
        parameters.Add($"isOverseas={(IsOverseas ? 1 : 0)}");
        return parameters;
    }
}
