namespace KeHoachHocTap.Web.Pages;

using System.ComponentModel.DataAnnotations;
using KeHoachHocTap.Web.Data;
using KeHoachHocTap.Web.Domain;
using KeHoachHocTap.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class IndexModel : PageModel
{
    private readonly IPlanService _planService;
    private readonly IPlanRepository _planRepository;
    private readonly IAdminAuthService _authService;
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IPlanService planService,
        IPlanRepository planRepository,
        IAdminAuthService authService,
        IAuthorizationService authorizationService,
        ILogger<IndexModel> logger)
    {
        _planService = planService;
        _planRepository = planRepository;
        _authService = authService;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    public ScheduleResult? Schedule { get; private set; }
    public IReadOnlyList<ScheduledSubject> Items => Schedule?.Items ?? Array.Empty<ScheduledSubject>();
    public IReadOnlyList<ScheduledActivity> Activities => Schedule?.Activities ?? Array.Empty<ScheduledActivity>();
    public GradeSummary Summary { get; private set; } = new(0, 0, 0, null, null);
    public IReadOnlyList<string> Warnings => Schedule?.Warnings ?? Array.Empty<string>();

    public bool IsAdmin => User.Identity?.IsAuthenticated == true;
    public bool IsPasswordConfigured => _authService.IsPasswordConfigured();
    public string? DatabaseError { get; private set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class ActivityInputModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên hoạt động không được để trống.")]
        [StringLength(200, ErrorMessage = "Tên hoạt động tối đa 200 ký tự.")]
        public string Name { get; set; } = string.Empty;

        public PlanActivityType Type { get; set; } = PlanActivityType.Interim;

        public int? AfterSubjectId { get; set; }

        public DateOnly? FromDate { get; set; }

        public DateOnly? ToDate { get; set; }

        [Range(1, 1000, ErrorMessage = "Số buổi phải lớn hơn 0.")]
        public int? Sessions { get; set; }

        [Range(1, 10000, ErrorMessage = "Số tiết phải lớn hơn 0.")]
        public int? Periods { get; set; }

        [StringLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự.")]
        public string? Note { get; set; }
    }

    public class SubjectInputModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên môn học không được để trống.")]
        [StringLength(200, ErrorMessage = "Tên môn học tối đa 200 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [Range(1, 10000, ErrorMessage = "Số tiết học phải là số nguyên lớn hơn 0.")]
        public int Periods { get; set; }

        [Range(0, 10, ErrorMessage = "Điểm phải nằm trong khoảng từ 0 đến 10.")]
        public decimal? Grade { get; set; }

        [Range(0, 10000, ErrorMessage = "Số tiết dự phòng phải lớn hơn hoặc bằng 0.")]
        public int? ReservePeriods { get; set; }

        [StringLength(100, ErrorMessage = "Ghi chú tối đa 100 ký tự.")]
        public string? Note { get; set; }

        [StringLength(50, ErrorMessage = "Chế độ thi tối đa 50 ký tự.")]
        public string ExamMode { get; set; } = "Immediately";

        [StringLength(100, ErrorMessage = "Buổi thi tùy chỉnh tối đa 100 ký tự.")]
        public string? CustomExamSlot { get; set; }
    }

    public async Task OnGetAsync()
    {
        try
        {
            Schedule = await _planService.GetScheduleAsync();
            var subjects = Schedule.Items.Select(item => item.Subject).ToList();
            Summary = GradeCalculator.Calculate(subjects);
            ViewData["TotalSubjects"] = subjects.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tải kế hoạch học tập từ cơ sở dữ liệu.");
            DatabaseError = "Không kết nối được cơ sở dữ liệu. Vui lòng kiểm tra lại dịch vụ SQL Server và kết nối hệ thống.";
            ViewData["TotalSubjects"] = 0;
        }
    }

    public async Task<IActionResult> OnPostAddSubjectAsync(SubjectInputModel input)
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

        if (!ValidateSubjectInput(input, out var validationError))
        {
            ErrorMessage = validationError;
            return RedirectToPage();
        }

        try
        {
            var subject = new Subject
            {
                Name = input.Name.Trim(),
                Periods = input.Periods,
                Grade = input.Grade,
                ReservePeriods = input.ReservePeriods,
                Note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim(),
                ExamMode = string.IsNullOrWhiteSpace(input.ExamMode) ? "Immediately" : input.ExamMode.Trim(),
                CustomExamSlot = string.IsNullOrWhiteSpace(input.CustomExamSlot) ? null : input.CustomExamSlot.Trim(),
                SortOrder = 0 // Repository sẽ tự động xếp vào cuối
            };

            await _planRepository.AddSubjectAsync(subject);
            SuccessMessage = $"Đã thêm thành công môn học '{subject.Name}'.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi thêm môn học mới.");
            ErrorMessage = "Không thể thêm môn học vào cơ sở dữ liệu: " + ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateSubjectAsync(SubjectInputModel input)
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

        if (!ValidateSubjectInput(input, out var validationError))
        {
            ErrorMessage = validationError;
            return RedirectToPage();
        }

        try
        {
            var existing = await _planRepository.GetSubjectByIdAsync(input.Id);
            if (existing == null)
            {
                ErrorMessage = "Môn học không tồn tại trong hệ thống.";
                return RedirectToPage();
            }

            existing.Name = input.Name.Trim();
            existing.Periods = input.Periods;
            if (input.Grade.HasValue)
            {
                existing.Grade = input.Grade;
            }
            existing.ReservePeriods = input.ReservePeriods;
            existing.Note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim();
            existing.ExamMode = string.IsNullOrWhiteSpace(input.ExamMode) ? "Immediately" : input.ExamMode.Trim();
            existing.CustomExamSlot = string.IsNullOrWhiteSpace(input.CustomExamSlot) ? null : input.CustomExamSlot.Trim();

            await _planRepository.UpdateSubjectAsync(existing);
            SuccessMessage = $"Đã cập nhật thành công môn học '{existing.Name}'.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật môn học Id {Id}.", input.Id);
            ErrorMessage = "Không thể cập nhật môn học: " + ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteSubjectAsync(int id)
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
            var existing = await _planRepository.GetSubjectByIdAsync(id);
            if (existing == null)
            {
                ErrorMessage = "Môn học không tồn tại hoặc đã bị xóa.";
                return RedirectToPage();
            }

            var name = existing.Name;
            await _planRepository.DeleteSubjectAsync(id);
            SuccessMessage = $"Đã xóa môn học '{name}' thành công.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa môn học Id {Id}.", id);
            ErrorMessage = "Không thể xóa môn học: " + ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMoveSubjectAsync(int id, int direction)
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
            await _planRepository.MoveSubjectAsync(id, direction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đổi thứ tự môn học Id {Id}.", id);
            ErrorMessage = "Không thể đổi vị trí môn học: " + ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAddActivityAsync(ActivityInputModel input)
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
            ErrorMessage = "Tên hoạt động không được để trống.";
            return RedirectToPage();
        }

        try
        {
            var isClosing = !input.AfterSubjectId.HasValue || input.AfterSubjectId.Value <= 0;
            var activity = new PlanActivity
            {
                Name = input.Name.Trim(),
                Type = isClosing ? PlanActivityType.Closing : PlanActivityType.Interim,
                AfterSubjectId = isClosing ? null : input.AfterSubjectId,
                FromDate = input.FromDate,
                ToDate = input.ToDate,
                Sessions = input.Sessions,
                Periods = input.Periods,
                Note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim()
            };

            await _planRepository.AddActivityAsync(activity);
            SuccessMessage = $"Đã thêm hoạt động '{activity.Name}' thành công.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi thêm hoạt động.");
            ErrorMessage = "Không thể thêm hoạt động: " + ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateActivityAsync(ActivityInputModel input)
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
            ErrorMessage = "Tên hoạt động không được để trống.";
            return RedirectToPage();
        }

        try
        {
            var existing = await _planRepository.GetActivityByIdAsync(input.Id);
            if (existing == null)
            {
                ErrorMessage = "Hoạt động không tồn tại.";
                return RedirectToPage();
            }

            var isClosing = !input.AfterSubjectId.HasValue || input.AfterSubjectId.Value <= 0;
            existing.Name = input.Name.Trim();
            existing.Type = isClosing ? PlanActivityType.Closing : PlanActivityType.Interim;
            existing.AfterSubjectId = isClosing ? null : input.AfterSubjectId;
            existing.FromDate = input.FromDate;
            existing.ToDate = input.ToDate;
            existing.Sessions = input.Sessions;
            existing.Periods = input.Periods;
            existing.Note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim();

            await _planRepository.UpdateActivityAsync(existing);
            SuccessMessage = $"Đã cập nhật hoạt động '{existing.Name}' thành công.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật hoạt động Id {Id}.", input.Id);
            ErrorMessage = "Không thể cập nhật hoạt động: " + ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteActivityAsync(int id)
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
            var existing = await _planRepository.GetActivityByIdAsync(id);
            if (existing != null)
            {
                var name = existing.Name;
                await _planRepository.DeleteActivityAsync(id);
                SuccessMessage = $"Đã xóa hoạt động '{name}' thành công.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa hoạt động Id {Id}.", id);
            ErrorMessage = "Không thể xóa hoạt động: " + ex.Message;
        }

        return RedirectToPage();
    }

    private static bool ValidateSubjectInput(SubjectInputModel input, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            errorMessage = "Tên môn học không được để trống.";
            return false;
        }

        if (input.Periods <= 0)
        {
            errorMessage = "Số tiết học phải là số nguyên lớn hơn 0.";
            return false;
        }

        if (input.Grade.HasValue)
        {
            if (input.Grade.Value < 0 || input.Grade.Value > 10)
            {
                errorMessage = "Điểm phải nằm trong khoảng từ 0 đến 10.";
                return false;
            }

            // SPEC mục 6: Thang 10, tối đa 1 chữ số thập phân
            if (decimal.Round(input.Grade.Value, 1) != input.Grade.Value)
            {
                errorMessage = "Điểm chỉ được phép có tối đa 1 chữ số thập phân (ví dụ: 8.5).";
                return false;
            }
        }

        if (input.ReservePeriods.HasValue && input.ReservePeriods.Value < 0)
        {
            errorMessage = "Số tiết dự phòng phải lớn hơn hoặc bằng 0.";
            return false;
        }

        if (input.Note?.Length > 100)
        {
            errorMessage = "Ghi chú không được vượt quá 100 ký tự.";
            return false;
        }

        if (input.CustomExamSlot?.Length > 100)
        {
            errorMessage = "Buổi thi tùy chỉnh không được vượt quá 100 ký tự.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    public async Task<IActionResult> OnPostResetOrderFromCurriculumAsync(CancellationToken cancellationToken)
    {
        var auth = await _authorizationService.AuthorizeAsync(User, "Admin");
        if (!auth.Succeeded) return Forbid();

        try
        {
            var subjects = await _planRepository.GetSubjectsAsync(cancellationToken);
            var sortedByCurriculum = subjects.OrderBy(s => s.CurriculumOrder > 0 ? s.CurriculumOrder : s.SortOrder).ToList();
            for (int i = 0; i < sortedByCurriculum.Count; i++)
            {
                sortedByCurriculum[i].SortOrder = i + 1;
                await _planRepository.UpdateSubjectAsync(sortedByCurriculum[i], cancellationToken);
            }
            SuccessMessage = "Đã đặt lại thứ tự môn học trong Kế hoạch giảng dạy theo đúng Khung chương trình chuẩn.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đồng bộ thứ tự giảng dạy theo Khung CT.");
            ErrorMessage = "Lỗi khi đồng bộ thứ tự: " + ex.Message;
        }

        return RedirectToPage();
    }
}
