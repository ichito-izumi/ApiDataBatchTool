using System.Text.Json.Serialization;

namespace ApiDataBatchTool.Common.Models;

/// <summary>
/// APIレスポンスのラッパー
/// </summary>
/// <typeparam name="T">データ項目の型</typeparam>
public record ApiResponse<T>
{
    /// <summary>
    /// データ項目のリスト
    /// </summary>
    [JsonPropertyName("items")]
    public List<T>? Items { get; init; }

    /// <summary>
    /// 総件数
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; init; }

    /// <summary>
    /// null安全なItemsアクセサ
    /// </summary>
    [JsonIgnore]
    public List<T> SafeItems => Items ?? [];
}
