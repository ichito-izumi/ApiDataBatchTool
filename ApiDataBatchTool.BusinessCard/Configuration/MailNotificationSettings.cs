using System.ComponentModel.DataAnnotations;

namespace ApiDataBatchTool.BusinessCard.Configuration;

/// <summary>
/// メール通知設定
/// </summary>
public class MailNotificationSettings
{
    public const string SectionName = "MailNotification";

    /// <summary>
    /// メール通知が有効かどうか
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 開発メンバー向け通知設定
    /// </summary>
    [Required]
    public required MailTargetSettings Development { get; set; }

    /// <summary>
    /// 運用SE向け通知設定
    /// </summary>
    [Required]
    public required MailTargetSettings Operation { get; set; }
}

/// <summary>
/// 通知先ごとの設定
/// </summary>
public class MailTargetSettings
{
    /// <summary>
    /// 宛先メールアドレス
    /// </summary>
    [Required]
    public required string Recipient { get; set; }

    /// <summary>
    /// 件名
    /// </summary>
    [Required]
    public required string Subject { get; set; }

    /// <summary>
    /// 本文テンプレート
    /// プレースホルダー: {BatchName}, {ExitCode}, {FailureCount}, {DateTime}
    /// </summary>
    [Required]
    public required string BodyTemplate { get; set; }
}
