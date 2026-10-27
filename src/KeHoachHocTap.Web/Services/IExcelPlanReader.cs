using KeHoachHocTap.Web.Data;

namespace KeHoachHocTap.Web.Services;

/// <summary>
/// Kết quả đọc dữ liệu từ file Excel kế hoạch học tập.
/// </summary>
/// <param name="Subjects">Danh sách môn học trích xuất được.</param>
/// <param name="Holidays">Danh sách các kỳ nghỉ và ngày khai giảng.</param>
/// <param name="Warnings">Các cảnh báo trong quá trình đọc file.</param>
public record ExcelPlanReadResult(
    IReadOnlyList<Subject> Subjects,
    IReadOnlyList<Holiday> Holidays,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Giao diện dịch vụ đọc file Excel kế hoạch học tập.
/// </summary>
public interface IExcelPlanReader
{
    /// <summary>
    /// Đọc dữ liệu môn học và ngày nghỉ từ stream file Excel (xử lý hoàn toàn trong bộ nhớ).
    /// </summary>
    /// <param name="stream">Stream dữ liệu file Excel .xlsx.</param>
    /// <returns>Kết quả đọc dữ liệu.</returns>
    ExcelPlanReadResult Read(Stream stream);
}
