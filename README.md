# Hệ Thống Quản Lý Kế Hoạch Học Tập & Giảng Dạy (Web KeHoachHocTap)

Ứng dụng web chuyên nghiệp quản lý kế hoạch học tập, xếp lịch giảng dạy tự động, quản lý điểm và xuất biểu mẫu Word chuẩn theo quy định đào tạo.

Hệ thống được phát triển trên nền tảng **ASP.NET Core Razor Pages (.NET 8)**, lưu trữ dữ liệu an toàn trong **SQL Server**, xử lý đọc file Excel bằng **ClosedXML** và sinh biểu mẫu Word bằng **DocumentFormat.OpenXml**.

---

## 1. Điểm Nổi Bật & Kiến Trúc Hệ Thống

- **Phong cách giao diện Desktop Cổ Điển (Classic Desktop App Style):**
  - **Bảng màu:** Nền Trắng / Xám nhạt (`#FFFFFF`, `#F8F9FA`), chữ và viền Đen / Xám đậm (`#212529`, `#DEE2E6` sắc nét 1px), điểm nhấn Xanh dương nhạt (`#EBF3FC`, `#D0E2FF`, `#1D4ED8` cho nút chính và trạng thái kích hoạt).
  - **Bố cục ứng dụng:** Thanh tiêu đề ứng dụng (TitleBar), thanh công cụ (Toolbar), lưới dữ liệu dạng bảng chia ô rõ ràng (DataGrid), và thanh trạng thái dưới cùng (StatusBar).
- **Thuật toán xếp lịch tự động thông minh:**
  - 1 buổi = 5 tiết, mỗi ngày 2 buổi (Sáng: S, Chiều: C), chỉ học từ Thứ 2 đến Thứ 6 (nghỉ Thứ 7, Chủ nhật).
  - Tự động bỏ qua các khoảng ngày nghỉ lễ, Tết và khai giảng.
  - Buổi thi hết môn diễn ra ngay buổi tiếp theo sau buổi học cuối cùng. Môn học sau bắt đầu ngay buổi kế tiếp sau buổi thi của môn trước (có thể bắt đầu vào buổi chiều).
  - Định dạng buổi chuẩn: `S 11/9/2025` (`d/M/yyyy`, không đệm số 0 thừa).
- **Bảo mật & Phân quyền:**
  - Khách vãng lai: Toàn quyền tra cứu kế hoạch học tập, xem ngày nghỉ và tải biểu mẫu Word.
  - Quản trị viên: Đăng nhập bằng mật khẩu duy nhất bảo mật bằng `CryptographicOperations.FixedTimeEquals` và Cookie Authentication (hết hạn 8 giờ, `HttpOnly`, `SameSite=Lax`). Mọi thao tác ghi dữ liệu (thêm/sửa/xóa môn học, sửa điểm, đổi ngày nghỉ, import Excel, upload template) đều được bảo vệ nghiêm ngặt ở server bằng policy `Admin` và Anti-forgery token.
  - Khi mật khẩu quản trị để trống: Hệ thống tự động chuyển sang chế độ chỉ xem an toàn.
- **Xuất biểu mẫu Word (.docx) chuẩn:**
  - Nhân bản trực tiếp từ template mẫu OpenXml, bảo toàn 100% font chữ Calibri, cỡ chữ, lề văn bản A4 dọc và các ô gộp tiêu đề (`gridSpan`, `vMerge`).
  - Tự động điền đầy đủ dòng Khai giảng, danh sách môn học, Thi tốt nghiệp và Bế giảng.
  - Sinh file hoàn toàn trong bộ nhớ (`MemoryStream`), không ghi file tạm lên đĩa máy chủ.
