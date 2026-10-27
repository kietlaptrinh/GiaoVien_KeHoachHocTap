using ClosedXML.Excel;
using KeHoachHocTap.Web.Data;
using KeHoachHocTap.Web.Domain;
using KeHoachHocTap.Web.Services;
using Half = KeHoachHocTap.Web.Domain.Half;

namespace KeHoachHocTap.Tests;

public class ScheduleCalculatorTests
{
    private readonly ScheduleCalculator _calculator = new();
    private readonly ExcelPlanReader _reader = new();

    #region 1. Slot Unit Tests

    [Fact]
    public void Slot_ToString_FormatsCorrectlyWithoutLeadingZeros()
    {
        var morningSlot = new Slot(new DateOnly(2025, 9, 11), Half.S);
        var afternoonSlot = new Slot(new DateOnly(2026, 1, 6), Half.C);

        Assert.Equal("S 11/9/2025", morningSlot.ToString());
        Assert.Equal("C 6/1/2026", afternoonSlot.ToString());
    }

    [Fact]
    public void Slot_ParseAndTryParse_WorkAccurately()
    {
        var text = "C 18/9/2025";
        var parsed = Slot.Parse(text);

        Assert.Equal(new DateOnly(2025, 9, 18), parsed.Date);
        Assert.Equal(Half.C, parsed.Half);

        Assert.True(Slot.TryParse("S 1/1/2026", out var slot2));
        Assert.Equal(new DateOnly(2026, 1, 1), slot2.Date);
        Assert.Equal(Half.S, slot2.Half);

        Assert.False(Slot.TryParse("InvalidSlotString", out _));
    }

    [Fact]
    public void Slot_Comparison_OrdersChronologically()
    {
        var s1 = new Slot(new DateOnly(2025, 9, 11), Half.S);
        var s2 = new Slot(new DateOnly(2025, 9, 11), Half.C);
        var s3 = new Slot(new DateOnly(2025, 9, 12), Half.S);

        Assert.True(s1 < s2);
        Assert.True(s2 < s3);
        Assert.True(s1 < s3);
        Assert.Equal(0, s1.CompareTo(new Slot(new DateOnly(2025, 9, 11), Half.S)));
    }

    #endregion

    #region 2. Golden Test - 19 Môn Học Chuẩn SPEC Mục 4

    // Dữ liệu chuẩn được định nghĩa rõ ràng trong SPEC mục 4
    private static readonly (int Stt, string Name, int Periods, int Sessions, string Start, string End, string Exam)[] GoldenTable =
    [
        (1,  "Tên môn học 01", 60, 12, "S 11/9/2025",  "C 18/9/2025",  "S 19/9/2025"),
        (2,  "Tên môn học 02", 55, 11, "C 19/9/2025",  "C 26/9/2025",  "S 29/9/2025"),
        (3,  "Tên môn học 03", 55, 11, "C 29/9/2025",  "C 6/10/2025",  "S 7/10/2025"),
        (4,  "Tên môn học 04", 40, 8,  "C 7/10/2025",  "S 13/10/2025", "C 13/10/2025"),
        (5,  "Tên môn học 05", 60, 12, "S 14/10/2025", "C 21/10/2025", "S 22/10/2025"),
        (6,  "Tên môn học 06", 70, 14, "C 22/10/2025", "S 31/10/2025", "C 31/10/2025"),
        (7,  "Tên môn học 07", 45, 9,  "S 3/11/2025",  "S 7/11/2025",  "C 7/11/2025"),
        (8,  "Tên môn học 08", 45, 9,  "S 10/11/2025", "S 14/11/2025", "C 14/11/2025"),
        (9,  "Tên môn học 09", 40, 8,  "S 17/11/2025", "C 20/11/2025", "S 21/11/2025"),
        (10, "Tên môn học 10", 45, 9,  "C 21/11/2025", "C 27/11/2025", "S 28/11/2025"),
        (11, "Tên môn học 11", 70, 14, "C 28/11/2025", "S 9/12/2025",  "C 9/12/2025"),
        (12, "Tên môn học 12", 35, 7,  "S 10/12/2025", "S 15/12/2025", "C 15/12/2025"),
        (13, "Tên môn học 13", 50, 10, "S 16/12/2025", "C 22/12/2025", "S 23/12/2025"),
        (14, "Tên môn học 14", 45, 9,  "C 23/12/2025", "C 29/12/2025", "S 30/12/2025"),
        (15, "Tên môn học 15", 40, 8,  "C 30/12/2025", "S 6/1/2026",   "C 6/1/2026"),
        (16, "Tên môn học 16", 35, 7,  "S 7/1/2026",   "S 12/1/2026",  "C 12/1/2026"),
        (17, "Tên môn học 17", 30, 6,  "S 13/1/2026",  "C 15/1/2026",  "S 16/1/2026"),
        (18, "Tên môn học 18", 35, 7,  "C 16/1/2026",  "C 21/1/2026",  "S 22/1/2026"),
        (19, "Tên môn học 19", 40, 8,  "C 22/1/2026",  "S 28/1/2026",  "C 28/1/2026")
    ];

