using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ApiDataBatchTool.Common.Services;

/// <summary>
/// メール送信サービス実装（ダミー）
/// </summary>
/// <remarks>
/// 将来的に既存DLLを呼び出す実装に差し替える
/// </remarks>
public class MailService : IMailService
{
    private readonly ILogger<MailService> _logger;

    public MailService(ILogger<MailService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)
    {
        // TODO: 既存DLLを呼び出す実装に差し替える
        // 例: ExternalMailLibrary.MailSender.Send(recipient, subject, body);

        _logger.LogInformation(
            "メール送信（ダミー）: 宛先={Recipient}, 件名={Subject}",
            recipient,
            subject);

        _logger.LogDebug("メール本文: {Body}", body);

        return Task.CompletedTask;
    }
}
