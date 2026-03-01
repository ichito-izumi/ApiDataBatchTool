using ApiDataBatchTool.Common.Configuration;
using ApiDataBatchTool.Common.Data;
using ApiDataBatchTool.Common.Services;
using ApiDataBatchTool.Common.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
}
