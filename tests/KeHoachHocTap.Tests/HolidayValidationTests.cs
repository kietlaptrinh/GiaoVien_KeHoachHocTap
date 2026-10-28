namespace KeHoachHocTap.Tests;

using KeHoachHocTap.Web.Data;
using KeHoachHocTap.Web.Domain;
using KeHoachHocTap.Web.Services;

public class HolidayValidationTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _fixture;
    private readonly PlanRepository _repository;

    public HolidayValidationTests()
    {
        _fixture = new TestDatabaseFixture();
        _repository = new PlanRepository(_fixture.Context);
    }

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task AddHoliday_ShouldThrowArgumentException_WhenToDateIsBeforeFromDate()
    {
        var invalidHoliday = new Holiday
        {
            Name = "Kỳ nghỉ sai ngày",
            From = new DateOnly(2025, 10, 20),
            To = new DateOnly(2025, 10, 15), // Đến ngày < Từ ngày
            IsOpeningDay = false
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _repository.AddHolidayAsync(invalidHoliday));
        Assert.Contains("Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu", ex.Message);
    }

    [Fact]
    public async Task UpdateHoliday_ShouldThrowArgumentException_WhenToDateIsBeforeFromDate()
    {
        var holiday = new Holiday
        {
            Name = "Kỳ nghỉ hợp lệ ban đầu",
            From = new DateOnly(2025, 10, 10),
            To = new DateOnly(2025, 10, 12),
            IsOpeningDay = false
        };
        await _repository.AddHolidayAsync(holiday);

        // Cập nhật ngày kết thúc lùi về trước ngày bắt đầu
        holiday.To = new DateOnly(2025, 10, 5);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _repository.UpdateHolidayAsync(holiday));
        Assert.Contains("Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu", ex.Message);
    }

    [Fact]
    public async Task AddHoliday_OpeningDay_ShouldSupportDateRange_AndUnsetPreviousOpening()
    {
        // 1. Tạo ngày khai giảng ban đầu
        var initialOpening = new Holiday
        {
            Name = "Ngày khai giảng ban đầu",
            From = new DateOnly(2025, 9, 10),
            To = new DateOnly(2025, 9, 10),
            IsOpeningDay = true
        };
        await _repository.AddHolidayAsync(initialOpening);

        // 2. Thêm ngày khai giảng mới với To khác From (khoảng ngày kéo dài nhiều ngày)
        var newOpening = new Holiday
        {
            Name = "Ngày khai giảng điều chỉnh",
            From = new DateOnly(2025, 9, 15),
            To = new DateOnly(2025, 9, 20), // khác From
            IsOpeningDay = true
        };
        await _repository.AddHolidayAsync(newOpening);

        // 3. Kiểm tra: Ngày mới giữ nguyên khoảng ngày To >= From và IsOpeningDay == true
        var openingInDb = await _repository.GetOpeningDayAsync();
        Assert.NotNull(openingInDb);
        Assert.Equal("Ngày khai giảng điều chỉnh", openingInDb.Name);
        Assert.Equal(new DateOnly(2025, 9, 15), openingInDb.From);
        Assert.Equal(new DateOnly(2025, 9, 20), openingInDb.To); // To được hỗ trợ kéo dài nhiều ngày

        // 4. Ngày khai giảng cũ phải bị hủy đánh dấu IsOpeningDay
        var oldOpeningInDb = await _repository.GetHolidayByIdAsync(initialOpening.Id);
        Assert.NotNull(oldOpeningInDb);
        Assert.False(oldOpeningInDb.IsOpeningDay);
    }

    [Fact]
    public async Task DeleteHoliday_ShouldThrowInvalidOperationException_WhenDeletingOnlyOpeningDay()
    {
        var opening = new Holiday
        {
            Name = "Ngày khai giảng duy nhất",
            From = new DateOnly(2025, 9, 10),
            To = new DateOnly(2025, 9, 10),
            IsOpeningDay = true
        };
        await _repository.AddHolidayAsync(opening);

        // Cố tình xóa ngày khai giảng duy nhất
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.DeleteHolidayAsync(opening.Id));
        Assert.Contains("Không cho phép xóa ngày khai giảng duy nhất", ex.Message);
    }

    [Fact]
    public async Task UpdateHoliday_ShouldThrowInvalidOperationException_WhenUnsettingOpeningDayWithoutOtherOpening()
    {
        var opening = new Holiday
        {
            Name = "Ngày khai giảng duy nhất",
            From = new DateOnly(2025, 9, 10),
            To = new DateOnly(2025, 9, 10),
            IsOpeningDay = true
        };
        await _repository.AddHolidayAsync(opening);

        // Cố tình bỏ cờ khai giảng mà không có ngày khai giảng nào khác
        opening.IsOpeningDay = false;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.UpdateHolidayAsync(opening));
        Assert.Contains("Hệ thống phải có đúng một ngày khai giảng", ex.Message);
    }

    [Fact]
    public void ScheduleCalculator_ShouldShiftSubsequentSubjectDates_WhenHolidaysChange()
    {
        var calculator = new ScheduleCalculator();
        var subjects = new List<Subject>
        {
            new() { Id = 1, Name = "Môn 1", Periods = 10, SortOrder = 1 }, // 2 buổi
            new() { Id = 2, Name = "Môn 2", Periods = 10, SortOrder = 2 }  // 2 buổi
        };

        var openingDay = new Holiday
        {
            Name = "Khai giảng",
            From = new DateOnly(2025, 9, 1), // Thứ 2
            To = new DateOnly(2025, 9, 1),
            IsOpeningDay = true
        };

        // Kịch bản 1: Không có ngày nghỉ bổ sung
        // Môn 1: Khai giảng 1/9 (T2). Học: S 2/9, C 2/9. Thi: S 3/9.
        // Môn 2: Học: C 3/9, S 4/9. Thi: C 4/9.
        var resultNoHoliday = calculator.Calculate(subjects, new[] { openingDay });
        Assert.Equal(new Slot(new DateOnly(2025, 9, 3), Half.C), resultNoHoliday.Items[1].Start);

        // Kịch bản 2: Thêm ngày nghỉ ngày 3/9 (Thứ 4)
        var extraHoliday = new Holiday
        {
            Name = "Nghỉ lễ thêm",
            From = new DateOnly(2025, 9, 3),
            To = new DateOnly(2025, 9, 3),
            IsOpeningDay = false
        };

        // Khi có ngày nghỉ 3/9:
        // Môn 1: Học S 2/9, C 2/9. Thi ngày 3/9 nghỉ -> dời sang S 4/9!
        // Môn 2: Học ngay sau thi -> bắt đầu vào C 4/9!
        var resultWithHoliday = calculator.Calculate(subjects, new[] { openingDay, extraHoliday });

        Assert.Equal(new Slot(new DateOnly(2025, 9, 4), Half.S), resultWithHoliday.Items[0].Exam);
        Assert.Equal(new Slot(new DateOnly(2025, 9, 4), Half.C), resultWithHoliday.Items[1].Start);
    }
}
