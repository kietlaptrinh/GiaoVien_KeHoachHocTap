using System.Globalization;
using ClosedXML.Excel;

namespace KeHoachHocTap.Web.Services;

/// <summary>
/// Dịch vụ đọc danh sách học viên và bảng điểm từ file Excel bằng ClosedXML.
/// Hỗ trợ cả định dạng chuẩn 1 dòng/học viên và định dạng bảng điểm K56 thực tế.
/// </summary>
public class ExcelGradeImporter : IExcelGradeImporter
{
    public ExcelGradeImportResult ReadGradeSheet(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Không tìm thấy file Excel bảng điểm tại '{filePath}'");
        }

        using var stream = File.OpenRead(filePath);
        return ReadGradeSheet(stream);
    }

    public ExcelGradeImportResult ReadGradeSheet(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var warnings = new List<string>();

        // Ưu tiên sheet có tên 'DanhSach' hoặc 'BangDiem' hoặc sheet đầu tiên
        var sheet = workbook.Worksheets.FirstOrDefault(w => w.Name.Equals("DanhSach", StringComparison.OrdinalIgnoreCase))
                 ?? workbook.Worksheets.FirstOrDefault(w => w.Name.Equals("BangDiem", StringComparison.OrdinalIgnoreCase))
                 ?? workbook.Worksheets.FirstOrDefault();

        if (sheet == null)
        {
            warnings.Add("File Excel không chứa bất kỳ sheet nào.");
            return new ExcelGradeImportResult(Array.Empty<string>(), Array.Empty<ImportedStudent>(), warnings);
        }

        // Thử đọc theo định dạng chuẩn (Format A: có hàng tiêu đề STT, Mã HV, Họ lót, Tên, Ngày sinh...)
        var result = TryReadStandardFormat(sheet, warnings);
        if (result != null && result.Students.Count > 0)
        {
            return result;
        }

        // Nếu không được, thử đọc theo định dạng K56 gốc (Format B: tiêu đề môn ở hàng 4, dữ liệu học viên hàng 8, 10...)
        result = TryReadK56OriginalFormat(sheet, warnings);
        if (result != null && result.Students.Count > 0)
        {
            return result;
        }

        warnings.Add($"Không nhận diện được cấu trúc bảng điểm trong sheet '{sheet.Name}'.");
        return new ExcelGradeImportResult(Array.Empty<string>(), Array.Empty<ImportedStudent>(), warnings);
    }

    private static ExcelGradeImportResult? TryReadStandardFormat(IXLWorksheet sheet, List<string> warnings)
    {
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
        var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        if (lastRow < 2 || lastCol < 4)
        {
            return null;
        }

        // Tìm hàng tiêu đề (trong 10 hàng đầu tiên)
        int headerRowIndex = 0;
        int sttCol = -1, codeCol = -1, lastNameCol = -1, firstNameCol = -1, dobCol = -1, genderCol = -1;
        int countCol = -1, avgCol = -1, rankCol = -1;

        for (int r = 1; r <= Math.Min(10, lastRow); r++)
        {
            for (int c = 1; c <= lastCol; c++)
            {
                var text = sheet.Cell(r, c).GetString().Trim().ToLowerInvariant();
                if (sttCol == -1 && (text == "stt" || text == "số tt")) sttCol = c;
                else if (codeCol == -1 && (text.Contains("mã") || text == "studentcode")) codeCol = c;
                else if (lastNameCol == -1 && (text.Contains("họ lót") || text.Contains("họ đệm") || text == "họ")) lastNameCol = c;
                else if (firstNameCol == -1 && (text == "tên" || text == "firstname")) firstNameCol = c;
                else if (dobCol == -1 && (text.Contains("ngày sinh") || text == "ns" || text == "dob")) dobCol = c;
                else if (genderCol == -1 && (text.Contains("phái") || text.Contains("giới tính"))) genderCol = c;
                else if (countCol == -1 && (text.Contains("số môn"))) countCol = c;
                else if (avgCol == -1 && (text.Contains("đtb") || text.Contains("điểm tb"))) avgCol = c;
                else if (rankCol == -1 && (text.Contains("xếp loại") || text == "loại")) rankCol = c;
            }

            if ((lastNameCol != -1 || firstNameCol != -1) && (sttCol != -1 || codeCol != -1 || dobCol != -1))
            {
                headerRowIndex = r;
                break;
            }

            // Reset nếu chưa phải hàng header chính
            sttCol = codeCol = lastNameCol = firstNameCol = dobCol = genderCol = countCol = avgCol = rankCol = -1;
        }

        if (headerRowIndex == 0)
        {
            return null;
        }

        // Xác định các cột môn học: các cột không phải metadata
        var metaCols = new HashSet<int> { sttCol, codeCol, lastNameCol, firstNameCol, dobCol, genderCol, countCol, avgCol, rankCol };
        var subjectCols = new List<(int ColumnNumber, string SubjectName)>();

        for (int c = 1; c <= lastCol; c++)
        {
            if (metaCols.Contains(c)) continue;
            var headerText = sheet.Cell(headerRowIndex, c).GetString().Trim();
            if (!string.IsNullOrEmpty(headerText) && !headerText.Equals("đtb bằng chữ", StringComparison.OrdinalIgnoreCase))
            {
                subjectCols.Add((c, headerText));
            }
        }

        if (subjectCols.Count == 0)
        {
            return null;
        }

        var subjectNames = subjectCols.Select(sc => sc.SubjectName).ToList();
        var students = new List<ImportedStudent>();

        for (int r = headerRowIndex + 1; r <= lastRow; r++)
        {
            var lastName = lastNameCol > 0 ? sheet.Cell(r, lastNameCol).GetString().Trim() : "";
            var firstName = firstNameCol > 0 ? sheet.Cell(r, firstNameCol).GetString().Trim() : "";

            if (string.IsNullOrEmpty(lastName) && string.IsNullOrEmpty(firstName))
            {
                continue;
            }

            var sttVal = sttCol > 0 ? ParseInt(sheet.Cell(r, sttCol).Value) : null;
            var stt = sttVal ?? (students.Count + 1);

            var code = codeCol > 0 ? sheet.Cell(r, codeCol).GetString().Trim() : "";
            if (string.IsNullOrEmpty(code))
            {
                code = $"K75A56.01{stt:D2}";
            }

            var dob = dobCol > 0 ? ParseDateOnly(sheet.Cell(r, dobCol)) : null;
            var gender = genderCol > 0 ? sheet.Cell(r, genderCol).GetString().Trim() : null;

            var grades = new List<ImportedGrade>();
            foreach (var (colNum, subjName) in subjectCols)
            {
                var score = ParseDecimal(sheet.Cell(r, colNum));
                grades.Add(new ImportedGrade(subjName, score));
            }

            decimal? avgScore = avgCol > 0 ? ParseDecimal(sheet.Cell(r, avgCol)) : null;
            string? rank = rankCol > 0 ? sheet.Cell(r, rankCol).GetString().Trim() : null;

            students.Add(new ImportedStudent(
                stt,
                code,
                lastName,
                firstName,
                dob,
                string.IsNullOrWhiteSpace(gender) ? null : gender,
                grades,
                avgScore,
                string.IsNullOrWhiteSpace(rank) ? null : rank));
        }

        return new ExcelGradeImportResult(subjectNames, students, warnings);
    }

    private static ExcelGradeImportResult? TryReadK56OriginalFormat(IXLWorksheet sheet, List<string> warnings)
    {
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
        var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        if (lastRow < 8 || lastCol < 10)
        {
            return null;
        }

        // Ở định dạng gốc K56: hàng 4 chứa tên các môn học từ cột 6 đến 27
        int subjectHeaderRow = 4;
        var subjectCols = new List<(int ColumnNumber, string SubjectName)>();

        for (int c = 6; c <= Math.Min(35, lastCol); c++)
        {
            var subjName = sheet.Cell(subjectHeaderRow, c).GetString().Trim();
            if (!string.IsNullOrEmpty(subjName)
                && !subjName.Contains("Số môn")
                && !subjName.Contains("ĐTB")
                && !subjName.Contains("Xếp loại"))
            {
                subjectCols.Add((c, subjName));
            }
        }

        if (subjectCols.Count == 0)
        {
            return null;
        }

        var subjectNames = subjectCols.Select(sc => sc.SubjectName).ToList();
        var students = new List<ImportedStudent>();

        // Học viên bắt đầu từ hàng 8, mỗi học viên cách 2 hàng (hàng 8, 10, 12...)
        for (int r = 8; r <= lastRow; r += 2)
        {
            var lastName = sheet.Cell(r, 1).GetString().Trim();
            if (string.IsNullOrEmpty(lastName))
            {
                continue;
            }

            var firstName = sheet.Cell(r, 4).GetString().Trim();
            var dobPart1 = sheet.Cell(r, 5).GetString().Trim();
            var dobPart2 = r + 1 <= lastRow ? sheet.Cell(r + 1, 5).GetString().Trim() : "";

            // Kết hợp chuỗi ngày sinh (vd: "14/04/198" + "2" = "14/04/1982")
            var dobFullStr = dobPart1 + dobPart2;
            var dob = ParseDateOnlyString(dobFullStr);

            var stt = students.Count + 1;
            var studentCode = $"K75A56.01{stt:D2}";

            var grades = new List<ImportedGrade>();
            foreach (var (colNum, subjName) in subjectCols)
            {
                var score = ParseDecimal(sheet.Cell(r, colNum));
                grades.Add(new ImportedGrade(subjName, score));
            }

            // ĐTB ở cột 29 (hoặc cột ngay sau môn cuối)
            var avgScore = ParseDecimal(sheet.Cell(r, 29));
            var rank = sheet.Cell(r, 31).GetString().Trim();

            students.Add(new ImportedStudent(
                stt,
                studentCode,
                lastName,
                firstName,
                dob,
                null,
                grades,
                avgScore,
                string.IsNullOrWhiteSpace(rank) ? null : rank));
        }

        return new ExcelGradeImportResult(subjectNames, students, warnings);
    }

    private static int? ParseInt(XLCellValue value)
    {
        if (value.IsNumber)
        {
            return (int)value.GetNumber();
        }
        var str = value.ToString().Trim();
        if (int.TryParse(str, out var i)) return i;
        return null;
    }

    private static decimal? ParseDecimal(IXLCell cell)
    {
        if (cell.IsEmpty()) return null;
        if (cell.Value.IsNumber)
        {
            return Math.Round((decimal)cell.Value.GetNumber(), 2);
        }

        var str = cell.GetString().Trim().Replace(',', '.');
        if (string.IsNullOrEmpty(str) || str == "—" || str == "-")
        {
            return null;
        }

        if (decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            return Math.Round(result, 2);
        }

        return null;
    }

    private static DateOnly? ParseDateOnly(IXLCell cell)
    {
        if (cell.IsEmpty()) return null;
        if (cell.Value.IsDateTime)
        {
            return DateOnly.FromDateTime(cell.Value.GetDateTime());
        }

        return ParseDateOnlyString(cell.GetString().Trim());
    }

    private static DateOnly? ParseDateOnlyString(string str)
    {
        if (string.IsNullOrWhiteSpace(str)) return null;

        var formats = new[]
        {
            "dd/MM/yyyy", "d/M/yyyy", "dd/M/yyyy", "d/MM/yyyy",
            "yyyy-MM-dd", "d/M/yy", "dd/MM/yy"
        };

        if (DateTime.TryParseExact(str, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            return DateOnly.FromDateTime(dt);
        }

        if (DateTime.TryParse(str, CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out var viDt))
        {
            return DateOnly.FromDateTime(viDt);
        }

        return null;
    }
}
