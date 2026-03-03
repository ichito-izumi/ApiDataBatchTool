using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ApiDataBatchTool.BusinessCard.Models;

/// <summary>
/// 名刺APIレスポンス
/// </summary>
public class BusinessCardApiResponse
{
    /// <summary>
    /// データ項目のリスト
    /// </summary>
    [JsonPropertyName("items")]
    public List<BusinessCardDto>? Items { get; set; }

    /// <summary>
    /// 総件数
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>
    /// null安全なItemsアクセサ
    /// </summary>
    [JsonIgnore]
    public List<BusinessCardDto> SafeItems => Items ?? [];
}
