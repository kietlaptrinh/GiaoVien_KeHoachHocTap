using KeHoachHocTap.Web.Data;
using KeHoachHocTap.Web.Domain;

namespace KeHoachHocTap.Web.Services;

/// <summary>
/// Dịch vụ trung tâm cung cấp kế hoạch học tập, tính toán lịch học hoàn chỉnh từ dữ liệu database.
/// </summary>
public interface IPlanService
{
    /// <summary>
    /// Lấy kết quả xếp lịch hoàn chỉnh cho toàn bộ môn học dựa trên cơ sở dữ liệu hiện tại.
    /// </summary>
    /// <param name="cancellationToken">CancellationToken.</param>
    /// <returns>Kết quả tính toán lịch học bao gồm ngày khai giảng, các kỳ nghỉ, chi tiết từng môn và cảnh báo.</returns>
    Task<ScheduleResult> GetScheduleAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách môn học đã được sắp xếp theo SortOrder.
    /// </summary>
    Task<IReadOnlyList<Subject>> GetSubjectsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách các khoảng ngày nghỉ.
    /// </summary>
    Task<IReadOnlyList<Holiday>> GetHolidaysAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin ngày khai giảng khóa học.
    /// </summary>
    Task<Holiday?> GetOpeningDayAsync(CancellationToken cancellationToken = default);
}
