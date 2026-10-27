using KeHoachHocTap.Web.Data;
using KeHoachHocTap.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace KeHoachHocTap.Tests;

public class PlanRepositoryTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _fixture;
    private PlanRepository _repository = null!;

    public PlanRepositoryTests()
    {
        _fixture = new TestDatabaseFixture();
    }

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
        _repository = new PlanRepository(_fixture.Context);
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task Subject_CRUD_WorksCorrectly()
    {
        // 1. Add
        var sub1 = new Subject { Name = "Toán rời rạc", Periods = 45, SortOrder = 1 };
        await _repository.AddSubjectAsync(sub1);

        var list = await _repository.GetSubjectsAsync();
        Assert.Single(list);
        Assert.Equal("Toán rời rạc", list[0].Name);
        Assert.Equal(45, list[0].Periods);
        Assert.Equal(1, list[0].SortOrder);

        // 2. GetById
        var fetched = await _repository.GetSubjectByIdAsync(list[0].Id);
        Assert.NotNull(fetched);
        Assert.Equal("Toán rời rạc", fetched.Name);

        // 3. Update
        fetched.Name = "Toán rời rạc nâng cao";
        fetched.Grade = 8.5m;
        fetched.Note = "Lớp K56";
        await _repository.UpdateSubjectAsync(fetched);

        var updated = await _repository.GetSubjectByIdAsync(fetched.Id);
        Assert.NotNull(updated);
        Assert.Equal("Toán rời rạc nâng cao", updated.Name);
        Assert.Equal(8.5m, updated.Grade);
        Assert.Equal("Lớp K56", updated.Note);

        // 4. Delete
        await _repository.DeleteSubjectAsync(fetched.Id);
        var afterDelete = await _repository.GetSubjectsAsync();
        Assert.Empty(afterDelete);
    }

    [Fact]
    public async Task DeleteSubject_RenumberSortOrder_Continuously()
    {
        // Add 4 subjects
        for (int i = 1; i <= 4; i++)
        {
            await _repository.AddSubjectAsync(new Subject
            {
                Name = $"Môn {i}",
                Periods = 30,
                SortOrder = i
            });
        }

        var subjects = await _repository.GetSubjectsAsync();
        Assert.Equal(4, subjects.Count);
        var secondSubject = subjects[1]; // Môn 2 (SortOrder 2)

        // Delete subject 2
        await _repository.DeleteSubjectAsync(secondSubject.Id);

        var remaining = await _repository.GetSubjectsAsync();
        Assert.Equal(3, remaining.Count);
        Assert.Equal("Môn 1", remaining[0].Name);
        Assert.Equal(1, remaining[0].SortOrder);

        Assert.Equal("Môn 3", remaining[1].Name);
        Assert.Equal(2, remaining[1].SortOrder);

        Assert.Equal("Môn 4", remaining[2].Name);
        Assert.Equal(3, remaining[2].SortOrder);
    }

    [Fact]
    public async Task MoveSubject_SwapsPositions_AndUpdatesSortOrder()
    {
        for (int i = 1; i <= 3; i++)
        {
            await _repository.AddSubjectAsync(new Subject
            {
                Name = $"Môn {i}",
                Periods = 30,
                SortOrder = i
            });
        }

        var list = await _repository.GetSubjectsAsync();
        var sub2 = list[1]; // Môn 2 (SortOrder 2)

        // Move sub2 UP (direction = -1) -> should swap with Môn 1
        await _repository.MoveSubjectAsync(sub2.Id, -1);

        var afterUp = await _repository.GetSubjectsAsync();
        Assert.Equal("Môn 2", afterUp[0].Name);
        Assert.Equal(1, afterUp[0].SortOrder);
        Assert.Equal("Môn 1", afterUp[1].Name);
        Assert.Equal(2, afterUp[1].SortOrder);
        Assert.Equal("Môn 3", afterUp[2].Name);
        Assert.Equal(3, afterUp[2].SortOrder);

        // Move sub2 DOWN (direction = +1) -> should swap back with Môn 1
        await _repository.MoveSubjectAsync(sub2.Id, 1);

        var afterDown = await _repository.GetSubjectsAsync();
        Assert.Equal("Môn 1", afterDown[0].Name);
        Assert.Equal(1, afterDown[0].SortOrder);
        Assert.Equal("Môn 2", afterDown[1].Name);
        Assert.Equal(2, afterDown[1].SortOrder);
        Assert.Equal("Môn 3", afterDown[2].Name);
        Assert.Equal(3, afterDown[2].SortOrder);
    }

    [Fact]
    public async Task Holiday_OpeningDay_Constraints_AreEnforced()
    {
        // 1. Add opening day
        var opening = new Holiday
        {
            Name = "Lễ Khai giảng",
            From = new DateOnly(2025, 9, 10),
            To = new DateOnly(2025, 9, 10),
            IsOpeningDay = true
        };
        await _repository.AddHolidayAsync(opening);

        var currentOpening = await _repository.GetOpeningDayAsync();
        Assert.NotNull(currentOpening);
        Assert.Equal("Lễ Khai giảng", currentOpening.Name);

        // 2. Add second opening day -> old opening day must be automatically unset
        var newOpening = new Holiday
        {
            Name = "Khai giảng khóa mới",
            From = new DateOnly(2025, 9, 15),
            To = new DateOnly(2025, 9, 15),
            IsOpeningDay = true
        };
        await _repository.AddHolidayAsync(newOpening);

        var holidays = await _repository.GetHolidaysAsync();
        Assert.Equal(2, holidays.Count);

        var activeOpenings = holidays.Where(h => h.IsOpeningDay).ToList();
        Assert.Single(activeOpenings);
        Assert.Equal("Khai giảng khóa mới", activeOpenings[0].Name);

        // 3. Deleting the only opening day must throw InvalidOperationException
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _repository.DeleteHolidayAsync(activeOpenings[0].Id));

        // 4. Unchecking IsOpeningDay when it is the only opening day must throw
        var toModify = activeOpenings[0];
        toModify.IsOpeningDay = false;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _repository.UpdateHolidayAsync(toModify));

        // 5. Normal holiday can be deleted
        var normalHoliday = holidays.First(h => !h.IsOpeningDay);
        await _repository.DeleteHolidayAsync(normalHoliday.Id);

        var remainingHolidays = await _repository.GetHolidaysAsync();
        Assert.Single(remainingHolidays);
        Assert.True(remainingHolidays[0].IsOpeningDay);
    }

    [Fact]
    public async Task AppSetting_CRUD_WorksCorrectly()
    {
        // 1. Get default when key does not exist
        var val = await _repository.GetSettingAsync("TemplatePath", "default.docx");
        Assert.Equal("default.docx", val);

        // 2. Set new setting
        await _repository.SetSettingAsync("TemplatePath", "custom.docx");
        var updatedVal = await _repository.GetSettingAsync("TemplatePath");
        Assert.Equal("custom.docx", updatedVal);

        // 3. Update existing setting
        await _repository.SetSettingAsync("TemplatePath", "final.docx");
        var finalVal = await _repository.GetSettingAsync("TemplatePath");
        Assert.Equal("final.docx", finalVal);

        // 4. GetAllSettings
        await _repository.SetSettingAsync("ExamPeriods", "5");
        var allSettings = await _repository.GetAllSettingsAsync();
        Assert.Equal(2, allSettings.Count);
        Assert.Equal("final.docx", allSettings["TemplatePath"]);
        Assert.Equal("5", allSettings["ExamPeriods"]);
    }
}
