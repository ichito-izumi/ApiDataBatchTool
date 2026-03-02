using System.ComponentModel.DataAnnotations;

namespace ApiDataBatchTool.Common.Configuration;

/// <summary>
/// メール送信設定
/// </summary>
public class MailSettings
{
    public const string SectionName = "Mail";

    /// <summary>
    /// メール送信を有効にするかどうか
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 送信先メールアドレス
    /// </summary>
    [Required(ErrorMessage = "Mail:Recipient は必須です")]
    [EmailAddress(ErrorMessage = "Mail:Recipient は有効なメールアドレスではありません")]
    public required string Recipient { get; set; }

    /// <summary>
    /// メール件名
    /// </summary>
    [Required(ErrorMessage = "Mail:Subject は必須です")]
    public required string Subject { get; set; }
}
