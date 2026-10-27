using KeHoachHocTap.Web.Domain;
using KeHoachHocTap.Web.Export;

namespace KeHoachHocTap.Web.Services;

/// <summary>
/// Thông tin chi tiết về file mẫu Word hiện hành.
/// </summary>
public record TemplateInfo(
    string FileName,
    bool IsCustom,
    long FileSizeBytes,
    DateTime LastModified,
    string FullPath);

/// <summary>
/// Dịch vụ quản lý template Word và cấu hình nhiệm vụ đặc biệt (SPEC mục 7.3).
/// </summary>
public interface ITemplateService
{
    /// <summary>
    /// Lấy thông tin về template hiện hành đang được kích hoạt.
    /// </summary>
    TemplateInfo GetCurrentTemplateInfo();

    /// <summary>
    /// Lấy dữ liệu nhị phân của template hiện hành (tùy biến hoặc mặc định).
    /// </summary>
    byte[] GetCurrentTemplateBytes();

    /// <summary>
    /// Kiểm tra tính hợp lệ cấu trúc OpenXml của file mẫu tải lên.
    /// </summary>
    (bool IsValid, string? ErrorMessage) ValidateTemplate(Stream stream);

    /// <summary>
    /// Lưu template tùy biến mới sau khi đã kiểm tra hợp lệ.
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> SaveCustomTemplateAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Khôi phục về file mẫu gốc mặc định ban đầu.
    /// </summary>
    Task RestoreDefaultTemplateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy cấu hình ExportOptions từ database (AppSetting) ưu tiên hơn appsettings.json.
    /// </summary>
    Task<ExportOptions> GetExportOptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lưu cấu hình ExportOptions vào database (AppSetting).
    /// </summary>
    Task SaveExportOptionsAsync(ExportOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra và trả về danh sách cảnh báo nếu ngày của Thi tốt nghiệp / Bế giảng rơi vào ngày nghỉ hoặc cuối tuần.
    /// </summary>
    IReadOnlyList<string> GetExtraTaskWarnings(ScheduleResult result, ExportOptions options);
}
