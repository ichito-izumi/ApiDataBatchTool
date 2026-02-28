using ApiDataBatchTool.Configuration;
using ApiDataBatchTool.Data;
using ApiDataBatchTool.Data.Repositories;
using ApiDataBatchTool.Services;
using ApiDataBatchTool.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

// ========================================
// 設定の読み込み
// ========================================
builder.Services.Configure<ApiSettings>(
    builder.Configuration.GetSection(ApiSettings.SectionName));
builder.Services.Configure<DatabaseSettings>(
    builder.Configuration.GetSection(DatabaseSettings.SectionName));
builder.Services.Configure<BatchSettings>(
    builder.Configuration.GetSection(BatchSettings.SectionName));

// ========================================
// HttpClient の設定（リトライポリシー付き）
// ========================================
builder.Services.AddHttpClient("ApiClient", (sp, client) =>
{
    var apiSettings = sp.GetRequiredService<IOptions<ApiSettings>>().Value;
    client.BaseAddress = new Uri(apiSettings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(apiSettings.TimeoutSeconds);
})
.AddStandardResilienceHandler(options =>
{
    // リトライ設定をカスタマイズ
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.Delay = TimeSpan.FromSeconds(2);
});

// ========================================
// DbContext の設定
// ========================================
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var dbSettings = sp.GetRequiredService<IOptions<DatabaseSettings>>().Value;

    // TODO: 接続文字列を実際のものに置き換えてください
    // 現在は仮の接続文字列が設定されています
    options.UseOracle(dbSettings.ConnectionString, oracleOptions =>
    {
        oracleOptions.CommandTimeout(dbSettings.CommandTimeoutSeconds);
    });
});

// ========================================
// サービスの登録
// ========================================
builder.Services.AddScoped<IParameterService, ParameterService>();
builder.Services.AddScoped<IApiClientService, ApiClientService>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IBatchService, BatchService>();

// ========================================
// BackgroundService の登録
// ========================================
builder.Services.AddHostedService<BatchWorker>();

// ========================================
// アプリケーションの実行
// ========================================
var host = builder.Build();
await host.RunAsync();
