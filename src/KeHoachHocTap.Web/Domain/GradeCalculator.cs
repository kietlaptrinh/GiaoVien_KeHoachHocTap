namespace KeHoachHocTap.Web.Domain;

using KeHoachHocTap.Web.Data;

/// <summary>
/// Kết quả thống kê số tiết và điểm trung bình của danh sách môn học.
/// </summary>
/// <param name="TotalPeriods">Tổng số tiết học của toàn bộ môn.</param>
/// <param name="TotalSessions">Tổng số buổi học (mỗi buổi 5 tiết, làm tròn lên).</param>
/// <param name="GradedSubjectCount">Số lượng môn đã có điểm.</param>
/// <param name="SimpleAverage">Điểm trung bình cộng các môn đã có điểm.</param>
/// <param name="WeightedAverage">Điểm trung bình theo trọng số số tiết của các môn đã có điểm.</param>
public record GradeSummary(
    int TotalPeriods,
    int TotalSessions,
    int GradedSubjectCount,
    decimal? SimpleAverage,
    decimal? WeightedAverage);

/// <summary>
/// Thuật toán thuần tính toán điểm trung bình và tổng hợp số liệu môn học.
/// </summary>
public static class GradeCalculator
{
    /// <summary>
    /// Tính toán điểm trung bình cộng (chỉ tính các môn đã có điểm).
    /// Trả về null nếu không có môn nào có điểm.
    /// </summary>
    public static decimal? CalculateSimpleAverage(IEnumerable<Subject> subjects)
    {
        var graded = subjects.Where(s => s.Grade.HasValue).ToList();
        if (graded.Count == 0)
        {
            return null;
        }

        var sum = graded.Sum(s => s.Grade!.Value);
        var avg = sum / graded.Count;
        return Math.Round(avg, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Tính toán điểm trung bình theo số tiết (trọng số theo Periods của các môn đã có điểm).
    /// Trọng số: Sum(Grade * Periods) / Sum(Periods).
    /// Trả về null nếu không có môn nào có điểm.
    /// </summary>
    public static decimal? CalculateWeightedAverage(IEnumerable<Subject> subjects)
    {
        var graded = subjects.Where(s => s.Grade.HasValue && s.Periods > 0).ToList();
        if (graded.Count == 0)
        {
            return null;
        }

        decimal totalWeightedGrade = 0m;
        int totalGradedPeriods = 0;

        foreach (var s in graded)
        {
            totalWeightedGrade += s.Grade!.Value * s.Periods;
            totalGradedPeriods += s.Periods;
        }

        if (totalGradedPeriods == 0)
        {
            return null;
        }

        var avg = totalWeightedGrade / totalGradedPeriods;
        return Math.Round(avg, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Tính toán tổng kết toàn diện cho danh sách môn học.
    /// </summary>
    public static GradeSummary Calculate(IEnumerable<Subject> subjects)
    {
        var list = subjects.ToList();
        var totalPeriods = list.Sum(s => s.Periods);
        var totalSessions = list.Sum(s => (int)Math.Ceiling(s.Periods / 5.0m));
        var gradedCount = list.Count(s => s.Grade.HasValue);
        var simpleAvg = CalculateSimpleAverage(list);
        var weightedAvg = CalculateWeightedAverage(list);

        return new GradeSummary(totalPeriods, totalSessions, gradedCount, simpleAvg, weightedAvg);
    }

    /// <summary>
    /// Định dạng điểm số hiển thị: nếu null hoặc không có trả về "—", có giá trị trả về định dạng 1 hoặc 2 chữ số thập phân.
    /// </summary>
    public static string FormatGrade(decimal? grade, string format = "0.0#")
    {
        return grade.HasValue ? grade.Value.ToString(format) : "—";
    }
}
