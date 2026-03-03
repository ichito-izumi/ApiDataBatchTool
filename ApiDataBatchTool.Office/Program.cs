using System;
using System.IO;
using ApiDataBatchTool.Common.Configuration;
using ApiDataBatchTool.Common.Data;
using ApiDataBatchTool.Office;
using ApiDataBatchTool.Office.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Serilog;

// ========================================
// Serilog 設定
// ========================================
var logFolder = Path.Combine(AppContext.BaseDirectory, "log");
Directory.CreateDirectory(logFolder);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(logFolder, "log.txt"),
        rollingInterval: RollingInterval.Infinite,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);

// Serilog をロギングプロバイダーとして設定
builder.Services.AddSerilog();

// ========================================
// 設定の読み込み（バリデーション付き）
// ========================================
builder.Services.AddOptions<ApiSettings>()
    .Bind(builder.Configuration.GetSection(ApiSettings.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<DatabaseSettings>()
    .Bind(builder.Configuration.GetSection(DatabaseSettings.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<BatchSettings>()
    .Bind(builder.Configuration.GetSection(BatchSettings.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<CidSettings>()
    .Bind(builder.Configuration.GetSection(CidSettings.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<MailSettings>()
    .Bind(builder.Configuration.GetSection(MailSettings.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ========================================
// シャットダウンタイムアウト設定
// ========================================
var shutdownTimeout = builder.Configuration
    .GetSection(BatchSettings.SectionName)
    .GetValue<int>("ShutdownTimeoutSeconds", 60);

builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(shutdownTimeout);
});

// ========================================
// DbContext の設定
// ========================================
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var dbSettings = sp.GetRequiredService<IOptions<DatabaseSettings>>().Value;
    options.UseOracle(dbSettings.ConnectionString, oracleOptions =>
    {
        oracleOptions.CommandTimeout(dbSettings.CommandTimeoutSeconds);
    });
});

// ========================================
// HttpClient の設定（リトライポリシー付き）
// ========================================
var apiConfig = builder.Configuration.GetSection(ApiSettings.SectionName);
var httpClientName = apiConfig.GetValue<string>("HttpClientName")
    ?? throw new InvalidOperationException("Api:HttpClientName は必須です");
var retryCount = apiConfig.GetValue<int>("RetryCount", 3);

builder.Services.AddHttpClient(httpClientName, (sp, client) =>
{
    var apiSettings = sp.GetRequiredService<IOptions<ApiSettings>>().Value;
    client.Timeout = TimeSpan.FromSeconds(apiSettings.TimeoutSeconds);
})
.AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = retryCount;
    options.Retry.Delay = TimeSpan.FromSeconds(2);
});

// ========================================
// バッチワーカーの登録
// ========================================
builder.Services.AddHostedService<BatchWorker>();

// ========================================
// アプリケーションの実行
// ========================================
var host = builder.Build();

try
{
    await host.RunAsync();
}
finally
{
    await Log.CloseAndFlushAsync();
}