- **Quản lý Template & Nhiệm vụ trực tiếp trên Web (`/Templates`):**
  - Quản trị viên có thể tải file mẫu hiện tại về máy để chỉnh sửa trên Microsoft Word.
  - Upload file template `.docx` tùy biến mới (dung lượng tối đa 10 MB) với cơ chế xác thực cấu trúc OpenXml nghiêm ngặt (kiểm tra bảng, 2 hàng tiêu đề, 10 cột dữ liệu).
  - Nút khôi phục mẫu gốc mặc định nhanh chóng.
  - Form chỉnh sửa thông số nhiệm vụ đặc biệt (Thi tốt nghiệp, Bế giảng, Tiết tự học, Tiết ôn thi) lưu trữ linh hoạt trong database `AppSetting`.

---

## 2. Công Nghệ Sử Dụng (Tech Stack)

| Thành phần | Công nghệ / Thư viện | Ghi chú |
|---|---|---|
| **Khung ứng dụng** | ASP.NET Core Razor Pages (.NET 8) | Độc lập, không dùng SPA framework |
| **Cơ sở dữ liệu** | Microsoft SQL Server (LocalDB / Docker / Server) | EF Core 8.0, Code-First Migrations |
| **Đọc Excel** | ClosedXML 0.105.1 | Đọc sheet `Khung CT` và `Ngay nghi` qua Stream |
| **Xử lý Word** | DocumentFormat.OpenXml 3.5.1 | Xử lý tài liệu OpenXml SDK chuẩn |
| **Giao diện** | HTML5, CSS3, Bootstrap 5 (cục bộ) | Không phụ thuộc CDN hay mạng ngoài |
| **Kiểm thử tự động** | xUnit, Microsoft.EntityFrameworkCore.SqlServer | Bộ test bao phủ 100% thuật toán & nghiệp vụ |

---

## 3. Cấu Trúc Thư Mục Dự Án

```
GiaoVien_KeHoachHocTap/
├── AGENTS.md                         # Quy tắc làm việc cho AI Agent
├── PLAN.md                           # Kế hoạch phát triển theo từng giai đoạn
├── SPEC.md                           # Đặc tả nghiệp vụ chuẩn (nguồn sự thật)
├── README.md                         # Tài liệu hướng dẫn sử dụng và triển khai
├── KeHoachHocTap.sln                 # Solution Visual Studio / .NET Core
├── docs/
│   └── migration_script.sql          # Script SQL idempotent khởi tạo CSDL
├── src/
│   └── KeHoachHocTap.Web/            # Project Web ASP.NET Core Razor Pages
│       ├── App_Data/
│       │   ├── ke_hoach_hoc_tap.xlsx # Dữ liệu Excel kế hoạch mẫu
│       │   └── Templates/
│       │       └── BieuMau_KeHoachGiangDay.docx # Template Word mặc định
│       ├── Data/                     # Subject, Holiday, AppSetting, PlanDbContext
│       ├── Domain/                   # Slot, Half, ScheduledSubject, ScheduleResult, GradeCalculator
│       ├── Export/                   # IExporter, ExportOptions, DocxBieuMauExporter
│       ├── Pages/                    # Index, Holidays, Import, Export, Templates, Login, Error
│       ├── Services/                 # ScheduleCalculator, ExcelPlanReader, PlanRepository, PlanImporter, PlanService, TemplateService, AdminAuthService
│       ├── wwwroot/                  # css/site.css, js/site.js, lib/bootstrap
│       ├── Program.cs                # Điểm khởi chạy và cấu hình dịch vụ DI
│       └── appsettings.json          # Cấu hình chuỗi kết nối và thông số mặc định
└── tests/
    └── KeHoachHocTap.Tests/          # Project kiểm thử tự động xUnit
        ├── TestData/                 # Bản sao dữ liệu test Excel và Word mẫu
        ├── ScheduleCalculatorTests.cs# Kiểm thử thuật toán xếp lịch (Golden Test)
        ├── ExcelPlanReaderTests.cs   # Kiểm thử đọc dữ liệu Excel
        ├── PlanRepositoryTests.cs    # Kiểm thử thao tác cơ sở dữ liệu
        ├── PlanImporterTests.cs      # Kiểm thử nạp dữ liệu từ Excel vào CSDL
        ├── PlanServiceTests.cs       # Kiểm thử dịch vụ tích hợp
        ├── GradeCalculationTests.cs  # Kiểm thử tính điểm trung bình
        ├── HolidayValidationTests.cs # Kiểm thử ràng buộc ngày nghỉ & khai giảng
        ├── AuthTests.cs              # Kiểm thử xác thực & phân quyền quản trị
        └── DocxExporterTests.cs      # Kiểm thử xuất Word & quản lý template
```

