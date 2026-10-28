# SPEC — Web Kế hoạch học tập (ASP.NET Core)

> Đặc tả "cái gì". Thứ tự làm xem `PLAN.md`. Quy tắc làm việc xem `AGENTS.md`.
> **Phiên bản 2:** dữ liệu lưu trong **database**; Excel chỉ dùng để **nhập (import)**.
> Khi mâu thuẫn: **SPEC.md > PLAN.md > AGENTS.md**.

## 1. Mục tiêu

Web đơn giản lưu **môn học** và **ngày nghỉ** trong database, **tự tính** lịch học từng môn (dữ liệu mẫu có 19 môn). File Excel `ke_hoach_hoc_tap.xlsx` chỉ để nhập dữ liệu ban đầu. Hiển thị:

1. Danh sách môn học + số tiết + bảng điểm.
2. Xuất file; loại đầu tiên là **biểu mẫu Word "Kế hoạch giảng dạy và học tập"**.

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
                      : Advance(thiMônTrước, 1)        // buổi ngay sau buổi thi môn trước
end      = Advance(start, sessions - 1)
exam     = Advance(end, 1)                              // 1 buổi thi, ngay sau buổi học cuối
```
Môn có thể bắt đầu ở buổi chiều (C) nếu môn trước thi ở buổi sáng. Đây là hành vi đúng, khớp công thức Excel gốc.

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
  string Name;                        // bắt buộc, ≤ 200
  int Periods;                        // số tiết, > 0
  decimal? Grade;                     // điểm 0–10, 1 chữ số thập phân
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
record ScheduleResult(DateOnly OpeningDay, IReadOnlyList<Holiday> Holidays,
                      IReadOnlyList<ScheduledSubject> Items, IReadOnlyList<string> Warnings);
```

**Dịch vụ (DI, đều có interface):**
- `IScheduleCalculator.Calculate(subjects, holidays)` → `ScheduleResult`. Hàm thuần, không I/O, dễ test.
- `IPlanRepository` — CRUD `Subject`/`Holiday` qua EF Core, đổi thứ tự, đánh lại `SortOrder`.
- `IExcelPlanReader` — đọc Excel → `(subjects, holidays)` chưa lưu.
- `IPlanImporter` — mục 3.3.
- `IPlanService` — "lấy dữ liệu từ repository → tính lịch → trả `ScheduleResult`", dùng chung cho mọi trang.
- `IExporter` — mục 7.

Mở file Excel upload bằng `MemoryStream`, không ghi ra đĩa.

## 6. Trang 1 — Môn học & Bảng điểm (`/`)

Đọc từ database và hiển thị ngay khi mở trang. **Khách** chỉ xem; **quản trị** sửa được (mục 12).

Bảng, mỗi môn 1 dòng:

| Cột | Nội dung | Quản trị sửa |
|---|---|---|
| Stt | vị trí theo `SortOrder` | |
| Môn học | `Name` | Có |
| Số tiết | `Periods` | Có |
| Số buổi | `ceil(tiết/5)` | |
| Bắt đầu / Kết thúc / Thi | `S 11/9/2025`… | |
| **Điểm** | thang 10, 1 chữ số thập phân, trống = chưa có | **Có** |
| Dự phòng (tiết) | số nguyên ≥ 0, mặc định trống | Có |
| Ghi chú | text ≤ 100 ký tự | Có |
| Thao tác | ▲ ▼ đổi thứ tự, 🗑 xoá (có hỏi xác nhận) | Có |

- Quản trị có nút **Thêm môn**; mọi thay đổi lưu ngay (POST) rồi lịch tính lại.
- Dòng cuối: **Tổng số tiết**, **Điểm TB cộng**, **Điểm TB theo số tiết** (chỉ tính môn đã có điểm; hiện "—" nếu chưa có điểm nào).
- Kiểm tra hợp lệ ở server và hiện lỗi tại ô: điểm ngoài 0–10, số tiết ≤ 0, tên trống.
- "Dự phòng" và "Ghi chú" **không** ảnh hưởng thuật toán xếp lịch (đúng như biểu mẫu mẫu); chỉ đi vào file Word.
- Nút **Xuất file** → `/Export`. Quản trị có thêm nút **Nhập từ Excel** → `/Import`.
- Khu vực cảnh báo (`Warnings`) hiện trên đầu trang nếu có.
- Database trống ⇒ hiện hướng dẫn "Chưa có dữ liệu: thêm môn hoặc nhập từ Excel" (khách thì "Chưa có dữ liệu").

> **Giả định cần xác nhận:** "bảng điểm" = cột điểm nhập tay cho từng môn (Excel gốc không có điểm). Nếu cần nhiều cột điểm (chuyên cần, giữa kỳ, cuối kỳ…) thì thêm cột sau, không đổi kiến trúc.

### 6.1 Trang Ngày nghỉ (`/Holidays`)

- Bảng: Tên, Từ ngày, Đến ngày, Số ngày, Khai giảng (✓). Khách chỉ xem.
- Quản trị: thêm/sửa/xoá. Kiểm tra: `Đến ngày ≥ Từ ngày`; luôn đúng 1 ngày khai giảng (không xoá được dòng khai giảng duy nhất; đổi khai giảng sang dòng khác thì dòng cũ tự bỏ cờ).
- Sau khi lưu, lịch tính lại ngay.

### 6.2 Trang Nhập từ Excel (`/Import`, chỉ quản trị)

1. Chọn file `.xlsx` (≤ 5 MB) → kiểm tra: đọc được, có đủ 2 sheet `Khung CT` + `Ngay nghi`, ≥ 1 môn hợp lệ, có ngày khai giảng.
2. **Xem trước:** số môn, số khoảng nghỉ, ngày khai giảng, danh sách cảnh báo — chưa ghi gì.
3. Tick "Giữ điểm/dự phòng/ghi chú nếu trùng tên môn" (mặc định bật) → nút **Xác nhận thay thế toàn bộ** (có cảnh báo chữ đỏ: dữ liệu hiện tại sẽ bị thay).
4. Thực hiện theo 3.3 trong 1 transaction. Báo kết quả (đã nạp N môn, M khoảng nghỉ).

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
3. Xoá mọi hàng dữ liệu cũ (kể cả hàng trống cuối), rồi thêm hàng clone theo thứ tự: **Khai giảng → 19 môn → Thi tốt nghiệp → Bế giảng**.
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
