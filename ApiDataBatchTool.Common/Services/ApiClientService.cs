using System.Diagnostics;
using System.Net.Http.Json;
using ApiDataBatchTool.Common.Configuration;
using ApiDataBatchTool.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiDataBatchTool.Common.Services;

/// <summary>
/// APIクライアントサービス
/// </summary>
/// <typeparam name="TQueryParams">クエリパラメータの型</typeparam>
/// <typeparam name="TDto">DTOの型</typeparam>
/// <typeparam name="TSettings">設定の型</typeparam>
public class ApiClientService<TQueryParams, TDto, TSettings> : IApiClientService<TQueryParams, TDto>
    where TQueryParams : ApiQueryParametersBase
    where TSettings : ApiSettingsBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ApiClientService<TQueryParams, TDto, TSettings>> _logger;
    private readonly TSettings _apiSettings;

    public ApiClientService(
        IHttpClientFactory httpClientFactory,
        ILogger<ApiClientService<TQueryParams, TDto, TSettings>> logger,
        IOptions<TSettings> apiSettings)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _apiSettings = apiSettings.Value;
    }

    /// <inheritdoc/>
    public async Task<List<TDto>> GetAllPagesAsync(TQueryParams queryParameters, CancellationToken cancellationToken = default)
    {
        var allItems = new List<TDto>();
        var currentPage = 1;

        _logger.LogInformation("API全ページ取得を開始します: Endpoint={Endpoint}", _apiSettings.Endpoint);

        while (true)
        {
            var pageItems = await GetPageAsync(currentPage, queryParameters, cancellationToken);

            allItems.AddRange(pageItems);

            _logger.LogInformation(
                "ページ取得完了: Page={Page}, 取得件数={Count}, 累計件数={Total}",
                currentPage,
                pageItems.Count,
                allItems.Count);

            // 取得件数がPageSize未満なら最終ページ
            if (pageItems.Count < _apiSettings.PageSize)
            {
                break;
            }

            currentPage++;
        }

        _logger.LogInformation("API全ページ取得が完了しました: 総件数={Total}", allItems.Count);
        return allItems;
    }

    /// <summary>
    /// 指定ページのデータを取得する
    /// </summary>
    private async Task<List<TDto>> GetPageAsync(int page, TQueryParams queryParameters, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(_apiSettings.HttpClientName);

        var url = $"{_apiSettings.Endpoint}?page={page}&pageSize={_apiSettings.PageSize}{queryParameters.ToQueryString()}";

        _logger.LogDebug("APIリクエスト: {Url}", url);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<TDto>>(cancellationToken: cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "APIリクエスト完了: Page={Page}, 処理時間={Elapsed}ms",
                page,
                stopwatch.ElapsedMilliseconds);

            return apiResponse?.Items ?? [];
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "APIリクエストでエラーが発生しました: Page={Page}, Url={Url}, 処理時間={Elapsed}ms", page, url, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
