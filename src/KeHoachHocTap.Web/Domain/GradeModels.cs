using KeHoachHocTap.Web.Data;

namespace KeHoachHocTap.Web.Domain;

/// <summary>
/// Đại diện cho điểm của một môn học cụ thể của học viên.
/// </summary>
public record StudentGradeItem(int SubjectId, string SubjectName, decimal? Score);

/// <summary>
/// Thông tin tổng hợp kết quả học tập của một học viên.
/// </summary>
public record StudentSummary(
    Student Student,
    int Stt,
    IReadOnlyList<StudentGradeItem> SubjectGrades,
    int CompletedSubjectsCount,
    decimal? AverageScore,
    string AverageScoreInWords,
    string Classification);

/// <summary>
/// Toàn bộ bảng điểm tổng hợp của lớp học bao gồm danh sách môn, danh sách học viên và thống kê xếp loại.
/// </summary>
public record GradeSheetResult(
    IReadOnlyList<Subject> Subjects,
    IReadOnlyList<StudentSummary> Students,
    int TotalStudents,
    IReadOnlyDictionary<string, int> ClassificationStats);
