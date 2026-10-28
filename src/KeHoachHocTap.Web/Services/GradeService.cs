using KeHoachHocTap.Web.Data;
using KeHoachHocTap.Web.Domain;

namespace KeHoachHocTap.Web.Services;

/// <summary>
/// Dịch vụ tính toán bảng điểm tổng hợp học viên và thống kê xếp loại.
/// </summary>
public class GradeService : IGradeService
{
    private readonly IVietnameseNumberToWordsConverter _wordsConverter;
    private readonly IGradeRankClassifier _classifier;

    public GradeService(
        IVietnameseNumberToWordsConverter wordsConverter,
        IGradeRankClassifier classifier)
    {
        _wordsConverter = wordsConverter;
        _classifier = classifier;
    }

    public GradeSheetResult CalculateGradeSheet(IReadOnlyList<Subject> subjects, IReadOnlyList<Student> students)
    {
        var sortedSubjects = (subjects ?? Array.Empty<Subject>())
            .OrderBy(s => s.SortOrder)
            .ToList();

        var sortedStudents = (students ?? Array.Empty<Student>())
            .OrderBy(s => s.SortOrder)
            .ToList();

        var summaries = new List<StudentSummary>();

        var stats = new Dictionary<string, int>
        {
            [GradeRankClassifier.RankXuatSac] = 0,
            [GradeRankClassifier.RankGioi] = 0,
            [GradeRankClassifier.RankKha] = 0,
            [GradeRankClassifier.RankTrungBinh] = 0,
            [GradeRankClassifier.RankYeu] = 0,
            [GradeRankClassifier.RankChuaXepLoai] = 0
        };

        for (var i = 0; i < sortedStudents.Count; i++)
        {
            var student = sortedStudents[i];
            var stt = i + 1;

            var studentGradesDict = student.Grades?
                .Where(g => g != null)
                .ToDictionary(g => g.SubjectId, g => g.Score)
                ?? new Dictionary<int, decimal?>();

            var items = new List<StudentGradeItem>();
            foreach (var subject in sortedSubjects)
            {
                studentGradesDict.TryGetValue(subject.Id, out var score);
                items.Add(new StudentGradeItem(subject.Id, subject.Name, score));
            }

            var validScores = items
                .Where(item => item.Score.HasValue)
                .Select(item => item.Score!.Value)
                .ToList();

            var completedCount = validScores.Count;
            decimal? avgScore = null;
            if (completedCount > 0)
            {
                var sum = validScores.Sum();
                avgScore = Math.Round(sum / completedCount, 2, MidpointRounding.AwayFromZero);
            }

            var words = _wordsConverter.ConvertScoreToWords(avgScore);
            var classification = _classifier.Classify(avgScore, items.Select(item => item.Score));

            if (stats.ContainsKey(classification))
            {
                stats[classification]++;
            }
            else
            {
                stats[classification] = 1;
            }

            summaries.Add(new StudentSummary(
                student,
                stt,
                items,
                completedCount,
                avgScore,
                words,
                classification));
        }

        return new GradeSheetResult(
            sortedSubjects,
            summaries,
            sortedStudents.Count,
            stats);
    }
}
