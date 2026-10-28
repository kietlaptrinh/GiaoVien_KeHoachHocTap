namespace KeHoachHocTap.Web.Data;

/// <summary>
/// Thực thể thông tin học viên trong lớp học.
/// </summary>
public class Student
{
    public int Id { get; set; }

    /// <summary>
    /// Thứ tự hiển thị trong danh sách lớp (1..n).
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Mã số học viên (bắt buộc, tối đa 50 ký tự, vd: "K75A56.0101").
    /// </summary>
    public string StudentCode { get; set; } = string.Empty;

    /// <summary>
    /// Họ và tên đệm (bắt buộc, tối đa 100 ký tự, vd: "Đặng Thị Hội").
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Tên học viên (bắt buộc, tối đa 50 ký tự, vd: "An").
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Ngày sinh (vd: 14/04/1982).
    /// </summary>
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>
    /// Giới tính (Nam/Nữ, tối đa 10 ký tự).
    /// </summary>
    public string? Gender { get; set; }

    /// <summary>
    /// Ghi chú (tối đa 200 ký tự).
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Danh sách điểm các môn của học viên.
    /// </summary>
    public ICollection<StudentGrade> Grades { get; set; } = new List<StudentGrade>();

    /// <summary>
    /// Họ và tên đầy đủ của học viên.
    /// </summary>
    public string FullName => $"{LastName} {FirstName}".Trim();
}
