using System;
using System.Collections.Generic;

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

        parameters.Add($"isOverseas={(IsOverseas ? 1 : 0)}");

        return parameters.Count > 0 ? "&" + string.Join("&", parameters) : string.Empty;
    }
}
