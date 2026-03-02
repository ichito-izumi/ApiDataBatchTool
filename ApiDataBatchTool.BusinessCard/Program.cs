using ApiDataBatchTool.BusinessCard.Configuration;
using ApiDataBatchTool.BusinessCard.Data;
using ApiDataBatchTool.BusinessCard.Models;
using ApiDataBatchTool.BusinessCard.Services;
using ApiDataBatchTool.Common.Configuration;
using ApiDataBatchTool.Common.Extensions;
using ApiDataBatchTool.Common.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// ========================================
// 共通設定とサービスの登録
// ========================================
builder.Services.AddBatchCommonServices(builder.Configuration);

// ========================================
// 名刺固有の設定（バリデーション付き）
// ========================================
builder.Services.AddOptions<BusinessCardApiSettings>()
    .Bind(builder.Configuration.GetSection(ApiSettingsBase.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ========================================
// HttpClient の設定（リトライポリシー付き）
// ========================================
builder.Services.AddApiHttpClient<BusinessCardApiSettings>(builder.Configuration);

// ========================================
// 名刺固有サービスの登録
// ========================================
builder.Services.AddScoped<IParameterService<BusinessCardQueryParameters>, BusinessCardParameterService>();
builder.Services.AddScoped<IApiClientService<BusinessCardQueryParameters, BusinessCardDto>, ApiClientService<BusinessCardQueryParameters, BusinessCardDto, BusinessCardApiSettings>>();
builder.Services.AddScoped<IDataRepository<BusinessCardDto>, BusinessCardRepository>();
builder.Services.AddScoped<IBatchService, BatchService<BusinessCardQueryParameters, BusinessCardDto>>();

// ========================================
// アプリケーションの実行
// ========================================
var host = builder.Build();
await host.RunAsync();
