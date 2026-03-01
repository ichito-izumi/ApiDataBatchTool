using System.Diagnostics;
using System.Net.Http.Json;
using ApiDataBatchTool.Common.Configuration;
using ApiDataBatchTool.Common.Models;
using Microsoft.Extensions.Logging;

namespace ApiDataBatchTool.Common.Services;

/// <summary>
/// APIクライアントサービス基底クラス
/// </summary>
public abstract class ApiClientServiceBase<TQueryParams> : IApiClientService<TQueryParams>
    where TQueryParams : ApiQueryParametersBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly ApiSettingsBase _apiSettings;
    private readonly string _httpClientName;

    protected ApiClientServiceBase(
        IHttpClientFactory httpClientFactory,
        ILogger logger,
        ApiSettingsBase apiSettings,
        string httpClientName)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _apiSettings = apiSettings;
        _httpClientName = httpClientName;
    }

    /// <inheritdoc/>
    public async Task<List<T>> GetAllPagesAsync<T>(TQueryParams queryParameters, CancellationToken cancellationToken = default)
    {
        var allItems = new List<T>();
        var currentPage = 1;

        _logger.LogInformation("API全ページ取得を開始します: Endpoint={Endpoint}", _apiSettings.Endpoint);

        while (true)
        {
            var pageItems = await GetPageAsync<T>(currentPage, queryParameters, cancellationToken);

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
    private async Task<List<T>> GetPageAsync<T>(int page, TQueryParams queryParameters, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(_httpClientName);

        var url = $"{_apiSettings.Endpoint}?page={page}&pageSize={_apiSettings.PageSize}{queryParameters.ToQueryString()}";

        _logger.LogDebug("APIリクエスト: {Url}", url);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(cancellationToken: cancellationToken);

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
