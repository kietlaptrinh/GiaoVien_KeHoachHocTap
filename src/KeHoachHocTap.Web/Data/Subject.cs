namespace KeHoachHocTap.Web.Data;

/// <summary>
/// Thực thể môn học được lưu trữ trong cơ sở dữ liệu.
/// </summary>
public class Subject
{
    public int Id { get; set; }

    /// <summary>
    /// Thứ tự sắp xếp của môn học (1..n).
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Tên môn học (bắt buộc, tối đa 200 ký tự).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Tổng số tiết học (đã bao gồm 10 tiết tự nghiên cứu, yêu cầu > 0).
    /// </summary>
    public int Periods { get; set; }

    /// <summary>
    /// Điểm tổng kết (thang điểm 0 - 10, tối đa 1 chữ số thập phân).
    /// </summary>
    public decimal? Grade { get; set; }

    /// <summary>
    /// Thời gian dự phòng (tiết), yêu cầu >= 0.
    /// </summary>
    public int? ReservePeriods { get; set; }

    /// <summary>
    /// Ghi chú (tối đa 100 ký tự).
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Chế độ thi: "Immediately" (thi ngay sau khi học xong), "Delayed" (hoãn thi, học tiếp môn sau), "Custom" (chỉ định).
    /// </summary>
    public string ExamMode { get; set; } = "Immediately";

    /// <summary>
    /// Buổi thi chỉ định hoặc ghi chú thi dồn khi ExamMode là "Custom".
    /// </summary>
    public string? CustomExamSlot { get; set; }

    /// <summary>
    /// Hệ số học phần (1 cho môn lý thuyết thường, 0 cho thực tế, 2 cho thi tốt nghiệp). Mặc định là 1.0.
    /// </summary>
    public decimal Coefficient { get; set; } = 1.0m;

    /// <summary>
    /// Khối kiến thức của học phần trong khung chương trình (tối đa 150 ký tự).
    /// </summary>
    public string? KnowledgeBlock { get; set; }
}
