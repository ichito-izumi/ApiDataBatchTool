using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiDataBatchTool.Data.Entities;

/// <summary>
/// 商品マスタエンティティ
/// TODO: 実際のテーブル構造に合わせて調整してください
/// </summary>
[Table("PRODUCT_MASTER")]
public class ProductEntity
{
    /// <summary>
    /// 商品コード（主キー）
    /// </summary>
    [Key]
    [Column("PRODUCT_CODE")]
    [MaxLength(50)]
    public required string ProductCode { get; set; }

    /// <summary>
    /// 商品名
    /// </summary>
    [Column("PRODUCT_NAME")]
    [MaxLength(200)]
    public string? ProductName { get; set; }

    /// <summary>
    /// カテゴリコード
    /// </summary>
    [Column("CATEGORY_CODE")]
    [MaxLength(20)]
    public string? CategoryCode { get; set; }

    /// <summary>
    /// 単価
    /// </summary>
    [Column("UNIT_PRICE")]
    public decimal? UnitPrice { get; set; }

    /// <summary>
    /// 在庫数量
    /// </summary>
    [Column("STOCK_QUANTITY")]
    public int? StockQuantity { get; set; }

    /// <summary>
    /// 有効フラグ
    /// </summary>
    [Column("IS_ACTIVE")]
    public bool IsActive { get; set; } = true;

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
