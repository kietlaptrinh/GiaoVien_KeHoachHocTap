using System.Globalization;
using ClosedXML.Excel;
using KeHoachHocTap.Web.Data;

namespace KeHoachHocTap.Web.Services;

/// <summary>
/// Dịch vụ đọc file Excel kế hoạch học tập theo chuẩn SPEC mục 3.
/// </summary>
public class ExcelPlanReader : IExcelPlanReader
{
    public ExcelPlanReadResult Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var subjects = new List<Subject>();
        var holidays = new List<Holiday>();
        var warnings = new List<string>();

        using var workbook = new XLWorkbook(stream);

        // 1. Tìm Sheet "Khung CT"
        var subjectSheet = workbook.Worksheets.FirstOrDefault(ws =>
            ws.Name.Trim().Equals("Khung CT", StringComparison.OrdinalIgnoreCase))
            ?? workbook.Worksheets.FirstOrDefault(ws =>
            ws.Name.Contains("Khung", StringComparison.OrdinalIgnoreCase));

        if (subjectSheet == null)
        {
            warnings.Add("Thiếu sheet 'Khung CT' trong file Excel.");
        }
        else
        {
            ReadSubjects(subjectSheet, subjects, warnings);
        }

        // 2. Tìm Sheet "Ngay nghi"
        var holidaySheet = workbook.Worksheets.FirstOrDefault(ws =>
            ws.Name.Trim().Equals("Ngay nghi", StringComparison.OrdinalIgnoreCase))
            ?? workbook.Worksheets.FirstOrDefault(ws =>
            ws.Name.Contains("nghi", StringComparison.OrdinalIgnoreCase));

        if (holidaySheet == null)
        {
            warnings.Add("Thiếu sheet 'Ngay nghi' trong file Excel.");
        }
        else
        {
            ReadHolidays(holidaySheet, holidays, warnings);
        }

        // 3. Kiểm tra tính toàn vẹn dữ liệu
        if (subjects.Count == 0)
        {
            warnings.Add("Không tìm thấy môn học hợp lệ nào trong sheet 'Khung CT'.");
        }

        var openingHolidays = holidays.Where(h => h.IsOpeningDay).ToList();
        if (openingHolidays.Count == 0)
        {
            warnings.Add("Không tìm thấy ngày khai giảng trong sheet 'Ngay nghi'.");
        }
        else if (openingHolidays.Count > 1)
        {
            warnings.Add("Tìm thấy nhiều hơn một ngày khai giảng. Hệ thống chọn ngày đầu tiên làm ngày khai giảng chính thức.");
            // Giữ lại đúng 1 ngày đầu tiên là IsOpeningDay
            for (int i = 1; i < openingHolidays.Count; i++)
            {
                openingHolidays[i].IsOpeningDay = false;
            }
        }

