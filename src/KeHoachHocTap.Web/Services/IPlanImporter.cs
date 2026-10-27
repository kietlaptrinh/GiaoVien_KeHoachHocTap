namespace KeHoachHocTap.Web.Services;

/// <summary>
/// Kết quả của quá trình nạp dữ liệu từ file Excel vào cơ sở dữ liệu.
/// </summary>
/// <param name="SubjectsImported">Số lượng môn học đã được nạp thành công.</param>
/// <param name="HolidaysImported">Số lượng khoảng nghỉ/sự kiện đã nạp thành công.</param>
/// <param name="Warnings">Danh sách cảnh báo phát sinh khi đọc dữ liệu.</param>
/// <param name="Success">Trạng thái thành công của quá trình nạp.</param>
/// <param name="ErrorMessage">Thông báo lỗi chi tiết nếu quá trình thất bại.</param>
public record PlanImportResult(
    int SubjectsImported,
    int HolidaysImported,
    IReadOnlyList<string> Warnings,
    bool Success = true,
    string? ErrorMessage = null);

/// <summary>
/// Dịch vụ nhập dữ liệu kế hoạch học tập từ file Excel vào cơ sở dữ liệu trong một Transaction an toàn.
/// </summary>
public interface IPlanImporter
{
    /// <summary>
    /// Nhập dữ liệu môn học và ngày nghỉ từ stream file Excel vào database.
    /// </summary>
    /// <param name="excelStream">Stream chứa nội dung file Excel .xlsx.</param>
    /// <param name="preserveExistingData">Nếu true, giữ lại Điểm, Dự phòng và Ghi chú của môn học cũ nếu trùng tên.</param>
    /// <param name="cancellationToken">CancellationToken.</param>
    /// <returns>Kết quả thực hiện import.</returns>
    Task<PlanImportResult> ImportAsync(
        Stream excelStream,
        bool preserveExistingData = true,
        CancellationToken cancellationToken = default);
}
