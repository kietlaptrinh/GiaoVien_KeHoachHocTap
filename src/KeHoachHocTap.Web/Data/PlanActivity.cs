namespace KeHoachHocTap.Web.Data;

/// <summary>
/// Loại hoạt động đào tạo trong kế hoạch.
/// </summary>
public enum PlanActivityType
{
    /// <summary>
    /// Hoạt động xen kẽ giữa các môn (sau môn học AfterSubjectId).
    /// </summary>
    Interim = 0,

    /// <summary>
    /// Chuỗi hoạt động sau môn cuối cùng (Ôn thi tốt nghiệp, Thi tốt nghiệp, Họp lớp, Bế giảng).
    /// </summary>
    Closing = 1
}

/// <summary>
/// Thực thể hoạt động đào tạo (xen kẽ hoặc kết thúc khóa) trong kế hoạch.
/// </summary>
public class PlanActivity
{
    public int Id { get; set; }

    /// <summary>
    /// Tên hoạt động (bắt buộc, tối đa 200 ký tự).
    /// Ví dụ: "Đi nghiên cứu thực tế tại cơ sở", "Nghỉ viết bài thu hoạch môn học", "Ôn thi tốt nghiệp", "Họp lớp", "Bế giảng".
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Loại hoạt động: Interim (xen kẽ sau môn) hoặc Closing (chuỗi sau môn cuối).
    /// </summary>
    public PlanActivityType Type { get; set; } = PlanActivityType.Interim;

    /// <summary>
    /// ID môn học đứng trước (FK với Subject). Null nếu là Closing sau môn cuối.
    /// </summary>
    public int? AfterSubjectId { get; set; }

    /// <summary>
    /// Thứ tự sắp xếp (1..n).
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Ngày bắt đầu (nếu chỉ định khoảng ngày thực tế).
    /// </summary>
    public DateOnly? FromDate { get; set; }

    /// <summary>
    /// Ngày kết thúc (nếu chỉ định khoảng ngày thực tế, ToDate >= FromDate).
    /// </summary>
    public DateOnly? ToDate { get; set; }

    /// <summary>
    /// Số buổi (nếu tính theo số buổi học).
    /// </summary>
    public int? Sessions { get; set; }

    /// <summary>
    /// Số tiết (nếu có, ví dụ 15 tiết thi tốt nghiệp, 5 tiết bế giảng).
    /// </summary>
    public int? Periods { get; set; }

    /// <summary>
    /// Ghi chú hoạt động (tối đa 500 ký tự).
    /// </summary>
    public string? Note { get; set; }
}
