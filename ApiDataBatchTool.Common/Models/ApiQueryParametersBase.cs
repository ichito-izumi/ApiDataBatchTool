namespace ApiDataBatchTool.Common.Models;

/// <summary>
/// API呼び出し用のクエリパラメータ（基底クラス）
/// </summary>
public record ApiQueryParametersBase(
    string? Cid = null,
    string? CategoryCode = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null
)
{
    /// <summary>
    /// クエリパラメータのリストを取得（派生クラスでオーバーライドして追加パラメータを定義）
    /// </summary>
    protected virtual List<string> GetQueryParameters()
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

        return parameters;
    }

    /// <summary>
    /// クエリ文字列を生成（既存のクエリ文字列に追加する形式）
    /// パラメータがある場合は"&amp;param1=a&amp;param2=b"、ない場合は空文字を返す
    /// </summary>
    public string ToQueryString()
    {
        var parameters = GetQueryParameters();
        return parameters.Count > 0 ? "&" + string.Join("&", parameters) : string.Empty;
    }
}
