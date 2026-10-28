namespace KeHoachHocTap.Web.Services;

/// <summary>
/// Bộ phân loại học lực theo quy chế đào tạo chuẩn.
/// </summary>
public class GradeRankClassifier : IGradeRankClassifier
{
    public const string RankXuatSac = "Xuất sắc";
    public const string RankGioi = "Giỏi";
    public const string RankKha = "Khá";
    public const string RankTrungBinh = "Trung bình";
    public const string RankYeu = "Yếu";
    public const string RankChuaXepLoai = "Chưa xếp loại";

    public string Classify(decimal? averageScore, IEnumerable<decimal?> subjectScores)
    {
        if (!averageScore.HasValue)
        {
            return RankChuaXepLoai;
        }

        var scoresList = subjectScores?.Where(s => s.HasValue).Select(s => s!.Value).ToList();
        if (scoresList == null || scoresList.Count == 0)
        {
            return RankChuaXepLoai;
        }

        var avg = averageScore.Value;
        var minScore = scoresList.Min();

        // Nếu có môn thi lại/chưa đạt (< 5.0) hoặc ĐTB < 5.0 => Xếp loại Yếu
        if (minScore < 5.0m || avg < 5.0m)
        {
            return RankYeu;
        }

        if (avg >= 9.0m)
        {
            if (minScore >= 7.0m)
            {
                return RankXuatSac;
            }
            if (minScore >= 6.0m)
            {
                return RankGioi;
            }
            return RankKha;
        }

        if (avg >= 8.0m)
        {
            if (minScore >= 6.0m)
            {
                return RankGioi;
            }
            return RankKha;
        }

        if (avg >= 7.0m)
        {
            return RankKha;
        }

        return RankTrungBinh;
    }
}
