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
- [ ] `ScheduleCalculator.Calculate(subjects, holidays)` hàm thuần (SPEC mục 4): bung khoảng nghỉ, `NextWorkday`, `Advance`, tính từng môn. Nhận các `Subject`/`Holiday` đối tượng thường (không cần database).
- [ ] `ExcelPlanReader` (SPEC 3.1–3.2, 9): đọc `Khung CT` A–C và `Ngay nghi` A–D, tìm khai giảng, gom `Warnings`; mở bằng stream.
- [ ] Test:
  - [ ] Đọc đúng **19 môn**, tổng **895 tiết**, **6 khoảng nghỉ** (Excel mẫu), bung ra đúng **18 ngày**.
  - [ ] **Golden test**: 19 dòng Bắt đầu/Kết thúc/Thi khớp bảng SPEC mục 4.
  - [ ] Test biên: môn bắt đầu buổi chiều; kỳ nghỉ Tết 12–22/2 bị nhảy qua; số tiết không chia hết 5; tiết trống; khoảng nghỉ chồng nhau; thiếu khai giảng.

**Xong khi:** test xanh. **Không sang giai đoạn 2 nếu golden test chưa xanh.**

## Giai đoạn 2 — Database (SPEC mục 5, 3.3)
- [ ] Entity `Subject`, `Holiday`, `AppSetting`; `PlanDbContext`; dùng `UseSqlServer`; index theo SPEC.
- [ ] Tạo migration đầu tiên; `Database:AutoMigrate` chạy `Migrate()` lúc khởi động.
- [ ] `PlanRepository`: CRUD môn/ngày nghỉ, đổi thứ tự, đánh lại `SortOrder` (1 transaction); luật đúng 1 ngày khai giảng; CRUD cấu hình `AppSetting`.
- [ ] `PlanImporter`: Excel → database trong 1 transaction; tuỳ chọn giữ điểm/dự phòng/ghi chú theo tên môn.
- [ ] Seed lần đầu khi hai bảng trống và có file `Plan:SeedExcelPath`.
- [ ] `PlanService`: lấy dữ liệu → `ScheduleCalculator` → `ScheduleResult`.
- [ ] Test (SQL Server test, tạo database tạm cho mỗi lần chạy rồi xoá): import Excel mẫu rồi `PlanService` cho ra đúng golden 19 môn; thêm/xoá/đổi chỗ môn thì `SortOrder` liên tục và lịch đổi theo; import lần 2 giữ điểm theo tên; import lỗi giữa chừng không làm đổi dữ liệu cũ; khai giảng luôn đúng 1; lưu/đọc `AppSetting`.

**Xong khi:** test xanh; chạy app lần đầu với DB trống tự có 19 môn.

## Giai đoạn 3 — Đăng nhập quản trị (SPEC mục 12)
- [ ] Cookie auth + policy `Admin`; trang `/Login`, nút Đăng xuất; so mật khẩu bằng `FixedTimeEquals`, sai thì chờ 1 giây.
- [ ] `Admin:Password` trống ⇒ chế độ chỉ xem + cảnh báo.
- [ ] Anti-forgery cho mọi form POST; mọi handler ghi gắn `[Authorize(Policy = "Admin")]`.
- [ ] Test: chưa đăng nhập gọi handler ghi bị từ chối; đăng nhập đúng/sai mật khẩu.

**Xong khi:** khách không sửa được dữ liệu dù tự gửi request tay.

## Giai đoạn 4 — Trang Môn học & Bảng điểm (`/`) (SPEC mục 6)
- [ ] Bảng 19 môn: Stt, Môn, Số tiết, Số buổi, Bắt đầu, Kết thúc, Thi, Điểm, Dự phòng, Ghi chú.
- [ ] Quản trị: sửa ô tại chỗ, thêm môn, xoá (xác nhận), ▲▼ đổi thứ tự; kiểm tra hợp lệ ở server (điểm 0–10…).
- [ ] Hàng tổng: tổng tiết, điểm TB cộng, điểm TB theo số tiết (chỉ môn có điểm).
- [ ] Hiển thị `Warnings`; màn hình "chưa có dữ liệu"; trang lỗi thân thiện khi mất kết nối DB.
- [ ] Test: tính điểm TB; validate.

**Xong khi:** mở `/` thấy đủ 19 môn ngày đúng golden; quản trị sửa điểm/số tiết, F5 vẫn còn và lịch đổi theo.

## Giai đoạn 5 — Ngày nghỉ & Nhập từ Excel (SPEC 6.1, 6.2)
- [ ] `/Holidays`: bảng + thêm/sửa/xoá (quản trị), kiểm tra `Đến ≥ Từ`, đúng 1 khai giảng.
- [ ] `/Import`: upload `.xlsx` ≤ 5 MB (xử lý trong bộ nhớ) → xem trước → xác nhận thay thế toàn bộ.
- [ ] Test: đổi ngày nghỉ thì ngày bắt đầu/thi các môn sau dịch theo; import lỗi báo đúng thông điệp.

**Xong khi:** thêm 1 ngày nghỉ mới → lịch trang `/` đổi đúng; import lại Excel mẫu → về đúng golden.

