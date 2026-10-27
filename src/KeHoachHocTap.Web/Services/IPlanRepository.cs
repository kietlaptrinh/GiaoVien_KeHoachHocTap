using KeHoachHocTap.Web.Data;

namespace KeHoachHocTap.Web.Services;

/// <summary>
/// Giao diện repository quản lý dữ liệu Môn học, Ngày nghỉ và Cấu hình hệ thống.
/// </summary>
public interface IPlanRepository
{
    // ==========================================
    // Subject operations
    // ==========================================

    /// <summary>
    /// Lấy toàn bộ danh sách môn học theo thứ tự SortOrder tăng dần.
    /// </summary>
    Task<IReadOnlyList<Subject>> GetSubjectsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin môn học theo Id.
    /// </summary>
    Task<Subject?> GetSubjectByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Thêm một môn học mới. Nếu SortOrder không xác định (<= 0), sẽ đặt ở cuối danh sách.
    /// </summary>
    Task AddSubjectAsync(Subject subject, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật thông tin môn học.
    /// </summary>
    Task UpdateSubjectAsync(Subject subject, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa môn học và tự động đánh lại SortOrder liên tục (1..n) trong 1 transaction.
    /// </summary>
    Task DeleteSubjectAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đổi chỗ vị trí môn học với môn liền kề (direction: -1 lên trên, +1 xuống dưới) trong 1 transaction.
    /// </summary>
    Task MoveSubjectAsync(int subjectId, int direction, CancellationToken cancellationToken = default);

    // ==========================================
    // Holiday operations
    // ==========================================

    /// <summary>
    /// Lấy toàn bộ danh sách ngày nghỉ được sắp xếp theo thời gian bắt đầu.
    /// </summary>
    Task<IReadOnlyList<Holiday>> GetHolidaysAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin ngày nghỉ theo Id.
    /// </summary>
    Task<Holiday?> GetHolidayByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin ngày khai giảng khóa học (IsOpeningDay = true).
    /// </summary>
    Task<Holiday?> GetOpeningDayAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Thêm một ngày nghỉ mới. Nếu đánh dấu là ngày khai giảng, tự động hủy đánh dấu dòng cũ.
    /// </summary>
    Task AddHolidayAsync(Holiday holiday, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật thông tin ngày nghỉ. Luôn đảm bảo hệ thống có đúng 1 ngày khai giảng.
    /// </summary>
    Task UpdateHolidayAsync(Holiday holiday, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa một ngày nghỉ. Không cho phép xóa ngày khai giảng duy nhất.
    /// </summary>
    Task DeleteHolidayAsync(int id, CancellationToken cancellationToken = default);

    // ==========================================
    // AppSetting operations
    // ==========================================

    /// <summary>
    /// Lấy giá trị cấu hình theo khóa. Nếu không tồn tại, trả về defaultValue.
    /// </summary>
    Task<string?> GetSettingAsync(string key, string? defaultValue = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lưu hoặc cập nhật một giá trị cấu hình theo khóa.
    /// </summary>
    Task SetSettingAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy toàn bộ danh sách cấu hình hệ thống dưới dạng Dictionary.
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> GetAllSettingsAsync(CancellationToken cancellationToken = default);
}
