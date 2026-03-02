using ApiDataBatchTool.Common.Configuration;
using ApiDataBatchTool.Common.Extensions;
using ApiDataBatchTool.Common.Services;
using ApiDataBatchTool.Office.Data;
using ApiDataBatchTool.Office.Models;
using ApiDataBatchTool.Office.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// ========================================
// 共通設定とサービスの登録
// ========================================
builder.Services.AddBatchCommonServices(builder.Configuration);

// ========================================
// 事業所固有の設定（バリデーション付き）
// ========================================
builder.Services.AddOptions<ApiSettingsBase>()
    .Bind(builder.Configuration.GetSection(ApiSettingsBase.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ========================================
// HttpClient の設定（リトライポリシー付き）
// ========================================
builder.Services.AddApiHttpClient<ApiSettingsBase>(builder.Configuration);

// ========================================
// 事業所固有サービスの登録
// ========================================
builder.Services.AddScoped<IParameterService<OfficeQueryParameters>, OfficeParameterService>();
builder.Services.AddScoped<IApiClientService<OfficeQueryParameters, OfficeDto>, ApiClientService<OfficeQueryParameters, OfficeDto, ApiSettingsBase>>();
builder.Services.AddScoped<IDataRepository<OfficeDto>, OfficeRepository>();
builder.Services.AddScoped<IBatchService, BatchService<OfficeQueryParameters, OfficeDto>>();

// ========================================
// アプリケーションの実行
// ========================================
var host = builder.Build();
await host.RunAsync();
