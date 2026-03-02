using System.Threading;
using System.Threading.Tasks;

namespace ApiDataBatchTool.Common.Services;

/// <summary>
/// メール送信サービスインターフェース
/// </summary>
public interface IMailService
{
    /// <summary>
    /// メールを送信する
    /// </summary>
    /// <param name="recipient">宛先メールアドレス</param>
    /// <param name="subject">件名</param>
    /// <param name="body">本文</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default);
}
