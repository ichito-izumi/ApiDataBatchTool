using System;
using System.Text.Json.Serialization;

namespace ApiDataBatchTool.BusinessCard.Models;

/// <summary>
/// 名刺DTO（APIレスポンス用）
/// TODO: 実際のAPIレスポンスに合わせてプロパティを調整してください
/// </summary>
public record BusinessCardDto(
    [property: JsonPropertyName("cardId")] string CardId,
    [property: JsonPropertyName("personName")] string? PersonName,
    [property: JsonPropertyName("companyName")] string? CompanyName,
    [property: JsonPropertyName("department")] string? Department,
    [property: JsonPropertyName("position")] string? Position,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("isOverseas")] bool IsOverseas = false,
    [property: JsonPropertyName("updatedAt")] DateTime? UpdatedAt = null
);
