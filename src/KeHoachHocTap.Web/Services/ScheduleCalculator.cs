using KeHoachHocTap.Web.Data;
using KeHoachHocTap.Web.Domain;
using Half = KeHoachHocTap.Web.Domain.Half;

namespace KeHoachHocTap.Web.Services;

/// <summary>
/// Triển khai thuật toán xếp lịch học thuần túy (Pure Function, không I/O).
/// </summary>
public class ScheduleCalculator : IScheduleCalculator
{
    private const int MaxSafetyYears = 5;
    private const int MaxSafetyDays = 365 * MaxSafetyYears;
    private const int MaxSafetySlots = MaxSafetyDays * 2;

    public ScheduleResult Calculate(
        IEnumerable<Subject> subjects,
        IEnumerable<Holiday> holidays,
        Half firstSessionHalf = Half.S,
        IReadOnlyList<PlanActivity>? activities = null)
    {
        var subjectList = subjects?.ToList() ?? new List<Subject>();
        var holidayList = holidays?.ToList() ?? new List<Holiday>();
        var warnings = new List<string>();

        // 1. Tìm ngày khai giảng
        var openingHoliday = holidayList.FirstOrDefault(h =>
            h.IsOpeningDay ||
            h.Name.Contains("khai giảng", StringComparison.OrdinalIgnoreCase) ||
            h.Name.Contains("khai giang", StringComparison.OrdinalIgnoreCase));

        if (openingHoliday == null)
        {
            warnings.Add("Chưa thiết lập ngày khai giảng trong danh sách ngày nghỉ.");
            return new ScheduleResult(DateOnly.MinValue, holidayList, Array.Empty<ScheduledSubject>(), Array.Empty<ScheduledActivity>(), warnings);
        }

        var openingDay = openingHoliday.From;
        var openingEnd = openingHoliday.To >= openingHoliday.From ? openingHoliday.To : openingHoliday.From;

        // 2. Bung tập ngày nghỉ (bao gồm cả các ngày khai giảng là ngày không học môn)
        var holidayDates = new HashSet<DateOnly>();

        var curOpening = openingDay;
        while (curOpening <= openingEnd)
        {
            holidayDates.Add(curOpening);
            if (curOpening == DateOnly.MaxValue) break;
            curOpening = curOpening.AddDays(1);
        }

        foreach (var h in holidayList)
        {
            if (h.To < h.From)
            {
                warnings.Add($"Kỳ nghỉ '{h.Name}' có Đến ngày ({h.To:d/M/yyyy}) trước Từ ngày ({h.From:d/M/yyyy}). Đã bỏ qua khoảng nghỉ này.");
                continue;
            }

            var current = h.From;
            while (current <= h.To)
            {
                holidayDates.Add(current);
                if (current == DateOnly.MaxValue) break;
                current = current.AddDays(1);
            }
        }

        // Helper: Kiểm tra ngày làm việc hợp lệ (Thứ 2 - Thứ 6 và không phải ngày nghỉ)
        bool IsWorkday(DateOnly date)
        {
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                return false;
            return !holidayDates.Contains(date);
        }

        // Helper: Tìm ngày làm việc kế tiếp sau date
        DateOnly NextWorkday(DateOnly date)
        {
            var cur = date.AddDays(1);
            int safetyCount = 0;
            while (!IsWorkday(cur))
            {
                cur = cur.AddDays(1);
                safetyCount++;
                if (safetyCount > MaxSafetyDays)
                {
                    throw new InvalidOperationException("Lịch học kéo dài quá 5 năm hoặc không tìm thấy ngày học hợp lệ tiếp theo.");
                }
            }
            return cur;
        }

        // Helper: Tiến n buổi học hợp lệ (S rồi C, hết C sang S của NextWorkday)
        Slot Advance(Slot slot, int n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n), "Số buổi dịch chuyển phải >= 0.");
            if (n == 0) return slot;

            var curDate = slot.Date;
            var curHalf = slot.Half;
            int safetyCount = 0;

            for (int i = 0; i < n; i++)
            {
                safetyCount++;
                if (safetyCount > MaxSafetySlots)
                {
                    throw new InvalidOperationException("Lịch học kéo dài quá 5 năm (vượt giới hạn an toàn chống lặp vô tận).");
                }

                if (curHalf == Half.S)
                {
                    curHalf = Half.C;
                }
                else
                {
                    curDate = NextWorkday(curDate);
                    curHalf = Half.S;
                }
            }

