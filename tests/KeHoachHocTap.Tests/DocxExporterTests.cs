using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;
using KeHoachHocTap.Web.Data;
using KeHoachHocTap.Web.Domain;
using KeHoachHocTap.Web.Export;
using KeHoachHocTap.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace KeHoachHocTap.Tests;

public class DocxExporterTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public DocxExporterTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private string GetTemplatePath()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "BieuMau_KeHoachGiangDay.docx");
        if (File.Exists(path)) return path;

        return Path.Combine(Directory.GetCurrentDirectory(), "TestData", "BieuMau_KeHoachGiangDay.docx");
    }

    private ScheduleResult CreateGoldenScheduleResult()
    {
        var excelPath = Path.Combine(AppContext.BaseDirectory, "TestData", "ke_hoach_hoc_tap.xlsx");
        var reader = new ExcelPlanReader();
        using var stream = File.OpenRead(excelPath);
        var (subjects, holidays, _) = reader.Read(stream);

        var calculator = new ScheduleCalculator();
        return calculator.Calculate(subjects, holidays);
    }

    [Fact]
    public void Export_GeneratesValidDocx_WithoutOpenXmlErrors()
    {
        var exporter = new DocxBieuMauExporter();
        var result = CreateGoldenScheduleResult();
        var options = new ExportOptions
        {
            TemplatePath = GetTemplatePath()
        };

        var bytes = exporter.Export(result, options);
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);

        using var ms = new MemoryStream(bytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        Assert.NotNull(doc.MainDocumentPart);
        Assert.NotNull(doc.MainDocumentPart.Document);

        var validator = new OpenXmlValidator(FileFormatVersions.Office2019);
        var errors = validator.Validate(doc).ToList();
        Assert.Empty(errors);
    }

    [Fact]
    public void Export_PreservesDocumentTitle_AndMergedHeaderCells()
    {
        var exporter = new DocxBieuMauExporter();
        var result = CreateGoldenScheduleResult();
        var options = new ExportOptions
        {
            TemplatePath = GetTemplatePath()
        };

        var bytes = exporter.Export(result, options);
        using var ms = new MemoryStream(bytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        Assert.NotNull(doc.MainDocumentPart?.Document?.Body);
        var body = doc.MainDocumentPart.Document.Body;
        var textContent = body.InnerText;
        Assert.Contains("KẾ HOẠCH GIẢNG DẠY VÀ HỌC TẬP", textContent);

        // Kiểm tra bảo toàn các thuộc tính gộp ô gridSpan và vMerge trong hàng tiêu đề
        var tables = body.Elements<Table>().ToList();
        var mainTable = tables.First(t =>
            t.Elements<TableRow>().Any(row =>
                row.Elements<TableCell>().Any(cell =>
                    cell.InnerText.Contains("Tên môn học/Nhiệm vụ"))));

        var headerRow0 = mainTable.Elements<TableRow>().ElementAt(0);
        var headerRow1 = mainTable.Elements<TableRow>().ElementAt(1);

        // Header row 0 có chứa gridSpan
        var hasGridSpan = headerRow0.Elements<TableCell>().Any(c =>
            c.TableCellProperties?.GridSpan != null);
        Assert.True(hasGridSpan, "Hàng tiêu đề 0 phải bảo toàn ô gộp cột (gridSpan).");

        // Header row có chứa vMerge
        var hasVMerge = headerRow0.Elements<TableCell>().Concat(headerRow1.Elements<TableCell>()).Any(c =>
            c.TableCellProperties?.VerticalMerge != null);
        Assert.True(hasVMerge, "Hàng tiêu đề phải bảo toàn ô gộp dòng (vMerge).");
    }

    [Fact]
    public void Export_HasExactly22DataRows_MatchingGoldenData()
    {
        var exporter = new DocxBieuMauExporter();
        var result = CreateGoldenScheduleResult();
        var options = new ExportOptions
        {
            TemplatePath = GetTemplatePath()
        };

        var bytes = exporter.Export(result, options);
        using var ms = new MemoryStream(bytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        Assert.NotNull(doc.MainDocumentPart?.Document?.Body);
        var body = doc.MainDocumentPart.Document.Body;
        var tables = body.Elements<Table>().ToList();
        var mainTable = tables.First(t =>
            t.Elements<TableRow>().Any(row =>
                row.Elements<TableCell>().Any(cell =>
                    cell.InnerText.Contains("Tên môn học/Nhiệm vụ"))));

        var rows = mainTable.Elements<TableRow>().ToList();

        // 2 hàng tiêu đề + 1 Khai giảng + 19 môn + 1 Thi tốt nghiệp + 1 Bế giảng = 24 hàng
        Assert.Equal(24, rows.Count);

        var dataRows = rows.Skip(2).ToList();
        Assert.Equal(22, dataRows.Count);

        // 1. Dòng Khai giảng
        var r0Cells = dataRows[0].Elements<TableCell>().Select(c => c.InnerText.Trim()).ToList();
        Assert.Equal("1", r0Cells[0]);
        Assert.Equal("Khai giảng", r0Cells[1]);
        Assert.Equal("10", r0Cells[2]);
        Assert.Equal("S 10/9/2025", r0Cells[6]);
        Assert.Equal("C 10/9/2025", r0Cells[7]);

        // 2. Dòng môn học đầu tiên (Tên môn học 01)
        // KỲ VỌNG CHÍNH XÁC THEO SPEC: 50 | 10 | 5 | | S 11/9/2025 | C 18/9/2025 | S 19/9/2025
        var r1Cells = dataRows[1].Elements<TableCell>().Select(c => c.InnerText.Trim()).ToList();
        Assert.Equal("2", r1Cells[0]);
        Assert.Equal("Tên môn học 01", r1Cells[1]);
        Assert.Equal("50", r1Cells[2]); // Tiết giảng = 60 - 10 = 50
        Assert.Equal("10", r1Cells[3]); // Tự nghiên cứu
        Assert.Equal("5", r1Cells[4]);  // Ôn thi/thi
        Assert.Equal("", r1Cells[5]);   // Dự phòng (trống)
        Assert.Equal("S 11/9/2025", r1Cells[6]); // Bắt đầu
        Assert.Equal("C 18/9/2025", r1Cells[7]); // Kết thúc
        Assert.Equal("S 19/9/2025", r1Cells[8]); // Thi hết môn

        // 3. Dòng Thi tốt nghiệp
        var r20Cells = dataRows[20].Elements<TableCell>().Select(c => c.InnerText.Trim()).ToList();
        Assert.Equal("21", r20Cells[0]);
        Assert.Equal("Thi tốt nghiệp", r20Cells[1]);
        Assert.Equal("15", r20Cells[2]);
        Assert.Equal("S 22/02/2026", r20Cells[6]);
        Assert.Equal("S 24/02/2026", r20Cells[7]);

        // 4. Dòng Bế giảng
        var r21Cells = dataRows[21].Elements<TableCell>().Select(c => c.InnerText.Trim()).ToList();
        Assert.Equal("22", r21Cells[0]);
        Assert.Equal("Bế giảng", r21Cells[1]);
        Assert.Equal("5", r21Cells[2]);
        Assert.Equal("S 20/4/2026", r21Cells[6]);
        Assert.Equal("", r21Cells[7]);
    }

    [Fact]
    public void Export_ReflectsManualReservePeriods_AndNote()
    {
        var exporter = new DocxBieuMauExporter();
        var result = CreateGoldenScheduleResult();

        // Giả lập người dùng nhập tay Dự phòng = 5 và Ghi chú = "Họp lớp" cho môn 02
        var subject2 = result.Items[1].Subject;
        subject2.ReservePeriods = 5;
        subject2.Note = "Họp lớp";

        var options = new ExportOptions
        {
            TemplatePath = GetTemplatePath()
        };

        var bytes = exporter.Export(result, options);
        using var ms = new MemoryStream(bytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        Assert.NotNull(doc.MainDocumentPart?.Document?.Body);
        var body = doc.MainDocumentPart.Document.Body;
        var tables = body.Elements<Table>().ToList();
        var mainTable = tables.First(t =>
            t.Elements<TableRow>().Any(row =>
                row.Elements<TableCell>().Any(cell =>
                    cell.InnerText.Contains("Tên môn học/Nhiệm vụ"))));

        var dataRows = mainTable.Elements<TableRow>().Skip(2).ToList();
        var rowMon2 = dataRows[2].Elements<TableCell>().Select(c => c.InnerText.Trim()).ToList();

        Assert.Equal("3", rowMon2[0]);
        Assert.Equal("Tên môn học 02", rowMon2[1]);
        Assert.Equal("5", rowMon2[5]);        // Cột dự phòng
        Assert.Equal("Họp lớp", rowMon2[9]);   // Cột ghi chú
    }

    [Fact]
    public void TemplateService_ValidateTemplate_RejectsInvalidFiles()
    {
        var service = new TemplateService();

        // 1. Stream không phải là file Word hợp lệ
        using var invalidStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        var (isValid1, error1) = service.ValidateTemplate(invalidStream);
        Assert.False(isValid1);
        Assert.NotNull(error1);

        // 2. File mẫu hợp lệ chuẩn
        var templatePath = GetTemplatePath();
        using var validStream = File.OpenRead(templatePath);
        var (isValid2, error2) = service.ValidateTemplate(validStream);
        Assert.True(isValid2);
        Assert.Null(error2);
    }

    [Fact]
    public async Task TemplateService_ReadsExtraTasksFromAppSetting()
    {
        await _fixture.InitializeAsync();
        var context = _fixture.Context;

        var repo = new PlanRepository(context);
        await repo.SetSettingAsync(TemplateService.SettingKeyGraduationName, "Thi tốt nghiệp Khóa 2025");
        await repo.SetSettingAsync(TemplateService.SettingKeyGraduationPeriods, "20");
        await repo.SetSettingAsync(TemplateService.SettingKeyGraduationStart, "S 15/03/2026");
        await repo.SetSettingAsync(TemplateService.SettingKeyGraduationEnd, "C 17/03/2026");

        var config = new ConfigurationBuilder().Build();
        var service = new TemplateService(configuration: config, repository: repo);

        var exportOptions = await service.GetExportOptionsAsync();

        Assert.Equal("Thi tốt nghiệp Khóa 2025", exportOptions.Graduation.Name);
        Assert.Equal(20, exportOptions.Graduation.Periods);
        Assert.Equal("S 15/03/2026", exportOptions.Graduation.Start);
        Assert.Equal("C 17/03/2026", exportOptions.Graduation.End);
    }

    [Fact]
    public void TemplateService_GetExtraTaskWarnings_DetectsHolidaysAndWeekends()
    {
        var service = new TemplateService();
        var result = CreateGoldenScheduleResult();

        var options = new ExportOptions
        {
            Graduation = new ExtraTaskConfig
            {
                Name = "Thi tốt nghiệp",
                Start = "S 22/02/2026", // Ngày 22/02/2026 là Chủ nhật và nằm trong kỳ nghỉ Tết (12/2 - 22/2)
                End = "S 24/02/2026"
            },
            Closing = new ExtraTaskConfig
            {
                Name = "Bế giảng",
                Start = "S 20/4/2026", // 20/04/2026 là Thứ 2 bình thường
                End = ""
            }
        };

        var warnings = service.GetExtraTaskWarnings(result, options);
        Assert.NotEmpty(warnings);
        Assert.Contains(warnings, w => w.Contains("Thi tốt nghiệp") && w.Contains("ngày nghỉ"));
    }
}