        return new ExcelPlanReadResult(subjects, holidays, warnings);
    }

    private static void ReadSubjects(IXLWorksheet sheet, List<Subject> subjects, List<string> warnings)
    {
        int lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
        int sortOrder = 1;

        // Bắt đầu từ dòng 2 (bỏ qua dòng tiêu đề)
        for (int row = 2; row <= lastRow; row++)
        {
            var nameCell = sheet.Cell(row, 2); // Cột B: Tên môn
            var periodsCell = sheet.Cell(row, 3); // Cột C: Số tiết học

            var name = nameCell.GetString().Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                // Bỏ qua dòng trống tên môn
                continue;
            }

            // Đọc số tiết (Cột C) - TUYỆT ĐỐI KHÔNG đọc cột D-F (SPEC 3.1)
            int periods = 0;
            bool validPeriods = false;

            if (periodsCell.DataType == XLDataType.Number)
            {
                var val = periodsCell.GetDouble();
                if (val > 0 && Math.Abs(val - Math.Round(val)) < 0.001)
                {
                    periods = (int)Math.Round(val);
                    validPeriods = true;
                }
            }
            else if (int.TryParse(periodsCell.GetString().Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                if (parsed > 0)
                {
                    periods = parsed;
                    validPeriods = true;
                }
            }

            if (!validPeriods)
            {
                warnings.Add($"Dòng {row} sheet 'Khung CT': Môn '{name}' có số tiết không hợp lệ ({periodsCell.GetString().Trim()}). Đã bỏ qua môn này.");
                continue;
            }

            subjects.Add(new Subject
            {
                SortOrder = sortOrder++,
                Name = name,
                Periods = periods,
                Grade = null,
                ReservePeriods = null,
                Note = null
            });
        }
    }

    private static void ReadHolidays(IXLWorksheet sheet, List<Holiday> holidays, List<string> warnings)
    {
        int lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
        int holidayId = 1;

        // Bắt đầu từ dòng 2 (bỏ qua dòng tiêu đề)
        for (int row = 2; row <= lastRow; row++)
        {
            var nameCell = sheet.Cell(row, 2); // Cột B: Tên Ngày
            var fromCell = sheet.Cell(row, 3); // Cột C: Từ ngày
            var toCell = sheet.Cell(row, 4);   // Cột D: Đến ngày (nếu rỗng thì Đến ngày = Từ ngày)
            // Cột E-F: Bỏ qua hoàn toàn

            var name = nameCell.GetString().Trim();
            bool hasFrom = TryGetDateOnly(fromCell, out var fromDate);

            // Nếu cả tên và ngày từ đều rỗng (vd các dòng cuối chỉ có cột F) -> bỏ qua
            if (string.IsNullOrWhiteSpace(name) && !hasFrom)
            {
                continue;
            }

            if (!hasFrom)
            {
                warnings.Add($"Dòng {row} sheet 'Ngay nghi': Không thể đọc ngày bắt đầu của kỳ nghỉ '{name}'. Đã bỏ qua dòng này.");
                continue;
            }

            DateOnly toDate;
            if (toCell.IsEmpty() || string.IsNullOrWhiteSpace(toCell.GetString()))
            {
                toDate = fromDate;
            }
            else if (TryGetDateOnly(toCell, out var parsedTo))
            {
                toDate = parsedTo;
            }
            else
            {
                warnings.Add($"Dòng {row} sheet 'Ngay nghi': Không thể đọc ngày kết thúc của kỳ nghỉ '{name}'. Đã bỏ qua dòng này.");
                continue;
            }

            if (toDate < fromDate)
            {
                warnings.Add($"Dòng {row} sheet 'Ngay nghi': Kỳ nghỉ '{name}' có Đến ngày ({toDate:d/M/yyyy}) trước Từ ngày ({fromDate:d/M/yyyy}). Đã bỏ qua.");
                continue;
            }

            // Nhận diện ngày khai giảng (chứa "khai giảng" hoặc "khai giang", không phân biệt hoa/thường)
            bool isOpening = name.Contains("khai giảng", StringComparison.OrdinalIgnoreCase) ||
                             name.Contains("khai giang", StringComparison.OrdinalIgnoreCase);

            if (isOpening)
            {
                toDate = fromDate; // Khai giảng chỉ diễn ra trong đúng 1 ngày
            }

            holidays.Add(new Holiday
            {
                Id = holidayId++,
                Name = name,
                From = fromDate,
                To = toDate,
                IsOpeningDay = isOpening
            });
        }
    }

    private static bool TryGetDateOnly(IXLCell cell, out DateOnly date)
    {
        date = default;
        if (cell.IsEmpty()) return false;

        try
        {
            if (cell.DataType == XLDataType.DateTime)
            {
                var dt = cell.GetDateTime();
                date = DateOnly.FromDateTime(dt);
                return true;
            }

            if (cell.TryGetValue<DateTime>(out var parsedDt))
            {
                date = DateOnly.FromDateTime(parsedDt);
                return true;
            }

            var text = cell.GetString().Trim();
            if (string.IsNullOrWhiteSpace(text)) return false;

            string[] formats =
            [
                "d/M/yyyy", "dd/MM/yyyy", "d-M-yyyy", "dd-MM-yyyy",
                "yyyy-MM-dd", "M/d/yyyy", "MM/dd/yyyy",
                "d/M/yyyy h:mm:ss tt", "d/M/yyyy HH:mm:ss",
                "dd/MM/yyyy h:mm:ss tt", "dd/MM/yyyy HH:mm:ss"
            ];

            if (DateOnly.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                return true;
            }

            if (DateOnly.TryParse(text, CultureInfo.InvariantCulture, out date))
            {
                return true;
            }

            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var generalDt))
            {
                date = DateOnly.FromDateTime(generalDt);
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }
}
