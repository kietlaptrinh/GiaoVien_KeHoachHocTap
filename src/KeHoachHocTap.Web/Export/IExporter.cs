using KeHoachHocTap.Web.Domain;

namespace KeHoachHocTap.Web.Export;

/// <summary>
/// Giao diện chuẩn cho các bộ xuất file kế hoạch giảng dạy và học tập (SPEC mục 7).
/// </summary>
public interface IExporter
{
    /// <summary>
    /// Mã định danh duy nhất của bộ xuất file (vd: "docx-bieumau").
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Tên hiển thị người dùng (vd: "Biểu mẫu Kế hoạch giảng dạy và học tập (.docx)").
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Phần mở rộng file có dấu chấm (vd: ".docx").
    /// </summary>
    string Extension { get; }

    /// <summary>
    /// Content-Type MIME type chuẩn.
    /// </summary>
    string ContentType { get; }

    /// <summary>
    /// Sinh file kết quả trong bộ nhớ từ ScheduleResult và cấu hình ExportOptions.
    /// </summary>
    /// <param name="result">Kết quả tính toán lịch học.</param>
    /// <param name="options">Cấu hình xuất file và nhiệm vụ đặc biệt.</param>
    /// <returns>Mảng byte dữ liệu file sinh ra.</returns>
    byte[] Export(ScheduleResult result, ExportOptions options);
}
