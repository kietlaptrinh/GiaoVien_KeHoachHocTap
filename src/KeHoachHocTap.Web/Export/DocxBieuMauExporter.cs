using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using KeHoachHocTap.Web.Data;
using KeHoachHocTap.Web.Domain;
using Microsoft.AspNetCore.Hosting;

namespace KeHoachHocTap.Web.Export;

/// <summary>
/// Bộ xuất file Kế hoạch giảng dạy và học tập định dạng Microsoft Word (.docx) theo biểu mẫu chuẩn (SPEC mục 7.1).
/// </summary>
public class DocxBieuMauExporter : IExporter
{
    private readonly IWebHostEnvironment? _environment;

    public DocxBieuMauExporter(IWebHostEnvironment? environment = null)
    {
        _environment = environment;
    }

    public string Id => "docx-bieumau";

    public string DisplayName => "Biểu mẫu Kế hoạch giảng dạy và học tập (.docx)";

    public string Extension => ".docx";

    public string ContentType => "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public byte[] Export(ScheduleResult result, ExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(options);

        // 1. Lấy dữ liệu nhị phân của file mẫu (ưu tiên custom template, fallback file mẫu gốc)
        var templateBytes = GetTemplateBytes(options);

        // 2. Mở file mẫu trong MemoryStream (tuyệt đối không can thiệp hay sửa file trên đĩa)
        using var memoryStream = new MemoryStream();
        memoryStream.Write(templateBytes, 0, templateBytes.Length);
        memoryStream.Position = 0;