    [Fact]
    public void Calculate_Sample19Subjects_MatchesGoldenTable100Percent()
    {
        // 1. Arrange: Đọc subjects và holidays từ file Excel mẫu (nguồn thực tế)
        var excelPath = Path.Combine(AppContext.BaseDirectory, "TestData", "ke_hoach_hoc_tap.xlsx");
        using var stream = File.OpenRead(excelPath);
        var readResult = _reader.Read(stream);

        // 2. Act: Chạy thuật toán xếp lịch thuần túy
        var scheduleResult = _calculator.Calculate(readResult.Subjects, readResult.Holidays);

        // 3. Assert: Kiểm tra tổng quát
        Assert.Equal(new DateOnly(2025, 9, 10), scheduleResult.OpeningDay);
        Assert.Equal(19, scheduleResult.Items.Count);
        Assert.Empty(scheduleResult.Warnings);

        int totalSessions = scheduleResult.Items.Sum(item => item.Sessions);
        Assert.Equal(179, totalSessions);

        int totalPeriods = scheduleResult.Items.Sum(item => item.Subject.Periods);
        Assert.Equal(895, totalPeriods);

        // 4. Assert: Đối chiếu 100% từng dòng với bảng Golden trong SPEC mục 4
        for (int i = 0; i < GoldenTable.Length; i++)
        {
            var expected = GoldenTable[i];
            var actual = scheduleResult.Items[i];

            Assert.Equal(expected.Stt, actual.Number);
            Assert.Equal(expected.Name, actual.Subject.Name);
            Assert.Equal(expected.Periods, actual.Subject.Periods);
            Assert.Equal(expected.Sessions, actual.Sessions);
            Assert.Equal(expected.Start, actual.Start.ToString());
            Assert.Equal(expected.End, actual.End.ToString());
            Assert.Equal(expected.Exam, actual.Exam.ToString());
        }

        // 5. Assert: Đối chiếu thêm với giá trị cache trong cột D, E, F của file Excel mẫu
        using var workbook = new XLWorkbook(excelPath);
        var sheet = workbook.Worksheet("Khung CT");
        for (int i = 0; i < 19; i++)
        {
            int row = i + 2;
            var cachedStart = sheet.Cell(row, 4).GetString().Trim();
            var cachedEnd = sheet.Cell(row, 5).GetString().Trim();
            var cachedExam = sheet.Cell(row, 6).GetString().Trim();

            var actual = scheduleResult.Items[i];
            Assert.Equal(cachedStart, actual.Start.ToString());
            Assert.Equal(cachedEnd, actual.End.ToString());
            Assert.Equal(cachedExam, actual.Exam.ToString());
        }
    }

    #endregion

    #region 3. Edge Cases & Boundary Conditions

