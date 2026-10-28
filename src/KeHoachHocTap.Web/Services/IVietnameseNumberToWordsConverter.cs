namespace KeHoachHocTap.Web.Services;

/// <summary>
/// Giao diện dịch vụ chuyển đổi số điểm sang chữ tiếng Việt chuẩn học bạ và văn bằng đào tạo.
/// </summary>
public interface IVietnameseNumberToWordsConverter
{
    /// <summary>
    /// Chuyển đổi điểm số (thang điểm 10) sang chuỗi chữ tiếng Việt chuẩn.
    /// Ví dụ: 7.81 -> "Bảy phẩy tám mươi mốt", 8.00 -> "Tám phẩy không", null -> "—".
    /// </summary>
    /// <param name="score">Điểm số cần chuyển đổi.</param>
    /// <param name="fullTwoDigits">Nếu true, số tròn chục ở phần thập phân sẽ đọc đầy đủ (vd: 7.60 -> "Bảy phẩy sáu mươi"). Mặc định false (vd: 7.60 -> "Bảy phẩy sáu").</param>
    /// <returns>Chuỗi tiếng Việt viết hoa chữ cái đầu tiên hoặc "—" nếu null.</returns>
    string ConvertScoreToWords(decimal? score, bool fullTwoDigits = false);
}
