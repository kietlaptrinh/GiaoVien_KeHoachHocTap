namespace KeHoachHocTap.Web.Services;

/// <summary>
/// Điểm của một môn học được đọc từ file Excel.
/// </summary>
public record ImportedGrade(string SubjectName, decimal? Score);

/// <summary>
/// Thông tin học viên và điểm các môn được đọc từ file Excel.
/// </summary>
public record ImportedStudent(
    int Stt,
    string StudentCode,
    string LastName,
    string FirstName,
    DateOnly? DateOfBirth,
    string? Gender,
    IReadOnlyList<ImportedGrade> Grades,
    decimal? AverageScore = null,
    string? Classification = null);

/// <summary>
/// Kết quả đọc file Excel bảng điểm.
/// </summary>
public record ExcelGradeImportResult(
    IReadOnlyList<string> SubjectNames,
    IReadOnlyList<ImportedStudent> Students,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Giao diện dịch vụ đọc bảng điểm học viên từ file Excel (chuẩn K56 hoặc .xlsx).
/// </summary>
public interface IExcelGradeImporter
{
    /// <summary>
    /// Đọc dữ liệu bảng điểm từ Stream.
    /// </summary>
    ExcelGradeImportResult ReadGradeSheet(Stream stream);

    /// <summary>
    /// Đọc dữ liệu bảng điểm từ đường dẫn file.
    /// </summary>
    ExcelGradeImportResult ReadGradeSheet(string filePath);
}
