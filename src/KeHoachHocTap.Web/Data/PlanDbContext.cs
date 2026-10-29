using Microsoft.EntityFrameworkCore;

namespace KeHoachHocTap.Web.Data;

/// <summary>
/// DbContext chính của ứng dụng Kế hoạch học tập, quản lý Subjects, Holidays, AppSettings, PlanActivities, Students và StudentGrades.
/// </summary>
public class PlanDbContext : DbContext
{
    public PlanDbContext(DbContextOptions<PlanDbContext> options) : base(options)
    {
    }

    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<PlanActivity> PlanActivities => Set<PlanActivity>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<StudentGrade> StudentGrades => Set<StudentGrade>();

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
            entity.Property(e => e.Coefficient)
                .HasPrecision(4, 2)
                .HasDefaultValue(1.0m);
            entity.Property(e => e.KnowledgeBlock)
                .HasMaxLength(150);
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

        modelBuilder.Entity<PlanActivity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.Note)
                .HasMaxLength(500);
            entity.HasIndex(e => e.SortOrder);
            entity.HasIndex(e => e.AfterSubjectId);
            entity.HasOne<Subject>()
                .WithMany()
                .HasForeignKey(e => e.AfterSubjectId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StudentCode)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.LastName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.FirstName)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.Gender)
                .HasMaxLength(10);
            entity.Property(e => e.Note)
                .HasMaxLength(200);
            entity.HasIndex(e => e.StudentCode);
            entity.HasIndex(e => e.SortOrder);
        });

        modelBuilder.Entity<StudentGrade>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Score)
                .HasPrecision(4, 2);
            entity.Property(e => e.Note)
                .HasMaxLength(200);
            entity.HasIndex(e => new { e.StudentId, e.SubjectId })
                .IsUnique();
            entity.HasOne(e => e.Student)
                .WithMany(s => s.Grades)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Subject)
                .WithMany()
                .HasForeignKey(e => e.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
