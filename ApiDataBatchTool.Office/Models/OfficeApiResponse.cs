using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ApiDataBatchTool.Office.Models;

/// <summary>
/// 事業所APIレスポンス
/// </summary>
public class OfficeApiResponse
{
    /// <summary>
    /// データ項目のリスト
    /// </summary>
    [JsonPropertyName("items")]
    public List<OfficeDto>? Items { get; set; }

    /// <summary>
    /// 総件数
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>
    /// null安全なItemsアクセサ
    /// </summary>
    [JsonIgnore]
    public List<OfficeDto> SafeItems => Items ?? [];
}
