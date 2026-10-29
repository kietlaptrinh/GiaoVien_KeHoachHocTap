using KeHoachHocTap.Web.Data;
using KeHoachHocTap.Web.Domain;
using KeHoachHocTap.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeHoachHocTap.Web.Pages;

public class PrintStudentTranscriptsModel : PageModel
{
    private readonly IPlanRepository _repository;
    private readonly IGradeService _gradeService;

    public PrintStudentTranscriptsModel(
        IPlanRepository repository,
        IGradeService gradeService)
    {
        _repository = repository;
        _gradeService = gradeService;
    }

    public IReadOnlyList<Subject> Subjects { get; private set; } = Array.Empty<Subject>();
    public IReadOnlyList<StudentSummary> Students { get; private set; } = Array.Empty<StudentSummary>();
    public string PrintDateText { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync([FromQuery] int? studentId)
    {
        var subjects = await _repository.GetSubjectsAsync();
        var allStudents = await _repository.GetStudentsWithGradesAsync();

        var gradeSheet = _gradeService.CalculateGradeSheet(subjects, allStudents);
        Subjects = gradeSheet.Subjects;

        if (studentId.HasValue && studentId.Value > 0)
        {
            var single = gradeSheet.Students.FirstOrDefault(s => s.Student.Id == studentId.Value);
            if (single == null)
            {
                return NotFound($"Không tìm thấy học viên có ID = {studentId.Value}");
            }
            Students = [single];
        }
        else
        {
            Students = gradeSheet.Students;
        }

        var now = DateTime.Now;
        PrintDateText = $"Ngày {now.Day:D2} tháng {now.Month:D2} năm {now.Year}";

        return Page();
    }
}
