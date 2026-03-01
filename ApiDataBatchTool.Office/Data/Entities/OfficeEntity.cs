using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiDataBatchTool.Office.Data.Entities;

/// <summary>
/// 事業所マスタエンティティ
/// </summary>
[Table("OFFICE_MASTER")]
public class OfficeEntity
{
    /// <summary>
    /// 事業所コード（主キー）
    /// </summary>
    [Key]
    [Column("OFFICE_CODE")]
    [MaxLength(20)]
    public string OfficeCode { get; set; } = string.Empty;

    /// <summary>
    /// 事業所名
    /// </summary>
    [Column("OFFICE_NAME")]
    [MaxLength(200)]
    public string OfficeName { get; set; } = string.Empty;

    /// <summary>
    /// 事業所名カナ
    /// </summary>
    [Column("OFFICE_NAME_KANA")]
    [MaxLength(200)]
    public string? OfficeNameKana { get; set; }

    /// <summary>
    /// 郵便番号
    /// </summary>
    [Column("POSTAL_CODE")]
    [MaxLength(10)]
    public string? PostalCode { get; set; }

    /// <summary>
    /// 住所
    /// </summary>
    [Column("ADDRESS")]
    [MaxLength(500)]
    public string? Address { get; set; }

    /// <summary>
    /// 電話番号
    /// </summary>
    [Column("PHONE_NUMBER")]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// FAX番号
    /// </summary>
    [Column("FAX_NUMBER")]
    [MaxLength(20)]
    public string? FaxNumber { get; set; }

    /// <summary>
    /// 設立日
    /// </summary>
    [Column("ESTABLISHED_DATE")]
    public DateTime? EstablishedDate { get; set; }

    /// <summary>
    /// 閉鎖日
    /// </summary>
    [Column("CLOSED_DATE")]
    public DateTime? ClosedDate { get; set; }

    /// <summary>
    /// 有効フラグ
    /// </summary>
    [Column("IS_ACTIVE")]
    public bool IsActive { get; set; }

    /// <summary>
    /// 更新日時
    /// </summary>
    [Column("UPDATED_AT")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 登録日時
    /// </summary>
    [Column("CREATED_AT")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// システム更新日時
    /// </summary>
    [Column("SYS_UPDATED_AT")]
    public DateTime SysUpdatedAt { get; set; }
}
