using KeHoachHocTap.Web.Domain;
using KeHoachHocTap.Web.Export;
using KeHoachHocTap.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeHoachHocTap.Web.Pages;

public class PrintPlanModel : PageModel
{
    private readonly IPlanService _planService;
    private readonly ITemplateService _templateService;

    public PrintPlanModel(
        IPlanService planService,
        ITemplateService templateService)
    {
        _planService = planService;
        _templateService = templateService;
    }

    public ScheduleResult? Schedule { get; private set; }

    public ExportOptions? Options { get; private set; }

    public string PrintDateText { get; private set; } = string.Empty;

    public async Task OnGetAsync()
    {
        Schedule = await _planService.GetScheduleAsync();
        Options = await _templateService.GetExportOptionsAsync();

        var now = DateTime.Now;
        PrintDateText = $"Ngày {now.Day:D2} tháng {now.Month:D2} năm {now.Year}";
    }
}
