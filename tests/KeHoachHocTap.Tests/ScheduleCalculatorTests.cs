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

    #region 4. Flexible Opening Day & Exam Scheduling Tests

    [Fact]
    public void Calculate_MultiDayOpening_StartsOnNextWorkdayAfterToDate()
    {
        // Khai giảng kéo dài 3 ngày: Thứ Tư 10/9/2025 -> Thứ Sáu 12/9/2025
        // Thứ Bảy 13/9 và Chủ Nhật 14/9 là ngày nghỉ cuối tuần
        // Ngày làm việc đầu tiên sau khai giảng phải là Thứ Hai 15/9/2025
        var subjects = new List<Subject>
        {
            new() { SortOrder = 1, Name = "Môn 1", Periods = 10 } // 2 buổi
        };

        var holidays = new List<Holiday>
        {
            new() { Name = "Tuần lễ Khai giảng", From = new DateOnly(2025, 9, 10), To = new DateOnly(2025, 9, 12), IsOpeningDay = true }
        };

        var result = _calculator.Calculate(subjects, holidays);

        var item = Assert.Single(result.Items);
        Assert.Equal("S 15/9/2025", item.Start.ToString());
        Assert.Equal("C 15/9/2025", item.End.ToString());
        Assert.Equal("S 16/9/2025", item.Exam.ToString());
    }

    [Fact]
    public void Calculate_FirstSessionHalfAfternoon_StartsSubject1InAfternoon()
    {
        // Khai giảng ngày 10/9/2025 (T4)
        // Ngày làm việc đầu tiên: 11/9/2025 (T5)
        // firstSessionHalf = Half.C -> Môn 1 bắt đầu vào C 11/9/2025
        var subjects = new List<Subject>
        {
            new() { SortOrder = 1, Name = "Môn 1", Periods = 10 } // 2 buổi: C 11/9, S 12/9 -> Thi: C 12/9
        };

        var holidays = new List<Holiday>
        {
            new() { Name = "Khai giảng", From = new DateOnly(2025, 9, 10), To = new DateOnly(2025, 9, 10), IsOpeningDay = true }
        };

        var result = _calculator.Calculate(subjects, holidays, Half.C);

        var item = Assert.Single(result.Items);
        Assert.Equal("C 11/9/2025", item.Start.ToString());
        Assert.Equal("S 12/9/2025", item.End.ToString());
        Assert.Equal("C 12/9/2025", item.Exam.ToString());
    }

    [Fact]
    public void Calculate_DelayedExam_BatchesExamsAndNextSubjectStartsImmediately()
    {
        // Khai giảng: 10/9/2025 (T4)
        // Ngày làm việc đầu tiên: 11/9/2025 (T5)
        // Môn 1 (10 tiết = 2 buổi, ExamMode = "Delayed"):
        //   Học: S 11/9, C 11/9 (End: C 11/9/2025)
        //   Hoãn thi -> Môn 2 vào học luôn ngay buổi tiếp theo (S 12/9/2025)
        // Môn 2 (10 tiết = 2 buổi, ExamMode = "Immediately"):
        //   Học: S 12/9, C 12/9 (End: C 12/9/2025)
        //   Tổ chức thi lần lượt môn 1 rồi môn 2:
        //   13/9, 14/9 nghỉ cuối tuần -> Môn 1 thi: S 15/9/2025; Môn 2 thi: C 15/9/2025
        // Môn 3 (10 tiết = 2 buổi, ExamMode = "Immediately"):
        //   Học luôn buổi tiếp theo sau các buổi thi: S 16/9/2025
        //   Học: S 16/9, C 16/9 -> Thi: S 17/9/2025
        var subjects = new List<Subject>
        {
            new() { SortOrder = 1, Name = "Môn 1", Periods = 10, ExamMode = "Delayed" },
            new() { SortOrder = 2, Name = "Môn 2", Periods = 10, ExamMode = "Immediately" },
            new() { SortOrder = 3, Name = "Môn 3", Periods = 10, ExamMode = "Immediately" }
        };

        var holidays = new List<Holiday>
        {
            new() { Name = "Khai giảng", From = new DateOnly(2025, 9, 10), To = new DateOnly(2025, 9, 10), IsOpeningDay = true }
        };

        var result = _calculator.Calculate(subjects, holidays);

        Assert.Equal(3, result.Items.Count);

        // Môn 1
        var item1 = result.Items[0];
        Assert.Equal("S 11/9/2025", item1.Start.ToString());
        Assert.Equal("C 11/9/2025", item1.End.ToString());
        Assert.Equal("S 15/9/2025", item1.Exam.ToString());

        // Môn 2
        var item2 = result.Items[1];
        Assert.Equal("S 12/9/2025", item2.Start.ToString());
        Assert.Equal("C 12/9/2025", item2.End.ToString());
        Assert.Equal("C 15/9/2025", item2.Exam.ToString());

        // Môn 3
        var item3 = result.Items[2];
        Assert.Equal("S 16/9/2025", item3.Start.ToString());
        Assert.Equal("C 16/9/2025", item3.End.ToString());
        Assert.Equal("S 17/9/2025", item3.Exam.ToString());
    }

    [Fact]
    public void Calculate_CustomExamMode_UsesSpecifiedSlot()
    {
        var subjects = new List<Subject>
        {
            new() { SortOrder = 1, Name = "Môn Custom", Periods = 10, ExamMode = "Custom", CustomExamSlot = "S 22/9/2025" }
        };

        var holidays = new List<Holiday>
        {
            new() { Name = "Khai giảng", From = new DateOnly(2025, 9, 10), To = new DateOnly(2025, 9, 10), IsOpeningDay = true }
        };

        var result = _calculator.Calculate(subjects, holidays);

        var item = Assert.Single(result.Items);
        Assert.Equal("S 11/9/2025", item.Start.ToString());
        Assert.Equal("C 11/9/2025", item.End.ToString());
        Assert.Equal("S 22/9/2025", item.Exam.ToString());
    }

    [Fact]
    public void Calculate_LastSubjectDelayed_SchedulesExamAtEndSafely()
    {
        var subjects = new List<Subject>
        {
            new() { SortOrder = 1, Name = "Môn 1", Periods = 10, ExamMode = "Delayed" },
            new() { SortOrder = 2, Name = "Môn 2", Periods = 10, ExamMode = "Delayed" }
        };

        var holidays = new List<Holiday>
        {
            new() { Name = "Khai giảng", From = new DateOnly(2025, 9, 10), To = new DateOnly(2025, 9, 10), IsOpeningDay = true }
        };

        var result = _calculator.Calculate(subjects, holidays);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("S 11/9/2025", result.Items[0].Start.ToString());
        Assert.Equal("C 11/9/2025", result.Items[0].End.ToString());
        Assert.Equal("S 15/9/2025", result.Items[0].Exam.ToString());

        Assert.Equal("S 12/9/2025", result.Items[1].Start.ToString());
        Assert.Equal("C 12/9/2025", result.Items[1].End.ToString());
        Assert.Equal("C 15/9/2025", result.Items[1].Exam.ToString());
    }

    [Fact]
    public void Calculate_WithInterimActivity_DateRange_ShiftsNextSubjectToNextWorkdayMorning()
    {
        var subjects = new List<Subject>
        {
            new() { Id = 1, SortOrder = 1, Name = "Môn 1", Periods = 10 },
            new() { Id = 2, SortOrder = 2, Name = "Môn 2", Periods = 10 }
        };

        var holidays = new List<Holiday>
        {
            new() { Name = "Khai giảng", From = new DateOnly(2025, 9, 10), To = new DateOnly(2025, 9, 10), IsOpeningDay = true }
        };

        var activities = new List<PlanActivity>
        {
            new()
            {
                Id = 1,
                Name = "Đi nghiên cứu thực tế tại cơ sở",
                Type = PlanActivityType.Interim,
                AfterSubjectId = 1,
                FromDate = new DateOnly(2025, 9, 15), // Thứ 2
                ToDate = new DateOnly(2025, 9, 19),   // Thứ 6
                Periods = 10,
                Note = "Nghiên cứu cơ sở"
            }
        };

        var result = _calculator.Calculate(subjects, holidays, Half.S, activities);

        Assert.Equal(2, result.Items.Count);
        Assert.Single(result.Activities);

        // Môn 1 học S 11/9, C 11/9; thi S 12/9
        Assert.Equal("S 11/9/2025", result.Items[0].Start.ToString());
        Assert.Equal("C 11/9/2025", result.Items[0].End.ToString());
        Assert.Equal("S 12/9/2025", result.Items[0].Exam.ToString());

        // Hoạt động xen kẽ
        var act = result.Activities[0];
        Assert.Equal("Đi nghiên cứu thực tế tại cơ sở", act.Activity.Name);
        Assert.Equal("S 15/9/2025", act.Start!.ToString());
        Assert.Equal("C 19/9/2025", act.End!.ToString());

        // Môn 2 tự động dời sang sáng Thứ 2 tuần sau (NextWorkday của 19/9/2025 là 22/9/2025)
        Assert.Equal("S 22/9/2025", result.Items[1].Start.ToString());
        Assert.Equal("C 22/9/2025", result.Items[1].End.ToString());
        Assert.Equal("S 23/9/2025", result.Items[1].Exam.ToString());
    }

    [Fact]
    public void Calculate_WithInterimActivity_Sessions_ShiftsNextSubjectConsecutively()
    {
        var subjects = new List<Subject>
        {
            new() { Id = 1, SortOrder = 1, Name = "Môn 1", Periods = 10 },
            new() { Id = 2, SortOrder = 2, Name = "Môn 2", Periods = 10 }
        };

        var holidays = new List<Holiday>
        {
            new() { Name = "Khai giảng", From = new DateOnly(2025, 9, 10), To = new DateOnly(2025, 9, 10), IsOpeningDay = true }
        };

        var activities = new List<PlanActivity>
        {
            new()
            {
                Id = 1,
                Name = "Nghỉ viết bài thu hoạch môn học",
                Type = PlanActivityType.Interim,
                AfterSubjectId = 1,
                Sessions = 2
            }
        };

        var result = _calculator.Calculate(subjects, holidays, Half.S, activities);

        // Môn 1 thi vào S 12/9/2025
        Assert.Equal("S 12/9/2025", result.Items[0].Exam.ToString());

        // Hoạt động 2 buổi bắt đầu tại C 12/9/2025 đến S 15/9/2025 (Thứ 2)
        var act = Assert.Single(result.Activities);
        Assert.Equal("C 12/9/2025", act.Start!.ToString());
        Assert.Equal("S 15/9/2025", act.End!.ToString());

        // Môn 2 bắt đầu tại buổi kế tiếp: C 15/9/2025
        Assert.Equal("C 15/9/2025", result.Items[1].Start.ToString());
        Assert.Equal("S 16/9/2025", result.Items[1].End.ToString());
        Assert.Equal("C 16/9/2025", result.Items[1].Exam.ToString());
    }

    [Fact]
    public void Calculate_WithClosingActivities_SchedulesActivitiesAfterLastSubject()
    {
        var subjects = new List<Subject>
        {
            new() { Id = 1, SortOrder = 1, Name = "Môn cuối", Periods = 10 }
        };

        var holidays = new List<Holiday>
        {
            new() { Name = "Khai giảng", From = new DateOnly(2025, 9, 10), To = new DateOnly(2025, 9, 10), IsOpeningDay = true }
        };

        var activities = new List<PlanActivity>
        {
            new()
            {
                Id = 1,
                SortOrder = 1,
                Name = "Ôn thi tốt nghiệp",
                Type = PlanActivityType.Closing,
                Periods = 15 // 3 buổi
            },
            new()
            {
                Id = 2,
                SortOrder = 2,
                Name = "Thi tốt nghiệp",
                Type = PlanActivityType.Closing,
                FromDate = new DateOnly(2025, 9, 22),
                ToDate = new DateOnly(2025, 9, 24),
                Periods = 15
            },
            new()
            {
                Id = 3,
                SortOrder = 3,
                Name = "Họp lớp",
                Type = PlanActivityType.Closing,
                Sessions = 1
            },
            new()
            {
                Id = 4,
                SortOrder = 4,
                Name = "Bế giảng",
                Type = PlanActivityType.Closing,
                FromDate = new DateOnly(2025, 9, 29),
                ToDate = new DateOnly(2025, 9, 29),
                Periods = 5
            }
        };

        var result = _calculator.Calculate(subjects, holidays, Half.S, activities);

        Assert.Single(result.Items);
        Assert.Equal("S 12/9/2025", result.Items[0].Exam.ToString());

        Assert.Equal(4, result.Activities.Count);

        // 1. Ôn thi tốt nghiệp: 3 buổi từ C 12/9/2025 -> C 15/9/2025
        Assert.Equal("Ôn thi tốt nghiệp", result.Activities[0].Activity.Name);
        Assert.Equal("C 12/9/2025", result.Activities[0].Start!.ToString());
        Assert.Equal("C 15/9/2025", result.Activities[0].End!.ToString());

        // 2. Thi tốt nghiệp: From 22/9/2025 -> To 24/9/2025
        Assert.Equal("Thi tốt nghiệp", result.Activities[1].Activity.Name);
        Assert.Equal("S 22/9/2025", result.Activities[1].Start!.ToString());
        Assert.Equal("C 24/9/2025", result.Activities[1].End!.ToString());

        // 3. Họp lớp: 1 buổi sau Thi tốt nghiệp -> NextWorkday(24/9) là 25/9 sáng
        Assert.Equal("Họp lớp", result.Activities[2].Activity.Name);
        Assert.Equal("S 25/9/2025", result.Activities[2].Start!.ToString());
        Assert.Equal("S 25/9/2025", result.Activities[2].End!.ToString());

        // 4. Bế giảng: From 29/9/2025 -> To 29/9/2025
        Assert.Equal("Bế giảng", result.Activities[3].Activity.Name);
        Assert.Equal("S 29/9/2025", result.Activities[3].Start!.ToString());
        Assert.Equal("C 29/9/2025", result.Activities[3].End!.ToString());
    }

    [Fact]
    public void Calculate_Golden19Subjects_WithInterimActivity_AccuratelyShiftsSubsequentSubjects()
    {
        var excelPath = Path.Combine(AppContext.BaseDirectory, "TestData", "ke_hoach_hoc_tap.xlsx");
        var reader = new ExcelPlanReader();
        using var stream = File.OpenRead(excelPath);
        var (subjects, holidays, _) = reader.Read(stream);

        // Gán Id cho các môn theo SortOrder để liên kết với AfterSubjectId
        foreach (var s in subjects)
        {
            s.Id = s.SortOrder;
        }

        // Chèn hoạt động nghiên cứu thực tế 1 tuần sau môn 1 (22/9/2025 -> 26/9/2025)
        var activities = new List<PlanActivity>
        {
            new()
            {
                Id = 100,
                Name = "Đi nghiên cứu thực tế tại cơ sở",
                Type = PlanActivityType.Interim,
                AfterSubjectId = 1,
                FromDate = new DateOnly(2025, 9, 22),
                ToDate = new DateOnly(2025, 9, 26)
            }
        };

        var result = _calculator.Calculate(subjects, holidays, Half.S, activities);

        Assert.Equal(19, result.Items.Count);
        Assert.Single(result.Activities);

        // Môn 1 giữ nguyên lịch golden
        Assert.Equal("S 11/9/2025", result.Items[0].Start.ToString());
        Assert.Equal("C 18/9/2025", result.Items[0].End.ToString());
        Assert.Equal("S 19/9/2025", result.Items[0].Exam.ToString());

        // Hoạt động xen kẽ diễn ra từ S 22/9 đến C 26/9
        Assert.Equal("S 22/9/2025", result.Activities[0].Start!.ToString());
        Assert.Equal("C 26/9/2025", result.Activities[0].End!.ToString());

        // Môn 2 (nguyên bản là C 19/9 -> C 26/9, thi S 29/9)
        // Nay bắt đầu vào S 29/9/2025 (NextWorkday sau 26/9/2025)
        Assert.Equal("S 29/9/2025", result.Items[1].Start.ToString());
    }

    #endregion
}
