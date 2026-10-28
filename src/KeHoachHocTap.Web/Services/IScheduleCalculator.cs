using KeHoachHocTap.Web.Data;
using KeHoachHocTap.Web.Domain;
using Half = KeHoachHocTap.Web.Domain.Half;

namespace KeHoachHocTap.Web.Services;

/// <summary>
/// Giao diện dịch vụ tính toán lịch học thuần túy (Pure Function).
/// </summary>
public interface IScheduleCalculator
{
    /// <summary>
    /// Xếp lịch học cho danh sách môn học dựa trên danh sách ngày nghỉ, ngày khai giảng, buổi bắt đầu môn đầu tiên và danh sách hoạt động đào tạo.
    /// </summary>
    /// <param name="subjects">Danh sách môn học.</param>
    /// <param name="holidays">Danh sách ngày nghỉ và ngày khai giảng.</param>
    /// <param name="firstSessionHalf">Buổi học đầu tiên của môn 1 (mặc định là Half.S - Sáng).</param>
    /// <param name="activities">Danh sách hoạt động đào tạo xen kẽ và hoạt động kết thúc khóa (nếu có).</param>
    /// <returns>Kết quả xếp lịch chi tiết và các cảnh báo phát sinh.</returns>
    ScheduleResult Calculate(
        IEnumerable<Subject> subjects,
        IEnumerable<Holiday> holidays,
        Half firstSessionHalf = Half.S,
        IReadOnlyList<PlanActivity>? activities = null);
}