            return new Slot(curDate, curHalf);
        }

        // 3. Ngày làm việc đầu tiên sau khai giảng: NextWorkday(openingHoliday.To). Nếu From == To thì hành vi trùng khớp 100% như cũ.
        DateOnly firstWorkday;
        try
        {
            firstWorkday = NextWorkday(openingEnd);
        }
        catch (InvalidOperationException ex)
        {
            warnings.Add($"Không tìm thấy ngày làm việc hợp lệ sau khai giảng: {ex.Message}");
            return new ScheduleResult(openingDay, holidayList, Array.Empty<ScheduledSubject>(), Array.Empty<ScheduledActivity>(), warnings);
        }

        // 4. Lọc môn hợp lệ theo SortOrder
        var orderedSubjects = subjectList.OrderBy(s => s.SortOrder).ToList();
        var validSubjects = new List<Subject>();
        int displayNumberCounter = 1;
        var subjectDisplayNumbers = new Dictionary<Subject, int>();

        foreach (var subject in orderedSubjects)
        {
            if (subject.Periods <= 0)
            {
                warnings.Add($"Môn học '{subject.Name}' (Stt {displayNumberCounter}) có số tiết không hợp lệ ({subject.Periods} <= 0). Đã bỏ qua môn này.");
                displayNumberCounter++;
                continue;
            }

            if (subject.Periods % 5 != 0)
            {
                warnings.Add($"Môn học '{subject.Name}' có số tiết ({subject.Periods}) không chia hết cho 5 (được làm tròn lên buổi).");
            }

            subjectDisplayNumbers[subject] = displayNumberCounter++;
            validSubjects.Add(subject);
        }

        // 5. Xếp lịch từng môn
        var scheduledItems = new List<ScheduledSubject>();
        var scheduledActivities = new List<ScheduledActivity>();
        var pendingExams = new List<(Subject Subject, int Number, int Sessions, Slot Start, Slot End)>();
        Slot currentStart = new Slot(firstWorkday, firstSessionHalf);

        for (int i = 0; i < validSubjects.Count; i++)
        {
            var subject = validSubjects[i];
            int displayNumber = subjectDisplayNumbers[subject];
            int sessions = (int)Math.Ceiling(subject.Periods / 5.0);
            bool isLast = (i == validSubjects.Count - 1);

            try
            {
                var start = currentStart;
                var end = Advance(start, sessions - 1);
                var examMode = subject.ExamMode?.Trim() ?? "Immediately";

                if (string.Equals(examMode, "Delayed", StringComparison.OrdinalIgnoreCase) && !isLast)
                {
                    // Hoãn thi: Cho môn này vào pendingExams, môn tiếp theo vào học luôn ngay buổi kế tiếp sau buổi học cuối
                    pendingExams.Add((subject, displayNumber, sessions, start, end));
                    currentStart = Advance(end, 1);
                }
                else if (string.Equals(examMode, "Custom", StringComparison.OrdinalIgnoreCase))
                {
                    // Custom: Buổi thi do người dùng chỉ định
                    Slot examSlot;
                    if (Slot.TryParse(subject.CustomExamSlot, out var customSlot))
                    {
                        examSlot = customSlot;
                    }
                    else
                    {
                        examSlot = Advance(end, 1);
                    }

                    scheduledItems.Add(new ScheduledSubject(subject, displayNumber, sessions, start, end, examSlot));
                    currentStart = Advance(end, 1);
                }
                else
                {
                    // "Immediately" (hoặc là môn cuối):
                    // Xếp lịch thi:
                    // + Đầu tiên xếp thi cho các môn trong pendingExams (mỗi môn 1 buổi)
                    // + Sau đó xếp thi cho môn hiện tại (1 buổi)
                    // + Sau khi thi xong hết, môn tiếp theo bắt đầu ngay buổi kế tiếp: "thi xong học luôn buổi tiếp theo"!
                    var examCursor = Advance(end, 1);

                    foreach (var pending in pendingExams)
                    {
                        scheduledItems.Add(new ScheduledSubject(pending.Subject, pending.Number, pending.Sessions, pending.Start, pending.End, examCursor));
                        examCursor = Advance(examCursor, 1);
                    }
                    pendingExams.Clear();

                    scheduledItems.Add(new ScheduledSubject(subject, displayNumber, sessions, start, end, examCursor));

                    currentStart = Advance(examCursor, 1);
                }

                // Xử lý hoạt động xen kẽ gắn với môn học này (Interim Activities)
                if (activities != null)
                {
                    var interimList = activities
                        .Where(a => a.Type == PlanActivityType.Interim &&
                                    (a.AfterSubjectId == subject.Id || (subject.Id == 0 && a.AfterSubjectId == subject.SortOrder)))
                        .OrderBy(a => a.SortOrder)
                        .ToList();

                    foreach (var act in interimList)
                    {
                        if (act.FromDate.HasValue)
                        {
                            var fromDate = act.FromDate.Value;
                            var toDate = act.ToDate ?? fromDate;
                            if (toDate < fromDate)
                            {
                                warnings.Add($"Hoạt động xen kẽ '{act.Name}' có Đến ngày ({toDate:d/M/yyyy}) trước Từ ngày ({fromDate:d/M/yyyy}). Đã điều chỉnh Đến ngày = Từ ngày.");
                                toDate = fromDate;
                            }

                            var actStart = new Slot(fromDate, Half.S);
                            var actEnd = new Slot(toDate, Half.C);
                            scheduledActivities.Add(new ScheduledActivity(act, actStart, actEnd));

                            // Môn học k+1 sẽ bắt đầu tại buổi sáng của NextWorkday(activity.ToDate)
                            currentStart = new Slot(NextWorkday(toDate), Half.S);
                        }
                        else
                        {
                            // Chỉ định theo Sessions hoặc Periods
                            int actSessions = act.Sessions ?? (act.Periods.HasValue && act.Periods.Value > 0 ? (int)Math.Ceiling(act.Periods.Value / 5.0) : 1);
                            var actStart = currentStart;
                            var actEnd = Advance(actStart, actSessions - 1);
                            scheduledActivities.Add(new ScheduledActivity(act, actStart, actEnd));

                            // Môn học k+1 bắt đầu tại buổi kế tiếp sau khi hoạt động kết thúc
                            currentStart = Advance(actEnd, 1);
                        }
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                warnings.Add($"Đã dừng xếp lịch do vượt giới hạn an toàn: {ex.Message}");
                break;
            }
        }

        // Đảm bảo không còn môn nào bị sót trong pendingExams (nếu có)
        if (pendingExams.Count > 0 && scheduledItems.Count > 0)
        {
            try
            {
                var lastExam = scheduledItems.Max(s => s.Exam);
                var examCursor = Advance(lastExam, 1);
                foreach (var pending in pendingExams)
                {
                    scheduledItems.Add(new ScheduledSubject(pending.Subject, pending.Number, pending.Sessions, pending.Start, pending.End, examCursor));
                    examCursor = Advance(examCursor, 1);
                }
                pendingExams.Clear();
                currentStart = Advance(examCursor, 1);
            }
            catch (InvalidOperationException ex)
            {
                warnings.Add($"Không thể hoàn tất xếp lịch thi cho các môn hoãn thi: {ex.Message}");
            }
        }

        // 6. Xử lý chuỗi hoạt động sau môn cuối (Closing Activities)
        if (activities != null)
        {
            var closingList = activities
                .Where(a => a.Type == PlanActivityType.Closing)
                .OrderBy(a => a.SortOrder)
                .ToList();

            foreach (var act in closingList)
            {
                try
                {
                    if (act.FromDate.HasValue)
                    {
                        var fromDate = act.FromDate.Value;
                        var toDate = act.ToDate ?? fromDate;
                        if (toDate < fromDate)
                        {
                            warnings.Add($"Hoạt động kết thúc '{act.Name}' có Đến ngày ({toDate:d/M/yyyy}) trước Từ ngày ({fromDate:d/M/yyyy}). Đã điều chỉnh Đến ngày = Từ ngày.");
                            toDate = fromDate;
                        }

                        var actStart = new Slot(fromDate, Half.S);
                        var actEnd = new Slot(toDate, Half.C);
                        scheduledActivities.Add(new ScheduledActivity(act, actStart, actEnd));

                        currentStart = new Slot(NextWorkday(toDate), Half.S);
                    }
                    else
                    {
                        int actSessions = act.Sessions ?? (act.Periods.HasValue && act.Periods.Value > 0 ? (int)Math.Ceiling(act.Periods.Value / 5.0) : 1);
                        var actStart = currentStart;
                        var actEnd = Advance(actStart, actSessions - 1);
                        scheduledActivities.Add(new ScheduledActivity(act, actStart, actEnd));

                        currentStart = Advance(actEnd, 1);
                    }
                }
                catch (InvalidOperationException ex)
                {
                    warnings.Add($"Đã dừng xếp lịch cho hoạt động '{act.Name}' do vượt giới hạn an toàn: {ex.Message}");
                    break;
                }
            }
        }

        var sortedItems = scheduledItems.OrderBy(s => s.Number).ToList();
        return new ScheduleResult(openingDay, holidayList, sortedItems, scheduledActivities, warnings);
    }
}
