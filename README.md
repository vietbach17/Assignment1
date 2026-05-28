# Dự án Cấu trúc 3-Layers - Hệ thống Xác thực & Phân quyền

Đây là bộ khung dự án (Boilerplate) tiêu chuẩn sử dụng **ASP.NET Core MVC (.NET 8)** kết hợp với **Entity Framework Core** và **PostgreSQL**, được cấu trúc nghiêm ngặt theo mô hình **3-Layers (Presentation - Business Logic - Data Access)**. 

Dự án đã tích hợp sẵn module **Authentication & Authorization** bằng **Cookie Authentication** và **BCrypt** làm nền tảng bảo mật. Các thành viên trong nhóm có thể trực tiếp kế thừa bộ khung này để phát triển nhanh các tính năng khác của Assignment.

---

## 🏗️ Cấu trúc thư mục dự án (3-Layers)

Dự án được phân chia độc lập thành 3 Layer (Class Library) rõ ràng để phân công công việc thuận lợi:

1. **`DataAccessLayer (DAL)`**: Quản lý thực thể (Entities), kết nối Cơ sở dữ liệu (`DbContext`) và các mẫu truy xuất dữ liệu (`Repositories`).
2. **`BussinessLayer (BLL)`**: Chứa các lớp nghiệp vụ xử lý logic (`Services`). Giao tiếp trực tiếp giữa Controller và Repository.
3. **`PresentationLayer (PL)`**: Ứng dụng Web MVC (Controllers, Views, ViewModels). Chịu trách nhiệm hiển thị giao diện và cấu hình hệ thống (`Program.cs`).

---

## 🚀 Hướng dẫn nhanh cho thành viên trong nhóm bắt đầu

Khi một thành viên clone dự án này về máy, họ chỉ cần làm theo các bước sau để chạy được dự án ngay lập tức:

### 1. Cấu hình Database cục bộ
Mở file `PresentationLayer/appsettings.json`, chỉnh sửa thông tin đăng nhập PostgreSQL cục bộ trên máy của bạn:
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Database=Assignment1Db;Username=tên_user;Password=mật_khẩu_của_bạn"
}
```

### 2. Khởi tạo Cơ sở dữ liệu tự động
Mở Terminal tại thư mục gốc dự án và chạy lệnh sau để EF Core tự tạo DB và nạp tài khoản mẫu:
```powershell
dotnet ef database update --project DataAccessLayer --startup-project PresentationLayer
```

### 3. Chạy ứng dụng web
```powershell
cd PresentationLayer
dotnet run
```
Truy cập `http://localhost:5000` trên trình duyệt.

---

## 🔑 Danh sách tài khoản dùng thử (Seed Data)
Hệ thống đã nạp sẵn 3 tài khoản tương ứng với 3 phân quyền khác nhau trong Database để test:
*   **Admin** (Trang quản trị bảo mật): `admin` / `admin123` -> Dẫn tới `/Admin/Dashboard`
*   **Lecturer** (Quảng lý tài liệu): `lecturer` / `lecturer123` -> Dẫn tới `/Document/Index`
*   **Student** (Không gian thảo luận): `student` / `student123` -> Dẫn tới `/Home/Chat`

---

## 🛠️ Hướng dẫn thành viên nhóm viết thêm tính năng mới

Để giữ cho code sạch và tuân thủ mô hình 3-Layers, khi làm các tính năng mới (ví dụ: Quản lý Tài liệu, Lịch học, Điểm số...), các thành viên hãy thực hiện theo đúng 4 bước quy chuẩn sau:

### Bước 1: Định nghĩa Thực thể ở tầng DataAccessLayer
Tạo class trong thư mục `DataAccessLayer/Models/` và đăng ký `DbSet` trong `AppDbContext.cs`.
```csharp
// Ví dụ: DataAccessLayer/Models/Document.cs
public class Document {
    public int Id { get; set; }
    public string Title { get; set; }
    public string FilePath { get; set; }
}
```

### Bước 2: Tạo Repository ở tầng DataAccessLayer
*   Tạo Interface và Class triển khai truy vấn database trong thư mục `DataAccessLayer/Repositories/` (Ví dụ: `IDocumentRepository.cs` và `DocumentRepository.cs`).
*   Đăng ký Dependency Injection trong [Program.cs](file:///d:/Assignment1/PresentationLayer/Program.cs):
    ```csharp
    builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
    ```

### Bước 3: Tạo Service ở tầng BussinessLayer
*   Tạo Interface và Class xử lý logic nghiệp vụ trong thư mục `BussinessLayer/Services/` (Ví dụ: `IDocumentService.cs` và `DocumentService.cs`).
*   Tiêm (Inject) `IDocumentRepository` vào constructor của Service để làm việc.
*   Đăng ký Dependency Injection trong [Program.cs](file:///d:/Assignment1/PresentationLayer/Program.cs):
    ```csharp
    builder.Services.AddScoped<IDocumentService, DocumentService>();
    ```

### Bước 4: Tạo Controller & Views ở tầng PresentationLayer
*   Tạo Controller trong thư mục `PresentationLayer/Controllers/`. Tiêm `IDocumentService` vào để lấy dữ liệu.
*   Tạo giao diện `.cshtml` tương ứng trong thư mục `PresentationLayer/Views/`.
*   **Chặn quyền truy cập (Rất quan trọng):** Dùng thẻ `[Authorize]` hoặc phân quyền cụ thể bằng cách thêm attribute lên đầu Controller hoặc Action:
    ```csharp
    [Authorize(Roles = "Lecturer")] // Chỉ Giảng viên mới được truy cập Controller này
    public class DocumentController : Controller { ... }
    ```

---

## 🤝 Quy trình đẩy code lên Git của nhóm (Best Practice)

Để tránh xung đột code (Conflict), các thành viên nên tuân thủ quy trình sau:
1. Trước khi viết code mới, luôn cập nhật code mới nhất từ nhánh chính:
   ```bash
   git checkout main
   git pull origin main
   ```
2. Tạo nhánh riêng để làm tính năng của mình:
   ```bash
   git checkout -b feature/ten-tinh-nang
   ```
3. Sau khi hoàn thành và test không lỗi, thực hiện commit và đẩy nhánh lên:
   ```bash
   git add .
   git commit -m "Mô tả tính năng đã làm"
   git push origin feature/ten-tinh-nang
   ```
4. Lên GitHub tạo **Pull Request (PR)** để nhóm trưởng duyệt và merge vào nhánh `main`.
