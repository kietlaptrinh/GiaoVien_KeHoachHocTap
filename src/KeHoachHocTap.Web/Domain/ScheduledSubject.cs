using KeHoachHocTap.Web.Data;

namespace KeHoachHocTap.Web.Domain;

/// <summary>
/// Môn học đã được xếp lịch với thông tin buổi bắt đầu, kết thúc và thi.
/// </summary>
/// <param name="Subject">Thực thể môn học tương ứng.</param>
/// <param name="Number">Số thứ tự hiển thị (1..n).</param>
/// <param name="Sessions">Số buổi học (1 buổi = 5 tiết).</param>
/// <param name="Start">Buổi bắt đầu học.</param>
/// <param name="End">Buổi kết thúc học.</param>
/// <param name="Exam">Buổi thi hết môn (ngay sau buổi học cuối).</param>
public record ScheduledSubject(
    Subject Subject,
    int Number,
    int Sessions,
    Slot Start,
    Slot End,
    Slot Exam);
