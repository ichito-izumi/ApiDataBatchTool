using ApiDataBatchTool.Configuration;
using ApiDataBatchTool.Data;
using ApiDataBatchTool.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiDataBatchTool.Services;

/// <summary>
/// パラメータサービス実装
/// </summary>
public class ParameterService : IParameterService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ParameterService> _logger;
    private readonly ApiSettings _apiSettings;

    // TODO: 実際のパラメータキー名に置き換えてください
    private const string FromDateKey = "FROM_DATE";
    private const string ToDateKey = "TO_DATE";

    public ParameterService(
        AppDbContext context,
        ILogger<ParameterService> logger,
        IOptions<ApiSettings> apiSettings)
    {
        _context = context;
        _logger = logger;
        _apiSettings = apiSettings.Value;
    }

    /// <inheritdoc/>
    public async Task<ApiQueryParameters> GetApiQueryParametersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("バッチパラメータを取得します");

        // 連携制御テーブルからCategoryCodeを取得
        var linkageControl = await _context.LinkageControls
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        var categoryCode = linkageControl?.CategoryCode;

        // バッチパラメータテーブルからその他のパラメータを取得
        var parameters = await _context.BatchParameters
            .AsNoTracking()
            .ToDictionaryAsync(p => p.ParameterKey, p => p.ParameterValue, cancellationToken);

        var queryParameters = new ApiQueryParameters(
            CategoryCode: categoryCode,
            FromDate: ParseDate(GetParameterValue(parameters, FromDateKey)),
            ToDate: ParseDate(GetParameterValue(parameters, ToDateKey)),
            IsOverseas: _apiSettings.IsOverseas
        );

        _logger.LogInformation(
            "バッチパラメータを取得しました: CategoryCode={CategoryCode}, FromDate={FromDate}, ToDate={ToDate}, IsOverseas={IsOverseas}",
            queryParameters.CategoryCode ?? "(なし)",
            queryParameters.FromDate?.ToString("yyyy-MM-dd") ?? "(なし)",
            queryParameters.ToDate?.ToString("yyyy-MM-dd") ?? "(なし)",
            queryParameters.IsOverseas);

        return queryParameters;
    }

    /// <summary>
    /// パラメータ値を取得する
    /// </summary>
    private static string? GetParameterValue(Dictionary<string, string?> parameters, string key)
    {
        return parameters.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// 日付文字列をパースする
    /// </summary>
    private DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        if (DateTime.TryParse(value, out var date))
            return date;

        _logger.LogWarning("日付のパースに失敗しました: {Value}", value);
        return null;
    }
}
