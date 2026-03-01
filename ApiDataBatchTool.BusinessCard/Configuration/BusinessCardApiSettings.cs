using ApiDataBatchTool.Common.Configuration;

namespace ApiDataBatchTool.BusinessCard.Configuration;

/// <summary>
/// 名刺API接続設定
/// </summary>
public class BusinessCardApiSettings : ApiSettingsBase
{
    /// <summary>
    /// 海外フラグ
    /// </summary>
    public bool IsOverseas { get; set; } = false;
}
