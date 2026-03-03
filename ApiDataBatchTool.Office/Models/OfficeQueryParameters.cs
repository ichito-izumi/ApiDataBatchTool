using System;
using System.Collections.Generic;

namespace ApiDataBatchTool.Office.Models;

/// <summary>
/// 事業所API用クエリパラメータ
/// </summary>
public record OfficeQueryParameters(
    string Cid,
    string CategoryCode,
    DateTime? FromDate = null,
    DateTime? ToDate = null
)
{
    /// <summary>
    /// クエリ文字列を生成
    /// </summary>
    public string ToQueryString()
    {
        var parameters = new List<string>();

        if (!string.IsNullOrEmpty(Cid))
            parameters.Add($"cid={Uri.EscapeDataString(Cid)}");

        if (!string.IsNullOrEmpty(CategoryCode))
            parameters.Add($"categoryCode={Uri.EscapeDataString(CategoryCode)}");

        if (FromDate.HasValue)
            parameters.Add($"fromDate={FromDate.Value:yyyy-MM-dd}");

        if (ToDate.HasValue)
            parameters.Add($"toDate={ToDate.Value:yyyy-MM-dd}");

        return parameters.Count > 0 ? "&" + string.Join("&", parameters) : string.Empty;
    }
}
