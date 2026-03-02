using System;
using ApiDataBatchTool.Common.Configuration;
using ApiDataBatchTool.Common.Data;
using ApiDataBatchTool.Common.Services;
using ApiDataBatchTool.Common.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

namespace ApiDataBatchTool.Common.Extensions;

/// <summary>
/// サービス登録の拡張メソッド
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 共通設定とサービスを登録
    /// </summary>
    public static IServiceCollection AddBatchCommonServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 共通設定の読み込み（バリデーション付き）
        services.AddOptions<DatabaseSettings>()
            .Bind(configuration.GetSection(DatabaseSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<BatchSettings>()
            .Bind(configuration.GetSection(BatchSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<CidSettings>()
            .Bind(configuration.GetSection(CidSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Graceful Shutdown のタイムアウト設定
        var shutdownTimeout = configuration
            .GetSection(BatchSettings.SectionName)
            .GetValue<int>("ShutdownTimeoutSeconds", 60);

        services.Configure<HostOptions>(options =>
        {
            options.ShutdownTimeout = TimeSpan.FromSeconds(shutdownTimeout);
        });

        // DbContext の設定
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var dbSettings = sp.GetRequiredService<IOptions<DatabaseSettings>>().Value;
            options.UseOracle(dbSettings.ConnectionString, oracleOptions =>
            {
                oracleOptions.CommandTimeout(dbSettings.CommandTimeoutSeconds);
            });
        });

        // 共通サービスの登録
        services.AddSingleton<ICidProvider, CidProvider>();

        // BackgroundService の登録
        services.AddHostedService<BatchWorkerBase>();

        return services;
    }

    /// <summary>
    /// API用HttpClientを登録（リトライポリシー付き）
    /// </summary>
    /// <typeparam name="TSettings">API設定の型</typeparam>
    public static IServiceCollection AddApiHttpClient<TSettings>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TSettings : ApiSettingsBase
    {
        var apiConfig = configuration.GetSection(ApiSettingsBase.SectionName);
        var httpClientName = apiConfig.GetValue<string>("HttpClientName")
            ?? throw new InvalidOperationException("Api:HttpClientName は必須です");
        var retryCount = apiConfig.GetValue<int>("RetryCount", 3);

        services.AddHttpClient(httpClientName, (sp, client) =>
        {
            var apiSettings = sp.GetRequiredService<IOptions<TSettings>>().Value;
            client.BaseAddress = new Uri(apiSettings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(apiSettings.TimeoutSeconds);
        })
        .AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = retryCount;
            options.Retry.Delay = TimeSpan.FromSeconds(2);
        });

        return services;
    }

    /// <summary>
    /// 失敗通知サービスを登録（オプション機能）
    /// </summary>
    /// <remarks>
    /// この機能を有効にするには、appsettings.json に Mail と ExecutionHistory セクションを追加してください
    /// </remarks>
    public static IServiceCollection AddFailureNotificationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // メール設定の読み込み（バリデーション付き）
        services.AddOptions<MailSettings>()
            .Bind(configuration.GetSection(MailSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 実行履歴設定の読み込み（バリデーション付き）
        services.AddOptions<ExecutionHistorySettings>()
            .Bind(configuration.GetSection(ExecutionHistorySettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // サービスの登録
        services.AddSingleton<IExecutionHistoryService, ExecutionHistoryService>();
        services.AddSingleton<IMailService, MailService>();

        return services;
    }
}
