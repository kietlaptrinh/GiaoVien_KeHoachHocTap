using System.ComponentModel.DataAnnotations;
using System.Text;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using KeHoachHocTap.Web.Data;
using KeHoachHocTap.Web.Domain;
using KeHoachHocTap.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeHoachHocTap.Web.Pages;

public class GradesModel : PageModel
{
    private readonly IPlanRepository _repository;
    private readonly IGradeService _gradeService;
    private readonly IExcelGradeImporter _gradeImporter;
    private readonly IAdminAuthService _authService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<GradesModel> _logger;

    public GradesModel(
        IPlanRepository repository,
        IGradeService gradeService,
        IExcelGradeImporter gradeImporter,
        IAdminAuthService authService,
        IAuthorizationService authorizationService,
        IWebHostEnvironment environment,
        ILogger<GradesModel> logger)
    {
        _repository = repository;
        _gradeService = gradeService;
        _gradeImporter = gradeImporter;
        _authService = authService;
        _authorizationService = authorizationService;
        _environment = environment;
        _logger = logger;
    }

    public GradeSheetResult? GradeSheet { get; private set; }
    public IReadOnlyList<Subject> Subjects => GradeSheet?.Subjects ?? Array.Empty<Subject>();
    public IReadOnlyList<StudentSummary> DisplayStudents { get; private set; } = Array.Empty<StudentSummary>();
    public int TotalStudents => GradeSheet?.TotalStudents ?? 0;
    public IReadOnlyDictionary<string, int> ClassificationStats => GradeSheet?.ClassificationStats ?? new Dictionary<string, int>();

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? RankFilter { get; set; }

    public bool IsAdmin => User.Identity?.IsAuthenticated == true;
    public bool IsPasswordConfigured => _authService.IsPasswordConfigured();
    public string? DatabaseError { get; private set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class StudentInputModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Mã số học viên không được để trống.")]
        [StringLength(50, ErrorMessage = "Mã số học viên tối đa 50 ký tự.")]
        public string StudentCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ lót không được để trống.")]
        [StringLength(100, ErrorMessage = "Họ lót tối đa 100 ký tự.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên học viên không được để trống.")]
        [StringLength(50, ErrorMessage = "Tên tối đa 50 ký tự.")]
        public string FirstName { get; set; } = string.Empty;

        public DateOnly? DateOfBirth { get; set; }

        [StringLength(10, ErrorMessage = "Giới tính tối đa 10 ký tự.")]
        public string? Gender { get; set; }

        [StringLength(200, ErrorMessage = "Ghi chú tối đa 200 ký tự.")]
        public string? Note { get; set; }
    }

    public async Task OnGetAsync()
    {
        try
        {
            var subjects = await _repository.GetSubjectsAsync();
            var students = await _repository.GetStudentsWithGradesAsync();

            GradeSheet = _gradeService.CalculateGradeSheet(subjects, students);

            var filtered = GradeSheet.Students.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var term = Search.Trim().ToLowerInvariant();
                filtered = filtered.Where(s =>
                    s.Student.FullName.ToLowerInvariant().Contains(term) ||
                    s.Student.StudentCode.ToLowerInvariant().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(RankFilter))
            {
                var rank = RankFilter.Trim();
                filtered = filtered.Where(s => string.Equals(s.Classification, rank, StringComparison.OrdinalIgnoreCase));
            }

            DisplayStudents = filtered.ToList();
            ViewData["TotalSubjects"] = subjects.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tải bảng điểm học viên.");
            DatabaseError = "Không thể tải dữ liệu bảng điểm từ cơ sở dữ liệu: " + ex.Message;
        }
    }

    public async Task<IActionResult> OnPostAddStudentAsync(StudentInputModel input)
    {
        var auth = await _authorizationService.AuthorizeAsync(User, "Admin");
        if (!auth.Succeeded) return Forbid();

        if (!ModelState.IsValid)
        {
            ErrorMessage = "Dữ liệu nhập không hợp lệ.";
            return RedirectToPage(new { search = Search, rankFilter = RankFilter });
        }

        try
        {
            var student = new Student
            {
                StudentCode = input.StudentCode.Trim(),
                LastName = input.LastName.Trim(),
                FirstName = input.FirstName.Trim(),
                DateOfBirth = input.DateOfBirth,
                Gender = string.IsNullOrWhiteSpace(input.Gender) ? null : input.Gender.Trim(),
                Note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim()
            };

            await _repository.AddStudentAsync(student);
            SuccessMessage = $"Đã thêm học viên '{student.FullName}' ({student.StudentCode}) thành công.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi thêm học viên.");
            ErrorMessage = "Không thể thêm học viên: " + ex.Message;
        }

