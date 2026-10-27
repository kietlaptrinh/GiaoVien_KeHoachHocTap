using KeHoachHocTap.Web.Export;
using KeHoachHocTap.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeHoachHocTap.Web.Pages;

[Authorize(Policy = "Admin")]
public class TemplatesModel : PageModel
{
    private readonly ITemplateService _templateService;

    public TemplatesModel(ITemplateService templateService)
    {
        _templateService = templateService;
    }

    public TemplateInfo CurrentTemplate { get; private set; } = null!;

    [BindProperty]
    public ExportOptions Options { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        CurrentTemplate = _templateService.GetCurrentTemplateInfo();
        Options = await _templateService.GetExportOptionsAsync();

        ViewData["StatusMessage"] = "Quản lý mẫu biểu Word và thông số nhiệm vụ";
    }

    public IActionResult OnGetDownloadCurrent()
    {
        try
        {
            var bytes = _templateService.GetCurrentTemplateBytes();
            var info = _templateService.GetCurrentTemplateInfo();
            return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", info.FileName);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể tải file mẫu: {ex.Message}";
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostUploadTemplateAsync(IFormFile? templateFile)
    {
        if (templateFile == null || templateFile.Length == 0)
        {
            ErrorMessage = "Vui lòng chọn một file Microsoft Word (.docx) để tải lên.";
            return RedirectToPage();
        }

        if (!templateFile.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
        {
            ErrorMessage = "Định dạng file không hợp lệ. Hệ thống chỉ chấp nhận file định dạng Microsoft Word (.docx).";
            return RedirectToPage();
        }

        const long maxSizeBytes = 10 * 1024 * 1024; // 10 MB theo SPEC mục 7.3
        if (templateFile.Length > maxSizeBytes)
        {
            ErrorMessage = "Dung lượng file vượt quá giới hạn cho phép (tối đa 10 MB).";
            return RedirectToPage();
        }

        await using var stream = templateFile.OpenReadStream();
        var (success, error) = await _templateService.SaveCustomTemplateAsync(stream);

        if (success)
        {
            SuccessMessage = "Tải lên và kích hoạt mẫu biểu Word tùy biến thành công! Các lần xuất file tiếp theo sẽ áp dụng mẫu biểu này.";
        }
        else
        {
            ErrorMessage = error ?? "File mẫu Word không hợp lệ hoặc không đáp ứng cấu trúc quy định.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRestoreDefaultAsync()
    {
        await _templateService.RestoreDefaultTemplateAsync();
        SuccessMessage = "Đã khôi phục thành công về file mẫu gốc mặc định của hệ thống.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSaveExtraTasksAsync()
    {
        if (!ModelState.IsValid)
        {
            CurrentTemplate = _templateService.GetCurrentTemplateInfo();
            return Page();
        }

        if (Options.SelfStudyPeriods < 0)
        {
            ErrorMessage = "Số tiết tự nghiên cứu phải là số không âm (>= 0).";
            return RedirectToPage();
        }

        if (Options.ExamPeriods < 0)
        {
            ErrorMessage = "Số tiết ôn thi / thi phải là số không âm (>= 0).";
            return RedirectToPage();
        }

        await _templateService.SaveExportOptionsAsync(Options);
        SuccessMessage = "Đã lưu và cập nhật cấu hình thông số nhiệm vụ biểu mẫu thành công vào cơ sở dữ liệu.";

        return RedirectToPage();
    }
}
