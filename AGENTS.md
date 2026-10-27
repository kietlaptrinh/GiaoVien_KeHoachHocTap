# AGENTS.md — Quy tắc cho AI Agent (Antigravity)

Bạn đang xây **web Kế hoạch học tập** bằng ASP.NET Core, dữ liệu lưu trong database, deploy được lên hosting free.

## Đọc theo thứ tự này trước khi viết code
1. `SPEC.md` — đặc tả đầy đủ, **nguồn sự thật**. Có bảng kết quả chuẩn (golden) cho 19 môn.
2. `PLAN.md` — các giai đoạn 0 → 8 kèm checkbox.
3. File này — quy tắc làm việc.

Thứ tự ưu tiên khi mâu thuẫn: `SPEC.md` > `PLAN.md` > `AGENTS.md`. Nếu SPEC thiếu hoặc mơ hồ: chọn phương án đơn giản nhất, ghi 1 dòng vào mục "Quyết định" cuối `PLAN.md`, rồi làm tiếp. Không dừng lại hỏi vì chuyện nhỏ.

## Cách làm việc
- Làm **từng giai đoạn theo PLAN**, không nhảy cóc. Xong giai đoạn: tick `[x]` trong `PLAN.md`, chạy `dotnet build -warnaserror` và `dotnet test`, sửa đến khi xanh rồi mới sang giai đoạn sau.
- **Viết test trước cho phần lõi** (thuật toán xếp lịch). Golden test (19 môn) phải xanh trước khi đụng tới database và giao diện.
- Mỗi giai đoạn là 1 commit: `feat(phase-N): <mô tả ngắn>`.
- Chỉ làm những gì có trong SPEC. Không thêm tính năng, thư viện, trang "cho đẹp".

## Stack (cố định)
ASP.NET Core Razor Pages · .NET 8 · EF Core + **SQL Server** (chỉ dùng SQL Server) · ClosedXML (đọc Excel) · DocumentFormat.OpenXml (ghi Word) · xUnit · Bootstrap 5 cục bộ. **Không** SPA framework, **không** CDN/mạng ngoài, **không** ORM khác.

## Lệnh
```
dotnet restore
dotnet build -warnaserror
dotnet test
dotnet run --project src/KeHoachHocTap.Web
dotnet ef migrations add <Ten> --project src/KeHoachHocTap.Web
```

## Quy ước code
- Tên lớp/biến/commit: tiếng Anh. Chuỗi hiển thị cho người dùng: **tiếng Việt có dấu**. Mọi file UTF-8.
- Ngày dùng `DateOnly`. Định dạng buổi: `S 11/9/2025` (`d/M/yyyy`, không đệm số 0) — viết **một** hàm định dạng dùng chung.
- Logic nghiệp vụ nằm trong `Domain/` và `Services/`, **không** để trong `.cshtml`/PageModel. `ScheduleCalculator` là hàm thuần, không đụng database hay file.
- Trang chỉ gọi service. `PlanService` là nguồn dữ liệu chung cho Index, Holidays, Export.
- DI + interface cho calculator, reader, repository, importer, exporter. Cấu hình qua `IOptions`, không hard-code đường dẫn/chuỗi kết nối.
- Truy vấn EF dùng `async`; tránh SQL thô nếu không cần.
- Không hard-code số 19, tên "Tên môn học 01", hay các ngày cụ thể trong code chạy (chỉ trong test và `appsettings.json`).
- Bắt lỗi ở biên (database, upload, xuất file) và trả thông báo tiếng Việt dễ hiểu; không lộ stack trace hay chuỗi kết nối cho người dùng.

