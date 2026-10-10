# 🏡 SAIGON STAYS - BACKEND WEB API (.NET 8)

> **Dự án:** Hệ thống Đặt phòng Homestay Tự Check-in (Saigon Stays)  
> **Framework:** C# / .NET 8 Web API | Entity Framework Core 8  
> **Database:** SQL Server (`35.240.181.166` | Database: `homestay`)  
> **Authentication:** JWT Bearer + Google OAuth 2.0 (Google Identity Services)  
> **Email & Background Processing:** MailKit / MimeKit + Hangfire Engine  
> **Payment & Settlement:** VietQR Dynamic QR Code & Webhook Auto-Reconciliation

---

## 🚀 1. Cấu trúc Thư mục Dự án

```
d:/homestay/
├── SaigonStays.sln                  # Visual Studio Solution
├── init_db.sql                      # SQL Migration Script khởi tạo trên SQL Server
├── BACKEND_SPEC.md                  # Bản đặc tả kỹ thuật chi tiết
│
├── SaigonStays.API/                 # Main .NET 8 Web API Project
│   ├── Controllers/                 # RESTful API Controllers
│   │   ├── AuthController.cs        # Đăng nhập Google, Dev Login, Profile, JWT
│   │   ├── BranchesController.cs    # Quản lý Chi nhánh Homestay (Quận 1, Thảo Điền)
│   │   ├── RoomsController.cs       # Quản lý Phòng, Tiện nghi, Check-in Info, Pricing Slots
│   │   ├── AvailabilityController.cs# Kiểm tra lịch trống / đặt phòng theo ngày
│   │   ├── BookingsController.cs    # Tạo đơn đặt phòng, tra cứu mã BK_XXXX
│   │   └── PaymentsController.cs    # Tạo VietQR, Webhook gạch nợ ngân hàng, Duyệt thanh toán
│   ├── Data/                        # EF Core DbContext & Data Seeding
│   │   ├── ApplicationDbContext.cs  # Fluent API, quan hệ bảng & cấu hình kiểu dữ liệu
│   │   ├── DbInitializer.cs         # Khởi tạo dữ liệu mẫu chi nhánh, phòng, slot giá, admin
│   │   └── Migrations/              # EF Core Code-First Migrations
│   ├── DTOs/                        # Request & Response Data Transfer Objects
│   │   ├── AuthDtos.cs
│   │   ├── BranchDtos.cs
│   │   ├── RoomDtos.cs
│   │   ├── BookingDtos.cs
│   │   ├── AvailabilityDtos.cs
│   │   └── PaymentDtos.cs
│   ├── Models/                      # Entity Framework Models
│   │   ├── User.cs
│   │   ├── Branch.cs
│   │   ├── Room.cs
│   │   ├── PricingSlot.cs
│   │   ├── Booking.cs
│   │   └── RoomCheckinInfo.cs
│   ├── Services/                    # Business Logic Layer
│   │   ├── IJwtService.cs / JwtService.cs
│   │   ├── IVietQrService.cs / VietQrService.cs
│   │   ├── IEmailService.cs / EmailService.cs
│   │   └── IBookingService.cs / BookingService.cs
│   ├── Common/                      # ApiResponse<T> & Constants
│   ├── appsettings.json             # File cấu hình Server, Email, JWT, VietQR
│   └── Program.cs                   # Pipeline Middleware, Swagger, DI, Hangfire Dashboard
│
└── SaigonStays.Tests/               # XUnit Unit & Integration Tests
    ├── BookingServiceTests.cs       # Kiểm thử logic đặt phòng, chống trùng slot, thanh toán
    └── ServicesTests.cs             # Kiểm thử VietQR URL format & JWT Generator
```

---

## 🛠️ 2. Hướng dẫn Chạy Backend & Kiểm thử

### **A. Khởi động Web API:**
```bash
cd SaigonStays.API
dotnet run
```
- **Swagger UI:** Mở trình duyệt tại `http://localhost:5000` hoặc `https://localhost:7001`
- **Hangfire Dashboard:** Mở tại `http://localhost:5000/hangfire`

### **B. Chạy Bộ kiểm thử tự động (Unit Tests):**
```bash
dotnet test
```

---

## 📡 3. Danh sách Endpoints RESTful API

| Method | Endpoint | Quyền hạn | Mô tả |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/auth/google-login` | Public | Đăng nhập Google với `idToken` từ Google Identity Services |
| `POST` | `/api/v1/auth/dev-login` | Public | Đăng nhập Test / Dev không cần Google Client ID |
| `GET` | `/api/v1/auth/me` | `Authorize` | Lấy thông tin user hiện tại từ JWT |
| `GET` | `/api/v1/branches` | Public | Lấy danh sách tất cả các chi nhánh |
| `GET` | `/api/v1/branches/{id}` | Public | Lấy chi tiết chi nhánh kèm danh sách phòng & giá |
| `GET` | `/api/v1/rooms?branchId={id}` | Public | Danh sách phòng theo chi nhánh |
| `GET` | `/api/v1/rooms/{id}` | Public | Chi tiết phòng & khung giờ |
| `GET` | `/api/v1/availability/calendar` | Public | Tra cứu lịch trống theo Chi nhánh & Ngày (`YYYY-MM-DD`) |
| `POST` | `/api/v1/bookings` | Public | Đặt phòng (Sinh mã `BK_XXXX`, tính tổng tiền, trả về VietQR) |
| `GET` | `/api/v1/bookings/{id}` | Public | Tra cứu thông tin đơn đặt phòng (Mở mật mã khóa cửa khi đã thanh toán) |
| `POST` | `/api/v1/payments/create-qr` | Public | Tạo lại mã QR thanh toán VietQR cho đơn đặt phòng |
| `GET` | `/api/v1/payments/{bookingId}/status` | Public | Kiểm tra trạng thái thanh toán đơn phòng |
| `POST` | `/api/v1/payments/webhook` | Public | Webhook gạch nợ tự động & kích hoạt Hangfire gửi Gmail Check-in |
| `POST` | `/api/v1/payments/manual-confirm/{bookingId}` | Staff/Admin | Xác nhận thanh toán thủ công & gửi email |

---

## 💳 4. Quy trình Tự Check-in & Thanh toán VietQR

1. **Khách hàng chọn phòng & Khung giờ (Slot):**
   - Gọi `GET /api/v1/availability/calendar?branchId=...&date=YYYY-MM-DD` để xem khung giờ trống.
2. **Khách hàng tạo đơn đặt:**
   - Gọi `POST /api/v1/bookings` với thông tin khách và `pricingSlotId`.
   - Hệ thống tạo mã `BK_XXXX` (trạng thái `PENDING`) và trả về `VietQrImageUrl`.
3. **Thanh toán qua Ngân hàng / Quét VietQR:**
   - Quét mã QR, nội dung chuyển khoản tự động điền: `THANH TOAN BK_XXXX`.
4. **Gạch nợ & Gửi thông tin Tự Check-in:**
   - Ngân hàng gọi `POST /api/v1/payments/webhook` (hoặc nhân viên bấm duyệt).
   - Đơn chuyển sang `PAID`.
   - **Hangfire Job** gửi ngay email HTML với:
     - 🔑 **Mật mã khoá cửa điện tử (Door Passcode)**
     - 📶 **Tên Wifi & Mật khẩu phòng**
     - 📍 **Địa chỉ chi nhánh & Số Hotline hỗ trợ 24/7**
