using KeHoachHocTap.Web.Data;
using KeHoachHocTap.Web.Services;
using Xunit;

namespace KeHoachHocTap.Tests;

public class GradeServiceTests
{
    private readonly IVietnameseNumberToWordsConverter _wordsConverter = new VietnameseNumberToWordsConverter();
    private readonly IGradeRankClassifier _classifier = new GradeRankClassifier();
    private readonly GradeService _gradeService;

    public GradeServiceTests()
    {
        _gradeService = new GradeService(_wordsConverter, _classifier);
    }

    [Theory]
    [InlineData(9.2, new double[] { 8.0, 9.0, 9.5, 7.5 }, GradeRankClassifier.RankXuatSac)]
    [InlineData(9.2, new double[] { 6.5, 9.0, 9.5, 9.0 }, GradeRankClassifier.RankGioi)]
    [InlineData(8.34, new double[] { 7.5, 8.0, 8.5, 8.25 }, GradeRankClassifier.RankGioi)]
    [InlineData(8.34, new double[] { 5.5, 8.0, 8.5, 9.0 }, GradeRankClassifier.RankKha)]
    [InlineData(7.81, new double[] { 7.0, 7.5, 8.0, 8.25 }, GradeRankClassifier.RankKha)]
    [InlineData(7.81, new double[] { 4.5, 8.0, 8.0, 8.0 }, GradeRankClassifier.RankYeu)]
    [InlineData(6.5, new double[] { 5.0, 6.0, 7.0, 7.5 }, GradeRankClassifier.RankTrungBinh)]
    [InlineData(4.8, new double[] { 5.0, 5.0, 4.5, 5.0 }, GradeRankClassifier.RankYeu)]
    public void Classifier_StandardCases_ClassifiesCorrectly(double avg, double[] scores, string expectedRank)
    {
        var scoresDecimal = scores.Select(s => (decimal?)((decimal)s)).ToList();
        var rank = _classifier.Classify((decimal)avg, scoresDecimal);
        Assert.Equal(expectedRank, rank);
    }

    [Fact]
    public void Classifier_NullAverage_ReturnsChuaXepLoai()
    {
        var rank = _classifier.Classify(null, new List<decimal?> { 7.5m });
        Assert.Equal(GradeRankClassifier.RankChuaXepLoai, rank);
    }

    [Fact]
    public void CalculateGradeSheet_K56Student_CalculatesAccurately()
    {
        var subjectScores = new[]
        {
            7.50m, 8.00m, 7.00m, 7.50m, 7.75m, 7.75m, 8.00m, 8.00m, 7.75m, 7.00m,
            8.00m, 8.00m, 8.25m, 7.50m, 8.25m, 8.00m, 7.50m, 7.75m, 7.75m, 7.50m,
            8.25m, 8.25m
        };

        var subjects = new List<Subject>();
        for (int i = 0; i < subjectScores.Length; i++)
        {
            subjects.Add(new Subject
            {
                Id = i + 1,
                SortOrder = i + 1,
                Name = $"Môn {i + 1:D2}",
                Periods = 45
            });
        }

        var student = new Student
        {
            Id = 1,
            SortOrder = 1,
            StudentCode = "K75A56.0101",
            LastName = "Đặng Thị Hội",
            FirstName = "An",
            DateOfBirth = new DateOnly(1982, 4, 14)
        };

        for (int i = 0; i < subjectScores.Length; i++)
        {
            student.Grades.Add(new StudentGrade
            {
                Id = i + 1,
                StudentId = 1,
                SubjectId = i + 1,
                Score = subjectScores[i]
            });
        }

        var result = _gradeService.CalculateGradeSheet(subjects, new List<Student> { student });

        Assert.Equal(1, result.TotalStudents);
        Assert.Equal(22, result.Subjects.Count);

        var summary = result.Students.Single();
        Assert.Equal(1, summary.Stt);
        Assert.Equal("Đặng Thị Hội An", summary.Student.FullName);
        Assert.Equal(22, summary.CompletedSubjectsCount);
        Assert.Equal(7.78m, summary.AverageScore);
        Assert.Equal("Bảy phẩy bảy mươi tám", summary.AverageScoreInWords);
        Assert.Equal(GradeRankClassifier.RankKha, summary.Classification);
        Assert.Equal(1, result.ClassificationStats[GradeRankClassifier.RankKha]);
    }

    [Fact]
    public void CalculateGradeSheet_StudentWithMissingGrades_CalculatesOnlyCompleted()
    {
        var subjects = new List<Subject>
        {
            new() { Id = 1, SortOrder = 1, Name = "Môn A", Periods = 30 },
            new() { Id = 2, SortOrder = 2, Name = "Môn B", Periods = 30 },
            new() { Id = 3, SortOrder = 3, Name = "Môn C", Periods = 30 }
        };

        var student = new Student
        {
            Id = 10,
            SortOrder = 1,
            StudentCode = "K75A56.0110",
            LastName = "Lê",
            FirstName = "Bình"
        };
        // Chỉ có điểm môn 1 và 2, môn 3 chưa có
        student.Grades.Add(new StudentGrade { StudentId = 10, SubjectId = 1, Score = 8.0m });
        student.Grades.Add(new StudentGrade { StudentId = 10, SubjectId = 2, Score = 8.5m });

        var result = _gradeService.CalculateGradeSheet(subjects, new List<Student> { student });
        var summary = result.Students.Single();

        Assert.Equal(2, summary.CompletedSubjectsCount);
        Assert.Equal(8.25m, summary.AverageScore);
        Assert.Equal("Tám phẩy hai mươi lăm", summary.AverageScoreInWords);
        Assert.Equal(GradeRankClassifier.RankGioi, summary.Classification);
    }
}
