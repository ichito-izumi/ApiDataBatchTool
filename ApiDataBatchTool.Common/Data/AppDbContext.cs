using ApiDataBatchTool.Common.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiDataBatchTool.Common.Data;

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
    /// 連携制御
    /// </summary>
    public DbSet<LinkageControlEntity> LinkageControls => Set<LinkageControlEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LinkageControlEntity>(entity =>
        {
            entity.HasKey(e => e.ControlId);
        });
    }
}
