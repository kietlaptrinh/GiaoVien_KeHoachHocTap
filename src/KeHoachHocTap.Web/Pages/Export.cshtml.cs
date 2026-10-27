using KeHoachHocTap.Web.Domain;
using KeHoachHocTap.Web.Export;
using KeHoachHocTap.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeHoachHocTap.Web.Pages;

public class ExportModel : PageModel
{
    private readonly IPlanService _planService;
    private readonly IEnumerable<IExporter> _exporters;
    private readonly ITemplateService _templateService;

    public ExportModel(
        IPlanService planService,
        IEnumerable<IExporter> exporters,
        ITemplateService templateService)
    {
        _planService = planService;
        _exporters = exporters;
        _templateService = templateService;
    }

    public IReadOnlyList<IExporter> Exporters { get; private set; } = [];

    public ScheduleResult? Schedule { get; private set; }

    public ExportOptions? Options { get; private set; }

    public IReadOnlyList<string> ExtraTaskWarnings { get; private set; } = [];

    public TemplateInfo? CurrentTemplate { get; private set; }

    public async Task OnGetAsync()
    {
        Exporters = _exporters.ToList();
        Schedule = await _planService.GetScheduleAsync();
        Options = await _templateService.GetExportOptionsAsync();
        ExtraTaskWarnings = _templateService.GetExtraTaskWarnings(Schedule, Options);
        CurrentTemplate = _templateService.GetCurrentTemplateInfo();

        ViewData["TotalSubjects"] = Schedule.Items.Count.ToString();
        ViewData["StatusMessage"] = "Sẵn sàng xuất kế hoạch giảng dạy";
    }

    public async Task<IActionResult> OnGetDownloadAsync(string? id)
    {
        var targetId = string.IsNullOrWhiteSpace(id) ? "docx-bieumau" : id.Trim();
        var exporter = _exporters.FirstOrDefault(e => e.Id.Equals(targetId, StringComparison.OrdinalIgnoreCase));

        if (exporter == null)
        {
            return NotFound($"Không tìm thấy bộ xuất file với mã định danh '{targetId}'.");
        }

        var schedule = await _planService.GetScheduleAsync();
        var options = await _templateService.GetExportOptionsAsync();

        var fileBytes = exporter.Export(schedule, options);
        var fileName = $"KeHoachGiangDayHocTap_{DateTime.Now:yyyyMMdd}{exporter.Extension}";

        return File(fileBytes, exporter.ContentType, fileName);
    }
}