---

## 4. Hướng Dẫn Chạy Môi Trường Phát Triển (Development)

### 4.1. Yêu cầu cài đặt
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (chọn 1 trong 2 cách dưới đây):
  - **Cách A: SQL Server LocalDB trên Windows** (đi kèm Visual Studio): `Server=(localdb)\MSSQLLocalDB`
  - **Cách B: SQL Server trên Docker / Container**:
    ```bash
    docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=123" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
    ```

### 4.2. Cấu hình chuỗi kết nối
Mở file `src/KeHoachHocTap.Web/appsettings.json` (hoặc tạo `appsettings.Development.json`):
```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=KeHoachHocTap;User Id=sa;Password=123;TrustServerCertificate=True;",
    "Test": "Server=localhost;Database=KeHoachHocTap_Test;User Id=sa;Password=123;TrustServerCertificate=True;"
  },
  "Database": {
    "AutoMigrate": true
  },
  "Admin": {
    "Password": "matkhauquantri"
  }
}
```

### 4.3. Chạy ứng dụng
Mở terminal tại thư mục gốc của dự án:
```bash
# Phục hồi các gói phụ thuộc
dotnet restore

# Biên dịch kiểm tra cảnh báo
dotnet build -warnaserror

# Khởi chạy ứng dụng Web
dotnet run --project src/KeHoachHocTap.Web
```

Mở trình duyệt truy cập: `https://localhost:5001` hoặc `http://localhost:5000`.

Khi khởi chạy lần đầu tiên với cơ sở dữ liệu trống, hệ thống sẽ:
1. Tự động áp dụng EF Core Migrations (`AutoMigrate = true`).
2. Tự động nạp dữ liệu ban đầu từ `App_Data/ke_hoach_hoc_tap.xlsx` (19 môn học và 6 khoảng ngày nghỉ).
3. Sẵn sàng sử dụng ngay mà không cần cấu hình thủ công.

---

## 5. Hướng Dẫn Sử Dụng Các Tính Năng

### 5.1. Trang Môn học & Bảng điểm (`/`)
- Hiển thị danh sách 19 môn học theo đúng thứ tự giảng dạy, bao gồm:
  - Tên môn, Số tiết, Số buổi học (`ceil(tiết/5)`).
  - Buổi bắt đầu, Buổi kết thúc, Buổi thi hết môn.
  - Điểm tổng kết (thang điểm 10, hiển thị 1 chữ số thập phân).
  - Số tiết dự phòng và Ghi chú.
- **Linh hoạt chế độ thi hết môn:**
  - *Thi ngay sau khi kết thúc môn* (Mặc định): Học xong buổi cuối $\rightarrow$ Buổi tiếp theo thi $\rightarrow$ Môn sau học ngay sau buổi thi.
  - *Hoãn thi (Dồn thi)*: Môn sau vào học luôn ngay buổi tiếp theo sau buổi học cuối; đợt thi được dồn sau khi môn kế tiếp kết thúc (thi lần lượt 2 môn rồi môn thứ 3 học tiếp ngay).
  - *Chỉ định buổi thi tùy chỉnh*: Nhập slot thi cụ thể (ví dụ: `S 20/10/2025`).
