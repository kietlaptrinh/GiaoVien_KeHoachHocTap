using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using KeHoachHocTap.Web.Domain;
using KeHoachHocTap.Web.Export;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace KeHoachHocTap.Web.Services;

/// <summary>
/// Triển khai dịch vụ quản lý template và cấu hình ExtraTasks (SPEC mục 7.1, 7.3).
/// </summary>
public class TemplateService : ITemplateService
{
    private readonly IWebHostEnvironment? _environment;
    private readonly IConfiguration? _configuration;
    private readonly IPlanRepository? _repository;

    public const string SettingKeySelfStudyPeriods = "Export:SelfStudyPeriods";
    public const string SettingKeyExamPeriods = "Export:ExamPeriods";
    public const string SettingKeyGraduationName = "Export:ExtraTasks:Graduation:Name";
    public const string SettingKeyGraduationPeriods = "Export:ExtraTasks:Graduation:Periods";
    public const string SettingKeyGraduationStart = "Export:ExtraTasks:Graduation:Start";
    public const string SettingKeyGraduationEnd = "Export:ExtraTasks:Graduation:End";
    public const string SettingKeyClosingName = "Export:ExtraTasks:Closing:Name";
    public const string SettingKeyClosingPeriods = "Export:ExtraTasks:Closing:Periods";
    public const string SettingKeyClosingStart = "Export:ExtraTasks:Closing:Start";
    public const string SettingKeyClosingEnd = "Export:ExtraTasks:Closing:End";

    public TemplateService(
        IWebHostEnvironment? environment = null,
        IConfiguration? configuration = null,
        IPlanRepository? repository = null)
    {
        _environment = environment;
        _configuration = configuration;
        _repository = repository;
    }

    private string GetBaseDirectory()
    {
        return _environment?.ContentRootPath ?? AppContext.BaseDirectory;
    }

    private string GetCustomTemplatePath()
    {
        return Path.Combine(GetBaseDirectory(), "App_Data", "Templates", "Custom_BieuMau_KeHoachGiangDay.docx");
    }

    private string GetDefaultTemplatePath()
    {
        var configured = _configuration?["Export:TemplatePath"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            if (Path.IsPathRooted(configured)) return configured;
            return Path.Combine(GetBaseDirectory(), configured);
        }

        var path = Path.Combine(GetBaseDirectory(), "App_Data", "Templates", "BieuMau_KeHoachGiangDay.docx");
        if (File.Exists(path)) return path;

        var testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData", "BieuMau_KeHoachGiangDay.docx");
        if (File.Exists(testDataPath)) return testDataPath;

        return path;
    }

    public TemplateInfo GetCurrentTemplateInfo()
    {
        var customPath = GetCustomTemplatePath();
        if (File.Exists(customPath))
        {
            var fi = new FileInfo(customPath);
            return new TemplateInfo(
                FileName: "Custom_BieuMau_KeHoachGiangDay.docx",
                IsCustom: true,
                FileSizeBytes: fi.Length,
                LastModified: fi.LastWriteTime,
                FullPath: customPath);
        }

        var defaultPath = GetDefaultTemplatePath();
        if (File.Exists(defaultPath))
        {
            var fi = new FileInfo(defaultPath);
            return new TemplateInfo(
                FileName: "BieuMau_KeHoachGiangDay.docx",
                IsCustom: false,
                FileSizeBytes: fi.Length,
                LastModified: fi.LastWriteTime,
                FullPath: defaultPath);
        }

        return new TemplateInfo(
            FileName: "BieuMau_KeHoachGiangDay.docx",
            IsCustom: false,
            FileSizeBytes: 0,
            LastModified: DateTime.MinValue,
            FullPath: defaultPath);
    }

    public byte[] GetCurrentTemplateBytes()
    {
        var customPath = GetCustomTemplatePath();
        if (File.Exists(customPath))
        {
            return File.ReadAllBytes(customPath);
        }

        var defaultPath = GetDefaultTemplatePath();
        if (File.Exists(defaultPath))
        {
            return File.ReadAllBytes(defaultPath);
        }

        throw new FileNotFoundException("Không tìm thấy file mẫu Word gốc hoặc tùy biến.");
    }

