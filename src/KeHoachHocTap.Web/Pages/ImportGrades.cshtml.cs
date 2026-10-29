namespace KeHoachHocTap.Web.Pages;

using KeHoachHocTap.Web.Data;
using KeHoachHocTap.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

[Authorize(Policy = "Admin")]
public class ImportGradesModel : PageModel
{
    private readonly IExcelGradeImporter _gradeImporter;
    private readonly IPlanRepository _repository;
    private readonly IAdminAuthService _authService;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<ImportGradesModel> _logger;

    public ImportGradesModel(
        IExcelGradeImporter gradeImporter,
        IPlanRepository repository,
        IAdminAuthService authService,
        IMemoryCache memoryCache,
        ILogger<ImportGradesModel> logger)
    {
        _gradeImporter = gradeImporter;
        _repository = repository;
        _authService = authService;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public bool IsPasswordConfigured => _authService.IsPasswordConfigured();

    [BindProperty]
    public IFormFile? ExcelFile { get; set; }

    [BindProperty]
    public bool PreserveExisting { get; set; } = false;

    public bool IsPreview { get; private set; }
    public string? ImportTicket { get; private set; }
    public IReadOnlyList<string> PreviewSubjectNames { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<ImportedStudent> PreviewStudents { get; private set; } = Array.Empty<ImportedStudent>();
    public IReadOnlyList<string> PreviewWarnings { get; private set; } = Array.Empty<string>();
    public int MatchedSubjectsCount { get; private set; }
    public int TotalStudentsInDb { get; private set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        IsPreview = false;
        TotalStudentsInDb = await GetTotalStudentsCountAsync();
    }

    private async Task<int> GetTotalStudentsCountAsync()
    {
        var students = await _repository.GetStudentsWithGradesAsync();
        return students.Count;
    }

    public async Task<IActionResult> OnPostPreviewAsync()
    {
        if (!IsPasswordConfigured)
        {
            ErrorMessage = "Hệ thống đang ở chế độ chỉ xem do chưa thiết lập mật khẩu quản trị.";
            return RedirectToPage();
        }

        if (ExcelFile == null || ExcelFile.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Vui lòng chọn một file Excel (.xlsx, .xls) để tải lên.");
            TotalStudentsInDb = await GetTotalStudentsCountAsync();
            return Page();
        }

        // Giới hạn dung lượng <= 5 MB
        const long maxFileSize = 5 * 1024 * 1024;
        if (ExcelFile.Length > maxFileSize)
        {
            ModelState.AddModelError(string.Empty, "Dung lượng file vượt quá giới hạn cho phép (tối đa 5 MB).");
            TotalStudentsInDb = await GetTotalStudentsCountAsync();
            return Page();
        }

        var extension = Path.GetExtension(ExcelFile.FileName).ToLowerInvariant();
        if (extension != ".xlsx" && extension != ".xls")
        {
            ModelState.AddModelError(string.Empty, "Định dạng file không hợp lệ. Chỉ chấp nhận file Excel đuôi .xlsx hoặc .xls.");
            TotalStudentsInDb = await GetTotalStudentsCountAsync();
            return Page();
        }

        try
        {
            using var memoryStream = new MemoryStream();
            await ExcelFile.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var importResult = _gradeImporter.ReadGradeSheet(memoryStream);

            if (importResult.Students.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Không tìm thấy dữ liệu học viên hợp lệ trong file Excel tải lên. Vui lòng kiểm tra định dạng cột.");
                TotalStudentsInDb = await GetTotalStudentsCountAsync();
                return Page();
            }

            var dbSubjects = await _repository.GetSubjectsAsync();
            var dbSubjectMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in dbSubjects)
            {
                dbSubjectMap[s.Name.Trim()] = s.Id;
            }

            // Kiểm tra số môn khớp
            int matched = 0;
            var warnings = new List<string>(importResult.Warnings);
            foreach (var subName in importResult.SubjectNames)
            {
                if (dbSubjectMap.ContainsKey(subName.Trim()))
                {
                    matched++;
                }
                else
                {
                    warnings.Add($"Môn học '{subName}' trong file Excel không trùng khớp với môn nào trong Kế hoạch đào tạo hiện tại.");
                }
            }

            // Lưu dữ liệu vào MemoryCache an toàn (15 phút)
            var ticket = Guid.NewGuid().ToString("N");
            var cacheData = new GradeImportCacheData(importResult, PreserveExisting);
            _memoryCache.Set(ticket, cacheData, TimeSpan.FromMinutes(15));

            ImportTicket = ticket;
            PreviewSubjectNames = importResult.SubjectNames;
            PreviewStudents = importResult.Students;
            PreviewWarnings = warnings;
            MatchedSubjectsCount = matched;
            TotalStudentsInDb = await GetTotalStudentsCountAsync();
            IsPreview = true;

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đọc file Excel bảng điểm.");
            ModelState.AddModelError(string.Empty, "Không thể đọc dữ liệu file Excel: " + ex.Message);
            TotalStudentsInDb = await GetTotalStudentsCountAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostConfirmImportAsync(string importTicket, bool preserveExistingData = false)
    {
        if (!IsPasswordConfigured)
        {
            ErrorMessage = "Hệ thống đang ở chế độ chỉ xem do chưa thiết lập mật khẩu quản trị.";
            return RedirectToPage("/Grades");
        }

        if (string.IsNullOrEmpty(importTicket) || !_memoryCache.TryGetValue(importTicket, out GradeImportCacheData? cacheData) || cacheData == null)
        {
            ErrorMessage = "Phiên xem trước đã hết hạn hoặc không tồn tại. Vui lòng tải lại file Excel.";
            return RedirectToPage();
        }

        try
        {
            var dbSubjects = await _repository.GetSubjectsAsync();
            var dbSubjectMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in dbSubjects)
            {
                dbSubjectMap[s.Name.Trim()] = s.Id;
            }

            var studentsToAdd = new List<Student>();
            foreach (var imp in cacheData.Result.Students)
            {
                var st = new Student
                {
                    SortOrder = imp.Stt,
                    StudentCode = imp.StudentCode,
                    LastName = imp.LastName,
                    FirstName = imp.FirstName,
                    DateOfBirth = imp.DateOfBirth,
                    Gender = imp.Gender
                };

                foreach (var g in imp.Grades)
                {
                    if (dbSubjectMap.TryGetValue(g.SubjectName.Trim(), out var subId))
                    {
                        st.Grades.Add(new StudentGrade
                        {
                            SubjectId = subId,
                            Score = g.Score
                        });
                    }
                }
                studentsToAdd.Add(st);
            }

            await _repository.ImportStudentsAndGradesAsync(studentsToAdd, preserveExisting: preserveExistingData);
            _memoryCache.Remove(importTicket);

            SuccessMessage = $"Đã nhập thành công {studentsToAdd.Count} học viên và điểm từ file Excel vào hệ thống.";
            return RedirectToPage("/Grades");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xác nhận lưu bảng điểm vào cơ sở dữ liệu.");
            ErrorMessage = "Lỗi khi lưu bảng điểm vào cơ sở dữ liệu: " + ex.Message;
            return RedirectToPage();
        }
    }

    public record GradeImportCacheData(ExcelGradeImportResult Result, bool PreserveExisting);
}