## Giai đoạn 6 — Xuất file: khung + biểu mẫu Word & Quản lý Template trên Web (SPEC mục 7, 7.3)
- [ ] `IExporter` + đăng ký DI `IEnumerable<IExporter>`; trang `/Export` liệt kê; endpoint `/Export/Download?id=` (khách tải được, sinh trong bộ nhớ).
- [ ] `DocxBieuMauExporter`:
  - [ ] Mở bản copy của file mẫu (lấy file template tùy biến nếu có, fallback file mẫu gốc) trong `MemoryStream`.
  - [ ] Tìm bảng chính theo chữ "Tên môn học/Nhiệm vụ"; giữ 2 hàng tiêu đề.
  - [ ] Lấy hàng môn đầu làm hàng mẫu, xoá hàng dữ liệu cũ, clone theo thứ tự: Khai giảng → các môn → Thi tốt nghiệp → Bế giảng.
  - [ ] Ánh xạ cột (Tiết giảng = Số tiết − 10, Tự nghiên cứu 10, Ôn thi/thi 5, dự phòng, ghi chú lấy từ `Subject`).
  - [ ] Đọc ExtraTasks (ưu tiên từ `AppSetting`, fallback `appsettings.json`); cảnh báo nếu rơi vào ngày nghỉ.
- [ ] Trang Quản lý Template trực tiếp trên Web (`/Templates`, yêu cầu quyền Admin):
  - [ ] Xem trạng thái template đang kích hoạt, tải template hiện tại về máy (.docx).
  - [ ] Upload file template `.docx` mới trực tiếp qua web; kiểm tra hợp lệ cấu trúc OpenXml (bảng 10 cột, header).
  - [ ] Nút khôi phục file mẫu mặc định ban đầu.
  - [ ] Form chỉnh sửa thông số ExtraTasks (Khai giảng, Thi tốt nghiệp, Bế giảng, Tiết tự học, Tiết ôn thi) lưu vào `AppSetting`.
- [ ] Test: mở lại bằng OpenXml SDK không lỗi; đúng `19 + 3` hàng dữ liệu; hàng "Tên môn học 01" là `50 | 10 | 5 | | S 11/9/2025 | C 18/9/2025 | S 19/9/2025`; dự phòng/ghi chú nhập tay hiện đúng; tiêu đề "KẾ HOẠCH GIẢNG DẠY VÀ HỌC TẬP" còn; ô gộp tiêu đề còn (đếm `gridSpan`/`vMerge`); upload template không hợp lệ bị từ chối; cập nhật ExtraTasks đổi kết quả xuất file.
- [ ] Nếu có LibreOffice: `soffice --headless --convert-to pdf`, xem ảnh trang 1 để kiểm bố cục.

**Xong khi:** tải được `.docx` mở bằng Word không báo lỗi, 19 môn trùng biểu mẫu mẫu; quản lý và cập nhật template/ExtraTasks trực tiếp trên web hoạt động trơn tru.

## Giai đoạn 7 — Hoàn thiện & triển khai (SPEC mục 10, 13)
- [ ] Rà soát toàn bộ thông báo (tiếng Việt có dấu, UTF-8).
- [ ] Thử tay các trường hợp biên SPEC mục 9 (tắt DB, DB trống, nhập điểm 11, import sai file, gọi POST khi chưa đăng nhập).
- [ ] Cấu hình publish: template `.docx` có trong output (`CopyToOutputDirectory`); HTTPS redirect + HSTS ở Production.
- [ ] Xuất script migration `--idempotent` đưa vào `docs/` (để chạy tay nếu host không cho tự migrate).
- [ ] `README.md`: yêu cầu .NET 8, cách chạy dev (LocalDB hoặc Docker SQL Server), biến môi trường (`Admin__Password`, `ConnectionStrings__Default`), các bước deploy lên host free.
- [ ] `dotnet build -warnaserror` sạch; `dotnet test` xanh; không còn TODO/hard-code "19".

## Giai đoạn 8 — Exporter bổ sung (tuỳ chọn, chỉ làm khi 0–7 xong và được yêu cầu)
- [ ] `xlsx-plan` · [ ] `csv-plan` · [ ] `ics-calendar` (SPEC 7.2). Mỗi loại chỉ thêm 1 class + đăng ký DI + 1 test.

---

## Checklist nghiệm thu cuối
- [ ] `dotnet test` xanh, gồm golden test 19 môn.
- [ ] DB trống → khởi động lần đầu tự có dữ liệu từ Excel mẫu; `/` hiển thị 19 môn, số tiết, bảng điểm.
- [ ] Quản trị sửa môn/ngày nghỉ/điểm → trang `/` và file Word đều đổi theo; khách không sửa được.
- [ ] `/Export` tải `.docx` biểu mẫu mở Word không lỗi.
- [ ] `/Import` thay toàn bộ dữ liệu an toàn (transaction).
- [ ] Không có bí mật (mật khẩu, chuỗi kết nối) nằm trong git. Không sửa file mẫu trong `App_Data/Templates`.

## Quyết định (agent ghi vào đây nếu phải tự chọn)
-
