using System.Text.Json.Serialization;

namespace ApiDataBatchTool.Office.Models;

/// <summary>
/// 事業所DTOレコード（APIレスポンス用）
/// </summary>
public record OfficeDto(
    [property: JsonPropertyName("officeCode")] string OfficeCode,
    [property: JsonPropertyName("officeName")] string OfficeName,
    [property: JsonPropertyName("officeNameKana")] string? OfficeNameKana,
    [property: JsonPropertyName("postalCode")] string? PostalCode,
    [property: JsonPropertyName("address")] string? Address,
    [property: JsonPropertyName("phoneNumber")] string? PhoneNumber,
    [property: JsonPropertyName("faxNumber")] string? FaxNumber,
    [property: JsonPropertyName("establishedDate")] DateTime? EstablishedDate,
    [property: JsonPropertyName("closedDate")] DateTime? ClosedDate,
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("updatedAt")] DateTime UpdatedAt
);
