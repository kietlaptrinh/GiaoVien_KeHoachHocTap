# SPEC — Web Kế hoạch học tập & Bảng điểm tổng hợp (ASP.NET Core)

> Đặc tả "cái gì". Thứ tự làm xem `PLAN.md`. Quy tắc làm việc xem `AGENTS.md`.
> **Phiên bản 2.1:** Dữ liệu lưu trong **database**; Excel chỉ dùng để **nhập (import)**; Tách biệt 2 phân hệ chuyên biệt: **Kế hoạch giảng dạy** & **Bảng điểm tổng hợp**.
> Khi mâu thuẫn: **SPEC.md > PLAN.md > AGENTS.md**.

## 1. Mục tiêu

Hệ thống quản lý đào tạo & kế hoạch học tập chuyên sâu, lưu trữ **môn học**, **hoạt động xen kẽ/kết thúc**, **ngày nghỉ** và **danh sách học viên - điểm số** trong database SQL Server. Tự động tính toán tiến độ giảng dạy và xếp loại học tập. Hệ thống tách biệt thành 2 giao diện độc lập:

1. **Trang Kế hoạch giảng dạy & học tập (`/` hoặc `/Plan`)**:
   - Quản lý toàn bộ tiến trình đào tạo: Ngày khai giảng, các môn học theo khung chương trình, và lịch thi.
   - **Chèn hoạt động xen kẽ sau môn học bất kỳ**: Cho phép giáo viên chèn thêm các sự kiện đào tạo thực tế (ví dụ: lớp đi nghiên cứu thực tế tại cơ sở, nghỉ từ ngày đến ngày để viết bài thu hoạch/tiểu luận môn học...). Khi chèn hoạt động này sau môn X, hệ thống tự động đẩy lịch bắt đầu của môn X+1 nối tiếp ngay sau khi hoạt động kết thúc.
   - **Chèn chuỗi hoạt động sau môn học cuối cùng**: Cho phép quản lý linh hoạt các sự kiện kết thúc khóa học: *Ôn thi tốt nghiệp*, *Thi tốt nghiệp*, *Họp lớp tổng kết*, *Bế giảng*...
   - Xuất file biểu mẫu Word chuẩn: **"Kế hoạch giảng dạy và học tập" (.docx)** giữ nguyên định dạng, quốc hiệu, tiêu đề, bảng 10 cột, tự động điền các môn học, hoạt động xen kẽ và các nhiệm vụ kết thúc khóa.
2. **Trang Bảng điểm tổng hợp học viên (`/Grades`)**:
   - Quản lý bảng điểm tổng kết lớp học thực tế (chuẩn lớp K56 tham chiếu từ `BANG DIEM TONG K56.01.xls`).
   - **Đúng số môn như khung chương trình**: Giữ nguyên danh sách các môn học chính khóa (không bị lẫn các hoạt động ngoại khóa, nghiên cứu thực tế vào cột điểm).
   - Hiển thị dạng bảng ma trận (DataGrid): Hàng là danh sách học viên (*Stt, Mã số HV, Họ lót, Tên, Ngày sinh*), các cột điểm từng môn theo khung chương trình.
   - **Tự động tính toán kết quả**: Cột *Số môn* (đã có điểm), *Điểm trung bình (ĐTB)* làm tròn 2 chữ số thập phân, *ĐTB bằng chữ* (chuyển đổi chuẩn tiếng Việt, vd: 7.81 -> "Bảy phẩy tám mươi mốt"), và *Xếp loại* (Xuất sắc, Giỏi, Khá, Trung bình...).
   - Hỗ trợ nhập điểm trực tiếp, import danh sách học viên & điểm từ Excel, và xuất file Excel bảng điểm tổng hợp (.xlsx).

Chạy local và deploy được lên hosting ASP.NET Core miễn phí (mục 13). Có **1 tài khoản quản trị** (mật khẩu duy nhất, mục 12); khách chỉ xem.

## 2. Tech stack (cố định)

