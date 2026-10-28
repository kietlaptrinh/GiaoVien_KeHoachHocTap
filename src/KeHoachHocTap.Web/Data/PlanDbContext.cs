using Microsoft.EntityFrameworkCore;

namespace KeHoachHocTap.Web.Data;

/// <summary>
/// DbContext chính của ứng dụng Kế hoạch học tập, quản lý Subjects, Holidays và AppSettings.
/// </summary>
public class PlanDbContext : DbContext
{
    public PlanDbContext(DbContextOptions<PlanDbContext> options) : base(options)
    {
    }

    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.Grade)
                .HasPrecision(4, 1);
            entity.Property(e => e.Note)
                .HasMaxLength(100);
            entity.Property(e => e.ExamMode)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Immediately");
            entity.Property(e => e.CustomExamSlot)
                .HasMaxLength(100);
            entity.HasIndex(e => e.SortOrder);
        });

        modelBuilder.Entity<Holiday>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
            entity.HasIndex(e => e.From);
        });

        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.HasKey(e => e.Key);
            entity.Property(e => e.Key)
                .HasMaxLength(100);
            entity.Property(e => e.Value)
                .IsRequired();
        });
    }
}