        using (var wordDoc = WordprocessingDocument.Open(memoryStream, true))
        {
            var body = wordDoc.MainDocumentPart?.Document?.Body
                ?? throw new InvalidOperationException("File mẫu Word không có phần nội dung chính (Body).");

            // 3. Tìm bảng chính chứa ô có nội dung "Tên môn học/Nhiệm vụ"
            var targetTable = body.Elements<Table>().FirstOrDefault(t =>
                t.Elements<TableRow>().Any(row =>
                    row.Elements<TableCell>().Any(cell =>
                        cell.InnerText.Contains("Tên môn học/Nhiệm vụ", StringComparison.OrdinalIgnoreCase))));

            if (targetTable == null)
            {
                throw new InvalidOperationException("Không tìm thấy bảng Kế hoạch giảng dạy và học tập trong file mẫu Word.");
            }

            var allRows = targetTable.Elements<TableRow>().ToList();
            if (allRows.Count < 2)
            {
                throw new InvalidOperationException("Bảng Kế hoạch giảng dạy trong file mẫu phải có ít nhất 2 hàng tiêu đề.");
            }

            // 4. Lấy hàng môn học đầu tiên làm hàng mẫu để clone định dạng (giữ font, borders, alignments)
            var sampleRow = allRows.Skip(2).FirstOrDefault(r => r.Elements<TableCell>().Count() == 10)
                            ?? allRows.FirstOrDefault(r => r.Elements<TableCell>().Count() == 10)
                            ?? allRows.Last();

            var templateRow = (TableRow)sampleRow.CloneNode(true);

            // 5. Xóa toàn bộ hàng dữ liệu cũ (chỉ giữ lại 2 hàng tiêu đề)
            foreach (var oldRow in allRows.Skip(2).ToList())
            {
                oldRow.Remove();
            }

            // 6. Thêm các hàng dữ liệu mới theo thứ tự chuẩn: Khai giảng → Các môn học (kèm hoạt động xen kẽ) → Chuỗi hoạt động sau môn cuối (hoặc mặc định Thi tốt nghiệp → Bế giảng)
            int currentTt = 1;

            // Dòng 1: Khai giảng (TT = 1)
            var openingRow = (TableRow)templateRow.CloneNode(true);
            var openingHoliday = result.Holidays?.FirstOrDefault(h => h.IsOpeningDay);
            var openingStartDay = openingHoliday?.From ?? result.OpeningDay;
            var openingEndDay = (openingHoliday != null && openingHoliday.To >= openingHoliday.From)
                ? openingHoliday.To
                : openingStartDay;
            var openingStart = $"S {openingStartDay.Day}/{openingStartDay.Month}/{openingStartDay.Year}";
            var openingEnd = $"C {openingEndDay.Day}/{openingEndDay.Month}/{openingEndDay.Year}";
            SetRowCells(openingRow, [
                (currentTt++).ToString(),
                "Khai giảng",
                "10",
                string.Empty,
                string.Empty,
                string.Empty,
                openingStart,
                openingEnd,
                string.Empty,
                string.Empty
            ]);
            targetTable.AppendChild(openingRow);

            // Dòng các môn học và hoạt động xen kẽ
            for (int i = 0; i < result.Items.Count; i++)
            {
                var item = result.Items[i];
                var subjectRow = (TableRow)templateRow.CloneNode(true);
                var tt = (currentTt++).ToString();

                var teachingPeriods = Math.Max(0, item.Subject.Periods - options.SelfStudyPeriods);
                var reserveStr = item.Subject.ReservePeriods.HasValue && item.Subject.ReservePeriods.Value > 0
                    ? item.Subject.ReservePeriods.Value.ToString()
                    : string.Empty;

                SetRowCells(subjectRow, [
                    tt,
                    item.Subject.Name,
                    teachingPeriods.ToString(),
                    options.SelfStudyPeriods.ToString(),
                    options.ExamPeriods.ToString(),
                    reserveStr,
                    item.Start.ToString(),
                    item.End.ToString(),
                    item.Exam.ToString(),
                    item.Subject.Note ?? string.Empty
                ]);
                targetTable.AppendChild(subjectRow);

                // Kiểm tra hoạt động xen kẽ sau môn học này
                if (result.Activities != null)
                {
                    var interimActivities = result.Activities
                        .Where(a => a.Activity.Type == PlanActivityType.Interim &&
                                    (a.Activity.AfterSubjectId == item.Subject.Id || (item.Subject.Id == 0 && a.Activity.AfterSubjectId == item.Subject.SortOrder)))
                        .OrderBy(a => a.Activity.SortOrder)
                        .ToList();

                    foreach (var act in interimActivities)
                    {
                        var interimRow = (TableRow)templateRow.CloneNode(true);
                        var actTt = (currentTt++).ToString();
                        var periodsOrSessions = act.Activity.Periods.HasValue
                            ? act.Activity.Periods.Value.ToString()
                            : (act.Activity.Sessions.HasValue ? act.Activity.Sessions.Value.ToString() : string.Empty);

                        SetRowCells(interimRow, [
                            actTt,
                            act.Activity.Name,
                            periodsOrSessions,
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            act.Start?.ToString() ?? string.Empty,
                            act.End?.ToString() ?? string.Empty,
                            string.Empty,
                            act.Activity.Note ?? string.Empty
                        ]);
                        targetTable.AppendChild(interimRow);
                    }
                }
            }

            // Chuỗi hoạt động sau môn cuối cùng (Closing Activities)
            var closingActivities = result.Activities?
                .Where(a => a.Activity.Type == PlanActivityType.Closing)
                .OrderBy(a => a.Activity.SortOrder)
                .ToList();

            if (closingActivities != null && closingActivities.Count > 0)
            {
                foreach (var act in closingActivities)
                {
                    var closingActRow = (TableRow)templateRow.CloneNode(true);
                    var actTt = (currentTt++).ToString();
                    var periodsOrSessions = act.Activity.Periods.HasValue
                        ? act.Activity.Periods.Value.ToString()
                        : (act.Activity.Sessions.HasValue ? act.Activity.Sessions.Value.ToString() : string.Empty);

                    SetRowCells(closingActRow, [
                        actTt,
                        act.Activity.Name,
                        periodsOrSessions,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        act.Start?.ToString() ?? string.Empty,
                        act.End?.ToString() ?? string.Empty,
                        string.Empty,
                        act.Activity.Note ?? string.Empty
                    ]);
                    targetTable.AppendChild(closingActRow);
                }
            }
            else
            {
                // Dòng Thi tốt nghiệp (fallback options.Graduation)
                var gradRow = (TableRow)templateRow.CloneNode(true);
                var gradTt = (currentTt++).ToString();
                var gradPeriods = options.Graduation.Periods > 0 ? options.Graduation.Periods.ToString() : string.Empty;
                SetRowCells(gradRow, [
                    gradTt,
                    options.Graduation.Name,
                    gradPeriods,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    options.Graduation.Start ?? string.Empty,
                    options.Graduation.End ?? string.Empty,
                    string.Empty,
                    string.Empty
                ]);
                targetTable.AppendChild(gradRow);

                // Dòng Bế giảng (fallback options.Closing)
                var closingRow = (TableRow)templateRow.CloneNode(true);
                var closingTt = (currentTt++).ToString();
                var closingPeriods = options.Closing.Periods > 0 ? options.Closing.Periods.ToString() : string.Empty;
                SetRowCells(closingRow, [
                    closingTt,
                    options.Closing.Name,
                    closingPeriods,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    options.Closing.Start ?? string.Empty,
                    options.Closing.End ?? string.Empty,
                    string.Empty,
                    string.Empty
                ]);
                targetTable.AppendChild(closingRow);
            }

            // Lưu thay đổi vào stream
            wordDoc.MainDocumentPart.Document.Save();
        }