## Quy tắc nghiệp vụ dễ sai (đọc kỹ)
1. **Khi import Excel, không đọc cột D–F của sheet `Khung CT`.** Đó là công thức Excel. Chỉ đọc Stt, Tên môn, Số tiết; lịch tự tính bằng C#. Cache D–F chỉ dùng đối chiếu trong test.
2. **Không dùng cột `NgayNghi_List`.** Lưu khoảng `Từ ngày → Đến ngày`, tính lúc cần bằng cách bung ra từng ngày.
3. 1 buổi = 5 tiết, 1 ngày 2 buổi (S rồi C), chỉ thứ 2–6, bỏ ngày nghỉ. Môn sau bắt đầu **ngay buổi kế tiếp** sau buổi thi môn trước — có thể là buổi chiều.
4. Buổi thi = 1 buổi, ngay sau buổi học cuối.
5. **Không lưu kết quả lịch** vào database; luôn tính lại từ `Subject` + `Holiday`.
6. Luôn có đúng 1 ngày khai giảng; `SortOrder` của môn luôn liên tục 1..n.
7. Trong file Word: `Tiết giảng = Số tiết − 10`, `Tự nghiên cứu = 10`, `Ôn thi/thi = 5`. Dự phòng và Ghi chú nhập tay, **không** làm đổi lịch.
8. File Word sinh bằng cách **clone file mẫu** (giữ font, gộp ô, lề). Không dựng lại bảng từ đầu. File mẫu gốc `App_Data/Templates/BieuMau_KeHoachGiangDay.docx` luôn được bảo vệ làm bản fallback mặc định; khi quản trị viên upload template tùy biến qua web, hệ thống kiểm tra tính hợp lệ cấu trúc OpenXml trước khi áp dụng.
9. Import Excel và mọi thao tác nhiều bước ghi database phải nằm trong **1 transaction**.
10. **Giao diện tuân thủ phong cách Desktop cổ điển (Classic Desktop App Style):** Nền Trắng/Xám nhạt (#FFFFFF, #F8F9FA), chữ và viền Đen/Xám đậm (#212529, #333333, #DEE2E6 sắc nét 1px), điểm nhấn Xanh dương nhạt (#EBF3FC nền header/hover, #D0E2FF highlight, #0D6EFD/#1D4ED8 nút chính). Thiết kế theo mô thức Toolbar thanh công cụ, DataGrid chia ô rõ ràng và StatusBar chân trang như các ứng dụng desktop quản lý đào tạo cổ điển.

## Bảo mật (host free có địa chỉ công khai)
- Mọi thao tác ghi yêu cầu policy `Admin` **ở server** + anti-forgery token; không chỉ ẩn nút.
- Mật khẩu quản trị và chuỗi kết nối chỉ lấy từ biến môi trường hoặc `appsettings.Production.json` (đã `.gitignore`). **Không bao giờ commit bí mật.**
- Không ghi log mật khẩu hay chuỗi kết nối.
- Ứng dụng không ghi file lúc chạy ngoại trừ template tùy biến được xác thực (Word và upload xử lý trong bộ nhớ).

## Việc KHÔNG được làm
- Không sửa, xoá, ghi đè file mẫu gốc mặc định `App_Data/Templates/BieuMau_KeHoachGiangDay.docx` (template tùy biến upload qua web được lưu riêng, luôn có nút khôi phục mẫu gốc).
- Không đổi kết quả golden để cho test qua. Test đỏ ⇒ sửa code, không sửa kỳ vọng. Chỉ sửa golden nếu SPEC đổi.
- Không dùng `Thread.Sleep`, `async void`, `catch {}` nuốt lỗi im lặng.
- Không sửa migration đã tạo; cần đổi schema thì thêm migration mới.
- Không thêm đăng ký tài khoản, nhiều người dùng, gửi email, telemetry.
- Không commit `bin/`, `obj/`, `*.db`, `appsettings.Production.json`.

## Định nghĩa "Xong" cho mỗi giai đoạn
- `dotnet build -warnaserror` không lỗi, không cảnh báo.
- `dotnet test` xanh.
- Chạy `dotnet run`, mở trang liên quan, thử đúng kịch bản trong mục "Xong khi" của PLAN.
- Checkbox trong PLAN đã tick; phần nào chưa làm được thì để trống và ghi lý do ở mục "Quyết định".

## Khi báo cáo cuối cùng
Nêu ngắn gọn: giai đoạn đã xong, kết quả `dotnet test`, giả định đã chọn, việc còn dở (nếu có). Không kể lại từng bước.
