using KeHoachHocTap.Web.Data;

namespace KeHoachHocTap.Web.Domain;

/// <summary>
/// Kết quả xếp lịch chi tiết cho một hoạt động đào tạo (xen kẽ hoặc kết thúc khóa).
/// </summary>
/// <param name="Activity">Thực thể hoạt động.</param>
/// <param name="Start">Buổi/ngày bắt đầu.</param>
/// <param name="End">Buổi/ngày kết thúc.</param>
public record ScheduledActivity(PlanActivity Activity, Slot? Start, Slot? End);