    [Fact]
    public void Calculate_SubjectStartsInAfternoon_WhenPreviousExamIsInMorning()
    {
        // Khi môn trước thi vào buổi sáng (ví dụ S 19/9/2025), môn sau bắt đầu vào buổi chiều (C 19/9/2025)
        var subjects = new List<Subject>
        {
            new() { SortOrder = 1, Name = "Môn 1", Periods = 10 }, // 2 buổi: S 11/9, C 11/9 -> Thi: S 12/9
            new() { SortOrder = 2, Name = "Môn 2", Periods = 10 }  // Bắt đầu: C 12/9
        };

        var holidays = new List<Holiday>
        {
            new() { Name = "Khai giảng", From = new DateOnly(2025, 9, 10), To = new DateOnly(2025, 9, 10), IsOpeningDay = true }
        };

        var result = _calculator.Calculate(subjects, holidays);

        Assert.Equal(2, result.Items.Count);
        // Môn 1
        Assert.Equal("S 11/9/2025", result.Items[0].Start.ToString());
        Assert.Equal("C 11/9/2025", result.Items[0].End.ToString());
        Assert.Equal("S 12/9/2025", result.Items[0].Exam.ToString());

        // Môn 2 bắt đầu ngay buổi chiều cùng ngày với buổi thi môn 1
        Assert.Equal("C 12/9/2025", result.Items[1].Start.ToString());
    }

    [Fact]
    public void Calculate_SkipsTetHoliday_Correctly()
    {
        // Môn học kéo dài qua kỳ nghỉ Tết Nguyên Đán 12/02/2026 - 22/02/2026
        // Ngày 11/02/2026 là Thứ Tư -> ngày nghỉ từ Thứ Năm 12/02 đến Chủ Nhật 22/02 -> Thứ Hai 23/02/2026 học tiếp
        var subjects = new List<Subject>
        {
            new() { SortOrder = 1, Name = "Môn xuyên Tết", Periods = 20 } // 4 buổi
        };

        var holidays = new List<Holiday>
        {
            new() { Name = "Khai giảng", From = new DateOnly(2026, 2, 10), To = new DateOnly(2026, 2, 10), IsOpeningDay = true },
            new() { Name = "Tết Nguyên Đán", From = new DateOnly(2026, 2, 12), To = new DateOnly(2026, 2, 22), IsOpeningDay = false }
        };

        var result = _calculator.Calculate(subjects, holidays);

        var item = Assert.Single(result.Items);
        // Start: S 11/2/2026 (buổi 1)
        // Chiều 11/2/2026 (buổi 2)
        // 12/2 đến 22/2 nghỉ Tết. 21-22/2 là T7-CN.
        // Buổi 3: S 23/2/2026
        // Buổi 4: C 23/2/2026 (End)
        // Thi: S 24/2/2026
        Assert.Equal("S 11/2/2026", item.Start.ToString());
        Assert.Equal("C 23/2/2026", item.End.ToString());
        Assert.Equal("S 24/2/2026", item.Exam.ToString());
    }

    [Fact]
    public void Calculate_PeriodsNotDivisibleBy5_RoundsUpSessionsAndWarns()
    {
        var subjects = new List<Subject>
        {
            new() { SortOrder = 1, Name = "Môn 42 tiết", Periods = 42 } // 42 / 5 = 8.4 -> 9 buổi
        };

        var holidays = new List<Holiday>
        {
            new() { Name = "Khai giảng", From = new DateOnly(2025, 9, 10), To = new DateOnly(2025, 9, 10), IsOpeningDay = true }
        };

        var result = _calculator.Calculate(subjects, holidays);

        var item = Assert.Single(result.Items);
        Assert.Equal(9, item.Sessions);
        Assert.Contains(result.Warnings, w => w.Contains("không chia hết cho 5"));
    }