- **Nút bật/tắt cột Điểm:** Mặc định ẩn cột Điểm để bảng gọn gàng tập trung vào kế hoạch giảng dạy; khi cần nhập điểm tổng kết có thể bấm nút **"👁️ Hiện cột điểm"** để xem và nhập điểm, tự động tính Điểm TB cộng & Điểm TB gia quyền.
- **Thống kê tổng hợp:** Dòng chân trang tự động tính Tổng số tiết (895 tiết), Điểm trung bình cộng và Điểm trung bình gia quyền theo số tiết (chỉ tính cho các môn đã có điểm).
- **Thao tác quản trị viên:**
  - Nhập điểm trực tiếp tại ô; chỉnh sửa số tiết, dự phòng, ghi chú, chế độ thi.
  - Di chuyển thứ tự môn học (▲ lên / ▼ xuống) — lịch học toàn khóa học tự động tính lại ngay lập tức.
  - Thêm môn học mới hoặc xóa môn học (có hộp thoại xác nhận).

### 5.2. Trang Ngày nghỉ & Khai giảng (`/Holidays`)
- Quản lý các sự kiện nghỉ lễ, nghỉ Tết và sự kiện khai giảng.
- **Khai giảng linh hoạt:**
  - Cho phép ngày Khai giảng kéo dài nhiều ngày (`Từ ngày → Đến ngày`), toàn bộ khoảng thời gian này được tính là ngày lễ/nghỉ khóa học.
  - **Linh hoạt buổi bắt đầu môn 1:** Tùy chọn bắt đầu vào buổi **Sáng (S)** hoặc buổi **Chiều (C)** sau đợt khai giảng.
- Ràng buộc toàn vẹn dữ liệu: `Đến ngày >= Từ ngày`.
- Hệ thống luôn bảo đảm có duy nhất đúng 1 ngày khai giảng khóa học. Khi chuyển cờ khai giảng sang ngày khác, cờ ở ngày cũ sẽ tự động bị hủy.
- Mỗi khi thay đổi ngày nghỉ, lịch của tất cả các môn học phía sau tự động dịch chuyển tương ứng.

### 5.3. Trang Nhập kế hoạch từ Excel (`/Import`)
- Chỉ dành cho Quản trị viên.
- Chọn file `.xlsx` (dung lượng tối đa 5 MB) chứa 2 sheet `Khung CT` và `Ngay nghi`.
- **Chế độ xem trước (Preview):** Hiển thị số lượng môn, khoảng nghỉ, ngày khai giảng và các cảnh báo trước khi lưu.
- Tùy chọn: **"Giữ lại Điểm, Dự phòng và Ghi chú nếu trùng tên môn"** (mặc định bật).
- Thao tác nạp dữ liệu thực hiện hoàn toàn trong **1 transaction** duy nhất, bảo đảm an toàn tuyệt đối không làm mất dữ liệu cũ nếu phát sinh lỗi giữa chừng.

### 5.4. Trang Biểu mẫu Word & Xuất file (`/Export`)
- Khách vãng lai và giáo viên đều có thể tải xuống.
- Nhấn nút **"Tải xuống (.docx)"** để tải về máy file kế hoạch hoàn chỉnh có tên `KeHoachGiangDayHocTap_yyyyMMdd.docx`.
- File được sinh tức thì trong bộ nhớ từ template mẫu, chứa đầy đủ:
  - Dòng 1: Khai giảng (10 tiết, từ Sáng đến Chiều ngày khai giảng).
  - Dòng 2 đến 20: 19 môn học với phân rã tiết giảng (`Số tiết - 10`), tiết tự học (10 tiết), tiết ôn thi (5 tiết).
  - Dòng 21: Thi tốt nghiệp (15 tiết).
  - Dòng 22: Bế giảng (5 tiết).
- Hiển thị cảnh báo nếu ngày của Thi tốt nghiệp / Bế giảng rơi vào ngày nghỉ hoặc cuối tuần.

