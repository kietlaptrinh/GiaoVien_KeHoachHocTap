namespace KeHoachHocTap.Tests;

using KeHoachHocTap.Web.Data;
using KeHoachHocTap.Web.Domain;

public class GradeCalculationTests
{
    [Fact]
    public void CalculateSimpleAverage_ShouldReturnNull_WhenNoSubjectHasGrade()
    {
        var subjects = new List<Subject>
        {
            new() { Id = 1, Name = "Môn 1", Periods = 30, Grade = null },
            new() { Id = 2, Name = "Môn 2", Periods = 45, Grade = null }
        };

        var avg = GradeCalculator.CalculateSimpleAverage(subjects);

        Assert.Null(avg);
    }

    [Fact]
    public void CalculateSimpleAverage_ShouldReturnNull_WhenListIsEmpty()
    {
        var avg = GradeCalculator.CalculateSimpleAverage(Array.Empty<Subject>());

        Assert.Null(avg);
    }

    [Fact]
    public void CalculateSimpleAverage_ShouldOnlyIncludeGradedSubjects()
    {
        var subjects = new List<Subject>
        {
            new() { Id = 1, Name = "Môn 1", Periods = 30, Grade = 8.0m },
            new() { Id = 2, Name = "Môn 2", Periods = 40, Grade = null }, // Chưa có điểm
            new() { Id = 3, Name = "Môn 3", Periods = 50, Grade = 9.0m }
        };

        var avg = GradeCalculator.CalculateSimpleAverage(subjects);

        Assert.NotNull(avg);
        Assert.Equal(8.5m, avg.Value);
    }

    [Fact]
    public void CalculateSimpleAverage_ShouldHandleSingleSubject()
    {
        var subjects = new List<Subject>
        {
            new() { Id = 1, Name = "Môn 1", Periods = 30, Grade = 7.5m }
        };

        var avg = GradeCalculator.CalculateSimpleAverage(subjects);

        Assert.NotNull(avg);
        Assert.Equal(7.5m, avg.Value);
    }

    [Fact]
    public void CalculateWeightedAverage_ShouldReturnNull_WhenNoSubjectHasGrade()
    {
        var subjects = new List<Subject>
        {
            new() { Id = 1, Name = "Môn 1", Periods = 30, Grade = null }
        };

        var avg = GradeCalculator.CalculateWeightedAverage(subjects);

        Assert.Null(avg);
    }

    [Fact]
    public void CalculateWeightedAverage_ShouldCalculateCorrectWeightedScore()
    {
        // Môn 1: 60 tiết, điểm 8.0
        // Môn 2: 40 tiết, điểm 9.0
        // Trọng số: (60 * 8.0 + 40 * 9.0) / (60 + 40) = (480 + 360) / 100 = 840 / 100 = 8.40
        var subjects = new List<Subject>
        {
            new() { Id = 1, Name = "Môn 1", Periods = 60, Grade = 8.0m },
            new() { Id = 2, Name = "Môn 2", Periods = 40, Grade = 9.0m }
        };

        var avg = GradeCalculator.CalculateWeightedAverage(subjects);

        Assert.NotNull(avg);
        Assert.Equal(8.4m, avg.Value);
    }

    [Fact]
    public void CalculateWeightedAverage_ShouldIgnoreUngradedSubjectsFromPeriodsWeight()
    {
        // Môn 1: 50 tiết, điểm 8.0
        // Môn 2: 70 tiết, KHÔNG có điểm (phải không tính 70 tiết này vào mẫu số)
        // Môn 3: 50 tiết, điểm 10.0
        // Trọng số: (50 * 8.0 + 50 * 10.0) / (50 + 50) = (400 + 500) / 100 = 9.00
        var subjects = new List<Subject>
        {
            new() { Id = 1, Name = "Môn 1", Periods = 50, Grade = 8.0m },
            new() { Id = 2, Name = "Môn 2", Periods = 70, Grade = null },
            new() { Id = 3, Name = "Môn 3", Periods = 50, Grade = 10.0m }
        };

        var avg = GradeCalculator.CalculateWeightedAverage(subjects);

        Assert.NotNull(avg);
        Assert.Equal(9.0m, avg.Value);
    }

    [Fact]
    public void CalculateSummary_ShouldAggregateAllMetricsAccurately()
    {
        var subjects = new List<Subject>
        {
            new() { Id = 1, Name = "Môn 1", Periods = 60, Grade = 8.0m }, // 12 buổi
            new() { Id = 2, Name = "Môn 2", Periods = 55, Grade = 9.0m }, // 11 buổi
            new() { Id = 3, Name = "Môn 3", Periods = 42, Grade = null }   // 9 buổi (ceil(42/5))
        };

        var summary = GradeCalculator.Calculate(subjects);

        Assert.Equal(157, summary.TotalPeriods); // 60 + 55 + 42
        Assert.Equal(32, summary.TotalSessions);  // 12 + 11 + 9
        Assert.Equal(2, summary.GradedSubjectCount);
        Assert.Equal(8.5m, summary.SimpleAverage); // (8 + 9) / 2
        // Trọng số: (60*8 + 55*9) / (60+55) = (480 + 495) / 115 = 975 / 115 = 8.478... -> 8.48
        Assert.Equal(8.48m, summary.WeightedAverage);
    }

    [Fact]
    public void FormatGrade_ShouldReturnDash_WhenGradeIsNull()
    {
        Assert.Equal("—", GradeCalculator.FormatGrade(null));
    }

    [Fact]
    public void FormatGrade_ShouldFormatDecimalsProperly()
    {
        Assert.Equal("8.5", GradeCalculator.FormatGrade(8.5m));
        Assert.Equal("9.0", GradeCalculator.FormatGrade(9.0m, "0.0"));
    }
}
