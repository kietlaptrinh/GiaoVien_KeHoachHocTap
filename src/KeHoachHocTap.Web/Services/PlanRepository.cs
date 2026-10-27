using KeHoachHocTap.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KeHoachHocTap.Web.Services;

/// <summary>
/// Triển khai repository truy xuất cơ sở dữ liệu qua EF Core cho Môn học, Ngày nghỉ và Cấu hình.
/// </summary>
public class PlanRepository : IPlanRepository
{
    private readonly PlanDbContext _context;

    public PlanRepository(PlanDbContext context)
    {
        _context = context;
    }

    // ==========================================
    // Subject operations
    // ==========================================

    public async Task<IReadOnlyList<Subject>> GetSubjectsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Subjects
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Id)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Subject?> GetSubjectByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Subjects
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task AddSubjectAsync(Subject subject, CancellationToken cancellationToken = default)
    {
        var hasExistingTx = _context.Database.CurrentTransaction != null;
        IDbContextTransaction? tx = hasExistingTx ? null : await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            if (subject.SortOrder <= 0)
            {
                var maxOrder = await _context.Subjects.MaxAsync(s => (int?)s.SortOrder, cancellationToken) ?? 0;
                subject.SortOrder = maxOrder + 1;
                _context.Subjects.Add(subject);
                await _context.SaveChangesAsync(cancellationToken);
            }
            else
            {
                _context.Subjects.Add(subject);
                await _context.SaveChangesAsync(cancellationToken);
                await RenumberSubjectsInternalAsync(cancellationToken);
            }

            if (tx != null)
            {
                await tx.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            if (tx != null)
            {
                await tx.RollbackAsync(cancellationToken);
            }
            throw;
        }
    }

    public async Task UpdateSubjectAsync(Subject subject, CancellationToken cancellationToken = default)
    {
        var tracked = _context.Subjects.Local.FirstOrDefault(s => s.Id == subject.Id);
        if (tracked != null)
        {
            if (!ReferenceEquals(tracked, subject))
            {
                _context.Entry(tracked).CurrentValues.SetValues(subject);
            }
        }
        else
        {
            _context.Subjects.Update(subject);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteSubjectAsync(int id, CancellationToken cancellationToken = default)
    {
        var subject = await _context.Subjects.FindAsync(new object[] { id }, cancellationToken);
        if (subject == null)
        {
            return;
        }

        var hasExistingTx = _context.Database.CurrentTransaction != null;
        IDbContextTransaction? tx = hasExistingTx ? null : await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            _context.Subjects.Remove(subject);
            await _context.SaveChangesAsync(cancellationToken);

            await RenumberSubjectsInternalAsync(cancellationToken);

            if (tx != null)
            {
                await tx.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            if (tx != null)
            {
                await tx.RollbackAsync(cancellationToken);
            }
            throw;
        }
    }

    public async Task MoveSubjectAsync(int subjectId, int direction, CancellationToken cancellationToken = default)
    {
        if (direction != -1 && direction != 1)
        {
            throw new ArgumentOutOfRangeException(nameof(direction), "Hướng di chuyển chỉ có thể là -1 (lên) hoặc +1 (xuống).");
        }

        var hasExistingTx = _context.Database.CurrentTransaction != null;
        IDbContextTransaction? tx = hasExistingTx ? null : await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var subjects = await _context.Subjects
                .OrderBy(s => s.SortOrder)
                .ThenBy(s => s.Id)
                .ToListAsync(cancellationToken);

            var currentIndex = subjects.FindIndex(s => s.Id == subjectId);
            if (currentIndex < 0)
            {
                throw new KeyNotFoundException($"Không tìm thấy môn học có Id = {subjectId}.");
            }

            var targetIndex = currentIndex + direction;
            if (targetIndex >= 0 && targetIndex < subjects.Count)
            {
                var movingSubject = subjects[currentIndex];
                subjects.RemoveAt(currentIndex);
                subjects.Insert(targetIndex, movingSubject);

                for (int i = 0; i < subjects.Count; i++)
                {
                    subjects[i].SortOrder = i + 1;
                }

                await _context.SaveChangesAsync(cancellationToken);
            }

            if (tx != null)
            {
                await tx.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            if (tx != null)
            {
                await tx.RollbackAsync(cancellationToken);
            }
            throw;
        }
    }

    private async Task RenumberSubjectsInternalAsync(CancellationToken cancellationToken)
    {
        var subjects = await _context.Subjects
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Id)
            .ToListAsync(cancellationToken);

        for (int i = 0; i < subjects.Count; i++)
        {
            subjects[i].SortOrder = i + 1;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    // ==========================================
    // Holiday operations
    // ==========================================

    public async Task<IReadOnlyList<Holiday>> GetHolidaysAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Holidays
            .OrderBy(h => h.From)
            .ThenBy(h => h.To)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Holiday?> GetHolidayByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Holidays
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
    }

    public async Task<Holiday?> GetOpeningDayAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Holidays
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.IsOpeningDay, cancellationToken);
    }

    public async Task AddHolidayAsync(Holiday holiday, CancellationToken cancellationToken = default)
    {
        if (holiday.To < holiday.From)
        {
            throw new ArgumentException("Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
        }

        var hasExistingTx = _context.Database.CurrentTransaction != null;
        IDbContextTransaction? tx = hasExistingTx ? null : await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            if (holiday.IsOpeningDay)
            {
                holiday.To = holiday.From;
                var existingOpenings = await _context.Holidays
                    .Where(h => h.IsOpeningDay)
                    .ToListAsync(cancellationToken);

                foreach (var o in existingOpenings)
                {
                    o.IsOpeningDay = false;
                }
            }

            _context.Holidays.Add(holiday);
            await _context.SaveChangesAsync(cancellationToken);

            if (tx != null)
            {
                await tx.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            if (tx != null)
            {
                await tx.RollbackAsync(cancellationToken);
            }
            throw;
        }
    }

    public async Task UpdateHolidayAsync(Holiday holiday, CancellationToken cancellationToken = default)
    {
        if (holiday.To < holiday.From)
        {
            throw new ArgumentException("Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
        }

        var hasExistingTx = _context.Database.CurrentTransaction != null;
        IDbContextTransaction? tx = hasExistingTx ? null : await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            if (holiday.IsOpeningDay)
            {
                holiday.To = holiday.From;
                var otherOpenings = await _context.Holidays
                    .Where(h => h.IsOpeningDay && h.Id != holiday.Id)
                    .ToListAsync(cancellationToken);

                foreach (var o in otherOpenings)
                {
                    o.IsOpeningDay = false;
                }
            }
            else
            {
                var hasOtherOpening = await _context.Holidays
                    .AnyAsync(h => h.IsOpeningDay && h.Id != holiday.Id, cancellationToken);

                if (!hasOtherOpening)
                {
                    throw new InvalidOperationException("Hệ thống phải có đúng một ngày khai giảng. Vui lòng chỉ định ngày khai giảng khác trước khi bỏ chọn ngày này.");
                }
            }

            var tracked = _context.Holidays.Local.FirstOrDefault(h => h.Id == holiday.Id);
            if (tracked != null)
            {
                if (!ReferenceEquals(tracked, holiday))
                {
                    _context.Entry(tracked).CurrentValues.SetValues(holiday);
                }
            }
            else
            {
                _context.Holidays.Update(holiday);
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (tx != null)
            {
                await tx.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            if (tx != null)
            {
                await tx.RollbackAsync(cancellationToken);
            }
            throw;
        }
    }

    public async Task DeleteHolidayAsync(int id, CancellationToken cancellationToken = default)
    {
        var holiday = await _context.Holidays.FindAsync(new object[] { id }, cancellationToken);
        if (holiday == null)
        {
            return;
        }

        if (holiday.IsOpeningDay)
        {
            throw new InvalidOperationException("Không cho phép xóa ngày khai giảng duy nhất của khóa học.");
        }

        _context.Holidays.Remove(holiday);
        await _context.SaveChangesAsync(cancellationToken);
    }

    // ==========================================
    // AppSetting operations
    // ==========================================

    public async Task<string?> GetSettingAsync(string key, string? defaultValue = null, CancellationToken cancellationToken = default)
    {
        var setting = await _context.AppSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

        return setting?.Value ?? defaultValue;
    }

    public async Task SetSettingAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var tracked = _context.AppSettings.Local.FirstOrDefault(s => s.Key == key);
        if (tracked != null)
        {
            tracked.Value = value;
        }
        else
        {
            var existing = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
            if (existing != null)
            {
                existing.Value = value;
            }
            else
            {
                _context.AppSettings.Add(new AppSetting { Key = key, Value = value });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetAllSettingsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AppSettings
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);
    }
}
