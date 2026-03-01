using ApiDataBatchTool.Common.Models;

namespace ApiDataBatchTool.Office.Models;

/// <summary>
/// 事業所APIクエリパラメータ
/// </summary>
public record OfficeQueryParameters(
    string Cid,
    string CategoryCode,
    DateTime? FromDate = null,
    DateTime? ToDate = null
) : ApiQueryParametersBase(Cid, CategoryCode, FromDate, ToDate);
