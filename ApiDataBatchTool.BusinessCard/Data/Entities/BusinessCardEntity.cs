using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiDataBatchTool.BusinessCard.Data.Entities;

/// <summary>
/// 名刺エンティティ
/// TODO: 実際のテーブル構造に合わせて調整してください
/// </summary>
[Table("BUSINESS_CARD_MASTER")]
public class BusinessCardEntity
{
    /// <summary>
    /// 名刺ID（主キー）
    /// </summary>
    [Key]
    [Column("CARD_ID")]
    [MaxLength(50)]
    public required string CardId { get; set; }

    /// <summary>
    /// 氏名
    /// </summary>
    [Column("PERSON_NAME")]
    [MaxLength(100)]
    public string? PersonName { get; set; }

    /// <summary>
    /// 会社名
    /// </summary>
    [Column("COMPANY_NAME")]
    [MaxLength(200)]
    public string? CompanyName { get; set; }

    /// <summary>
    /// 部署
    /// </summary>
    [Column("DEPARTMENT")]
    [MaxLength(100)]
    public string? Department { get; set; }

    /// <summary>
    /// 役職
    /// </summary>
    [Column("POSITION")]
    [MaxLength(100)]
    public string? Position { get; set; }

    /// <summary>
    /// メールアドレス
    /// </summary>
    [Column("EMAIL")]
    [MaxLength(200)]
    public string? Email { get; set; }

    /// <summary>
    /// 電話番号
    /// </summary>
    [Column("PHONE")]
    [MaxLength(50)]
    public string? Phone { get; set; }

    /// <summary>
    /// 海外フラグ
    /// </summary>
    [Column("IS_OVERSEAS")]
    public bool IsOverseas { get; set; }

    /// <summary>
    /// API更新日時
    /// </summary>
    [Column("API_UPDATED_AT")]
    public DateTime? ApiUpdatedAt { get; set; }

    /// <summary>
    /// 登録日時
    /// </summary>
    [Column("CREATED_AT")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新日時
    /// </summary>
    [Column("UPDATED_AT")]
    public DateTime UpdatedAt { get; set; }
}
