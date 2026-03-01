using System.Text.Json.Serialization;

namespace ApiDataBatchTool.Common.Models;

/// <summary>
/// APIレスポンスのラッパー
/// </summary>
/// <typeparam name="T">データ項目の型</typeparam>
public record ApiResponse<T>(
    [property: JsonPropertyName("items")] List<T> Items,
    [property: JsonPropertyName("total")] int Total
);
