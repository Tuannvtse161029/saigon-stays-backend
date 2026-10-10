# 🛠️ BACKEND TECHNICAL SPECIFICATION (.NET 8 API)
> **Dự án:** Hệ thống Đặt phòng Homestay Tự Check-in (Saigon Stays)
> **Framework:** C# / .NET 8 Web API | Entity Framework Core 8
> **Database:** SQL Server (Server: 35.240.181.166 | Database: homestay)
> **Authentication:** JWT + Google OAuth 2.0 (Google Identity Services)

---

## 1. TECH STACK & PACKAGES
- **Framework:** .NET 8 Web API
- **ORM:** Entity Framework Core 8
- **Database:** SQL Server
- **Auth:** JWT + `Google.Apis.Auth`
- **Email Engine:** MailKit / MimeKit (Gửi qua SMTP Gmail)
- **Background Jobs:** Hangfire hoặc MediatR
- **Payment API:** Tích hợp VietQR / Webhook gạch nợ

### **NuGet Packages Bắt buộc:**
```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Google.Apis.Auth
dotnet add package MailKit
dotnet add package Hangfire.AspNetCore
dotnet add package Hangfire.SqlServer
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

---

## 2. CẤU HÌNH DATABASE & CONNECTION STRING

### A. File `appsettings.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=35.240.181.166;Database=homestay;User Id=sa;Password=baopro2147!;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=true"
  },
  "GoogleAuth": {
    "ClientId": "YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com"
  },
  "EmailSettings": {
    "From": "your-homestay@gmail.com",
    "SmtpServer": "smtp.gmail.com",
    "Port": 587,
    "Username": "your-homestay@gmail.com",
    "Password": "YOUR_GMAIL_APP_PASSWORD"
  },
  "JwtSettings": {
    "Secret": "YourSuperSecretKeyForSaigonStaysHomestayJwtAuth2026!",
    "Issuer": "SaigonStaysAPI",
    "Audience": "SaigonStaysClient",
    "ExpiryMinutes": 1440
  },
  "VietQR": {
    "BankId": "MB",
    "AccountNo": "0987654321",
    "AccountName": "SAIGON STAYS HOMESTAY",
    "Template": "compact"
  }
}
```

### B. Khai báo DbContext trong `Program.cs`
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

---

## 3. ENTITY FRAMEWORK CORE MODELS (C# Classes)

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? GoogleId { get; set; }
    [Required, MaxLength(100)]
    public string Email { get; set; } = string.Empty;
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    [MaxLength(20)]
    public string Role { get; set; } = "GUEST"; // GUEST, STAFF, ADMIN
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Branch
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    [Required, MaxLength(255)]
    public string Address { get; set; } = string.Empty;
    [MaxLength(20)]
    public string? Hotline { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}

public class Room
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; }
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    [MaxLength(20)]
    public string Status { get; set; } = "AVAILABLE"; // AVAILABLE, CLEANING, MAINTENANCE

    [ForeignKey("BranchId")]
    public Branch? Branch { get; set; }
    public RoomCheckinInfo? CheckinInfo { get; set; }
    public ICollection<PricingSlot> PricingSlots { get; set; } = new List<PricingSlot>();
}

public class PricingSlot
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoomId { get; set; }
    [Required, MaxLength(50)]
    public string SlotName { get; set; } = string.Empty; // VD: 11:30 - 14:30
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Price { get; set; }

    [ForeignKey("RoomId")]
    public Room? Room { get; set; }
}

public class Booking
{
    [Key, MaxLength(20)]
    public string Id { get; set; } = string.Empty; // Mã đơn: BK_9981
    public Guid? UserId { get; set; }
    public Guid RoomId { get; set; }
    public Guid PricingSlotId { get; set; }
    [Required, MaxLength(100)]
    public string GuestName { get; set; } = string.Empty;
    [Required, MaxLength(20)]
    public string GuestPhone { get; set; } = string.Empty;
    [Required, MaxLength(100)]
    public string GuestEmail { get; set; } = string.Empty;
    public DateOnly BookingDate { get; set; }
    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalPrice { get; set; }
    [MaxLength(20)]
    public string Status { get; set; } = "PENDING"; // PENDING, PAID, CHECKED_IN, CANCELLED
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("UserId")]
    public User? User { get; set; }
    [ForeignKey("RoomId")]
    public Room? Room { get; set; }
    [ForeignKey("PricingSlotId")]
    public PricingSlot? PricingSlot { get; set; }
}

public class RoomCheckinInfo
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoomId { get; set; }
    [Required, MaxLength(50)]
    public string DoorPasscode { get; set; } = string.Empty;
    [MaxLength(50)]
    public string? WifiSSID { get; set; }
    [MaxLength(50)]
    public string? WifiPassword { get; set; }

    [ForeignKey("RoomId")]
    public Room? Room { get; set; }
}
```

---

## 4. ENDPOINTS API RESTFUL

### A. Authenticate Google (`POST /api/v1/auth/google-login`)
- **Payload:** `{ "idToken": "GOOGLE_CREDENTIAL_ID_TOKEN" }`
- **Xử lý:** Validate Token bằng `GoogleJsonWebSignature.ValidateAsync()`, kiểm tra hoặc tạo User trong database homestay, trả về JWT Token.

### B. Booking & Payment
- `GET /api/v1/branches`
- `GET /api/v1/rooms?branchId={id}`
- `GET /api/v1/availability/calendar?branchId={id}&date=YYYY-MM-DD`
- `POST /api/v1/bookings`
- `POST /api/v1/payments/create-qr` (Trả về ảnh VietQR)
- `GET /api/v1/payments/{bookingId}/status`
- `POST /api/v1/payments/webhook` (Ngân hàng gọi về gạch nợ $\rightarrow$ Kích hoạt Hangfire Job gửi Gmail)

---

## 5. SERVICE GỬI GMAIL TỰ ĐỘNG

```csharp
public async Task ProcessPaymentSuccessAsync(string bookingId)
{
    var booking = await _db.Bookings
        .Include(b => b.Room).ThenInclude(r => r.Branch)
        .FirstOrDefaultAsync(b => b.Id == bookingId);

    var checkinInfo = await _db.RoomCheckinInfos
        .FirstOrDefaultAsync(c => c.RoomId == booking.RoomId);

    booking.Status = "PAID";
    await _db.SaveChangesAsync();

    // Enqueue gửi mail tự động
    BackgroundJob.Enqueue<IEmailService>(x => 
        x.SendCheckinEmailAsync(booking.GuestEmail, booking, checkinInfo));
}
```

---

## 6. SCRIPT SQL KHỞI TẠO TẠI SERVER (35.240.181.166)
```sql
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'homestay')
BEGIN
    CREATE DATABASE homestay;
END
GO
USE homestay;
GO
```