### 5.5. Trang Quản lý Mẫu biểu Word (`/Templates`)
- Dành riêng cho Quản trị viên (yêu cầu đăng nhập).
- **Xem thông tin template:** Tên file, dung lượng, thời gian cập nhật cuối và trạng thái đang áp dụng (Mẫu tùy biến của trường hay Mẫu gốc mặc định).
- **Tải file mẫu hiện tại:** Nút tải file `.docx` về máy để chỉnh sửa quốc hiệu, tên đơn vị, tiêu đề hoặc định dạng theo nhu cầu của nhà trường.
- **Upload template tùy biến mới:** Tải lên file `.docx` (tối đa 10 MB). Hệ thống tự động kiểm tra nghiêm ngặt cấu trúc OpenXml (chứa ô "Tên môn học/Nhiệm vụ", có 2 hàng tiêu đề, đủ 10 cột dữ liệu). Nếu hợp lệ, template mới sẽ được kích hoạt ngay lập tức.
- **Khôi phục mẫu mặc định:** Nút xóa mẫu tùy biến và quay về sử dụng file mẫu gốc ban đầu trong `App_Data/Templates/BieuMau_KeHoachGiangDay.docx`.
- **Cấu hình thông số nhiệm vụ đặc biệt:** Chỉnh sửa trực tiếp trên giao diện web số tiết và ngày bắt đầu/kết thúc của Thi tốt nghiệp, Bế giảng, Tiết tự nghiên cứu, Tiết ôn thi. Các giá trị này được lưu trữ trong cơ sở dữ liệu và tự động phản ánh vào file Word khi xuất.

---

## 6. Hướng Dẫn Triển Khai Lên Hosting Miễn Phí (MonsterASP.NET, Docker, IIS)

### 6.1. Nguyên tắc triển khai môi trường Production
- **Không ghi file lúc chạy:** Ứng dụng không ghi file đĩa lúc chạy ngoại trừ template tùy biến được xác thực (Word và upload Excel xử lý hoàn toàn trong bộ nhớ `MemoryStream`).
- **Template Word gốc đi kèm:** File mẫu mặc định được thiết lập `CopyToOutputDirectory="PreserveNewest"` nên tự động có mặt trong thư mục publish.
- **Bảo mật biến môi trường:** Tuyệt đối không commit mật khẩu và chuỗi kết nối vào git. Cấu hình thông qua biến môi trường của host hoặc file `appsettings.Production.json` (đã nằm trong `.gitignore`).

### 6.2. Cấu hình biến môi trường trên Hosting
Khi cấu hình trên cPanel, Portal hoặc Docker của dịch vụ hosting, đặt các biến môi trường sau:

| Tên biến môi trường | Giá trị ví dụ | Ý nghĩa |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Chế độ môi trường vận hành |
| `ConnectionStrings__Default` | `Server=tcp:sql.freehost.com;Database=db_kehoach;User Id=user;Password=pass;TrustServerCertificate=True;` | Chuỗi kết nối SQL Server của hosting |
| `Admin__Password` | `MatKhauBaoMat123!` | Mật khẩu truy cập quyền quản trị |
| `Database__AutoMigrate` | `true` | Tự động migrate schema khi khởi động |

### 6.3. Khởi tạo cơ sở dữ liệu bằng SQL Script Idempotent
Nếu hosting không cho phép người dùng chạy `dotnet ef database update` hoặc `AutoMigrate` tự động từ mã nguồn, hãy dùng script SQL idempotent đã được chuẩn bị sẵn:

- File script: `docs/migration_script.sql`
- Script này có thể thực thi nhiều lần mà không gây lỗi (`IF NOT EXISTS`), an toàn cho mọi môi trường SQL Server.
- Bạn chỉ cần mở SQL Server Management Studio (SSMS), Azure Data Studio hoặc trang quản lý SQL Web của host, copy toàn bộ nội dung file `docs/migration_script.sql` và nhấn **Execute**.

### 6.4. Đóng gói và phát hành ứng dụng
Thực hiện lệnh publish:
```bash
dotnet publish src/KeHoachHocTap.Web/KeHoachHocTap.Web.csproj -c Release -o publish/
```
Sau đó nén toàn bộ thư mục `publish/` hoặc tải lên hosting thông qua FTP / Web Deploy / Git.

---

## 7. Báo Cáo Kiểm Thử Tự Động (Golden Test Results)

