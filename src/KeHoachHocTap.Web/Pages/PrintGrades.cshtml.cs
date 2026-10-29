using KeHoachHocTap.Web.Domain;
using KeHoachHocTap.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeHoachHocTap.Web.Pages;

public class PrintGradesModel : PageModel
{
    private readonly IPlanRepository _repository;
    private readonly IGradeService _gradeService;

    public PrintGradesModel(
        IPlanRepository repository,
        IGradeService gradeService)
    {
        _repository = repository;
        _gradeService = gradeService;
    }

    public GradeSheetResult? GradeSheet { get; private set; }

    public string PrintDateText { get; private set; } = string.Empty;

    public async Task OnGetAsync()
    {
        var subjects = await _repository.GetSubjectsAsync();
        var students = await _repository.GetStudentsWithGradesAsync();

        GradeSheet = _gradeService.CalculateGradeSheet(subjects, students);

        var now = DateTime.Now;
        PrintDateText = $"Ngày {now.Day:D2} tháng {now.Month:D2} năm {now.Year}";
    }
}