        return RedirectToPage(new { search = Search, rankFilter = RankFilter });
    }

    public async Task<IActionResult> OnPostUpdateStudentAsync(StudentInputModel input)
    {
        var auth = await _authorizationService.AuthorizeAsync(User, "Admin");
        if (!auth.Succeeded) return Forbid();

        if (!ModelState.IsValid)
        {
            ErrorMessage = "Dữ liệu nhập không hợp lệ.";
            return RedirectToPage(new { search = Search, rankFilter = RankFilter });
        }

        try
        {
            var student = await _repository.GetStudentByIdAsync(input.Id);
            if (student == null)
            {
                ErrorMessage = "Học viên không tồn tại.";
                return RedirectToPage(new { search = Search, rankFilter = RankFilter });
            }

            student.StudentCode = input.StudentCode.Trim();
            student.LastName = input.LastName.Trim();
            student.FirstName = input.FirstName.Trim();
            student.DateOfBirth = input.DateOfBirth;
            student.Gender = string.IsNullOrWhiteSpace(input.Gender) ? null : input.Gender.Trim();
            student.Note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim();

            await _repository.UpdateStudentAsync(student);
            SuccessMessage = $"Đã cập nhật thông tin học viên '{student.FullName}' thành công.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật học viên Id {Id}.", input.Id);
            ErrorMessage = "Không thể cập nhật học viên: " + ex.Message;
        }

        return RedirectToPage(new { search = Search, rankFilter = RankFilter });
    }

    public async Task<IActionResult> OnPostDeleteStudentAsync(int id)
    {
        var auth = await _authorizationService.AuthorizeAsync(User, "Admin");
        if (!auth.Succeeded) return Forbid();

        try
        {
            var student = await _repository.GetStudentByIdAsync(id);
            if (student != null)
            {
                var name = student.FullName;
                await _repository.DeleteStudentAsync(id);
                SuccessMessage = $"Đã xóa học viên '{name}' thành công.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa học viên Id {Id}.", id);
            ErrorMessage = "Không thể xóa học viên: " + ex.Message;
        }

        return RedirectToPage(new { search = Search, rankFilter = RankFilter });
    }

    public async Task<IActionResult> OnPostUpdateGradeAsync(int studentId, int subjectId, decimal? score)
    {
        var auth = await _authorizationService.AuthorizeAsync(User, "Admin");
        if (!auth.Succeeded)
        {
            return StatusCode(403, new { success = false, message = "Bạn không có quyền thực hiện thao tác này." });
        }

        if (score.HasValue && (score.Value < 0 || score.Value > 10))
        {
            return BadRequest(new { success = false, message = "Điểm phải nằm trong thang điểm từ 0.0 đến 10.0." });
        }

        try
        {
            decimal? roundedScore = score.HasValue ? Math.Round(score.Value, 2, MidpointRounding.AwayFromZero) : null;
            await _repository.UpdateSingleGradeAsync(studentId, subjectId, roundedScore);

            return new JsonResult(new { success = true, score = roundedScore });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật điểm cho StudentId {StudentId}, SubjectId {SubjectId}.", studentId, subjectId);
            return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
        }
    }

    public async Task<IActionResult> OnPostImportExcelAsync(IFormFile? file, bool preserveExisting = false)
    {
        var auth = await _authorizationService.AuthorizeAsync(User, "Admin");
        if (!auth.Succeeded) return Forbid();

        if (file == null || file.Length == 0)
        {
            ErrorMessage = "Vui lòng chọn file Excel bảng điểm (.xlsx, .xls).";
            return RedirectToPage();
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var importResult = _gradeImporter.ReadGradeSheet(stream);

            if (importResult.Students.Count == 0)
            {
                ErrorMessage = "Không tìm thấy dữ liệu học viên hợp lệ trong file Excel tải lên.";
                return RedirectToPage();
            }

            var subjects = await _repository.GetSubjectsAsync();
            var subjectMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in subjects)
            {
                subjectMap[s.Name.Trim()] = s.Id;
            }

            var studentsToAdd = new List<Student>();
            foreach (var imp in importResult.Students)
            {
                var st = new Student
                {
                    SortOrder = imp.Stt,
                    StudentCode = imp.StudentCode,
                    LastName = imp.LastName,
                    FirstName = imp.FirstName,
                    DateOfBirth = imp.DateOfBirth,
                    Gender = imp.Gender
                };

                foreach (var g in imp.Grades)
                {
                    if (subjectMap.TryGetValue(g.SubjectName.Trim(), out var subId))
                    {
                        st.Grades.Add(new StudentGrade
                        {
                            SubjectId = subId,
                            Score = g.Score
                        });
                    }
                }
                studentsToAdd.Add(st);
            }

            await _repository.ImportStudentsAndGradesAsync(studentsToAdd, preserveExisting: preserveExisting);
            SuccessMessage = $"Đã nhập thành công {studentsToAdd.Count} học viên và điểm từ file Excel.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi nhập bảng điểm từ Excel.");
            ErrorMessage = "Không thể nạp bảng điểm từ Excel: " + ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetExportExcelAsync()
    {
        try
        {
            var subjects = await _repository.GetSubjectsAsync();
            var students = await _repository.GetStudentsWithGradesAsync();
            var gradeSheet = _gradeService.CalculateGradeSheet(subjects, students);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("BangDiemTongHop");

            // Title
            worksheet.Cell("A1").Value = "BẢNG ĐIỂM TỔNG HỢP HỌC VIÊN";
            worksheet.Cell("A1").Style.Font.Bold = true;
            worksheet.Cell("A1").Style.Font.FontSize = 16;
            worksheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell("A2").Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm} | Tổng số học viên: {gradeSheet.TotalStudents} | Số môn: {gradeSheet.Subjects.Count}";
            worksheet.Cell("A2").Style.Font.Italic = true;
            worksheet.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Header Row 4
            int r = 4;
            worksheet.Cell(r, 1).Value = "STT";
            worksheet.Cell(r, 2).Value = "Mã số HV";
            worksheet.Cell(r, 3).Value = "Họ và tên đệm";
            worksheet.Cell(r, 4).Value = "Tên";
            worksheet.Cell(r, 5).Value = "Ngày sinh";

            int c = 6;
            foreach (var sub in gradeSheet.Subjects)
            {
                worksheet.Cell(r, c).Value = sub.Name;
                c++;
            }

            int countCol = c;
            worksheet.Cell(r, countCol).Value = "Số môn";
            int avgCol = c + 1;
            worksheet.Cell(r, avgCol).Value = "ĐTB";
            int wordsCol = c + 2;
            worksheet.Cell(r, wordsCol).Value = "ĐTB bằng chữ";
            int rankCol = c + 3;
            worksheet.Cell(r, rankCol).Value = "Xếp loại";

            int totalCols = rankCol;

            worksheet.Range(1, 1, 1, totalCols).Merge();
            worksheet.Range(2, 1, 2, totalCols).Merge();

            var headerRange = worksheet.Range(r, 1, r, totalCols);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#EBF3FC");
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Data Rows
            r = 5;
            for (int i = 0; i < gradeSheet.Students.Count; i++)
            {
                var summary = gradeSheet.Students[i];
                var st = summary.Student;

                worksheet.Cell(r, 1).Value = summary.Stt;
                worksheet.Cell(r, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(r, 2).Value = st.StudentCode;
                worksheet.Cell(r, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(r, 3).Value = st.LastName;
                worksheet.Cell(r, 4).Value = st.FirstName;

                worksheet.Cell(r, 5).Value = st.DateOfBirth?.ToString("dd/MM/yyyy") ?? "";
                worksheet.Cell(r, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                int sc = 6;
                foreach (var gItem in summary.SubjectGrades)
                {
                    if (gItem.Score.HasValue)
                    {
                        worksheet.Cell(r, sc).Value = gItem.Score.Value;
                        worksheet.Cell(r, sc).Style.NumberFormat.Format = "0.00";
                    }
                    else
                    {
                        worksheet.Cell(r, sc).Value = "";
                    }
                    worksheet.Cell(r, sc).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    sc++;
                }

                worksheet.Cell(r, countCol).Value = summary.CompletedSubjectsCount;
                worksheet.Cell(r, countCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                if (summary.AverageScore.HasValue)
                {
                    worksheet.Cell(r, avgCol).Value = summary.AverageScore.Value;
                    worksheet.Cell(r, avgCol).Style.NumberFormat.Format = "0.00";
                    worksheet.Cell(r, avgCol).Style.Font.Bold = true;
                }
                else
                {
                    worksheet.Cell(r, avgCol).Value = "";
                }
                worksheet.Cell(r, avgCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(r, wordsCol).Value = summary.AverageScoreInWords;
                worksheet.Cell(r, rankCol).Value = summary.Classification;
                worksheet.Cell(r, rankCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                var dataRowRange = worksheet.Range(r, 1, r, totalCols);
                dataRowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                r++;
            }

            // Summary Stats footer
            r++;
            worksheet.Cell(r, 3).Value = "THỐNG KÊ XẾP LOẠI LỚP HỌC:";
            worksheet.Cell(r, 3).Style.Font.Bold = true;

            r++;
            foreach (var kv in gradeSheet.ClassificationStats)
            {
                worksheet.Cell(r, 3).Value = kv.Key;
                worksheet.Cell(r, 4).Value = $"{kv.Value} học viên";
                if (gradeSheet.TotalStudents > 0)
                {
                    var pct = (decimal)kv.Value / gradeSheet.TotalStudents * 100;
                    worksheet.Cell(r, 5).Value = $"{pct:0.0}%";
                }
                r++;
            }

            worksheet.Columns().AdjustToContents();

            using var memStream = new MemoryStream();
            workbook.SaveAs(memStream);
            var content = memStream.ToArray();

            var fileName = $"BANG_DIEM_TONG_HOP_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xuất Excel bảng điểm.");
            ErrorMessage = "Không thể xuất file Excel bảng điểm: " + ex.Message;
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnGetExportCsvAsync()
    {
        try
        {
            var subjects = await _repository.GetSubjectsAsync();
            var students = await _repository.GetStudentsWithGradesAsync();
            var gradeSheet = _gradeService.CalculateGradeSheet(subjects, students);

            var sb = new StringBuilder();
            sb.Append("STT,Mã HV,Họ và tên đệm,Tên,Ngày sinh");
            foreach (var sub in gradeSheet.Subjects)
            {
                sb.Append($",\"{sub.Name.Replace("\"", "\"\"")}\"");
            }
            sb.AppendLine(",Số môn hoàn thành,Điểm trung bình,Điểm bằng chữ,Xếp loại");

            foreach (var s in gradeSheet.Students)
            {
                var dob = s.Student.DateOfBirth?.ToString("dd/MM/yyyy") ?? "";
                sb.Append($"{s.Stt},\"{s.Student.StudentCode}\",\"{s.Student.LastName.Replace("\"", "\"\"")}\",\"{s.Student.FirstName.Replace("\"", "\"\"")}\",{dob}");
                foreach (var g in s.SubjectGrades)
                {
                    sb.Append($",{(g.Score.HasValue ? g.Score.Value.ToString("0.00") : "")}");
                }
                var avg = s.AverageScore.HasValue ? s.AverageScore.Value.ToString("0.00") : "";
                sb.AppendLine($",{s.CompletedSubjectsCount},{avg},\"{s.AverageScoreInWords}\",\"{s.Classification}\"");
            }

            var preamble = Encoding.UTF8.GetPreamble();
            var dataBytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fullBytes = new byte[preamble.Length + dataBytes.Length];
            Buffer.BlockCopy(preamble, 0, fullBytes, 0, preamble.Length);
            Buffer.BlockCopy(dataBytes, 0, fullBytes, preamble.Length, dataBytes.Length);

            var fileName = $"BANG_DIEM_TONG_HOP_{DateTime.Now:yyyyMMdd_HHmm}.csv";
            return File(fullBytes, "text/csv; charset=utf-8", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xuất CSV bảng điểm.");
            ErrorMessage = "Không thể xuất file CSV: " + ex.Message;
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnGetExportWordAsync()
    {
        try
        {
            var subjects = await _repository.GetSubjectsAsync();
            var students = await _repository.GetStudentsWithGradesAsync();
            var gradeSheet = _gradeService.CalculateGradeSheet(subjects, students);

            using var memStream = new MemoryStream();
            using (var wordDoc = WordprocessingDocument.Create(memStream, WordprocessingDocumentType.Document, true))
            {
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = new Body();

                // Kích thước trang A4 ngang (16838 x 11906 dxa), margin 720 dxa (0.5 inch)
                // Chiều rộng in khả dụng = 16838 - 1440 = 15398 dxa
                var sectionProps = new SectionProperties(
                    new PageSize { Width = 16838U, Height = 11906U, Orient = PageOrientationValues.Landscape },
                    new PageMargin { Top = 720, Bottom = 720, Left = 720, Right = 720 }
                );

                body.AppendChild(CreateWordParagraph("BẢNG ĐIỂM TỔNG HỢP HỌC VIÊN KHÓA HỌC", true, 26, JustificationValues.Center));
                body.AppendChild(CreateWordParagraph($"Tổng số: {gradeSheet.TotalStudents} học viên | Số môn học: {gradeSheet.Subjects.Count} môn", false, 20, JustificationValues.Center));

                // Tính toán độ rộng từng cột (dxa)
                const int sttW = 500;
                const int codeW = 1100;
                const int lastW = 1600;
                const int firstW = 850;
                const int dobW = 950;
                const int countW = 700;
                const int avgW = 700;
                const int rankW = 950;
                const int fixedWidthSum = sttW + codeW + lastW + firstW + dobW + countW + avgW + rankW; // 7350 dxa

                int subCount = Math.Max(1, gradeSheet.Subjects.Count);
                int subColWidth = Math.Max(380, (15398 - fixedWidthSum) / subCount);
                int totalTableWidth = fixedWidthSum + (subColWidth * gradeSheet.Subjects.Count);
                int totalCols = 5 + gradeSheet.Subjects.Count + 3;

                var table = new Table();
                var tblProps = new TableProperties(
                    new TableWidth { Type = TableWidthUnitValues.Dxa, Width = totalTableWidth.ToString() },
                    new TableLayout { Type = TableLayoutValues.Fixed },
                    new TableBorders(
                        new TopBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                        new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                        new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                        new RightBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                        new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" },
                        new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" }
                    ),
                    new TableJustification { Val = TableRowAlignmentValues.Center }
                );
                table.AppendChild(tblProps);

                // TableGrid - bắt buộc để Word Desktop & Word Online phân bổ lưới cột chính xác
                var tableGrid = new TableGrid();
                tableGrid.AppendChild(new GridColumn { Width = sttW.ToString() });
                tableGrid.AppendChild(new GridColumn { Width = codeW.ToString() });
                tableGrid.AppendChild(new GridColumn { Width = lastW.ToString() });
                tableGrid.AppendChild(new GridColumn { Width = firstW.ToString() });
                tableGrid.AppendChild(new GridColumn { Width = dobW.ToString() });
                foreach (var _ in gradeSheet.Subjects)
                {
                    tableGrid.AppendChild(new GridColumn { Width = subColWidth.ToString() });
                }
                tableGrid.AppendChild(new GridColumn { Width = countW.ToString() });
                tableGrid.AppendChild(new GridColumn { Width = avgW.ToString() });
                tableGrid.AppendChild(new GridColumn { Width = rankW.ToString() });
                table.AppendChild(tableGrid);

                // Header Row (lặp lại ở đầu mỗi trang nếu danh sách dài)
                var headerRow = new TableRow(
                    new TableRowProperties(
                        new TableRowHeight { Val = 1800U, HeightType = HeightRuleValues.AtLeast },
                        new TableHeader(),
                        new CantSplit()
                    )
                );
                headerRow.AppendChild(CreateWordCell("STT", true, true, sttW.ToString(), "EBF3FC"));
                headerRow.AppendChild(CreateWordCell("Mã HV", true, true, codeW.ToString(), "EBF3FC"));
                headerRow.AppendChild(CreateWordCell("Họ và tên đệm", true, false, lastW.ToString(), "EBF3FC"));
                headerRow.AppendChild(CreateWordCell("Tên", true, false, firstW.ToString(), "EBF3FC"));
                headerRow.AppendChild(CreateWordCell("Ngày sinh", true, true, dobW.ToString(), "EBF3FC"));
                foreach (var sub in gradeSheet.Subjects)
                {
                    headerRow.AppendChild(CreateWordCell(sub.Name, true, true, subColWidth.ToString(), "EBF3FC", isVertical: true, halfPtSize: 15));
                }
                headerRow.AppendChild(CreateWordCell("Số môn", true, true, countW.ToString(), "EBF3FC"));
                headerRow.AppendChild(CreateWordCell("Điểm TB", true, true, avgW.ToString(), "EBF3FC"));
                headerRow.AppendChild(CreateWordCell("Xếp loại", true, true, rankW.ToString(), "EBF3FC"));
                table.AppendChild(headerRow);

                if (gradeSheet.Students.Count == 0)
                {
                    var emptyRow = new TableRow(
                        new TableRowProperties(new CantSplit(), new TableRowHeight { Val = 600U }),
                        new TableCell(
                            new TableCellProperties(
                                new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = totalTableWidth.ToString() },
                                new GridSpan { Val = totalCols },
                                new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = "F8FAFC" },
                                new TableCellMargin(new TopMargin { Width = "200" }, new BottomMargin { Width = "200" })
                            ),
                            CreateWordParagraph("Chưa có dữ liệu học viên (Danh sách trống). Vui lòng nạp học viên từ file Excel hoặc thêm trực tiếp trên web.", false, 20, JustificationValues.Center)
                        )
                    );
                    table.AppendChild(emptyRow);
                }
                else
                {
                    foreach (var s in gradeSheet.Students)
                    {
                        var row = new TableRow(new TableRowProperties(new CantSplit()));
                        row.AppendChild(CreateWordCell(s.Stt.ToString(), false, true, sttW.ToString()));
                        row.AppendChild(CreateWordCell(s.Student.StudentCode, false, true, codeW.ToString()));
                        row.AppendChild(CreateWordCell(s.Student.LastName, false, false, lastW.ToString()));
                        row.AppendChild(CreateWordCell(s.Student.FirstName, true, false, firstW.ToString()));
                        row.AppendChild(CreateWordCell(s.Student.DateOfBirth?.ToString("dd/MM/yyyy") ?? "", false, true, dobW.ToString()));
                        foreach (var g in s.SubjectGrades)
                        {
                            var scoreStr = g.Score.HasValue ? g.Score.Value.ToString("0.00") : "";
                            row.AppendChild(CreateWordCell(scoreStr, false, true, subColWidth.ToString()));
                        }
                        row.AppendChild(CreateWordCell($"{s.CompletedSubjectsCount}/{gradeSheet.Subjects.Count}", false, true, countW.ToString()));
                        row.AppendChild(CreateWordCell(s.AverageScore.HasValue ? s.AverageScore.Value.ToString("0.00") : "—", true, true, avgW.ToString()));
                        row.AppendChild(CreateWordCell(s.Classification, true, true, rankW.ToString()));
                        table.AppendChild(row);
                    }
                }

                body.AppendChild(table);
                body.AppendChild(sectionProps);
                mainPart.Document.AppendChild(body);
            }

            var content = memStream.ToArray();
            var fileName = $"BANG_DIEM_TONG_HOP_{DateTime.Now:yyyyMMdd_HHmm}.docx";
            return File(content, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xuất Word bảng điểm.");
            ErrorMessage = "Không thể xuất file Word bảng điểm: " + ex.Message;
            return RedirectToPage();
        }
    }

    private static Paragraph CreateWordParagraph(string text, bool isBold, int halfPtSize, JustificationValues align)
    {
        return new Paragraph(
            new ParagraphProperties(new Justification { Val = align }),
            new Run(
                new RunProperties(
                    new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                    new FontSize { Val = halfPtSize.ToString() },
                    isBold ? new Bold() : new Bold { Val = false }
                ),
                new Text(text)
            )
        );
    }

    private static TableCell CreateWordCell(
        string text,
        bool isBold,
        bool isCenter,
        string width,
        string? bgColor = null,
        bool isVertical = false,
        int halfPtSize = 16)
    {
        var cellProps = new TableCellProperties(
            new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = width },
            new TableCellMargin(
                new TopMargin { Width = "60" },
                new BottomMargin { Width = "60" },
                new LeftMargin { Width = "60" },
                new RightMargin { Width = "60" }
            ),
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
        );

        if (!string.IsNullOrEmpty(bgColor))
        {
            cellProps.AppendChild(new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = bgColor });
        }

        if (isVertical)
        {
            cellProps.AppendChild(new TextDirection { Val = TextDirectionValues.BottomToTopLeftToRight });
        }

        return new TableCell(
            cellProps,
            new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = isCenter ? JustificationValues.Center : JustificationValues.Left }
                ),
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                        new FontSize { Val = halfPtSize.ToString() },
                        isBold ? new Bold() : new Bold { Val = false }
                    ),
                    new Text(text ?? "")
                )
            )
        );
    }
}
