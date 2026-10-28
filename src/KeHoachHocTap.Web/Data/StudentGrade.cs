namespace KeHoachHocTap.Web.Data;

/// <summary>
/// Thực thể điểm môn học của một học viên.
/// </summary>
public class StudentGrade
{
    public int Id { get; set; }

    /// <summary>
    /// Khóa ngoại liên kết tới học viên.
    /// </summary>
    public int StudentId { get; set; }

    /// <summary>
    /// Tham chiếu tới thực thể học viên.
    /// </summary>
    public Student? Student { get; set; }

    /// <summary>
    /// Khóa ngoại liên kết tới môn học trong khung chương trình.
    /// </summary>
    public int SubjectId { get; set; }

    /// <summary>
    /// Tham chiếu tới thực thể môn học.
    /// </summary>
    public Subject? Subject { get; set; }

    /// <summary>
    /// Điểm tổng kết hệ 10 (0.00 – 10.00), làm tròn tối đa 2 chữ số thập phân.
    /// </summary>
    public decimal? Score { get; set; }

    /// <summary>
    /// Ghi chú riêng cho điểm số (vd: "Thi lại", "Hoãn thi", v.v.).
    /// </summary>
    public string? Note { get; set; }
}
