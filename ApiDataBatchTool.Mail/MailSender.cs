using System;

namespace ApiDataBatchTool.Mail;

/// <summary>
/// メール送信クラス（モック実装）
/// </summary>
/// <remarks>
/// 将来的に実際のDLLに差し替える
/// </remarks>
public static class MailSender
{
    /// <summary>
    /// メールを送信する
    /// </summary>
    /// <param name="recipient">宛先メールアドレス</param>
    /// <param name="subject">件名</param>
    /// <param name="body">本文</param>
    public static void Send(string recipient, string subject, string body)
    {
        // モック実装: コンソールに出力するだけ
        Console.WriteLine("========================================");
        Console.WriteLine("[メール送信 - モック]");
        Console.WriteLine($"宛先: {recipient}");
        Console.WriteLine($"件名: {subject}");
        Console.WriteLine("本文:");
        Console.WriteLine(body);
        Console.WriteLine("========================================");
    }
}
