namespace KeHoachHocTap.Web.Export;

/// <summary>
/// Cấu hình cho nhiệm vụ đặc biệt (Khai giảng, Thi tốt nghiệp, Bế giảng).
/// </summary>
public class ExtraTaskConfig
{
    /// <summary>
    /// Tên hiển thị của nhiệm vụ (vd: "Thi tốt nghiệp", "Bế giảng").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Số tiết quy định (vd: 15, 5).
    /// </summary>
    public int Periods { get; set; }

    /// <summary>
    /// Buổi / ngày bắt đầu (vd: "S 22/02/2026", "S 20/4/2026").
    /// </summary>
    public string Start { get; set; } = string.Empty;

    /// <summary>
    /// Buổi / ngày kết thúc (vd: "S 24/02/2026" hoặc để trống).
    /// </summary>
    public string End { get; set; } = string.Empty;
}

/// <summary>
/// Tùy chọn cấu hình khi xuất biểu mẫu Word hoặc các định dạng khác.
/// </summary>
public class ExportOptions
{
    /// <summary>
    /// Đường dẫn file mẫu Word (.docx).
    /// </summary>
    public string? TemplatePath { get; set; }

    /// <summary>
    /// Dữ liệu nhị phân của file mẫu nếu đã nạp sẵn vào bộ nhớ.
    /// </summary>
    public byte[]? TemplateBytes { get; set; }

    /// <summary>
    /// Số tiết tự nghiên cứu mặc định (mặc định 10).
    /// </summary>
    public int SelfStudyPeriods { get; set; } = 10;

    /// <summary>
    /// Số tiết ôn thi / thi mặc định (mặc định 5).
    /// </summary>
    public int ExamPeriods { get; set; } = 5;

    /// <summary>
    /// Cấu hình nhiệm vụ "Thi tốt nghiệp".
    /// </summary>
    public ExtraTaskConfig Graduation { get; set; } = new()
    {
        Name = "Thi tốt nghiệp",
        Periods = 15,
        Start = "S 22/02/2026",
        End = "S 24/02/2026"
    };

    /// <summary>
    /// Cấu hình nhiệm vụ "Bế giảng".
    /// </summary>
    public ExtraTaskConfig Closing { get; set; } = new()
    {
        Name = "Bế giảng",
        Periods = 5,
        Start = "S 20/4/2026",
        End = ""
    };
}
