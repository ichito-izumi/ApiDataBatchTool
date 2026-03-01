using ApiDataBatchTool.Common.Configuration;
using ApiDataBatchTool.Common.Extensions;
using ApiDataBatchTool.Common.Services;
using ApiDataBatchTool.Office.Configuration;
using ApiDataBatchTool.Office.Data;
using ApiDataBatchTool.Office.Models;
using ApiDataBatchTool.Office.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

// ========================================
// 共通設定とサービスの登録
// ========================================
builder.Services.AddBatchCommonServices(builder.Configuration);

// ========================================
// 事業所固有の設定（バリデーション付き）
// ========================================
builder.Services.AddOptions<OfficeApiSettings>()
    .Bind(builder.Configuration.GetSection(ApiSettingsBase.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ========================================
// HttpClient の設定（リトライポリシー付き）
// ========================================
var apiConfig = builder.Configuration.GetSection(ApiSettingsBase.SectionName);
var retryCount = apiConfig.GetValue<int>("RetryCount", 3);

builder.Services.AddHttpClient("OfficeApi", (sp, client) =>
{
    var apiSettings = sp.GetRequiredService<IOptions<OfficeApiSettings>>().Value;
    client.BaseAddress = new Uri(apiSettings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(apiSettings.TimeoutSeconds);
})
.AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = retryCount;
    options.Retry.Delay = TimeSpan.FromSeconds(2);
});

// ========================================
// 事業所固有サービスの登録
// ========================================
builder.Services.AddScoped<IParameterService<OfficeQueryParameters>, OfficeParameterService>();
builder.Services.AddScoped<IApiClientService<OfficeQueryParameters>, OfficeApiClientService>();
builder.Services.AddScoped<IDataRepository<OfficeDto>, OfficeRepository>();
builder.Services.AddScoped<IBatchService, OfficeBatchService>();

// ========================================
// アプリケーションの実行
// ========================================
var host = builder.Build();
await host.RunAsync();
