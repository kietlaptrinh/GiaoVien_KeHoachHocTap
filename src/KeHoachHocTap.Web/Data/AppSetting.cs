namespace KeHoachHocTap.Web.Data;

/// <summary>
/// Thực thể cấu hình hệ thống lưu trữ động trong cơ sở dữ liệu.
/// </summary>
public class AppSetting
{
    /// <summary>
    /// Khóa cấu hình (khóa chính, tối đa 100 ký tự).
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Giá trị cấu hình.
    /// </summary>
    public string Value { get; set; } = string.Empty;
}
