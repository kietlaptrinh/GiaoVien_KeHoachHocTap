namespace KeHoachHocTap.Web.Pages;

using System.ComponentModel.DataAnnotations;
using KeHoachHocTap.Web.Data;
using KeHoachHocTap.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class HolidaysModel : PageModel
{
    private readonly IPlanRepository _planRepository;
    private readonly IAdminAuthService _authService;
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<HolidaysModel> _logger;

    public HolidaysModel(
        IPlanRepository planRepository,
        IAdminAuthService authService,
        IAuthorizationService authorizationService,
        ILogger<HolidaysModel> logger)
    {
        _planRepository = planRepository;
        _authService = authService;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    public IReadOnlyList<Holiday> Holidays { get; private set; } = Array.Empty<Holiday>();
    public bool IsAdmin => User.Identity?.IsAuthenticated == true;
    public bool IsPasswordConfigured => _authService.IsPasswordConfigured();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class HolidayInputModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên ngày nghỉ/sự kiện không được để trống.")]
        [StringLength(200, ErrorMessage = "Tên không được vượt quá 200 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn từ ngày.")]
        public DateOnly From { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn đến ngày.")]
        public DateOnly To { get; set; }

        public bool IsOpeningDay { get; set; }
    }

    public async Task OnGetAsync()
    {
        Holidays = await _planRepository.GetHolidaysAsync();
        ViewData["TotalHolidays"] = Holidays.Count;
    }

    public async Task<IActionResult> OnPostAddHolidayAsync(HolidayInputModel input)
    {
        var auth = await _authorizationService.AuthorizeAsync(User, "Admin");
        if (!auth.Succeeded)
        {
            return Forbid();
        }

        if (!IsPasswordConfigured)
        {
            ErrorMessage = "Hệ thống đang ở chế độ chỉ xem do chưa thiết lập mật khẩu quản trị.";
            return RedirectToPage();
        }

        if (string.IsNullOrWhiteSpace(input.Name))
        {
            ErrorMessage = "Tên ngày nghỉ/sự kiện không được để trống.";
            return RedirectToPage();
        }

        if (input.IsOpeningDay)
        {
            input.To = input.From;
        }
        else if (input.To < input.From)
        {
            ErrorMessage = "Đến ngày phải lớn hơn hoặc bằng Từ ngày.";
            return RedirectToPage();
        }

        try
        {
            var holiday = new Holiday
            {
                Name = input.Name.Trim(),
                From = input.From,
                To = input.To,
                IsOpeningDay = input.IsOpeningDay
            };

            await _planRepository.AddHolidayAsync(holiday);
            SuccessMessage = $"Đã thêm thành công ngày nghỉ '{holiday.Name}'.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi thêm ngày nghỉ.");
            ErrorMessage = "Không thể thêm ngày nghỉ: " + ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateHolidayAsync(HolidayInputModel input)
    {
        var auth = await _authorizationService.AuthorizeAsync(User, "Admin");
        if (!auth.Succeeded)
        {
            return Forbid();
        }

        if (!IsPasswordConfigured)
        {
            ErrorMessage = "Hệ thống đang ở chế độ chỉ xem do chưa thiết lập mật khẩu quản trị.";
            return RedirectToPage();
        }

        if (string.IsNullOrWhiteSpace(input.Name))
        {
            ErrorMessage = "Tên ngày nghỉ/sự kiện không được để trống.";
            return RedirectToPage();
        }

        if (input.IsOpeningDay)
        {
            input.To = input.From;
        }
        else if (input.To < input.From)
        {
            ErrorMessage = "Đến ngày phải lớn hơn hoặc bằng Từ ngày.";
            return RedirectToPage();
        }

        try
        {
            var existing = await _planRepository.GetHolidayByIdAsync(input.Id);
            if (existing == null)
            {
                ErrorMessage = "Ngày nghỉ không tồn tại trong hệ thống.";
                return RedirectToPage();
            }

            existing.Name = input.Name.Trim();
            existing.From = input.From;
            existing.To = input.To;
            existing.IsOpeningDay = input.IsOpeningDay;

            await _planRepository.UpdateHolidayAsync(existing);
            SuccessMessage = $"Đã cập nhật thành công ngày nghỉ '{existing.Name}'.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật ngày nghỉ Id {Id}.", input.Id);
            ErrorMessage = "Không thể cập nhật ngày nghỉ: " + ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteHolidayAsync(int id)
    {
        var auth = await _authorizationService.AuthorizeAsync(User, "Admin");
        if (!auth.Succeeded)
        {
            return Forbid();
        }

        if (!IsPasswordConfigured)
        {
            ErrorMessage = "Hệ thống đang ở chế độ chỉ xem do chưa thiết lập mật khẩu quản trị.";
            return RedirectToPage();
        }

        try
        {
            var existing = await _planRepository.GetHolidayByIdAsync(id);
            if (existing == null)
            {
                ErrorMessage = "Ngày nghỉ không tồn tại hoặc đã bị xóa.";
                return RedirectToPage();
            }

            if (existing.IsOpeningDay)
            {
                ErrorMessage = "Không thể xóa ngày khai giảng duy nhất của khóa học.";
                return RedirectToPage();
            }

            var name = existing.Name;
            await _planRepository.DeleteHolidayAsync(id);
            SuccessMessage = $"Đã xóa ngày nghỉ '{name}' thành công.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa ngày nghỉ Id {Id}.", id);
            ErrorMessage = "Không thể xóa ngày nghỉ: " + ex.Message;
        }

        return RedirectToPage();
    }
}
