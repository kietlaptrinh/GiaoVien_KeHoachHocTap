using KeHoachHocTap.Web.Data;

namespace KeHoachHocTap.Web.Domain;

/// <summary>
/// Kết quả xếp lịch tổng thể của toàn bộ khóa học.
/// </summary>
/// <param name="OpeningDay">Ngày khai giảng khóa học.</param>
/// <param name="Holidays">Danh sách các kỳ nghỉ đã áp dụng.</param>
/// <param name="Items">Danh sách các môn học kèm lịch chi tiết.</param>
/// <param name="Activities">Danh sách các hoạt động xen kẽ và kết thúc khóa kèm lịch chi tiết.</param>
/// <param name="Warnings">Danh sách cảnh báo phát sinh trong quá trình tính toán.</param>
public record ScheduleResult(
    DateOnly OpeningDay,
    IReadOnlyList<Holiday> Holidays,
    IReadOnlyList<ScheduledSubject> Items,
    IReadOnlyList<ScheduledActivity> Activities,
    IReadOnlyList<string> Warnings)
{
    /// <summary>
    /// Khởi tạo tương thích ngược cho các lời gọi không truyền Activities.
    /// </summary>
    public ScheduleResult(
        DateOnly openingDay,
        IReadOnlyList<Holiday> holidays,
        IReadOnlyList<ScheduledSubject> items,
        IReadOnlyList<string> warnings)
        : this(openingDay, holidays, items, Array.Empty<ScheduledActivity>(), warnings)
    {
    }
}
