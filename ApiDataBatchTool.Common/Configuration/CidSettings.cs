using System.ComponentModel.DataAnnotations;

namespace ApiDataBatchTool.Common.Configuration;

/// <summary>
/// CID取得設定
/// </summary>
public class CidSettings
{
    public const string SectionName = "Cid";

    /// <summary>
    /// CID取得用batファイルのパス（exeからの相対パスまたは絶対パス）
    /// </summary>
    [Required(ErrorMessage = "Cid:BatFilePath は必須です")]
    public required string BatFilePath { get; set; }

    /// <summary>
    /// batファイルに渡す引数（名刺/事業所などの識別子）
    /// </summary>
    public string? BatArguments { get; set; }

    /// <summary>
    /// batファイル実行のタイムアウト秒数
    /// </summary>
    [Range(1, 300, ErrorMessage = "Cid:TimeoutSeconds は1から300の間で指定してください")]
    public int TimeoutSeconds { get; set; } = 30;
}