        return memoryStream.ToArray();
    }

    private byte[] GetTemplateBytes(ExportOptions options)
    {
        if (options.TemplateBytes != null && options.TemplateBytes.Length > 0)
        {
            return options.TemplateBytes;
        }

        var candidatePaths = new List<string>();

        if (!string.IsNullOrWhiteSpace(options.TemplatePath))
        {
            candidatePaths.Add(options.TemplatePath);
            if (!Path.IsPathRooted(options.TemplatePath))
            {
                if (_environment != null)
                {
                    candidatePaths.Add(Path.Combine(_environment.ContentRootPath, options.TemplatePath));
                }
                candidatePaths.Add(Path.Combine(AppContext.BaseDirectory, options.TemplatePath));
            }
        }

        var basePath = _environment?.ContentRootPath ?? AppContext.BaseDirectory;
        candidatePaths.Add(Path.Combine(basePath, "App_Data", "Templates", "Custom_BieuMau_KeHoachGiangDay.docx"));
        candidatePaths.Add(Path.Combine(basePath, "App_Data", "Templates", "BieuMau_KeHoachGiangDay.docx"));
        candidatePaths.Add(Path.Combine(AppContext.BaseDirectory, "TestData", "BieuMau_KeHoachGiangDay.docx"));
        candidatePaths.Add(Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "Templates", "BieuMau_KeHoachGiangDay.docx"));

        foreach (var path in candidatePaths)
        {
            if (File.Exists(path))
            {
                return File.ReadAllBytes(path);
            }
        }

        throw new FileNotFoundException("Không tìm thấy file mẫu Word (.docx) để xuất kế hoạch giảng dạy.");
    }

    private static void SetRowCells(TableRow row, string[] values)
    {
        var cells = row.Elements<TableCell>().ToList();
        for (int i = 0; i < cells.Count && i < values.Length; i++)
        {
            SetCellText(cells[i], values[i]);
        }
    }

    private static void SetCellText(TableCell cell, string text)
    {
        var p = cell.Elements<Paragraph>().FirstOrDefault();
        if (p == null)
        {
            p = new Paragraph();
            cell.AppendChild(p);
        }

        foreach (var extraP in cell.Elements<Paragraph>().Skip(1).ToList())
        {
            extraP.Remove();
        }

        var run = p.Elements<Run>().FirstOrDefault();
        if (run == null)
        {
            run = new Run();
            p.AppendChild(run);
        }

        foreach (var extraRun in p.Elements<Run>().Skip(1).ToList())
        {
            extraRun.Remove();
        }

        var textNode = run.Elements<Text>().FirstOrDefault();
        if (textNode == null)
        {
            textNode = new Text();
            run.AppendChild(textNode);
        }

        textNode.Text = text;
        textNode.Space = SpaceProcessingModeValues.Preserve;
    }
}
