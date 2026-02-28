using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiDataBatchTool.Data.Entities;

/// <summary>
/// バッチパラメータエンティティ
/// TODO: 実際のテーブル構造に合わせて調整してください
/// </summary>
[Table("BATCH_PARAMETERS")]
public class BatchParameterEntity
{
    /// <summary>
    /// パラメータキー（主キー）
    /// </summary>
    [Key]
    [Column("PARAMETER_KEY")]
    [MaxLength(100)]
    public required string ParameterKey { get; set; }

    /// <summary>
    /// パラメータ値
    /// </summary>
    [Column("PARAMETER_VALUE")]
    [MaxLength(500)]
    public string? ParameterValue { get; set; }

    /// <summary>
    /// 説明
    /// </summary>
    [Column("DESCRIPTION")]
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 更新日時
    /// </summary>
    [Column("UPDATED_AT")]
    public DateTime UpdatedAt { get; set; }
}
