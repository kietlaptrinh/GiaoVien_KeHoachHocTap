using KeHoachHocTap.Web.Services;
using Xunit;

namespace KeHoachHocTap.Tests;

public class ExcelGradeImporterTests
{
    private readonly ExcelGradeImporter _importer = new();

    private static string GetTestFilePath()
    {
        var testPath = Path.Combine(AppContext.BaseDirectory, "TestData", "BANG DIEM TONG K56.01.xlsx");
        if (File.Exists(testPath)) return testPath;

        var repoPath = Path.Combine(Directory.GetCurrentDirectory(), "BANG DIEM TONG K56.01.xlsx");
        if (File.Exists(repoPath)) return repoPath;

        var appDataPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "BANG DIEM TONG K56.01.xlsx");
        if (File.Exists(appDataPath)) return appDataPath;

        throw new FileNotFoundException("Không tìm thấy file mẫu BANG DIEM TONG K56.01.xlsx");
    }

    [Fact]
    public void ReadGradeSheet_K56XlsxFile_ImportsAll48StudentsAnd22Subjects()
    {
        var filePath = GetTestFilePath();
        var result = _importer.ReadGradeSheet(filePath);

        Assert.NotNull(result);
        Assert.Equal(48, result.Students.Count);
        Assert.Equal(22, result.SubjectNames.Count);

        // Kiểm tra học viên đầu tiên: Đặng Thị Hội An
        var first = result.Students[0];
        Assert.Equal(1, first.Stt);
        Assert.Equal("K75A56.0101", first.StudentCode);
        Assert.Equal("Đặng Thị Hội", first.LastName);
        Assert.Equal("An", first.FirstName);
        Assert.Equal(new DateOnly(1982, 4, 14), first.DateOfBirth);
        Assert.Equal(22, first.Grades.Count);
        Assert.Equal(7.50m, first.Grades[0].Score);
        Assert.Equal(7.81m, first.AverageScore);
        Assert.Equal("Khá", first.Classification);

        // Kiểm tra học viên xếp loại Giỏi (vd ĐTB 8.34)
        var studentGioi = result.Students.FirstOrDefault(s => s.AverageScore >= 8.20m);
        Assert.NotNull(studentGioi);
        Assert.Equal("Giỏi", studentGioi.Classification);
    }

    [Fact]
    public void ReadGradeSheet_FromStream_ImportsSuccessfully()
    {
        var filePath = GetTestFilePath();
        using var stream = File.OpenRead(filePath);
        var result = _importer.ReadGradeSheet(stream);

        Assert.NotNull(result);
        Assert.Equal(48, result.Students.Count);
    }
}
