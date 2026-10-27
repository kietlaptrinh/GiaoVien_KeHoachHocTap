namespace KeHoachHocTap.Web.Data;

/// <summary>
/// Thực thể ngày nghỉ / sự kiện đặc biệt (Khai giảng, Lễ, Tết).
/// </summary>
public class Holiday
{
    public int Id { get; set; }

    /// <summary>
    /// Tên ngày nghỉ (tối đa 200 ký tự).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Ngày bắt đầu nghỉ (bao gồm cả ngày này).
    /// </summary>
    public DateOnly From { get; set; }

    /// <summary>
    /// Ngày kết thúc nghỉ (bao gồm cả ngày này, To >= From).
    /// </summary>
    public DateOnly To { get; set; }

    /// <summary>
    /// Đánh dấu là ngày khai giảng khóa học (đúng 1 dòng = true, From == To).
    /// </summary>
    public bool IsOpeningDay { get; set; }
}
