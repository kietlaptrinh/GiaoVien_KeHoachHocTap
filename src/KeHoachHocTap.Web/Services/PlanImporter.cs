using KeHoachHocTap.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KeHoachHocTap.Web.Services;

/// <summary>
/// Triển khai dịch vụ nhập dữ liệu kế hoạch học tập từ file Excel vào cơ sở dữ liệu.
/// </summary>
public class PlanImporter : IPlanImporter
{
    private readonly PlanDbContext _context;
    private readonly IExcelPlanReader _excelPlanReader;
    private readonly ILogger<PlanImporter> _logger;

    public PlanImporter(
        PlanDbContext context,
        IExcelPlanReader excelPlanReader,
        ILogger<PlanImporter> logger)
    {
        _context = context;
        _excelPlanReader = excelPlanReader;
        _logger = logger;
    }

    public async Task<PlanImportResult> ImportAsync(
        Stream excelStream,
        bool preserveExistingData = true,
        CancellationToken cancellationToken = default)
    {
        ExcelPlanReadResult readResult;
        try
        {
            readResult = _excelPlanReader.Read(excelStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi phân tích nội dung file Excel.");
            return new PlanImportResult(0, 0, Array.Empty<string>(), Success: false, ErrorMessage: $"Không thể đọc file Excel: {ex.Message}");
        }

        if (readResult.Subjects.Count == 0)
        {
            return new PlanImportResult(0, 0, readResult.Warnings, Success: false, ErrorMessage: "File Excel không chứa dữ liệu môn học hợp lệ.");
        }

        if (!readResult.Holidays.Any(h => h.IsOpeningDay))
        {
            return new PlanImportResult(0, 0, readResult.Warnings, Success: false, ErrorMessage: "Không tìm thấy ngày khai giảng trong sheet 'Ngay nghi'.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            Dictionary<string, Subject>? oldSubjectsDict = null;
            if (preserveExistingData)
            {
                var existingSubjects = await _context.Subjects
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                oldSubjectsDict = existingSubjects
                    .GroupBy(s => s.Name.Trim().ToLowerInvariant())
                    .ToDictionary(g => g.Key, g => g.First());
            }

            // Xóa toàn bộ môn học và ngày nghỉ cũ trong DB
            var currentSubjects = await _context.Subjects.ToListAsync(cancellationToken);
            var currentHolidays = await _context.Holidays.ToListAsync(cancellationToken);

            _context.Subjects.RemoveRange(currentSubjects);
            _context.Holidays.RemoveRange(currentHolidays);
            await _context.SaveChangesAsync(cancellationToken);

            // Thêm danh sách ngày nghỉ mới
            foreach (var h in readResult.Holidays)
            {
                _context.Holidays.Add(new Holiday
                {
                    Name = h.Name,
                    From = h.From,
                    To = h.To,
                    IsOpeningDay = h.IsOpeningDay
                });
            }

            // Thêm danh sách môn học mới và bảo toàn dữ liệu nếu cần
            int sortOrder = 1;
            foreach (var s in readResult.Subjects)
            {
                var newSub = new Subject
                {
                    SortOrder = sortOrder++,
                    Name = s.Name,
                    Periods = s.Periods,
                    Grade = s.Grade,
                    ReservePeriods = s.ReservePeriods,
                    Note = s.Note
                };

                if (preserveExistingData && oldSubjectsDict != null)
                {
                    var key = s.Name.Trim().ToLowerInvariant();
                    if (oldSubjectsDict.TryGetValue(key, out var oldSub))
                    {
                        newSub.Grade = oldSub.Grade;
                        newSub.ReservePeriods = oldSub.ReservePeriods;
                        newSub.Note = oldSub.Note;
                    }
                }

                _context.Subjects.Add(newSub);
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Đã import thành công {Subjects} môn học và {Holidays} ngày nghỉ vào cơ sở dữ liệu.",
                readResult.Subjects.Count,
                readResult.Holidays.Count);

            return new PlanImportResult(
                readResult.Subjects.Count,
                readResult.Holidays.Count,
                readResult.Warnings,
                Success: true);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Lỗi khi lưu dữ liệu kế hoạch học tập vào cơ sở dữ liệu. Đã rollback toàn bộ thay đổi.");
            return new PlanImportResult(0, 0, readResult.Warnings, Success: false, ErrorMessage: $"Lỗi khi lưu vào cơ sở dữ liệu: {ex.Message}");
        }
    }
}
