# PLAN — Thứ tự thực hiện (bản 2: có database)

> Làm **tuần tự từng giai đoạn**. Xong giai đoạn nào thì tick `[x]` ngay trong file này, chạy `dotnet build -warnaserror` + `dotnet test`, rồi mới sang giai đoạn tiếp. Chi tiết nghiệp vụ ở `SPEC.md` (số mục ghi trong ngoặc).

## Cấu trúc thư mục đích

```
KeHoachHocTap/
├─ AGENTS.md  PLAN.md  SPEC.md  README.md
├─ KeHoachHocTap.sln
├─ App_Data/
│  ├─ ke_hoach_hoc_tap.xlsx                  (file import mẫu, người dùng cung cấp)
│  └─ Templates/BieuMau_KeHoachGiangDay.docx (file người dùng cung cấp)
├─ src/KeHoachHocTap.Web/
│  ├─ Domain/        Slot, Half, ScheduledSubject, ScheduleResult
│  ├─ Data/          Subject, Holiday, PlanDbContext, Migrations/
│  ├─ Services/      ExcelPlanReader, ScheduleCalculator, PlanRepository, PlanImporter, PlanService
│  ├─ Export/        IExporter, DocxBieuMauExporter
│  ├─ Pages/         Index, Holidays, Import, Export, Login
│  ├─ wwwroot/       css, js, lib/bootstrap
│  └─ Program.cs, appsettings.json
└─ tests/KeHoachHocTap.Tests/
   ├─ ScheduleCalculatorTests.cs  ExcelPlanReaderTests.cs  PlanRepositoryTests.cs
   ├─ PlanImporterTests.cs  DocxExporterTests.cs
   └─ TestData/ (bản copy file Excel/Word mẫu)
```

---

