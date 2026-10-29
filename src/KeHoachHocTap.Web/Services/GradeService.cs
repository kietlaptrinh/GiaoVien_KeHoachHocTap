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
        // Bảng điểm tổng hợp luôn thống nhất sắp xếp theo Khung chương trình (CurriculumOrder)
        var sortedSubjects = (subjects ?? Array.Empty<Subject>())
            .OrderBy(s => s.CurriculumOrder > 0 ? s.CurriculumOrder : s.SortOrder)
            .ThenBy(s => s.Id)
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

            var scoredSubjectPairs = sortedSubjects
                .Where(s => studentGradesDict.TryGetValue(s.Id, out var sc) && sc.HasValue)
                .Select(s => new { Subject = s, Score = studentGradesDict[s.Id]!.Value })
                .ToList();

            var completedCount = scoredSubjectPairs.Count;
            decimal? avgScore = null;
            if (completedCount > 0)
            {
                var totalWeight = scoredSubjectPairs.Where(p => p.Subject.Coefficient > 0).Sum(p => p.Subject.Coefficient);
                if (totalWeight > 0)
                {
                    var weightedSum = scoredSubjectPairs.Where(p => p.Subject.Coefficient > 0).Sum(p => p.Score * p.Subject.Coefficient);
                    avgScore = Math.Round(weightedSum / totalWeight, 2, MidpointRounding.AwayFromZero);
                }
                else
                {
                    // Fallback nếu tất cả các môn đều có Hệ số = 0
                    var sum = scoredSubjectPairs.Sum(p => p.Score);
                    avgScore = Math.Round(sum / completedCount, 2, MidpointRounding.AwayFromZero);
                }
            }

            var words = _wordsConverter.ConvertScoreToWords(avgScore);
            // Chỉ xét điểm liệt cho các môn có tính điểm (Hệ số > 0)
            var gradableScores = sortedSubjects
                .Where(s => s.Coefficient > 0)
                .Select(s => studentGradesDict.TryGetValue(s.Id, out var sc) ? sc : null);

            var classification = _classifier.Classify(avgScore, gradableScores);

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
