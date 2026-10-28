using KeHoachHocTap.Web.Data;
using KeHoachHocTap.Web.Domain;

namespace KeHoachHocTap.Web.Services;

/// <summary>
/// Giao diện dịch vụ quản lý và tính toán bảng điểm tổng hợp học viên.
/// </summary>
public interface IGradeService
{
    /// <summary>
    /// Tính toán ma trận bảng điểm tổng hợp của lớp học:
    /// Đếm số môn hoàn thành, tính điểm trung bình (ĐTB), chuyển đổi ĐTB sang chữ tiếng Việt, và xếp loại học lực.
    /// </summary>
    GradeSheetResult CalculateGradeSheet(IReadOnlyList<Subject> subjects, IReadOnlyList<Student> students);
}
