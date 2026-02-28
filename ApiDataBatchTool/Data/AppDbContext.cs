using ApiDataBatchTool.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiDataBatchTool.Data;

/// <summary>
/// アプリケーションデータベースコンテキスト
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// 商品マスタ
    /// </summary>
    public DbSet<ProductEntity> Products => Set<ProductEntity>();

    /// <summary>
    /// バッチパラメータ
    /// </summary>
    public DbSet<BatchParameterEntity> BatchParameters => Set<BatchParameterEntity>();

    /// <summary>
    /// 連携制御
    /// </summary>
    public DbSet<LinkageControlEntity> LinkageControls => Set<LinkageControlEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ProductEntity の設定
        modelBuilder.Entity<ProductEntity>(entity =>
        {
            entity.HasKey(e => e.ProductCode);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 4);
        });

        // BatchParameterEntity の設定
        modelBuilder.Entity<BatchParameterEntity>(entity =>
        {
            entity.HasKey(e => e.ParameterKey);
        });
    }
}
