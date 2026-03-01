using System.Diagnostics;
using ApiDataBatchTool.Common.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiDataBatchTool.Common.Services;

/// <summary>
/// CID取得プロバイダー
/// </summary>
public class CidProvider : ICidProvider
{
    private readonly ILogger<CidProvider> _logger;
    private readonly CidSettings _cidSettings;

    public CidProvider(
        ILogger<CidProvider> logger,
        IOptions<CidSettings> cidSettings)
    {
        _logger = logger;
        _cidSettings = cidSettings.Value;
    }

    /// <inheritdoc/>
    public async Task<string> GetCidAsync(CancellationToken cancellationToken = default)
    {
        var batFilePath = ResolveBatFilePath(_cidSettings.BatFilePath);

        if (!File.Exists(batFilePath))
        {
            throw new FileNotFoundException($"CID取得用batファイルが見つかりません: {batFilePath}");
        }

        var arguments = _cidSettings.BatArguments;
        _logger.LogInformation(
            "CID取得batファイルを実行します: {BatFilePath}, 引数={Arguments}",
            batFilePath,
            arguments ?? "(なし)");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // batファイルと引数を組み立て
            var cmdArguments = string.IsNullOrEmpty(arguments)
                ? $"/c \"{batFilePath}\""
                : $"/c \"{batFilePath}\" {arguments}";

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = cmdArguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            stopwatch.Stop();

            if (process.ExitCode != 0)
            {
                _logger.LogError(
                    "CID取得batファイルがエラーで終了しました: ExitCode={ExitCode}, Error={Error}",
                    process.ExitCode,
                    error);
                throw new InvalidOperationException($"CID取得batファイルの実行に失敗しました: {error}");
            }

            var cid = output.Trim();

            if (string.IsNullOrEmpty(cid))
            {
                throw new InvalidOperationException("CID取得batファイルの出力が空です");
            }

            _logger.LogInformation(
                "CID取得完了: CID={Cid}, 処理時間={Elapsed}ms",
                cid,
                stopwatch.ElapsedMilliseconds);

            return cid;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("CID取得処理がキャンセルされました");
            throw;
        }
        catch (Exception ex) when (ex is not InvalidOperationException and not FileNotFoundException)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "CID取得batファイルの実行中にエラーが発生しました");
            throw new InvalidOperationException($"CID取得に失敗しました: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// batファイルのパスを解決する（相対パスの場合はexeディレクトリを基準）
    /// </summary>
    private static string ResolveBatFilePath(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        var baseDirectory = AppContext.BaseDirectory;
        return Path.Combine(baseDirectory, configuredPath);
    }
}