    [Fact]
    public void Calculate_SubjectWithInvalidPeriods_SkipsAndWarns()
    {
        var subjects = new List<Subject>
        {
            new() { SortOrder = 1, Name = "Môn Hợp Lệ", Periods = 20 },
            new() { SortOrder = 2, Name = "Môn 0 Tiết", Periods = 0 },
            new() { SortOrder = 3, Name = "Môn Âm Tiết", Periods = -5 }
        };

        var holidays = new List<Holiday>
        {
            new() { Name = "Khai giảng", From = new DateOnly(2025, 9, 10), To = new DateOnly(2025, 9, 10), IsOpeningDay = true }
        };

        var result = _calculator.Calculate(subjects, holidays);

        Assert.Single(result.Items);
        Assert.Equal("Môn Hợp Lệ", result.Items[0].Subject.Name);
        Assert.Equal(2, result.Warnings.Count);
        Assert.Contains(result.Warnings, w => w.Contains("Môn 0 Tiết"));
        Assert.Contains(result.Warnings, w => w.Contains("Môn Âm Tiết"));
    }

    [Fact]
    public void Calculate_OverlappingHolidays_MergesCorrectly()
    {
        // 2 khoảng nghỉ chồng nhau: 15/9 -> 18/9 và 17/9 -> 22/9
        var subjects = new List<Subject>
        {
            new() { SortOrder = 1, Name = "Môn test", Periods = 10 } // 2 buổi
        };

        var holidays = new List<Holiday>
        {
            new() { Name = "Khai giảng", From = new DateOnly(2025, 9, 10), To = new DateOnly(2025, 9, 10), IsOpeningDay = true },
            new() { Name = "Nghỉ đợt 1", From = new DateOnly(2025, 9, 15), To = new DateOnly(2025, 9, 18) },
            new() { Name = "Nghỉ đợt 2", From = new DateOnly(2025, 9, 17), To = new DateOnly(2025, 9, 22) } // 22/9 là Thứ Hai
        };

        var result = _calculator.Calculate(subjects, holidays);

        // Khai giảng 10/9 (T4).
        // 11/9 (T5): S, C môn học kết thúc
        // 12/9 (T6): Thi
        var item = Assert.Single(result.Items);
        Assert.Equal("S 11/9/2025", item.Start.ToString());
        Assert.Equal("C 11/9/2025", item.End.ToString());
        Assert.Equal("S 12/9/2025", item.Exam.ToString());
    }

    [Fact]
    public void Calculate_MissingOpeningDay_ReturnsEmptyItemsAndWarning()
    {
        var subjects = new List<Subject>
        {
            new() { SortOrder = 1, Name = "Môn 1", Periods = 30 }
        };

        var holidays = new List<Holiday>
        {
            new() { Name = "Tết Dương lịch", From = new DateOnly(2026, 1, 1), To = new DateOnly(2026, 1, 1) }
        };

        var result = _calculator.Calculate(subjects, holidays);

        Assert.Empty(result.Items);
        Assert.Contains(result.Warnings, w => w.Contains("khai giảng", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Calculate_SafetyLimit5Years_StopsSafelyWithWarning()
    {
        // Tạo khoảng nghỉ kéo dài hơn 5 năm
        var subjects = new List<Subject>
        {
            new() { SortOrder = 1, Name = "Môn không bao giờ học", Periods = 30 }
        };

        var holidays = new List<Holiday>
        {
            new() { Name = "Khai giảng", From = new DateOnly(2025, 9, 10), To = new DateOnly(2025, 9, 10), IsOpeningDay = true },
            new() { Name = "Nghỉ vô tận", From = new DateOnly(2025, 9, 11), To = new DateOnly(2035, 9, 11) } // Nghỉ 10 năm liên tiếp
        };

        var result = _calculator.Calculate(subjects, holidays);

        Assert.Empty(result.Items);
        Assert.Contains(result.Warnings, w => w.Contains("giới hạn an toàn") || w.Contains("5 năm"));
    }

    #endregion
}