    public (bool IsValid, string? ErrorMessage) ValidateTemplate(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            // Sao chép sang MemoryStream riêng để không làm ảnh hưởng con trỏ của stream gọi
            using var ms = new MemoryStream();
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }
            stream.CopyTo(ms);
            ms.Position = 0;

            using var doc = WordprocessingDocument.Open(ms, false);
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body == null)
            {
                return (false, "Tệp tin không phải tài liệu Word hợp lệ hoặc không có phần nội dung chính.");
            }

            var tables = body.Elements<Table>().ToList();
            var mainTable = tables.FirstOrDefault(t =>
                t.Elements<TableRow>().Any(row =>
                    row.Elements<TableCell>().Any(cell =>
                        cell.InnerText.Contains("Tên môn học/Nhiệm vụ", StringComparison.OrdinalIgnoreCase))));

            if (mainTable == null)
            {
                return (false, "Không tìm thấy bảng Kế hoạch giảng dạy chứa cột 'Tên môn học/Nhiệm vụ' trong file mẫu.");
            }

            var rows = mainTable.Elements<TableRow>().ToList();
            if (rows.Count < 2)
            {
                return (false, "Bảng Kế hoạch giảng dạy trong file mẫu phải có ít nhất 2 hàng tiêu đề.");
            }

            // Kiểm tra có hàng dữ liệu 10 cột hoặc tiêu đề có 10 ô
            var has10Cols = rows.Any(r => r.Elements<TableCell>().Count() == 10);
            if (!has10Cols)
            {
                var grid = mainTable.Elements<TableGrid>().FirstOrDefault();
                if (grid != null && grid.Elements<GridColumn>().Count() == 10)
                {
                    has10Cols = true;
                }
            }

            if (!has10Cols)
            {
                return (false, "Bảng Kế hoạch giảng dạy trong file mẫu không có đủ 10 cột dữ liệu chuẩn theo quy định.");
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"File mẫu Word không hợp lệ hoặc bị lỗi định dạng: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> SaveCustomTemplateAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var validation = ValidateTemplate(stream);
        if (!validation.IsValid)
        {
            return (false, validation.ErrorMessage);
        }

        var customPath = GetCustomTemplatePath();
        var dir = Path.GetDirectoryName(customPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        await using var fileStream = new FileStream(customPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fileStream, cancellationToken);

        return (true, null);
    }

    public Task RestoreDefaultTemplateAsync(CancellationToken cancellationToken = default)
    {
        var customPath = GetCustomTemplatePath();
        if (File.Exists(customPath))
        {
            File.Delete(customPath);
        }

        return Task.CompletedTask;
    }

    public async Task<ExportOptions> GetExportOptionsAsync(CancellationToken cancellationToken = default)
    {
        var options = new ExportOptions();

        // 1. Nạp từ cấu hình appsettings.json nếu có
        if (_configuration != null)
        {
            var exportSection = _configuration.GetSection("Export");
            if (exportSection.Exists())
            {
                options.SelfStudyPeriods = exportSection.GetValue("SelfStudyPeriods", 10);
                options.ExamPeriods = exportSection.GetValue("ExamPeriods", 5);

                var gradSection = exportSection.GetSection("ExtraTasks:Graduation");
                if (gradSection.Exists())
                {
                    options.Graduation.Name = gradSection.GetValue("Name", "Thi tốt nghiệp") ?? "Thi tốt nghiệp";
                    options.Graduation.Periods = gradSection.GetValue("Periods", 15);
                    options.Graduation.Start = gradSection.GetValue("Start", "S 22/02/2026") ?? "S 22/02/2026";
                    options.Graduation.End = gradSection.GetValue("End", "S 24/02/2026") ?? "S 24/02/2026";
                }

                var closingSection = exportSection.GetSection("ExtraTasks:Closing");
                if (closingSection.Exists())
                {
                    options.Closing.Name = closingSection.GetValue("Name", "Bế giảng") ?? "Bế giảng";
                    options.Closing.Periods = closingSection.GetValue("Periods", 5);
                    options.Closing.Start = closingSection.GetValue("Start", "S 20/4/2026") ?? "S 20/4/2026";
                    options.Closing.End = closingSection.GetValue("End", string.Empty) ?? string.Empty;
                }
            }
        }

        // 2. Ghi đè bằng cấu hình động trong database (AppSetting) nếu có
        if (_repository != null)
        {
            var settings = await _repository.GetAllSettingsAsync(cancellationToken);

            if (settings.TryGetValue(SettingKeySelfStudyPeriods, out var valSelf) && int.TryParse(valSelf, out var selfStudy))
                options.SelfStudyPeriods = selfStudy;

            if (settings.TryGetValue(SettingKeyExamPeriods, out var valExam) && int.TryParse(valExam, out var examPeriods))
                options.ExamPeriods = examPeriods;

            if (settings.TryGetValue(SettingKeyGraduationName, out var gradName) && !string.IsNullOrWhiteSpace(gradName))
                options.Graduation.Name = gradName;

            if (settings.TryGetValue(SettingKeyGraduationPeriods, out var valGradP) && int.TryParse(valGradP, out var gradP))
                options.Graduation.Periods = gradP;

            if (settings.TryGetValue(SettingKeyGraduationStart, out var gradStart) && !string.IsNullOrWhiteSpace(gradStart))
                options.Graduation.Start = gradStart;

            if (settings.TryGetValue(SettingKeyGraduationEnd, out var gradEnd))
                options.Graduation.End = gradEnd;

            if (settings.TryGetValue(SettingKeyClosingName, out var closingName) && !string.IsNullOrWhiteSpace(closingName))
                options.Closing.Name = closingName;

            if (settings.TryGetValue(SettingKeyClosingPeriods, out var valClosingP) && int.TryParse(valClosingP, out var closingP))
                options.Closing.Periods = closingP;

            if (settings.TryGetValue(SettingKeyClosingStart, out var closingStart) && !string.IsNullOrWhiteSpace(closingStart))
                options.Closing.Start = closingStart;

            if (settings.TryGetValue(SettingKeyClosingEnd, out var closingEnd))
                options.Closing.End = closingEnd;
        }

        // Gắn dữ liệu template hiện hành
        try
        {
            options.TemplateBytes = GetCurrentTemplateBytes();
        }
        catch
        {
            // Bỏ qua nếu chưa nạp được file
        }

        return options;
    }

    public async Task SaveExportOptionsAsync(ExportOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (_repository == null) return;

        await _repository.SetSettingAsync(SettingKeySelfStudyPeriods, options.SelfStudyPeriods.ToString(), cancellationToken);
        await _repository.SetSettingAsync(SettingKeyExamPeriods, options.ExamPeriods.ToString(), cancellationToken);

        await _repository.SetSettingAsync(SettingKeyGraduationName, options.Graduation.Name, cancellationToken);
        await _repository.SetSettingAsync(SettingKeyGraduationPeriods, options.Graduation.Periods.ToString(), cancellationToken);
        await _repository.SetSettingAsync(SettingKeyGraduationStart, options.Graduation.Start, cancellationToken);
        await _repository.SetSettingAsync(SettingKeyGraduationEnd, options.Graduation.End, cancellationToken);

        await _repository.SetSettingAsync(SettingKeyClosingName, options.Closing.Name, cancellationToken);
        await _repository.SetSettingAsync(SettingKeyClosingPeriods, options.Closing.Periods.ToString(), cancellationToken);
        await _repository.SetSettingAsync(SettingKeyClosingStart, options.Closing.Start, cancellationToken);
        await _repository.SetSettingAsync(SettingKeyClosingEnd, options.Closing.End, cancellationToken);
    }

    public IReadOnlyList<string> GetExtraTaskWarnings(ScheduleResult result, ExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(options);

        var warnings = new List<string>();

        void CheckDate(string taskName, string? slotText)
        {
            if (string.IsNullOrWhiteSpace(slotText)) return;

            if (Slot.TryParse(slotText, out var slot))
            {
                var date = slot.Date;
                bool isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
                bool isHoliday = result.Holidays.Any(h => h.From <= date && date <= h.To);

                if (isWeekend || isHoliday)
                {
                    var reason = isWeekend && isHoliday
                        ? "rơi vào ngày nghỉ và cuối tuần"
                        : (isWeekend ? "rơi vào cuối tuần" : "rơi vào ngày nghỉ");

                    warnings.Add($"Ngày bắt đầu của '{taskName}' ({slotText}) {reason}.");
                }
            }
        }

        CheckDate(options.Graduation.Name, options.Graduation.Start);
        CheckDate(options.Closing.Name, options.Closing.Start);

        return warnings;
    }
}