| Hạng mục | Chọn |
|---|---|
| Framework | ASP.NET Core Razor Pages, .NET 8 (`net8.0`) |
| Đọc Excel | `ClosedXML` |
| Xuất Word | `DocumentFormat.OpenXml` (clone từ file mẫu .docx) |
| Test | xUnit. Test cần database chạy trên **SQL Server** (chuỗi `ConnectionStrings:Test`, mỗi lần chạy tạo database tạm rồi xoá) |
| UI | Razor + Bootstrap 5 (file local trong `wwwroot/lib`, không CDN), JS thuần tối thiểu. **Phong cách Desktop cổ điển (Classic Desktop App Style)**: Bảng màu Trắng (#FFFFFF / #F6F8FA), Đen/Xám đậm (chữ và viền mảnh sắc nét #212529, #333333, #DEE2E6), Xanh dương nhạt nhấn nhá (#EBF3FC nền header/hover, #D0E2FF highlight, #0D6EFD/#1D4ED8 nút chính). Bố cục dạng Toolbar thanh công cụ, DataGrid phân chia rõ ràng, thanh Status Bar ở chân trang mang hơi hướng các phần mềm desktop quản lý trường học/đào tạo kinh điển |
| Database | EF Core + **SQL Server** duy nhất (Migrations). Dev dùng LocalDB (Windows) hoặc SQL Server (sa/123) |
| Đăng nhập quản trị | Cookie authentication, 1 mật khẩu từ cấu hình |
| Ngôn ngữ UI | Tiếng Việt. Code/tên biến/commit: tiếng Anh |

## 3. File đầu vào (Excel chỉ để import; đặt trong repo)

| File gốc | Đặt tại | Vai trò |
|---|---|---|
| `ke hoach hoc tap.xlsx` | `App_Data/ke_hoach_hoc_tap.xlsx` | **Nguồn import** (2 sheet bên dưới). Dùng để nạp database lần đầu (seed) và cho trang `/Import`. Khi app chạy bình thường **không đọc** file này |
| `Bieu_mau_trong_Ke_hoach_giang_day_hoc_tap.docx` | `App_Data/Templates/BieuMau_KeHoachGiangDay.docx` | **File mẫu xuất Word mặc định**. Giữ nguyên bản gốc này làm fallback; quản trị viên có thể tải về, chỉnh sửa và upload template mới trực tiếp trên web |
| `khung chuong trinh.xlsx` | Thư mục gốc / `docs/` | **Tài liệu tham chiếu thực tế**: Khung chương trình đào tạo chính trị/cán bộ thực tế (Triết học Mác - Lênin, Kinh tế chính trị, Xây dựng Đảng...) minh họa tính tổng quát cho hệ thống |
| `BANG DIEM TONG K56.01.xls` / `.pdf` | Thư mục gốc / `docs/` | **Tài liệu tham chiếu thực tế**: Bảng điểm tổng kết môn thực tế của lớp K56, cơ sở cho tính năng quản lý cột Điểm và tính Điểm TB |
| `script (1).sql` | Thư mục gốc / `docs/` | **CSDL tham chiếu (`Daotao_HV3`)**: Hệ thống quản lý đào tạo cổ điển, minh chứng cho các trường thông tin môn học, thời khóa biểu và biểu mẫu |

### 3.1 Sheet `Khung CT` (đọc cột A–C)

| Cột | Ý nghĩa | App dùng? |
|---|---|---|
| A `Stt` | Số thứ tự | Có |
| B `Môn học` | Tên môn | Có |
| C `Số tiết học` | Tổng số tiết (đã gồm 10 tiết tự nghiên cứu) | Có |
| D `Từ ngày`, E `Đến ngày`, F `Thi hết môn` | **Công thức** Excel dạng `"S 11/9/2025"` | **KHÔNG đọc.** App tự tính lại bằng C# (mục 4). Dùng giá trị cache của D–F chỉ làm đối chiếu trong test |

Đọc từ hàng 2 đến hết, bỏ qua hàng trống tên môn. File mẫu có 19 môn nhưng **không hard-code số 19**.

### 3.2 Sheet `Ngay nghi` (đọc cột A–D)

| Cột | Ý nghĩa |
|---|---|
| B `Tên Ngày` | Ví dụ "Ngày khai giảng", "Tết Dương lịch", "Tết Nguyên Dán", "Giỗ Tổ Hùng Vương", "Ngày Chiến Thắng", "Ngày Quốc Khánh" |
| C `Từ ngày`, D `Đến ngày` | Khoảng ngày nghỉ (D trống ⇒ D = C) |
| E–F `NgayNghi_List` | **Bỏ qua.** App tự bung khoảng C→D ra từng ngày (kết quả phải trùng 18 ngày trong cột F) |

- **Ngày khai giảng** = hàng có `Tên Ngày` chứa "khai giảng" (không phân biệt hoa thường/dấu). Không có ⇒ báo lỗi rõ ràng.
- Ngày khai giảng cũng là ngày không học môn (nằm trong tập ngày nghỉ).
- Dữ liệu mẫu có lỗi chính tả "Tết Nguyên Dán": giữ nguyên, không tự sửa.

### 3.3 Quy tắc import (Excel → database)

- `IPlanImporter` dùng `IExcelPlanReader` đọc Excel theo 3.1–3.2, rồi tạo `Subject` (SortOrder theo thứ tự hàng) và `Holiday` (hàng khai giảng ⇒ `IsOpeningDay = true`).
- Thực hiện trong **1 transaction**: thay thế toàn bộ `Subject` và `Holiday`; lỗi giữa chừng thì database giữ nguyên như cũ.
- Tuỳ chọn **"Giữ điểm/dự phòng/ghi chú nếu trùng tên môn"** (mặc định bật): môn mới trùng tên (không phân biệt hoa thường, bỏ khoảng trắng thừa) môn cũ thì chép lại `Grade`, `ReservePeriods`, `Note`.
- Seed lần đầu: khi khởi động, nếu bảng `Subject` **và** `Holiday` đều trống và file `Plan:SeedExcelPath` tồn tại ⇒ tự import (không giữ gì vì chưa có gì).

## 4. Thuật toán xếp lịch (lõi nghiệp vụ)

**Quy ước**
- 1 buổi = 5 tiết. 1 ngày có 2 buổi: **S** (sáng) rồi **C** (chiều).
- Ngày học = thứ 2 → thứ 6, **không** thuộc tập ngày nghỉ. Thứ 7, CN luôn nghỉ.
- Buổi (`Slot`) = (ngày, S|C). Có thứ tự: S trước C cùng ngày, rồi sang ngày học kế tiếp.

**Hàm**
- Tập ngày nghỉ = mọi ngày trong các khoảng `Holiday.From → Holiday.To` (bung ra từng ngày, khoảng chồng nhau thì gộp).
- `NextWorkday(d)` = ngày học đầu tiên **sau** `d`.
- `Advance(slot, n)` = buổi cách `slot` n buổi về sau (n ≥ 0), bỏ qua ngày nghỉ.

**Với mỗi môn theo `SortOrder` (Stt hiển thị = vị trí 1..n)**
```
sessions = ceil(SoTiet / 5)
start    = (môn đầu)  ? (NextWorkday(ngàyKhaiGiảng), S)
                      : (sau môn trước có hoạt động xen kẽ) 
                          ? Advance(kếtThúcHoạtĐộng, 1) // hoặc NextWorkday(activity.To)
                          : Advance(thiMônTrước, 1)    // buổi ngay sau buổi thi môn trước
end      = Advance(start, sessions - 1)
exam     = Advance(end, 1)                              // 1 buổi thi, ngay sau buổi học cuối
```
Môn có thể bắt đầu ở buổi chiều (C) nếu môn trước thi ở buổi sáng. Đây là hành vi đúng, khớp công thức Excel gốc.

**Xử lý hoạt động xen kẽ giữa các môn (`Interim Activities`):**
- Giáo viên có thể chèn thêm sự kiện/khoảng nghỉ vào sau bất kỳ môn học nào (ví dụ: *Lớp đi nghiên cứu thực tế tại cơ sở*, *Nghỉ từ ngày đến ngày để viết bài thu hoạch/tiểu luận*).
- Nếu cấu hình theo khoảng ngày (`FromDate` → `ToDate`): Hoạt động chiếm trọn khoảng ngày này. Môn học tiếp theo sẽ bắt đầu tại buổi sáng (S) của `NextWorkday(activity.ToDate)`.
- Nếu cấu hình theo số buổi/số tiết: Hoạt động bắt đầu tại buổi làm việc kế tiếp sau buổi thi của môn trước (`Advance(exam, 1)`), kéo dài $N$ buổi. Môn học tiếp theo bắt đầu ngay buổi kế tiếp sau khi hoạt động kết thúc.

**Xử lý chuỗi sự kiện sau môn cuối cùng (`Closing Activities`):**
- Sau môn học cuối cùng của chương trình, hệ thống hỗ trợ chèn chuỗi các nhiệm vụ kết thúc khóa học:
  1. *Ôn thi tốt nghiệp*: Thời gian bắt đầu, kết thúc, số tiết (mặc định 15 tiết).
  2. *Thi tốt nghiệp*: Lịch thi tốt nghiệp (3 môn tốt nghiệp TTN1, TTN2, TTN3).
  3. *Họp lớp tổng kết*: Đánh giá rèn luyện, bình xét thi đua.
  4. *Bế giảng*: Lễ bế giảng và trao bằng tốt nghiệp (mặc định 5 tiết).
- Các sự kiện này nối tiếp linh hoạt trên dòng thời gian Kế hoạch và tự động xuất vào các hàng cuối của file biểu mẫu Word.

**Hiển thị buổi:** `S 11/9/2025`, `C 18/9/2025` — định dạng ngày `d/M/yyyy`, **không** đệm số 0.

**Kết quả chuẩn (golden)** — với `ke_hoach_hoc_tap.xlsx` mẫu, thuật toán PHẢI cho đúng bảng sau (đã đối chiếu khớp 100% với giá trị Excel và với biểu mẫu Word mẫu):

| Stt | Môn học | Số tiết | Số buổi | Bắt đầu | Kết thúc | Thi hết môn |
|---|---|---|---|---|---|---|
| 1 | Tên môn học 01 | 60 | 12 | S 11/9/2025 | C 18/9/2025 | S 19/9/2025 |
| 2 | Tên môn học 02 | 55 | 11 | C 19/9/2025 | C 26/9/2025 | S 29/9/2025 |
| 3 | Tên môn học 03 | 55 | 11 | C 29/9/2025 | C 6/10/2025 | S 7/10/2025 |
| 4 | Tên môn học 04 | 40 | 8 | C 7/10/2025 | S 13/10/2025 | C 13/10/2025 |
| 5 | Tên môn học 05 | 60 | 12 | S 14/10/2025 | C 21/10/2025 | S 22/10/2025 |
| 6 | Tên môn học 06 | 70 | 14 | C 22/10/2025 | S 31/10/2025 | C 31/10/2025 |
| 7 | Tên môn học 07 | 45 | 9 | S 3/11/2025 | S 7/11/2025 | C 7/11/2025 |
| 8 | Tên môn học 08 | 45 | 9 | S 10/11/2025 | S 14/11/2025 | C 14/11/2025 |
| 9 | Tên môn học 09 | 40 | 8 | S 17/11/2025 | C 20/11/2025 | S 21/11/2025 |
| 10 | Tên môn học 10 | 45 | 9 | C 21/11/2025 | C 27/11/2025 | S 28/11/2025 |
| 11 | Tên môn học 11 | 70 | 14 | C 28/11/2025 | S 9/12/2025 | C 9/12/2025 |
| 12 | Tên môn học 12 | 35 | 7 | S 10/12/2025 | S 15/12/2025 | C 15/12/2025 |
| 13 | Tên môn học 13 | 50 | 10 | S 16/12/2025 | C 22/12/2025 | S 23/12/2025 |
| 14 | Tên môn học 14 | 45 | 9 | C 23/12/2025 | C 29/12/2025 | S 30/12/2025 |
| 15 | Tên môn học 15 | 40 | 8 | C 30/12/2025 | S 6/1/2026 | C 6/1/2026 |
| 16 | Tên môn học 16 | 35 | 7 | S 7/1/2026 | S 12/1/2026 | C 12/1/2026 |
| 17 | Tên môn học 17 | 30 | 6 | S 13/1/2026 | C 15/1/2026 | S 16/1/2026 |
| 18 | Tên môn học 18 | 35 | 7 | C 16/1/2026 | C 21/1/2026 | S 22/1/2026 |
| 19 | Tên môn học 19 | 40 | 8 | C 22/1/2026 | S 28/1/2026 | C 28/1/2026 |

Tổng: 19 môn, 895 tiết, 179 buổi học.

Test tự động bắt buộc: so sánh 19 dòng trên, và nếu file Excel có giá trị cache ở cột D–F thì so thêm với cache.

## 5. Mô hình dữ liệu & database

**Entity (EF Core):**

```csharp
class Subject {
  int Id; int SortOrder;              // SortOrder 1..n; Stt hiển thị = vị trí theo SortOrder
  string Name;                        // bắt buộc, ≤ 200 (thuộc khung chương trình đào tạo)
  int Periods;                        // số tiết, > 0
  decimal? Grade;                     // điểm 0–10, 1 chữ số thập phân (cũ/nhanh)
  int? ReservePeriods;                // dự phòng (tiết) ≥ 0
  string? Note;                       // ≤ 100
  string ExamMode;                    // "Immediately" (mặc định), "Delayed" (hoãn thi, dồn thi), "Custom" (tùy chỉnh)
  string? CustomExamSlot;             // Buổi thi tùy chỉnh (≤ 100, vd: "S 20/10/2025")
}
class Holiday {
  int Id; string Name;                // ≤ 200
  DateOnly From; DateOnly To;         // To ≥ From; Ngày khai giảng có thể là 1 ngày hoặc khoảng nhiều ngày
  bool IsOpeningDay;                  // đúng 1 dòng = true
}
class AppSetting {
  string Key;                         // Khóa cấu hình (PK, ≤ 100, vd: "Plan:FirstSessionHalf")
  string Value;                       // Giá trị cấu hình
}

// Thực thể Quản lý Hoạt động xen kẽ giữa các môn & Chuỗi hoạt động sau môn cuối
enum PlanActivityType { Interim = 0, Closing = 1 }
class PlanActivity {
  int Id;
  string Name;                        // Tên hoạt động (vd: "Nghiên cứu thực tế", "Nghỉ viết bài", "Ôn thi tốt nghiệp", "Họp lớp", "Bế giảng")
  PlanActivityType Type;              // Interim (xen kẽ sau môn), Closing (chuỗi sau môn cuối)
  int? AfterSubjectId;                // FK Subject; null nếu là Closing sau môn cuối
  int SortOrder;                      // Thứ tự trong chuỗi (1..n)
  DateOnly? FromDate;                 // Từ ngày (nếu chỉ định khoảng ngày thực tế)
  DateOnly? ToDate;                   // Đến ngày (ToDate >= FromDate)
  int? Sessions;                      // Số buổi (nếu tính theo số buổi học)
  int? Periods;                       // Số tiết (mặc định 5 tiết/buổi, vd: 15 tiết thi tốt nghiệp, 5 tiết bế giảng)
  string? Note;                       // Ghi chú hoạt động
}

// Thực thể Quản lý Học viên & Bảng điểm tổng hợp (chuẩn lớp K56)
class Student {
  int Id;
  int SortOrder;                      // Thứ tự hiển thị trong danh sách lớp (1..n)
  string StudentCode;                 // Mã số học viên (bắt buộc, ≤ 50, vd: "K75A56.0101")
  string LastName;                    // Họ lót (bắt buộc, ≤ 100, vd: "Đặng Thị Hội")
  string FirstName;                   // Tên (bắt buộc, ≤ 50, vd: "An")
  DateOnly? DateOfBirth;              // Ngày sinh (vd: 14/04/1982)
  string? Gender;                     // Giới tính (Nam/Nữ)
  string? Note;                       // Ghi chú
  ICollection<StudentGrade> Grades;   // Danh sách điểm các môn của học viên
}

class StudentGrade {
  int Id;
  int StudentId;                      // FK Student
  int SubjectId;                      // FK Subject (chỉ môn trong khung chương trình)
  decimal? Score;                     // Điểm hệ 10 (0.00 – 10.00), làm tròn 2 chữ số thập phân
  string? Note;                       // Ghi chú (nếu có)
}
```

- Index: `Subject(SortOrder)`, `Holiday(From)`. Dùng Migrations; `PlanDbContext` dùng `UseSqlServer`.
- Bảng `AppSetting` lưu các tùy chọn động: buổi bắt đầu học môn 1 (`Plan:FirstSessionHalf`), đường dẫn file template tùy biến, các thông số ExtraTasks (Thi tốt nghiệp, Bế giảng) được chỉnh sửa trực tiếp trên Web.
- **Tính toán lịch linh hoạt**:
  + Khai giảng: Hỗ trợ khoảng ngày `From` đến `To`. Ngày học đầu tiên = `NextWorkday(openingHoliday.To)`. Buổi bắt đầu học môn 1 có thể chọn Sáng (S) hoặc Chiều (C).
  + Lịch thi: Hỗ trợ 3 chế độ (`Immediately` - thi ngay; `Delayed` - hoãn thi, môn sau học luôn, dồn thi 2 môn; `Custom` - chỉ định buổi thi). Thi xong học luôn buổi tiếp theo.
- **Không lưu kết quả lịch** xuống database. Luôn tính lại từ `Subject` + `Holiday` (19 môn tính < 50 ms).
- Xoá hoặc đổi chỗ môn ⇒ đánh lại `SortOrder` liên tục 1..n trong cùng transaction.
- Ràng buộc "đúng 1 ngày khai giảng" kiểm ở tầng service (không dựa riêng vào database).

**Kiểu domain thuần (không phải entity):**

```csharp
enum Half { S = 0, C = 1 }
record Slot(DateOnly Date, Half Half);                 // IComparable
record ScheduledSubject(Subject Subject, int Number, int Sessions, Slot Start, Slot End, Slot Exam);
record ScheduledActivity(PlanActivity Activity, Slot? Start, Slot? End);
record ScheduleResult(DateOnly OpeningDay, IReadOnlyList<Holiday> Holidays,
                      IReadOnlyList<ScheduledSubject> Items, 
                      IReadOnlyList<ScheduledActivity> Activities,
                      IReadOnlyList<string> Warnings);

// Kiểu domain cho Bảng điểm tổng hợp
record StudentGradeItem(int SubjectId, string SubjectName, decimal? Score);
record StudentSummary(Student Student, int Stt, 
                      IReadOnlyList<StudentGradeItem> SubjectGrades, 
                      int CompletedSubjectsCount, 
                      decimal? AverageScore, 
                      string AverageScoreInWords, 
                      string Classification);
record GradeSheetResult(IReadOnlyList<Subject> Subjects, 
                        IReadOnlyList<StudentSummary> Students, 
                        int TotalStudents, 
                        IReadOnlyDictionary<string, int> ClassificationStats);
```

**Dịch vụ (DI, đều có interface):**
- `IScheduleCalculator.Calculate(subjects, holidays, activities)` → `ScheduleResult`. Hàm thuần, không I/O, dễ test.
- `IPlanRepository` — CRUD `Subject`/`Holiday`/`PlanActivity`/`Student`/`StudentGrade` qua EF Core, đổi thứ tự, đánh lại `SortOrder`.
- `IExcelPlanReader` — đọc Excel → `(subjects, holidays)` chưa lưu.
- `IPlanImporter` — nạp khung chương trình và ngày nghỉ từ Excel vào DB trong 1 transaction.
- `IPlanService` — "lấy dữ liệu từ repository → tính lịch → trả `ScheduleResult`", dùng chung cho kế hoạch giảng dạy và xuất file.
- `IGradeService` — quản lý học viên, tính toán ma trận điểm, tự động chuyển đổi điểm trung bình sang chữ tiếng Việt, xếp loại học lực.
- `IVietnameseNumberToWordsConverter` — đọc điểm thập phân sang tiếng Việt chuẩn học vụ (vd: 7.81 -> "Bảy phẩy tám mươi mốt").
- `IGradeRankClassifier` — phân loại học lực (Xuất sắc, Giỏi, Khá, Trung bình, Chưa đạt).
- `IExcelGradeImporter` — đọc danh sách học viên và điểm từ file Excel tổng hợp (`BANG DIEM TONG K56.01.xls`).
- `IExporter` — mục 7.

Mở file Excel upload bằng `MemoryStream`, không ghi ra đĩa.

## 6. Phân hệ 1 — Kế hoạch giảng dạy & học tập (`/` hoặc `/Plan`)

Đọc từ database và hiển thị tiến trình kế hoạch đào tạo toàn khóa ngay khi mở trang. **Khách** chỉ xem; **quản trị** sửa được (mục 12).

### 6.1 Giao diện Kế hoạch (Classic Desktop DataGrid)

Bảng tiến độ hiển thị tuần tự dòng thời gian bao gồm:
1. **Dòng Khai giảng**: Lấy từ bảng `Holiday` có cờ `IsOpeningDay = true` (có thể kéo dài 1 hoặc nhiều ngày, chọn buổi bắt đầu học môn 1 là Sáng hoặc Chiều).
2. **Các Môn học chính khóa**: Mỗi môn hiển thị Stt, Tên môn, Số tiết, Số buổi, Bắt đầu, Kết thúc, Buổi thi, Chế độ thi (`Immediately`, `Delayed`, `Custom`), Dự phòng, Ghi chú.
3. **Các Hoạt động xen kẽ sau môn (`Interim Activities`)**:
   - Hiển thị trực tiếp ngay dưới môn học được chỉ định (`AfterSubjectId`).
   - Được làm nổi bật trực quan bằng dải màu nền xanh dương nhạt (#EBF3FC), viền nét mảnh, biểu tượng 📌 ghi chú hoạt động.
   - Thể hiện rõ: Tên hoạt động (vd: *Lớp đi nghiên cứu thực tế tại cơ sở*, *Nghỉ viết bài thu hoạch môn học*), khoảng thời gian (Từ ngày → Đến ngày) hoặc số buổi/tiết.
   - Môn học phía sau tự động bắt đầu ngay sau khi hoạt động xen kẽ kết thúc.
4. **Chuỗi Hoạt động kết thúc khóa học (`Closing Activities`)**:
   - Nằm ở cuối bảng, ngay sau môn học cuối cùng.
   - Gồm các nhiệm vụ quan trọng: *Ôn thi tốt nghiệp*, *Thi tốt nghiệp (TTN1, TTN2, TTN3)*, *Họp lớp tổng kết*, *Bế giảng khóa học*.
   - Cho phép Quản trị viên thêm/sửa/xóa hoặc điều chỉnh thời gian và số tiết.

| Cột | Nội dung | Quản trị sửa |
|---|---|---|
| Stt | Vị trí môn (1..n) hoặc ký hiệu hoạt động xen kẽ (vd: 1.1, 19.1...) | |
| Tên môn / Hoạt động | `Subject.Name` hoặc `PlanActivity.Name` | Có |
| Số tiết | `Periods` (hoặc số tiết hoạt động) | Có |
| Số buổi | `ceil(tiết/5)` | |
| Bắt đầu / Kết thúc / Thi | `S 11/9/2025`… | Có (khi chọn chế độ thi Custom) |
| Chế độ thi | Thi ngay / Hoãn, dồn thi / Tùy chỉnh | Có |
| Dự phòng (tiết) | số nguyên ≥ 0, mặc định trống | Có |
| Ghi chú | text ≤ 100 ký tự | Có |
| Thao tác | ▲ ▼ đổi thứ tự, ➕ Chèn hoạt động sau môn, ✏ Sửa, 🗑 Xóa | Có |

- Quản trị có các nút thao tác trên Toolbar:
  + **➕ Thêm môn học**
  + **📌 Chèn hoạt động xen kẽ sau môn** (chọn môn đứng trước, tên hoạt động: Nghiên cứu thực tế / Nghỉ viết bài..., từ ngày đến ngày).
  + **🎓 Cấu hình hoạt động cuối khóa** (Ôn thi tốt nghiệp, Thi tốt nghiệp, Họp lớp, Bế giảng).
  + **🔄 Tải lại / Tính lại lịch**.
  + **📄 Xuất file biểu mẫu Word** (`/Export`).
- Kiểm tra hợp lệ ở server: số tiết > 0, tên không trống, đến ngày ≥ từ ngày.

### 6.2 Trang Ngày nghỉ (`/Holidays`)

- Bảng: Tên, Từ ngày, Đến ngày, Số ngày, Khai giảng (✓). Khách chỉ xem.
- Quản trị: thêm/sửa/xoá. Kiểm tra: `Đến ngày ≥ Từ ngày`; luôn đúng 1 ngày khai giảng (không xoá được dòng khai giảng duy nhất; đổi khai giảng sang dòng khác thì dòng cũ tự bỏ cờ).
- Hỗ trợ chọn buổi bắt đầu học môn 1 (Sáng hoặc Chiều) khi khai giảng nhiều ngày.
- Sau khi lưu, lịch tính lại ngay.

### 6.3 Trang Nhập kế hoạch từ Excel (`/Import`, chỉ quản trị)

1. Chọn file `.xlsx` (≤ 5 MB) → kiểm tra: đọc được, có đủ 2 sheet `Khung CT` + `Ngay nghi`, ≥ 1 môn hợp lệ, có ngày khai giảng.
2. **Xem trước:** số môn, số khoảng nghỉ, ngày khai giảng, danh sách cảnh báo — chưa ghi gì.
3. Tick "Giữ điểm/dự phòng/ghi chú nếu trùng tên môn" (mặc định bật) → nút **Xác nhận thay thế toàn bộ** (có cảnh báo chữ đỏ: dữ liệu hiện tại sẽ bị thay).
4. Thực hiện theo 3.3 trong 1 transaction. Báo kết quả (đã nạp N môn, M khoảng nghỉ).

## 6.4 Phân hệ 2 — Bảng điểm tổng hợp học viên (`/Grades`)

Tách biệt thành trang quản lý điểm độc lập, phục vụ công tác đào tạo và quản lý học vụ của lớp (chuẩn thực tế tham chiếu từ `BANG DIEM TONG K56.01.xls` và các hình ảnh đính kèm của lớp K56).

### 6.4.1 Nguyên tắc cấu trúc
1. **ĐÚNG số môn như khung chương trình**: Cột điểm chỉ bao gồm các môn học chính khóa trong Khung chương trình (Triết học, Kinh tế chính trị, CNXHKH, Tư tưởng Hồ Chí Minh, Lịch sử Đảng, Xây dựng Đảng, Giáo dục QP-AN, Quan hệ quốc tế, Chính trị học, Khoa học LĐ, NN và PL, Lý luận và PL về QCN, Quản lý kinh tế, Kinh tế phát triển, VH và PT, XHH trong LĐ-QL, Giới trong LĐ-QL, TG và TN, LLDT và QHĐT ở VN, TTN1, TTN2, TTN3...). **Tuyệt đối không lẫn các hoạt động ngoại khóa, nghiên cứu thực tế hay ngày nghỉ vào cột điểm**.
2. **Bố cục DataGrid phong cách Desktop Cổ Điển**:
   - **Các cột thông tin học viên cố định (Sticky Left Columns)**:
     + `Stt`: Thứ tự học viên (1, 2, 3...)
     + `Mã số`: Mã số học viên (vd: `K75A56.0101`, `K75A56.0102`...)
     + `Họ lót`: Họ và tên đệm (vd: `Đặng Thị Hội`, `Đoàn Phan`...)
     + `Tên`: Tên học viên (vd: `An`, `Anh`, `Bảy`, `Bê`...)
     + `Ngày sinh`: Định dạng `dd/MM/yyyy` (vd: `14/04/1982`)
   - **Các cột điểm môn học theo Khung chương trình**:
     + Mỗi cột ứng với một môn học theo thứ tự `SortOrder`.
     + Tiêu đề cột được định dạng compact hoặc xoay dọc (vertical text header 90 độ) thanh lịch, tiết kiệm diện tích màn hình.
     + Ô điểm: Hiển thị 2 chữ số thập phân (vd: `7.50`, `8.00`, `7.75`, `8.25`).
     + Cho phép Quản trị viên nhập/chỉnh sửa điểm trực tiếp (Inline Editing) hoặc mở Modal nhập điểm theo từng học viên.
   - **Các cột tổng kết học tập tự động (Sticky Right Columns)**:
     + `Số môn`: Đếm số lượng môn học viên đã có điểm (vd: 22 môn).
     + `ĐTB`: Điểm trung bình cộng các môn đã có điểm, tính theo công thức:
       $$\text{ĐTB} = \frac{\sum_{i=1}^{k} \text{Score}_i}{k}$$
       Làm tròn 2 chữ số thập phân (vd: `7.81`, `7.26`, `8.34`, `7.50`).
     + `ĐTB bằng chữ`: Tự động đọc số điểm ĐTB sang chữ tiếng Việt chuẩn văn bằng, học bạ:
       * `7.81` → *"Bảy phẩy tám mươi mốt"*
       * `7.26` → *"Bảy phẩy hai mươi sáu"*
       * `7.35` → *"Bảy phẩy ba mươi lăm"*
       * `7.60` → *"Bảy phẩy sáu"* hoặc *"Bảy phẩy sáu mươi"*
       * `8.34` → *"Tám phẩy ba mươi bốn"*
       * `8.23` → *"Tám phẩy hai mươi ba"*
     + `Xếp loại`: Tự động xếp loại học lực theo thang điểm chuẩn:
       * **Xuất sắc**: $\text{ĐTB} \ge 9.0$ và không có môn nào dưới 7.0
       * **Giỏi**: $\text{ĐTB} \ge 8.0$ và không có môn nào dưới 6.0
       * **Khá**: $\text{ĐTB} \ge 7.0$ và không có môn nào dưới 5.0
       * **Trung bình**: $\text{ĐTB} \ge 5.0$
       * **Yếu / Chưa đạt**: $\text{ĐTB} < 5.0$ hoặc có môn thi lại/chưa đạt.

### 6.4.2 Tính năng quản lý bảng điểm
- **Thanh Toolbar quản trị**:
  + **➕ Thêm học viên**: Nhập Mã số, Họ lót, Tên, Ngày sinh, Giới tính.
  + **⚡ Nhập điểm nhanh**: Hỗ trợ di chuyển bằng phím Tab / Enter giữa các ô điểm.
  + **📥 Import Bảng điểm từ Excel**: Đọc danh sách học viên và điểm từ file `BANG DIEM TONG K56.01.xls` hoặc file Excel chuẩn theo định dạng.
  + **📤 Xuất Excel Bảng điểm**: Xuất file `.xlsx` định dạng chuẩn như file mẫu, có đầy đủ công thức ĐTB, số môn, ĐTB bằng chữ, xếp loại và styling viền bảng.
  + **🔍 Tìm kiếm & Lọc học viên**: Tìm theo tên, mã số, hoặc lọc theo xếp loại (Giỏi, Khá, Trung bình...).
- Khách vãng lai: Chỉ xem bảng điểm và xem thống kê xếp loại lớp học.

## 7. Xuất file (`/Export`)

Kiến trúc mở rộng:

```csharp
interface IExporter {
    string Id { get; }            // "docx-bieumau"
    string DisplayName { get; }   // "Biểu mẫu Kế hoạch giảng dạy và học tập (.docx)"
    string Extension { get; }     // ".docx"
    string ContentType { get; }
    byte[] Export(ScheduleResult result, ExportOptions options);   // điểm/dự phòng/ghi chú nằm sẵn trong Subject
}
```
- Đăng ký qua DI (`IEnumerable<IExporter>`); trang `/Export` liệt kê mọi exporter, mỗi cái 1 nút **Tải xuống**. Thêm loại mới = thêm 1 class, **không** sửa trang.
- Endpoint: `GET /Export/Download?id=docx-bieumau` trả `File(bytes, contentType, fileName)`. Khách tải được; file sinh trong bộ nhớ, **không ghi ra đĩa** server.
- Tên file: `KeHoachGiangDayHocTap_yyyyMMdd.docx`.

### 7.1 Exporter #1 — `docx-bieumau` (BẮT BUỘC)

Sinh từ file mẫu `App_Data/Templates/BieuMau_KeHoachGiangDay.docx` bằng cách **clone mẫu**, không dựng văn bản từ đầu, để giữ nguyên font Calibri, cỡ chữ, khung bảng, lề A4 dọc.

Cấu trúc mẫu: bảng đầu 2×2 rỗng (khối quốc hiệu/đơn vị) → tiêu đề căn giữa **KẾ HOẠCH GIẢNG DẠY VÀ HỌC TẬP** (đậm, 14pt) → bảng chính 10 cột:

`TT | Tên môn học/Nhiệm vụ | Tiết giảng | Tự nghiên cứu | Ôn thi/thi | Thời gian dự phòng | Bắt đầu | Kết thúc | Thi hết môn | Ghi chú`

(2 hàng tiêu đề có gộp ô — giữ nguyên, không chạm.)

**Cách làm bảng chính**
1. Tìm bảng có ô chứa "Tên môn học/Nhiệm vụ" (không dựa vào chỉ số bảng).
2. Giữ 2 hàng tiêu đề. Lấy hàng dữ liệu đầu tiên của môn học làm **hàng mẫu** (định dạng ô).
3. Xoá mọi hàng dữ liệu cũ (kể cả hàng trống cuối), rồi thêm hàng clone theo thứ tự thời gian: **Khai giảng → Các môn học + Các hoạt động xen kẽ sau môn (Nghiên cứu thực tế, Viết bài...) → Chuỗi hoạt động sau môn cuối (Ôn thi tốt nghiệp, Thi tốt nghiệp, Họp lớp, Bế giảng)**.
4. Chỉ thay chữ trong run, giữ nguyên `rPr`; ô rỗng vẫn phải có đoạn văn.

**Ánh xạ cột** (khớp biểu mẫu mẫu)

| Cột Word | Môn học | Ghi chú |
|---|---|---|
| TT | 1, 2, 3… liên tục từ dòng Khai giảng | Khai giảng = 1, môn đầu = 2 |
| Tên | `Subject.Name` | |
| Tiết giảng | `Periods − 10` | `10` = tự nghiên cứu (cấu hình `Export:SelfStudyPeriods`) |
| Tự nghiên cứu | `10` | |
| Ôn thi/thi | `5` | = 1 buổi thi (cấu hình `Export:ExamPeriods`) |
| Dự phòng | `Subject.ReservePeriods` hoặc trống | |
| Bắt đầu / Kết thúc | `S 11/9/2025` | |
| Thi hết môn | `S 19/9/2025` | |
| Ghi chú | `Subject.Note` hoặc trống | |

Môn có `Periods ≤ 10` ⇒ Tiết giảng = 0 và thêm cảnh báo.

**Các dòng nhiệm vụ đặc biệt** (cấu hình `appsettings.json`, mục `Export:ExtraTasks`)

| Dòng | Số tiết | Bắt đầu | Kết thúc | Giá trị mặc định |
|---|---|---|---|---|
| Khai giảng | 10 | `S <ngày khai giảng>` | `C <ngày khai giảng>` | tự lấy từ sheet Ngay nghi |
| Thi tốt nghiệp | 15 | `S 22/02/2026` | `S 24/02/2026` | lấy đúng theo biểu mẫu mẫu, **người dùng sửa** |
| Bế giảng | 5 | `S 20/4/2026` | (trống) | như trên |

Chỉ điền cột "Tiết giảng" (cột 3) cho 3 dòng này; các cột 4–6 và 9–10 để trống. Nếu ngày của 2 dòng cuối rơi vào ngày nghỉ/cuối tuần theo dữ liệu Excel (mặc định sẽ như vậy: 22/02/2026 là Chủ nhật và nằm trong kỳ nghỉ Tết) ⇒ chỉ **cảnh báo** trên trang Export, vẫn xuất bình thường.

**Tiêu chí chấp nhận**
- Mở bằng Word không có hộp thoại "repair/sửa lỗi".
- Với dữ liệu mẫu, 19 dòng môn học trùng cột Bắt đầu/Kết thúc/Thi hết môn với biểu mẫu mẫu (golden ở mục 4).
- Bố cục, font, gộp ô tiêu đề giống mẫu. Nếu có LibreOffice trong môi trường: chuyển sang PDF và xem bằng mắt 1 lần.

### 7.2 Exporter bổ sung (làm sau, tuỳ chọn — xem PLAN giai đoạn 8)

| Id | Nội dung |
|---|---|
| `xlsx-plan` | Sheet "Kế hoạch" (bảng như trang 1) + sheet "Ngày nghỉ", dùng ClosedXML, giá trị là chữ/ngày thật (không công thức) |
| `ics-calendar` | Mỗi buổi học/thi 1 sự kiện. Giờ lấy từ cấu hình `Export:Ics:MorningStart` / `AfternoonStart` (mặc định 07:30 và 13:30, mỗi buổi 4 giờ); tiêu đề buổi thi bắt đầu bằng "THI:" |
| `csv-plan` | CSV UTF-8 có BOM của bảng trang 1 |

### 7.3 Quản lý & Chỉnh sửa Template trực tiếp trên Web (`/Templates`)

Để giáo viên và quản trị viên dễ dàng cập nhật mẫu biểu mà không cần can thiệp mã nguồn hay deploy lại, hệ thống cung cấp trang quản lý template:

1. **Xem & Tải về template hiện hành**:
   - Hiển thị thông tin file mẫu đang áp dụng (tên file, dung lượng, lần cập nhật cuối).
   - Nút **"Tải file mẫu hiện tại (.docx)"** để tải về máy, chỉnh sửa quốc hiệu, tên đơn vị, tiêu đề hoặc định dạng bằng Microsoft Word.
2. **Tải lên (Upload) template mới**:
   - Cho phép Quản trị viên upload file `.docx` mới (dung lượng ≤ 10 MB).
   - Kiểm tra hợp lệ nghiêm ngặt bằng OpenXml: File phải là tài liệu Word chuẩn, tìm thấy bảng có ô chứa chữ "Tên môn học/Nhiệm vụ", có 2 hàng tiêu đề và đủ 10 cột.
   - Nếu không hợp lệ: Báo lỗi chi tiết bằng tiếng Việt và giữ nguyên template đang hoạt động.
   - Nếu hợp lệ: Lưu an toàn và kích hoạt ngay cho các lần xuất file tiếp theo.
3. **Khôi phục mẫu gốc mặc định**:
   - Nút **"Khôi phục mẫu mặc định"** để quay về sử dụng file mẫu gốc ban đầu trong `App_Data/Templates/BieuMau_KeHoachGiangDay.docx`.
4. **Cập nhật thông số nhiệm vụ đặc biệt (ExtraTasks) trực tiếp trên Web**:
   - Form chỉnh sửa các thông số biểu mẫu:
     + **Thi tốt nghiệp**: Tên hiển thị, số tiết, ngày bắt đầu (vd: `S 22/02/2026`), ngày kết thúc.
     + **Bế giảng**: Tên hiển thị, số tiết, ngày bắt đầu (vd: `S 20/4/2026`).
     + **Tiết tự nghiên cứu mặc định**: (mặc định 10).
     + **Tiết ôn thi/thi mặc định**: (mặc định 5).
   - Các giá trị này được lưu trữ trong database (`AppSetting` / `SystemSetting`) ưu tiên hơn `appsettings.json`, giúp người dùng đổi ngày bế giảng/thi tốt nghiệp theo từng khóa học một cách trực quan.

## 8. Cấu hình (`appsettings.json`)

```json
{
  "ConnectionStrings": { "Default": "Server=(localdb)\\MSSQLLocalDB;Database=KeHoachHocTap;Trusted_Connection=True;" },
  "Database": { "AutoMigrate": true },
  "Plan": {
    "SeedExcelPath": "App_Data/ke_hoach_hoc_tap.xlsx",
    "PeriodsPerSession": 5
  },
  "Admin": { "Password": "" },
  "Export": {
    "TemplatePath": "App_Data/Templates/BieuMau_KeHoachGiangDay.docx",
    "SelfStudyPeriods": 10,
    "ExamPeriods": 5,
    "ExtraTasks": {
      "Graduation": { "Name": "Thi tốt nghiệp", "Periods": 15, "Start": "S 22/02/2026", "End": "S 24/02/2026" },
      "Closing":    { "Name": "Bế giảng",       "Periods": 5,  "Start": "S 20/4/2026",  "End": "" }
    }
  }
}
```

- **Không commit** mật khẩu thật và chuỗi kết nối thật. Đặt qua biến môi trường (`Admin__Password`, `ConnectionStrings__Default`) hoặc `appsettings.Production.json` nằm trong `.gitignore`.

## 9. Xử lý lỗi & trường hợp biên

| Tình huống | Hành vi |
|---|---|
| Không kết nối được database | Trang lỗi thân thiện "Không kết nối được cơ sở dữ liệu"; log chi tiết phía server; **không** hiện chuỗi kết nối |
| Database trống / chưa có ngày khai giảng | Trang hướng dẫn thêm dữ liệu hoặc import; không văng lỗi |
| Import: thiếu sheet / file hỏng / không có môn / không có khai giảng | Báo rõ lý do bằng tiếng Việt; database không bị đổi |
| `Số tiết` trống, không phải số nguyên > 0 (Excel hoặc form) | Import: bỏ môn đó + cảnh báo nêu số dòng Excel. Form: báo lỗi tại ô |
| `Số tiết` không chia hết cho 5 | Làm tròn lên số buổi + cảnh báo |
| Hai khoảng nghỉ chồng nhau | Gộp, không lỗi |
| `Đến ngày` < `Từ ngày` | Import: bỏ dòng + cảnh báo. Form: báo lỗi |
| Lịch dài quá 5 năm (vòng lặp bất thường) | Dừng ở giới hạn an toàn, báo lỗi (chống treo) |
| Thiếu file mẫu .docx | `/Export` báo rõ, các exporter khác vẫn dùng được |
| Ghi database lỗi | Báo lỗi, không mất dữ liệu cũ (transaction) |
| Chưa đăng nhập mà gọi thao tác ghi | Từ chối ở server (redirect `/Login` hoặc 403), không chỉ ẩn nút |

## 10. Phi chức năng

- Mở trang dưới 1 giây với 19 môn; tính lịch dưới 50 ms.
- Chạy được trên Windows/macOS/Linux (cần SQL Server: LocalDB hoặc Docker khi dev).
- Chạy ổn trong **256 MB RAM**: không giữ file lớn trong bộ nhớ; giới hạn upload 5 MB.
- Cảnh báo/lỗi hiển thị bằng tiếng Việt có dấu; mọi file dùng UTF-8.
- Mọi ngày dùng `DateOnly`; không dính múi giờ.
- Không gọi dịch vụ ngoài nào ngoài database.

## 11. Ngoài phạm vi

Nhiều tài khoản và phân quyền chi tiết, nhiều lớp/nhiều khoá, gửi email, đăng ký/quên mật khẩu.

## 12. Quản trị (đăng nhập đơn giản)

Vì host free có địa chỉ công khai, mọi thao tác **ghi** phải được bảo vệ.

- Mật khẩu quản trị duy nhất lấy từ `Admin:Password` (biến môi trường). **Để trống ⇒ app chạy ở chế độ chỉ xem** (khoá mọi thao tác ghi) và hiện cảnh báo cho dev.
- Trang `/Login` (form mật khẩu) → cookie authentication, hết hạn sau 8 giờ, `HttpOnly`, `Secure` khi chạy HTTPS, `SameSite=Lax`. Nút **Đăng xuất**.
- So sánh mật khẩu bằng `CryptographicOperations.FixedTimeEquals`; sai mật khẩu thì chờ 1 giây trước khi trả lời.
- Mọi handler ghi (sửa/thêm/xoá môn, ngày nghỉ, import) yêu cầu policy `Admin` **ở server** và có anti-forgery token. Khách thấy giao diện chỉ đọc (không có nút sửa).
- Khách xem được `/`, `/Holidays`, `/Export`.

## 13. Triển khai lên hosting free

Mục tiêu: host ASP.NET Core miễn phí có database (ví dụ MonsterASP.NET: SQL Server 1 GB, 256 MB RAM, máy chủ châu Âu, không gửi được email). Đọc điều khoản và giới hạn của host trước khi deploy.

- `dotnet publish -c Release`; đưa lên host bằng cách host hỗ trợ (Web Deploy / FTP / GitHub).
- Chuỗi kết nối và `Admin__Password` đặt trong biến môi trường hoặc `appsettings.Production.json` (không commit).
- `Database:AutoMigrate = true`: khởi động thì tự `Migrate()`. Nếu host không cho chạy migration tự động, dev xuất script `dotnet ef migrations script --idempotent` để chạy tay.
- Ứng dụng **không ghi file lúc chạy**: file Word sinh trong bộ nhớ, upload Excel xử lý bằng bộ nhớ, template `.docx` chỉ đọc (cấu hình `CopyToOutputDirectory` để có trong thư mục publish).
- Bật HTTPS redirect, HSTS ở môi trường Production.
- Seed tự động chỉ chạy khi database trống; trên host, nếu không upload kèm file Excel, dùng trang `/Import` sau khi đăng nhập.