## Giai đoạn 0 — Khởi tạo
- [x] Tạo solution + `src/KeHoachHocTap.Web` (Razor Pages, net8.0) + `tests/KeHoachHocTap.Tests` (xUnit).
- [x] Package (Web): `ClosedXML`, `DocumentFormat.OpenXml`, `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design`. Package (Tests): `Microsoft.EntityFrameworkCore.SqlServer`.
- [x] Copy 2 file đầu vào đúng chỗ (SPEC mục 3); copy Excel + Word mẫu vào `tests/.../TestData/`.
- [x] Bootstrap 5 tải về `wwwroot/lib` (không CDN). Tích hợp theme **Desktop cổ điển (Classic Desktop App Style)**: bảng màu Trắng (#FFFFFF, #F8F9FA), Đen/Xám đậm (chữ, viền sắc nét 1px), Xanh dương nhạt (#EBF3FC, #D0E2FF, #1D4ED8) nhấn nhá; thanh công cụ Toolbar phía trên, lưới dữ liệu DataGrid chia ô rõ ràng, thanh StatusBar phía dưới.
- [x] Layout có menu: **Môn học** · **Ngày nghỉ** · **Biểu mẫu Word & Xuất file** · **Quản lý Mẫu biểu** · Đăng nhập/Đăng xuất.
- [x] `appsettings.json` theo SPEC mục 8; bind bằng `IOptions<...>`. Thêm `.gitignore` (bin, obj, `appsettings.Production.json`, `*.db`).
- [x] Dev dùng SQL Server LocalDB (Windows) hoặc SQL Server trong Docker; đặt chuỗi kết nối vào `appsettings.Development.json` và `ConnectionStrings:Test` cho test.

**Xong khi:** `dotnet run` mở trang trống có menu; `dotnet test` chạy được.

## Giai đoạn 1 — Thuật toán xếp lịch + đọc Excel (làm TEST trước)
- [x] `ScheduleCalculator.Calculate(subjects, holidays)` hàm thuần (SPEC mục 4): bung khoảng nghỉ, `NextWorkday`, `Advance`, tính từng môn. Nhận các `Subject`/`Holiday` đối tượng thường (không cần database).
- [x] `ExcelPlanReader` (SPEC 3.1–3.2, 9): đọc `Khung CT` A–C và `Ngay nghi` A–D, tìm khai giảng, gom `Warnings`; mở bằng stream.
- [x] Test:
  - [x] Đọc đúng **19 môn**, tổng **895 tiết**, **6 khoảng nghỉ** (Excel mẫu), bung ra đúng **18 ngày**.
  - [x] **Golden test**: 19 dòng Bắt đầu/Kết thúc/Thi khớp bảng SPEC mục 4.
  - [x] Test biên: môn bắt đầu buổi chiều; kỳ nghỉ Tết 12–22/2 bị nhảy qua; số tiết không chia hết 5; tiết trống; khoảng nghỉ chồng nhau; thiếu khai giảng.

**Xong khi:** test xanh. **Không sang giai đoạn 2 nếu golden test chưa xanh.**

## Giai đoạn 2 — Database (SPEC mục 5, 3.3)
- [x] Entity `Subject`, `Holiday`, `AppSetting`; `PlanDbContext`; dùng `UseSqlServer`; index theo SPEC.
- [x] Tạo migration đầu tiên; `Database:AutoMigrate` chạy `Migrate()` lúc khởi động.
- [x] `PlanRepository`: CRUD môn/ngày nghỉ, đổi thứ tự, đánh lại `SortOrder` (1 transaction); luật đúng 1 ngày khai giảng; CRUD cấu hình `AppSetting`.
- [x] `PlanImporter`: Excel → database trong 1 transaction; tuỳ chọn giữ điểm/dự phòng/ghi chú theo tên môn.
- [x] Seed lần đầu khi hai bảng trống và có file `Plan:SeedExcelPath`.
- [x] `PlanService`: lấy dữ liệu → `ScheduleCalculator` → `ScheduleResult`.
- [x] Test (SQL Server test, tạo database tạm cho mỗi lần chạy rồi xoá): import Excel mẫu rồi `PlanService` cho ra đúng golden 19 môn; thêm/xoá/đổi chỗ môn thì `SortOrder` liên tục và lịch đổi theo; import lần 2 giữ điểm theo tên; import lỗi giữa chừng không làm đổi dữ liệu cũ; khai giảng luôn đúng 1; lưu/đọc `AppSetting`.

**Xong khi:** test xanh; chạy app lần đầu với DB trống tự có 19 môn.

## Giai đoạn 3 — Đăng nhập quản trị (SPEC mục 12)
- [x] Cookie auth + policy `Admin`; trang `/Login`, nút Đăng xuất; so mật khẩu bằng `FixedTimeEquals`, sai thì chờ 1 giây.
- [x] `Admin:Password` trống ⇒ chế độ chỉ xem + cảnh báo.
- [x] Anti-forgery cho mọi form POST; mọi handler ghi gắn `[Authorize(Policy = "Admin")]`.
- [x] Test: chưa đăng nhập gọi handler ghi bị từ chối; đăng nhập đúng/sai mật khẩu.

**Xong khi:** khách không sửa được dữ liệu dù tự gửi request tay.

## Giai đoạn 4 — Trang Môn học & Bảng điểm (`/`) (SPEC mục 6)
- [x] Bảng 19 môn: Stt, Môn, Số tiết, Số buổi, Bắt đầu, Kết thúc, Thi, Điểm, Dự phòng, Ghi chú.
- [x] Quản trị: sửa ô tại chỗ, thêm môn, xoá (xác nhận), ▲▼ đổi thứ tự; kiểm tra hợp lệ ở server (điểm 0–10…).
- [x] Hàng tổng: tổng tiết, điểm TB cộng, điểm TB theo số tiết (chỉ môn có điểm).
- [x] Hiển thị `Warnings`; màn hình "chưa có dữ liệu"; trang lỗi thân thiện khi mất kết nối DB.
- [x] Test: tính điểm TB; validate.

**Xong khi:** mở `/` thấy đủ 19 môn ngày đúng golden; quản trị sửa điểm/số tiết, F5 vẫn còn và lịch đổi theo.

## Giai đoạn 5 — Ngày nghỉ & Nhập từ Excel (SPEC 6.1, 6.2)
- [x] `/Holidays`: bảng + thêm/sửa/xoá (quản trị), kiểm tra `Đến ≥ Từ`, đúng 1 khai giảng.
- [x] `/Import`: upload `.xlsx` ≤ 5 MB (xử lý trong bộ nhớ) → xem trước → xác nhận thay thế toàn bộ.
- [x] Test: đổi ngày nghỉ thì ngày bắt đầu/thi các môn sau dịch theo; import lỗi báo đúng thông điệp.

**Xong khi:** thêm 1 ngày nghỉ mới → lịch trang `/` đổi đúng; import lại Excel mẫu → về đúng golden.

## Giai đoạn 6 — Xuất file: khung + biểu mẫu Word & Quản lý Template trên Web (SPEC mục 7, 7.3)
- [x] `IExporter` + đăng ký DI `IEnumerable<IExporter>`; trang `/Export` liệt kê; endpoint `/Export/Download?id=` (khách tải được, sinh trong bộ nhớ).
- [x] `DocxBieuMauExporter`:
  - [x] Mở bản copy của file mẫu (lấy file template tùy biến nếu có, fallback file mẫu gốc) trong `MemoryStream`.
  - [x] Tìm bảng chính theo chữ "Tên môn học/Nhiệm vụ"; giữ 2 hàng tiêu đề.
  - [x] Lấy hàng môn đầu làm hàng mẫu, xoá hàng dữ liệu cũ, clone theo thứ tự: Khai giảng → các môn → Thi tốt nghiệp → Bế giảng.
  - [x] Ánh xạ cột (Tiết giảng = Số tiết − 10, Tự nghiên cứu 10, Ôn thi/thi 5, dự phòng, ghi chú lấy từ `Subject`).
  - [x] Đọc ExtraTasks (ưu tiên từ `AppSetting`, fallback `appsettings.json`); cảnh báo nếu rơi vào ngày nghỉ.
- [x] Trang Quản lý Template trực tiếp trên Web (`/Templates`, yêu cầu quyền Admin):
  - [x] Xem trạng thái template đang kích hoạt, tải template hiện tại về máy (.docx).
  - [x] Upload file template `.docx` mới trực tiếp qua web; kiểm tra hợp lệ cấu trúc OpenXml (bảng 10 cột, header).
  - [x] Nút khôi phục file mẫu mặc định ban đầu.
  - [x] Form chỉnh sửa thông số ExtraTasks (Khai giảng, Thi tốt nghiệp, Bế giảng, Tiết tự học, Tiết ôn thi) lưu vào `AppSetting`.
- [x] Test: mở lại bằng OpenXml SDK không lỗi; đúng `19 + 3` hàng dữ liệu; hàng "Tên môn học 01" là `50 | 10 | 5 | | S 11/9/2025 | C 18/9/2025 | S 19/9/2025`; dự phòng/ghi chú nhập tay hiện đúng; tiêu đề "KẾ HOẠCH GIẢNG DẠY VÀ HỌC TẬP" còn; ô gộp tiêu đề còn (đếm `gridSpan`/`vMerge`); upload template không hợp lệ bị từ chối; cập nhật ExtraTasks đổi kết quả xuất file.
- [x] Nếu có LibreOffice: `soffice --headless --convert-to pdf`, xem ảnh trang 1 để kiểm bố cục (môi trường không cài sẵn soffice, đã kiểm định chi tiết cấu trúc OpenXml qua OpenXmlValidator).

**Xong khi:** tải được `.docx` mở bằng Word không báo lỗi, 19 môn trùng biểu mẫu mẫu; quản lý và cập nhật template/ExtraTasks trực tiếp trên web hoạt động trơn tru.

## Giai đoạn 7 — Hoàn thiện & triển khai (SPEC mục 10, 13)
- [x] Rà soát toàn bộ thông báo (tiếng Việt có dấu, UTF-8).
- [x] Thử tay các trường hợp biên SPEC mục 9 (tắt DB, DB trống, nhập điểm 11, import sai file, gọi POST khi chưa đăng nhập).
- [x] Cấu hình publish: template `.docx` có trong output (`CopyToOutputDirectory`); HTTPS redirect + HSTS ở Production.
- [x] Xuất script migration `--idempotent` đưa vào `docs/` (để chạy tay nếu host không cho tự migrate).
- [x] `README.md`: yêu cầu .NET 8, cách chạy dev (LocalDB hoặc Docker SQL Server), biến môi trường (`Admin__Password`, `ConnectionStrings__Default`), các bước deploy lên host free.
- [x] `dotnet build -warnaserror` sạch; `dotnet test` xanh; không còn TODO/hard-code "19".

## Giai đoạn 8 — Phân hệ Kế hoạch Nâng cao: Chèn Hoạt động xen kẽ & Chuỗi nhiệm vụ kết thúc khóa (SPEC mục 4, 5, 6)
- [ ] Mở rộng CSDL & Domain:
  - [ ] Thực thể `PlanActivity` (Id, Name, Type [`Interim` | `Closing`], AfterSubjectId, SortOrder, FromDate, ToDate, Sessions, Periods, Note).
  - [ ] Migration EF Core `AddPlanActivity` an toàn trên SQL Server.
  - [ ] Cập nhật `PlanRepository`: CRUD `PlanActivity`, sắp xếp thứ tự các hoạt động xen kẽ và hoạt động cuối khóa.
- [ ] Cập nhật thuật toán xếp lịch `ScheduleCalculator`:
  - [ ] Nếu sau môn $k$ có hoạt động xen kẽ (`Interim`):
    * Tính theo khoảng ngày (`FromDate` → `ToDate`): môn $k+1$ bắt đầu tại buổi sáng của `NextWorkday(ToDate)`.
    * Hoặc tính theo số buổi: môn $k+1$ bắt đầu tại buổi kế tiếp sau khi hoạt động kết thúc.
  - [ ] Xử lý chuỗi hoạt động sau môn cuối cùng (`Closing`): Ôn thi tốt nghiệp, Thi tốt nghiệp, Họp lớp, Bế giảng... tự động tính buổi bắt đầu/kết thúc hoặc áp dụng cấu hình tùy chọn.
- [ ] Cập nhật giao diện Trang Kế hoạch (`/` hoặc `/Plan`):
  - [ ] Bảng DataGrid hiển thị các dòng hoạt động xen kẽ ngay dưới môn học tương ứng (badge icon 📌, nền highlight dịu nhẹ #EBF3FC).
  - [ ] Nút & Modal "Chèn hoạt động xen kẽ sau môn" (chọn môn, nhập tên: Nghiên cứu thực tế / Nghỉ viết bài..., khoảng ngày).
  - [ ] Khối hiển thị & quản lý chuỗi sự kiện cuối khóa (Ôn thi tốt nghiệp, Thi tốt nghiệp, Họp lớp, Bế giảng).
- [ ] Cập nhật `DocxBieuMauExporter`: Tự động chèn các hoạt động xen kẽ và sự kiện kết thúc khóa vào bảng 10 cột theo đúng thứ tự thời gian.
- [ ] Viết unit test & integration test cho việc chèn hoạt động xen kẽ, kiểm tra tính toán dời lịch các môn sau.

**Xong khi:** Chèn 1 đợt nghiên cứu thực tế sau môn bất kỳ → lịch các môn sau tự động dịch chuyển chính xác; xuất Word có đủ dòng hoạt động; test xanh.

## Giai đoạn 9 — Phân hệ Bảng điểm Tổng hợp (`/Grades`) chuẩn K56 (SPEC 5, 6.4)
- [ ] Mở rộng CSDL & Domain cho Bảng điểm học viên:
  - [ ] Thực thể `Student` (Id, StudentCode, LastName, FirstName, DateOfBirth, Gender, SortOrder, Note).
  - [ ] Thực thể `StudentGrade` (Id, StudentId, SubjectId, Score, Note).
  - [ ] Migration EF Core `AddStudentAndGrades` an toàn.
  - [ ] Cập nhật `PlanDbContext`, cấu hình relationship, unique index `(StudentId, SubjectId)`, index `StudentCode`.
- [ ] Dịch vụ nghiệp vụ & thuật toán tính điểm:
  - [ ] `VietnameseNumberToWordsConverter`: Hàm thuần chuyển đổi điểm số sang chữ tiếng Việt chuẩn học bạ (vd: 7.81 -> "Bảy phẩy tám mươi mốt", 8.34 -> "Tám phẩy ba mươi bốn", 7.60 -> "Bảy phẩy sáu" / "Bảy phẩy sáu mươi").
  - [ ] `GradeRankClassifier`: Xếp loại tự động (Xuất sắc, Giỏi, Khá, Trung bình, Yếu/Chưa đạt) căn cứ ĐTB và điểm liệt từng môn.
  - [ ] `GradeService`: Tính ma trận điểm, số môn đã hoàn thành, ĐTB, ĐTB bằng chữ và thống kê xếp loại cả lớp.
- [ ] Giao diện Trang Bảng điểm Tổng hợp (`/Grades`):
  - [ ] Bố cục Desktop Cổ Điển DataGrid chia ô sắc nét:
    * Cột cố định bên trái (Sticky): Stt, Mã số HV, Họ lót, Tên, Ngày sinh.
    * Cột động môn học: ĐÚNG danh sách các môn trong Khung chương trình (header xoay đứng 90 độ hoặc compact).
    * Cột cố định bên phải (Sticky): Số môn, ĐTB, ĐTB bằng chữ, Xếp loại.
  - [ ] Toolbar: Nút Thêm học viên, Sửa thông tin, Nhập điểm nhanh (hỗ trợ phím Tab/Enter), Tìm kiếm học viên theo tên/mã số.
  - [ ] Modal Quản trị: Thêm/Sửa học viên, Nhập điểm theo từng học viên.
- [ ] Import & Export Excel cho Bảng điểm:
  - [ ] `ExcelGradeImporter`: Đọc trực tiếp file `BANG DIEM TONG K56.01.xls` để nạp danh sách học viên và điểm toàn bộ các môn.
  - [ ] `ExcelGradeExporter`: Xuất file Excel bảng điểm tổng hợp (.xlsx) định dạng chuẩn, đầy đủ công thức và styling.
- [ ] Test tự động: Test chuyển đổi số sang chữ (7.81, 8.34, 7.26...), test tính ĐTB và xếp loại, test import file `BANG DIEM TONG K56.01.xls`.

**Xong khi:** Mở `/Grades` hiển thị đầy đủ danh sách học viên K56, ma trận điểm đúng số môn khung CT, ĐTB, ĐTB bằng chữ và Xếp loại khớp 100% với file mẫu; nhập điểm hoặc import Excel thành công; test xanh.

## Giai đoạn 10 — Tinh chỉnh Toolbar điều hướng, Tối ưu Responsive & Kiểm thử toàn diện
- [ ] Cập nhật Navbar/Toolbar chung của hệ thống:
  - [ ] Tách rõ 2 menu chính: **📅 Kế hoạch giảng dạy** (`/`) và **📊 Bảng điểm tổng hợp** (`/Grades`).
  - [ ] Giữ nguyên các menu hỗ trợ: **🏖 Ngày nghỉ**, **📄 Biểu mẫu & Xuất Word**, **⚙ Quản lý Mẫu biểu**, **Đăng nhập/Đăng xuất**.
- [ ] Tối ưu Responsive trên Mobile và Desktop:
  - [ ] Trang Bảng điểm: Hỗ trợ cuộn ngang mượt mà với sticky columns, chế độ xem Thẻ học viên trên màn hình nhỏ.
  - [ ] Trang Kế hoạch: Badge trạng thái rõ ràng, không bị tràn màn hình.
- [ ] Kiểm thử hồi quy toàn diện:
  - [ ] Chạy `dotnet build -warnaserror` đảm bảo 0 warning, 0 error.
  - [ ] Chạy toàn bộ test suite xUnit đảm bảo 100% tests xanh (cả test cũ và test mới).
- [ ] Cập nhật tài liệu `README.md` hướng dẫn sử dụng cả 2 phân hệ.

## Giai đoạn 11 — Exporter bổ sung (tuỳ chọn, chỉ làm khi 0–10 xong và được yêu cầu)
- [ ] `xlsx-plan` · [ ] `csv-plan` · [ ] `ics-calendar` (SPEC 7.2). Mỗi loại chỉ thêm 1 class + đăng ký DI + 1 test.

---

## Checklist nghiệm thu cuối
- [x] `dotnet test` xanh, gồm golden test 19 môn.
- [x] DB trống → khởi động lần đầu tự có dữ liệu từ Excel mẫu; `/` hiển thị 19 môn, số tiết, bảng điểm.
- [x] Quản trị sửa môn/ngày nghỉ/điểm → trang `/` và file Word đều đổi theo; khách không sửa được.
- [x] `/Export` tải `.docx` biểu mẫu mở Word không lỗi.
- [x] `/Import` thay toàn bộ dữ liệu an toàn (transaction).
- [x] Không có bí mật (mật khẩu, chuỗi kết nối) nằm trong git. Không sửa file mẫu trong `App_Data/Templates`.

## Quyết định (agent ghi vào đây nếu phải tự chọn)
- Giai đoạn 6 & 7: Hoàn thành toàn diện IExporter, DocxBieuMauExporter, ITemplateService, TemplateService, các trang /Export và /Templates, script migration idempotent docs/migration_script.sql, tài liệu README.md và 100% test xanh (67/67 tests).
- Template tùy biến khi người dùng upload qua web được lưu trữ an toàn tại `App_Data/Templates/Custom_BieuMau_KeHoachGiangDay.docx`, giữ nguyên bản gốc `BieuMau_KeHoachGiangDay.docx` để có thể khôi phục bất cứ lúc nào.
- Thông số ExtraTasks (Khai giảng, Thi tốt nghiệp, Bế giảng, Tiết tự học, Tiết ôn thi) được lưu vào bảng `AppSetting` trong database với độ ưu tiên cao hơn `appsettings.json`.
- [Agent 001] Nâng cấp Lịch thi & Khai giảng linh hoạt: Thực thể `Subject` bổ sung `ExamMode` ("Immediately", "Delayed", "Custom") và `CustomExamSlot`. Migration `AddFlexibleExamAndOpeningDay` được thêm an toàn. Khai giảng hỗ trợ nhiều ngày (`NextWorkday(openingHoliday.To)`) và chọn buổi bắt đầu `firstSessionHalf`. Cơ chế hoãn/dồn thi (`Delayed`) xếp thi tuần tự khi gặp môn `Immediately` hoặc môn cuối, môn tiếp theo vào học luôn; 100% test xanh (72/72 tests bao gồm Golden 19 môn).
- [Agent 002] Tích hợp Giao diện Web & Exporter: PlanService đọc cấu hình `Plan:FirstSessionHalf` từ AppSetting; Trang Holidays cho phép khoảng ngày khai giảng kéo dài nhiều ngày (`To >= From`) và chọn buổi bắt đầu học môn 1 (Sáng/Chiều); Trang Môn học (Index) hỗ trợ chọn Chế độ thi (`Immediately`, `Delayed`, `Custom`) kèm `CustomExamSlot` trong Modal Thêm/Sửa, gắn badge trực quan trên DataGrid và Mobile Cards; DocxBieuMauExporter điền chính xác `item.Exam.ToString()` và hỗ trợ khoảng ngày khai giảng; 100% test xanh (75/75 tests).
