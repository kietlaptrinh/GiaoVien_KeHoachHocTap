namespace KeHoachHocTap.Web.Services;

/// <summary>
/// Giao diện bộ phân loại và xếp loại học lực của học viên.
/// </summary>
public interface IGradeRankClassifier
{
    /// <summary>
    /// Xếp loại học lực căn cứ theo Điểm trung bình (ĐTB) và điểm các môn thành phần.
    /// - Xuất sắc: ĐTB >= 9.0 và không có môn < 7.0
    /// - Giỏi: ĐTB >= 8.0 và không có môn < 6.0
    /// - Khá: ĐTB >= 7.0 và không có môn < 5.0
    /// - Trung bình: ĐTB >= 5.0 và không có môn < 5.0
    /// - Yếu: ĐTB < 5.0 hoặc có môn thi lại/chưa đạt (< 5.0)
    /// - Chưa có điểm: "Chưa xếp loại"
    /// </summary>
    string Classify(decimal? averageScore, IEnumerable<decimal?> subjectScores);
}
