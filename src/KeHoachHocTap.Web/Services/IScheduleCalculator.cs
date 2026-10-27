using KeHoachHocTap.Web.Data;
using KeHoachHocTap.Web.Domain;

namespace KeHoachHocTap.Web.Services;

/// <summary>
/// Giao diện dịch vụ tính toán lịch học thuần túy (Pure Function).
/// </summary>
public interface IScheduleCalculator
{
    /// <summary>
    /// Xếp lịch học cho danh sách môn học dựa trên danh sách ngày nghỉ và ngày khai giảng.
    /// </summary>
    /// <param name="subjects">Danh sách môn học.</param>
    /// <param name="holidays">Danh sách ngày nghỉ và ngày khai giảng.</param>
    /// <returns>Kết quả xếp lịch chi tiết và các cảnh báo phát sinh.</returns>
    ScheduleResult Calculate(IEnumerable<Subject> subjects, IEnumerable<Holiday> holidays);
}
