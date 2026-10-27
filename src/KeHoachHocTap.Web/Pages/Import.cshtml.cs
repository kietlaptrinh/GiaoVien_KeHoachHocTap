namespace KeHoachHocTap.Web.Pages;

using System.ComponentModel.DataAnnotations;
using KeHoachHocTap.Web.Data;
using KeHoachHocTap.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

[Authorize(Policy = "Admin")]
public class ImportModel : PageModel
{
    private readonly IExcelPlanReader _excelReader;
    private readonly IPlanImporter _planImporter;
    private readonly IAdminAuthService _authService;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<ImportModel> _logger;

    public ImportModel(
        IExcelPlanReader excelReader,
        IPlanImporter planImporter,
        IAdminAuthService authService,
        IMemoryCache memoryCache,
        ILogger<ImportModel> logger)
    {
        _excelReader = excelReader;
        _planImporter = planImporter;
        _authService = authService;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public bool IsPasswordConfigured => _authService.IsPasswordConfigured();

    [BindProperty]
    public IFormFile? ExcelFile { get; set; }

    public bool IsPreview { get; private set; }
    public string? ImportTicket { get; private set; }
    public IReadOnlyList<Subject> PreviewSubjects { get; private set; } = Array.Empty<Subject>();
    public IReadOnlyList<Holiday> PreviewHolidays { get; private set; } = Array.Empty<Holiday>();
    public Holiday? PreviewOpeningDay { get; private set; }
    public IReadOnlyList<string> PreviewWarnings { get; private set; } = Array.Empty<string>();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
        IsPreview = false;
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
            ModelState.AddModelError(string.Empty, "Vui lòng chọn một file Excel (.xlsx) để tải lên.");
            return Page();
        }

        // SPEC mục 6.2: Giới hạn dung lượng <= 5 MB
        const long maxFileSize = 5 * 1024 * 1024;
        if (ExcelFile.Length > maxFileSize)
        {
            ModelState.AddModelError(string.Empty, "Dung lượng file vượt quá giới hạn cho phép (tối đa 5 MB).");
            return Page();
        }

        var extension = Path.GetExtension(ExcelFile.FileName).ToLowerInvariant();
        if (extension != ".xlsx")
        {
            ModelState.AddModelError(string.Empty, "Định dạng file không hợp lệ. Chỉ chấp nhận file Excel đuôi .xlsx.");
            return Page();
        }

        try
        {
            // Đọc hoàn toàn trong MemoryStream, tuyệt đối không ghi ra đĩa server (SPEC mục 3.3, 13)
            using var memoryStream = new MemoryStream();
            await ExcelFile.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            memoryStream.Position = 0;
            var planData = _excelReader.Read(memoryStream);

            if (planData.Subjects.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "File Excel không chứa bất kỳ môn học hợp lệ nào tại sheet 'Khung CT'.");
                return Page();
            }

            var opening = planData.Holidays.FirstOrDefault(h => h.IsOpeningDay);
            if (opening == null)
            {
                ModelState.AddModelError(string.Empty, "File Excel không có thông tin ngày khai giảng tại sheet 'Ngay nghi'.");
                return Page();
            }

            // Lưu fileBytes vào MemoryCache trong 15 phút để chờ người dùng xác nhận
            var ticket = Guid.NewGuid().ToString("N");
            _memoryCache.Set($"import_{ticket}", fileBytes, TimeSpan.FromMinutes(15));

            ImportTicket = ticket;
            PreviewSubjects = planData.Subjects;
            PreviewHolidays = planData.Holidays;
            PreviewOpeningDay = opening;
            PreviewWarnings = planData.Warnings;
            IsPreview = true;

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đọc file Excel upload.");
            ModelState.AddModelError(string.Empty, "Không thể đọc file Excel: " + ex.Message);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostConfirmImportAsync(string importTicket, bool preserveExistingData = true)
    {
        if (!IsPasswordConfigured)
        {
            ErrorMessage = "Hệ thống đang ở chế độ chỉ xem do chưa thiết lập mật khẩu quản trị.";
            return RedirectToPage();
        }

        if (string.IsNullOrWhiteSpace(importTicket) ||
            !_memoryCache.TryGetValue($"import_{importTicket}", out byte[]? fileBytes) ||
            fileBytes == null)
        {
            ErrorMessage = "Phiên làm việc nhập file đã hết hạn (quá 15 phút) hoặc không tìm thấy dữ liệu. Vui lòng tải lại file.";
            return RedirectToPage();
        }

        try
        {
            using var stream = new MemoryStream(fileBytes);
            var result = await _planImporter.ImportAsync(stream, preserveExistingData);

            // Xóa file khỏi cache sau khi xử lý xong
            _memoryCache.Remove($"import_{importTicket}");

            if (result.Success)
            {
                SuccessMessage = $"Nhập dữ liệu thành công! Đã nạp {result.SubjectsImported} môn học và {result.HolidaysImported} ngày nghỉ vào cơ sở dữ liệu.";
                _logger.LogInformation("Nhập dữ liệu Excel thành công: {Subjects} môn học, {Holidays} ngày nghỉ.",
                    result.SubjectsImported, result.HolidaysImported);
                return RedirectToPage("/Index");
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Nhập dữ liệu thất bại. Cơ sở dữ liệu đã được hoàn tác an toàn.";
                return RedirectToPage();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi nghiêm trọng khi thực hiện import dữ liệu Excel.");
            ErrorMessage = "Lỗi khi nhập dữ liệu vào cơ sở dữ liệu: " + ex.Message;
            return RedirectToPage();
        }
    }
}
