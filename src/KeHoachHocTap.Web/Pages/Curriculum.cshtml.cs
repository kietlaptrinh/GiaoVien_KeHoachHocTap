using ClosedXML.Excel;
using KeHoachHocTap.Web.Data;
using KeHoachHocTap.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace KeHoachHocTap.Web.Pages;

public class CurriculumModel : PageModel
{
    private readonly PlanDbContext _context;
    private readonly ICurriculumReader _curriculumReader;
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<CurriculumModel> _logger;

    public CurriculumModel(
        PlanDbContext context,
        ICurriculumReader curriculumReader,
        IAuthorizationService authorizationService,
        ILogger<CurriculumModel> logger)
    {
        _context = context;
        _curriculumReader = curriculumReader;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    public IReadOnlyList<Subject> Subjects { get; set; } = Array.Empty<Subject>();

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? BlockFilter { get; set; }

    public IReadOnlyList<string> KnowledgeBlocks { get; set; } = Array.Empty<string>();

    public int TotalPeriods { get; set; }
    public int GradableCount { get; set; }
    public int PracticeCount { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class SubjectInputModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Tên học phần không được để trống.")]
        [StringLength(200, ErrorMessage = "Tên học phần tối đa 200 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số tiết không được để trống.")]
        [Range(1, 500, ErrorMessage = "Số tiết phải từ 1 đến 500 tiết.")]
        public int Periods { get; set; } = 30;

        [Required(ErrorMessage = "Hệ số không được để trống.")]
        [Range(0, 10, ErrorMessage = "Hệ số phải từ 0 đến 10.")]
        public decimal Coefficient { get; set; } = 1.0m;

        [StringLength(150, ErrorMessage = "Khối kiến thức tối đa 150 ký tự.")]
        public string? KnowledgeBlock { get; set; }

        [StringLength(100, ErrorMessage = "Ghi chú tối đa 100 ký tự.")]
        public string? Note { get; set; }

        public string ExamMode { get; set; } = "Immediately";
    }

    public async Task OnGetAsync()
    {
        var query = _context.Subjects.AsNoTracking().OrderBy(s => s.SortOrder).AsQueryable();

        var allSubjects = await query.ToListAsync();

        KnowledgeBlocks = allSubjects
            .Where(s => !string.IsNullOrWhiteSpace(s.KnowledgeBlock))
            .Select(s => s.KnowledgeBlock!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(b => b)
            .ToList();

        TotalPeriods = allSubjects.Sum(s => s.Periods);
        GradableCount = allSubjects.Count(s => s.Coefficient > 0);
        PracticeCount = allSubjects.Count(s => s.Coefficient == 0);

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var term = Search.Trim().ToLowerInvariant();
            query = query.Where(s => s.Name.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(BlockFilter))
        {
            query = query.Where(s => s.KnowledgeBlock == BlockFilter);
        }

        Subjects = await query.ToListAsync();
    }

    public async Task<IActionResult> OnPostAddOrUpdateAsync(SubjectInputModel input)
    {
        var auth = await _authorizationService.AuthorizeAsync(User, "Admin");
        if (!auth.Succeeded) return Forbid();

        if (!ModelState.IsValid)
        {
            ErrorMessage = "Dữ liệu nhập không hợp lệ.";
            return RedirectToPage();
        }

        try
        {
            if (input.Id.HasValue && input.Id.Value > 0)
            {
                var subject = await _context.Subjects.FindAsync(input.Id.Value);
                if (subject == null)
                {
                    ErrorMessage = "Không tìm thấy học phần để cập nhật.";
                    return RedirectToPage();
                }

                subject.Name = input.Name.Trim();
                subject.Periods = input.Periods;
                subject.Coefficient = input.Coefficient;
                subject.KnowledgeBlock = string.IsNullOrWhiteSpace(input.KnowledgeBlock) ? null : input.KnowledgeBlock.Trim();
                subject.Note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim();
                subject.ExamMode = input.ExamMode;

                await _context.SaveChangesAsync();
                SuccessMessage = $"Đã cập nhật học phần '{subject.Name}'.";
            }
            else
            {
                var maxSort = await _context.Subjects.MaxAsync(s => (int?)s.SortOrder) ?? 0;
                var subject = new Subject
                {
                    SortOrder = maxSort + 1,
                    Name = input.Name.Trim(),
                    Periods = input.Periods,
                    Coefficient = input.Coefficient,
                    KnowledgeBlock = string.IsNullOrWhiteSpace(input.KnowledgeBlock) ? null : input.KnowledgeBlock.Trim(),
                    Note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim(),
                    ExamMode = input.ExamMode
                };

                _context.Subjects.Add(subject);
                await _context.SaveChangesAsync();
                SuccessMessage = $"Đã thêm mới học phần '{subject.Name}'.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lưu học phần.");
            ErrorMessage = "Lỗi khi lưu học phần: " + ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var auth = await _authorizationService.AuthorizeAsync(User, "Admin");
        if (!auth.Succeeded) return Forbid();

        try
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject != null)
            {
                _context.Subjects.Remove(subject);
                await _context.SaveChangesAsync();

                // Đánh lại STT liên tục 1..n
                var remaining = await _context.Subjects.OrderBy(s => s.SortOrder).ToListAsync();
                for (int i = 0; i < remaining.Count; i++)
                {
                    remaining[i].SortOrder = i + 1;
                }
                await _context.SaveChangesAsync();

                SuccessMessage = $"Đã xóa học phần '{subject.Name}'.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa học phần.");
            ErrorMessage = "Lỗi khi xóa học phần: " + ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMoveAsync(int id, int direction)
    {
        var auth = await _authorizationService.AuthorizeAsync(User, "Admin");
        if (!auth.Succeeded) return Forbid();

        try
        {
            var subjects = await _context.Subjects.OrderBy(s => s.SortOrder).ToListAsync();
            var index = subjects.FindIndex(s => s.Id == id);
            if (index >= 0)
            {
                var targetIndex = index + direction;
                if (targetIndex >= 0 && targetIndex < subjects.Count)
                {
                    var temp = subjects[index].SortOrder;
                    subjects[index].SortOrder = subjects[targetIndex].SortOrder;
                    subjects[targetIndex].SortOrder = temp;

                    await _context.SaveChangesAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đổi thứ tự học phần.");
            ErrorMessage = "Lỗi đổi vị trí: " + ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostImportExcelAsync(IFormFile? file, bool replaceExisting = true)
    {
        var auth = await _authorizationService.AuthorizeAsync(User, "Admin");
        if (!auth.Succeeded) return Forbid();

        if (file == null || file.Length == 0)
        {
            ErrorMessage = "Vui lòng chọn file Excel khung chương trình (.xlsx).";
            return RedirectToPage();
        }

        try
        {
            using var stream = file.OpenReadStream();
            var result = _curriculumReader.Read(stream);

            if (result.Subjects.Count == 0)
            {
                ErrorMessage = "Không tìm thấy dữ liệu học phần hợp lệ trong file Excel.";
                return RedirectToPage();
            }

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                if (replaceExisting)
                {
                    // Xóa điểm học viên liên kết nếu có hoặc xóa môn
                    var existingGrades = await _context.StudentGrades.ToListAsync();
                    _context.StudentGrades.RemoveRange(existingGrades);

                    var existingSubjects = await _context.Subjects.ToListAsync();
                    _context.Subjects.RemoveRange(existingSubjects);
                    await _context.SaveChangesAsync();

                    for (int i = 0; i < result.Subjects.Count; i++)
                    {
                        result.Subjects[i].SortOrder = i + 1;
                    }
                    await _context.Subjects.AddRangeAsync(result.Subjects);
                }
                else
                {
                    var maxSort = await _context.Subjects.MaxAsync(s => (int?)s.SortOrder) ?? 0;
                    for (int i = 0; i < result.Subjects.Count; i++)
                    {
                        result.Subjects[i].SortOrder = maxSort + i + 1;
                    }
                    await _context.Subjects.AddRangeAsync(result.Subjects);
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                SuccessMessage = $"Đã nạp thành công {result.Subjects.Count} học phần vào Khung chương trình.";
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi nạp khung chương trình từ file Excel.");
            ErrorMessage = "Lỗi khi nạp file Excel: " + ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostLoadDefaultTemplateAsync()
    {
        var auth = await _authorizationService.AuthorizeAsync(User, "Admin");
        if (!auth.Succeeded) return Forbid();

        var filePath = @"D:\All-Project\GiaoVien_KeHoachHocTap\khung chuong trinh.xlsx";
        if (!System.IO.File.Exists(filePath))
        {
            ErrorMessage = "Không tìm thấy file mẫu khung chương trình tại máy chủ.";
            return RedirectToPage();
        }

        try
        {
            using var stream = System.IO.File.OpenRead(filePath);
            var result = _curriculumReader.Read(stream);

            if (result.Subjects.Count == 0)
            {
                ErrorMessage = "Không đọc được dữ liệu từ file mẫu.";
                return RedirectToPage();
            }

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // Xóa bảng điểm cũ liên quan
                var existingGrades = await _context.StudentGrades.ToListAsync();
                _context.StudentGrades.RemoveRange(existingGrades);

                var existingSubjects = await _context.Subjects.ToListAsync();
                _context.Subjects.RemoveRange(existingSubjects);
                await _context.SaveChangesAsync();

                for (int i = 0; i < result.Subjects.Count; i++)
                {
                    result.Subjects[i].SortOrder = i + 1;
                }
                await _context.Subjects.AddRangeAsync(result.Subjects);

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                SuccessMessage = $"Đã nạp thành công {result.Subjects.Count} học phần từ file mẫu chuẩn 'khung chuong trinh.xlsx'.";
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi nạp file mẫu khung chương trình.");
            ErrorMessage = "Lỗi nạp file mẫu: " + ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetExportExcelAsync()
    {
        var subjects = await _context.Subjects.AsNoTracking().OrderBy(s => s.SortOrder).ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Khung CT");

        ws.Cell(1, 1).Value = "Stt";
        ws.Cell(1, 2).Value = "Học phần";
        ws.Cell(1, 3).Value = "Số tiết";
        ws.Cell(1, 4).Value = "Hệ số";
        ws.Cell(1, 5).Value = "Khối kiến thức";
        ws.Cell(1, 6).Value = "Ghi chú";

        var headerRange = ws.Range(1, 1, 1, 6);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(235, 243, 252);
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        for (int i = 0; i < subjects.Count; i++)
        {
            int r = i + 2;
            var s = subjects[i];
            ws.Cell(r, 1).Value = s.SortOrder;
            ws.Cell(r, 2).Value = s.Name;
            ws.Cell(r, 3).Value = s.Periods;
            ws.Cell(r, 4).Value = s.Coefficient;
            ws.Cell(r, 5).Value = s.KnowledgeBlock ?? "";
            ws.Cell(r, 6).Value = s.Note ?? "";

            ws.Range(r, 1, r, 6).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(r, 1, r, 6).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        }

        ws.Columns().AdjustToContents();

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        return File(
            memoryStream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"KhungChuongTrinh_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }
}
