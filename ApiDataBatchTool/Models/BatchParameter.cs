namespace ApiDataBatchTool.Models;

/// <summary>
/// バッチ処理パラメータ（DBから取得）
/// TODO: 実際のパラメータテーブル構造に合わせて調整してください
/// </summary>
public record BatchParameter(
    string ParameterKey,
    string? ParameterValue
);

/// <summary>
/// API呼び出し用のクエリパラメータ
/// </summary>
public record ApiQueryParameters(
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
