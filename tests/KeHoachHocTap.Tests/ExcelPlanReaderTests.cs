using ClosedXML.Excel;
using KeHoachHocTap.Web.Services;

namespace KeHoachHocTap.Tests;

public class ExcelPlanReaderTests
{
    private readonly ExcelPlanReader _reader = new();

    [Fact]
    public void Read_SampleExcelFile_ReturnsCorrectCountsAndOpeningDay()
    {
        // 1. Arrange: Mở file mẫu từ TestData
        var excelPath = Path.Combine(AppContext.BaseDirectory, "TestData", "ke_hoach_hoc_tap.xlsx");
        using var stream = File.OpenRead(excelPath);

        // 2. Act
        var result = _reader.Read(stream);

        // 3. Assert
        // Đọc đúng 19 môn
        Assert.Equal(19, result.Subjects.Count);

        // Tổng số tiết là 895
        var totalPeriods = result.Subjects.Sum(s => s.Periods);
        Assert.Equal(895, totalPeriods);

        // Đọc đúng 6 khoảng nghỉ
        Assert.Equal(6, result.Holidays.Count);

        // Ngày khai giảng là 10/09/2025
        var openingHoliday = result.Holidays.Single(h => h.IsOpeningDay);
        Assert.Equal(new DateOnly(2025, 9, 10), openingHoliday.From);
        Assert.Equal(new DateOnly(2025, 9, 10), openingHoliday.To);
        Assert.Contains("khai giảng", openingHoliday.Name, StringComparison.OrdinalIgnoreCase);

        // Bung ra đúng 18 ngày nghỉ (SPEC 3.2: bung khoảng C->D ra từng ngày, trùng 18 ngày trong cột F)
        var holidayDates = new HashSet<DateOnly>();
        foreach (var h in result.Holidays)
        {
            var cur = h.From;
            while (cur <= h.To)
            {
                holidayDates.Add(cur);
                cur = cur.AddDays(1);
            }
        }
        Assert.Equal(18, holidayDates.Count);

        // Không có cảnh báo lỗi với file mẫu
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Read_MissingKhungCTSheet_ReturnsWarning()
    {
        using var wb = new XLWorkbook();
        wb.AddWorksheet("Ngay nghi");
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;

        var result = _reader.Read(ms);

        Assert.Contains(result.Warnings, w => w.Contains("Khung CT", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Read_MissingNgayNghiSheet_ReturnsWarning()
    {
        using var wb = new XLWorkbook();
        wb.AddWorksheet("Khung CT");
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;

        var result = _reader.Read(ms);

        Assert.Contains(result.Warnings, w => w.Contains("Ngay nghi", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Read_InvalidSubjectPeriods_SkipsSubjectAndAddsWarning()
    {
        using var wb = new XLWorkbook();
        var wsKhung = wb.AddWorksheet("Khung CT");
        wsKhung.Cell(1, 1).Value = "Stt";
        wsKhung.Cell(1, 2).Value = "Môn học";
        wsKhung.Cell(1, 3).Value = "Số tiết học";

        wsKhung.Cell(2, 1).Value = 1;
        wsKhung.Cell(2, 2).Value = "Môn Hợp Lệ";
        wsKhung.Cell(2, 3).Value = 45;

        wsKhung.Cell(3, 1).Value = 2;
        wsKhung.Cell(3, 2).Value = "Môn Lỗi Số Tiết";
        wsKhung.Cell(3, 3).Value = "ABC"; // Không phải số

        var wsNghi = wb.AddWorksheet("Ngay nghi");
        wsNghi.Cell(1, 2).Value = "Tên Ngày";
        wsNghi.Cell(1, 3).Value = "Từ ngày";
        wsNghi.Cell(2, 2).Value = "Ngày khai giảng";
        wsNghi.Cell(2, 3).Value = new DateTime(2025, 9, 10);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;

        var result = _reader.Read(ms);

        Assert.Single(result.Subjects);
        Assert.Equal("Môn Hợp Lệ", result.Subjects[0].Name);
        Assert.Contains(result.Warnings, w => w.Contains("Môn Lỗi Số Tiết") || w.Contains("Dòng 3"));
    }

    [Fact]
    public void Read_HolidayToDateBeforeFromDate_SkipsRowAndAddsWarning()
    {
        using var wb = new XLWorkbook();
        var wsKhung = wb.AddWorksheet("Khung CT");
        wsKhung.Cell(1, 1).Value = "Stt";
        wsKhung.Cell(1, 2).Value = "Môn học";
        wsKhung.Cell(1, 3).Value = "Số tiết học";
        wsKhung.Cell(2, 1).Value = 1;
        wsKhung.Cell(2, 2).Value = "Môn 1";
        wsKhung.Cell(2, 3).Value = 30;

        var wsNghi = wb.AddWorksheet("Ngay nghi");
        wsNghi.Cell(1, 2).Value = "Tên Ngày";
        wsNghi.Cell(1, 3).Value = "Từ ngày";
        wsNghi.Cell(1, 4).Value = "Đến ngày";

        wsNghi.Cell(2, 2).Value = "Ngày khai giảng";
        wsNghi.Cell(2, 3).Value = new DateTime(2025, 9, 10);
        wsNghi.Cell(2, 4).Value = new DateTime(2025, 9, 10);

        wsNghi.Cell(3, 2).Value = "Nghỉ sai";
        wsNghi.Cell(3, 3).Value = new DateTime(2025, 12, 10);
        wsNghi.Cell(3, 4).Value = new DateTime(2025, 12, 5); // To < From

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;

        var result = _reader.Read(ms);

        Assert.Single(result.Holidays); // Chỉ còn khai giảng
        Assert.Contains(result.Warnings, w => w.Contains("Nghỉ sai") || w.Contains("nhỏ hơn"));
    }

    [Fact]
    public void Read_MissingOpeningDay_AddsWarning()
    {
        using var wb = new XLWorkbook();
        var wsKhung = wb.AddWorksheet("Khung CT");
        wsKhung.Cell(1, 1).Value = "Stt";
        wsKhung.Cell(1, 2).Value = "Môn học";
        wsKhung.Cell(1, 3).Value = "Số tiết học";
        wsKhung.Cell(2, 1).Value = 1;
        wsKhung.Cell(2, 2).Value = "Môn 1";
        wsKhung.Cell(2, 3).Value = 30;

        var wsNghi = wb.AddWorksheet("Ngay nghi");
        wsNghi.Cell(1, 2).Value = "Tên Ngày";
        wsNghi.Cell(1, 3).Value = "Từ ngày";
        wsNghi.Cell(2, 2).Value = "Tết Dương lịch";
        wsNghi.Cell(2, 3).Value = new DateTime(2026, 1, 1);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;

        var result = _reader.Read(ms);

        Assert.Contains(result.Warnings, w => w.Contains("khai giảng", StringComparison.OrdinalIgnoreCase));
    }
}