Hệ thống có bộ kiểm thử tự động toàn diện với **67+ test case** bao phủ toàn bộ các tình huống biên và nghiệp vụ cốt lõi:

```bash
dotnet test
```

### Kết quả kiểm định Golden Test 19 môn học (SPEC mục 4 & 7.1):

| STT | Tên môn học | Số tiết | Số buổi | Buổi bắt đầu | Buổi kết thúc | Thi hết môn | Tiết giảng Word |
|:---:|:---|:---:|:---:|:---:|:---:|:---:|:---:|
| - | **Khai giảng** | 10 | 2 | **S 10/9/2025** | **C 10/9/2025** | — | 10 |
| 1 | Tên môn học 01 | 60 | 12 | **S 11/9/2025** | **C 18/9/2025** | **S 19/9/2025** | 50 |
| 2 | Tên môn học 02 | 55 | 11 | **C 19/9/2025** | **C 26/9/2025** | **S 29/9/2025** | 45 |
| 3 | Tên môn học 03 | 55 | 11 | **C 29/9/2025** | **C 6/10/2025** | **S 7/10/2025** | 45 |
| 4 | Tên môn học 04 | 40 | 8 | **C 7/10/2025** | **S 13/10/2025** | **C 13/10/2025** | 30 |
| 5 | Tên môn học 05 | 60 | 12 | **S 14/10/2025** | **C 21/10/2025** | **S 22/10/2025** | 50 |
| 6 | Tên môn học 06 | 70 | 14 | **C 22/10/2025** | **S 31/10/2025** | **C 31/10/2025** | 60 |
| 7 | Tên môn học 07 | 45 | 9 | **S 3/11/2025** | **S 7/11/2025** | **C 7/11/2025** | 35 |
| 8 | Tên môn học 08 | 45 | 9 | **S 10/11/2025** | **S 14/11/2025** | **C 14/11/2025** | 35 |
| 9 | Tên môn học 09 | 40 | 8 | **S 17/11/2025** | **C 20/11/2025** | **S 21/11/2025** | 30 |
| 10 | Tên môn học 10 | 45 | 9 | **C 21/11/2025** | **C 27/11/2025** | **S 28/11/2025** | 35 |
| 11 | Tên môn học 11 | 70 | 14 | **C 28/11/2025** | **S 9/12/2025** | **C 9/12/2025** | 60 |
| 12 | Tên môn học 12 | 35 | 7 | **S 10/12/2025** | **S 15/12/2025** | **C 15/12/2025** | 25 |
| 13 | Tên môn học 13 | 50 | 10 | **S 16/12/2025** | **C 22/12/2025** | **S 23/12/2025** | 40 |
| 14 | Tên môn học 14 | 45 | 9 | **C 23/12/2025** | **C 29/12/2025** | **S 30/12/2025** | 35 |
| 15 | Tên môn học 15 | 40 | 8 | **C 30/12/2025** | **S 6/1/2026** | **C 6/1/2026** | 30 |
| 16 | Tên môn học 16 | 35 | 7 | **S 7/1/2026** | **S 12/1/2026** | **C 12/1/2026** | 25 |
| 17 | Tên môn học 17 | 30 | 6 | **S 13/1/2026** | **C 15/1/2026** | **S 16/1/2026** | 20 |
| 18 | Tên môn học 18 | 35 | 7 | **C 16/1/2026** | **C 21/1/2026** | **S 22/1/2026** | 25 |
| 19 | Tên môn học 19 | 40 | 8 | **C 22/1/2026** | **S 28/1/2026** | **C 28/1/2026** | 30 |
| - | **Thi tốt nghiệp** | 15 | - | **S 22/02/2026** | **S 24/02/2026** | — | 15 |
| - | **Bế giảng** | 5 | - | **S 20/4/2026** | — | — | 5 |

---

## 8. Giấy Phép & Bản Quyền
Dự án được xây dựng phục vụ công tác giảng dạy, lập kế hoạch đào tạo và xuất văn bản học tập theo quy định đào tạo.
