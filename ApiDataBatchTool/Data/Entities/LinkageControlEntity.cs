using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiDataBatchTool.Data.Entities;

/// <summary>
/// 連携制御エンティティ
/// TODO: 実際のテーブル構造に合わせて調整してください
/// </summary>
[Table("T_RENKEI_SEIGYO")]
public class LinkageControlEntity
{
    /// <summary>
    /// 制御ID（主キー）
    /// TODO: 実際の主キーカラム名に置き換えてください
    /// </summary>
    [Key]
    [Column("CONTROL_ID")]
    public int ControlId { get; set; }

    /// <summary>
    /// カテゴリコード
    /// </summary>
    [Column("CATEGORY_CODE")]
    [MaxLength(50)]
    public string? CategoryCode { get; set; }

    /// <summary>
    /// 更新日時
    /// </summary>
    [Column("UPDATED_AT")]
    public DateTime? UpdatedAt { get; set; }
}
