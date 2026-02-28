using System.Text.Json.Serialization;

namespace ApiDataBatchTool.Models;

/// <summary>
/// 商品情報DTO（APIレスポンス用）
/// TODO: 実際のAPIレスポンスに合わせてプロパティを調整してください
/// </summary>
public record ProductDto(
    [property: JsonPropertyName("productCode")] string ProductCode,
    [property: JsonPropertyName("productName")] string? ProductName,
    [property: JsonPropertyName("categoryCode")] string? CategoryCode,
    [property: JsonPropertyName("unitPrice")] decimal? UnitPrice,
    [property: JsonPropertyName("stockQuantity")] int? StockQuantity,
    [property: JsonPropertyName("isActive")] bool IsActive = true,
    [property: JsonPropertyName("updatedAt")] DateTime? UpdatedAt = null
);
