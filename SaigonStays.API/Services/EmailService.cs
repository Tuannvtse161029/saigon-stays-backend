using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using SaigonStays.API.Models;

namespace SaigonStays.API.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendCheckinEmailAsync(string guestEmail, Booking booking, RoomCheckinInfo? checkinInfo)
    {
        _logger.LogInformation("Preparing check-in email for Booking: {BookingId} to {GuestEmail}", booking.Id, guestEmail);

        var fromEmail = _config["EmailSettings:From"] ?? "saigonstays.vn@gmail.com";
        var smtpServer = _config["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
        var port = int.TryParse(_config["EmailSettings:Port"], out var p) ? p : 587;
        var username = _config["EmailSettings:Username"] ?? fromEmail;
        var password = _config["EmailSettings:Password"] ?? "";

        var roomName = booking.Room?.Name ?? "Saigon Stays Studio";
        var branchName = booking.Room?.Branch?.Name ?? "Saigon Stays Homestay";
        var branchAddress = booking.Room?.Branch?.Address ?? "TP. Ho Chi Minh";
        var hotline = booking.Room?.Branch?.Hotline ?? "0908889991";
        var slotName = booking.PricingSlot?.SlotName ?? "Slot Dat Phong";
        var bookingDateStr = booking.BookingDate.ToString("dd/MM/yyyy");
        var passcode = checkinInfo?.DoorPasscode ?? "123456#";
        var wifiSsid = checkinInfo?.WifiSSID ?? "SaigonStays_Wifi";
        var wifiPassword = checkinInfo?.WifiPassword ?? "saigonstays2026";

        var subject = $"[Saigon Stays] Xác nhận đặt phòng thành công & Thông tin Tự Check-in #{booking.Id}";

        var htmlBody = $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{subject}</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            margin: 0;
            padding: 0;
            background-color: #f4f7f6;
            color: #333333;
        }}
        .container {{
            max-width: 650px;
            margin: 20px auto;
            background: #ffffff;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 4px 15px rgba(0,0,0,0.08);
        }}
        .header {{
            background: linear-gradient(135deg, #1e3c72 0%, #2a5298 100%);
            color: #ffffff;
            padding: 30px 25px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0 0 10px 0;
            font-size: 26px;
            font-weight: 700;
            letter-spacing: 0.5px;
        }}
        .badge {{
            display: inline-block;
            background: #10b981;
            color: #ffffff;
            padding: 6px 14px;
            border-radius: 20px;
            font-weight: 600;
            font-size: 13px;
        }}
        .content {{
            padding: 25px 30px;
        }}
        .welcome {{
            font-size: 16px;
            line-height: 1.6;
            margin-bottom: 20px;
        }}
        .card {{
            background: #f8fafc;
            border: 1px solid #e2e8f0;
            border-radius: 8px;
            padding: 18px 20px;
            margin-bottom: 20px;
        }}
        .card-title {{
            font-size: 16px;
            font-weight: 700;
            color: #1e3c72;
            margin-top: 0;
            margin-bottom: 12px;
            border-bottom: 2px solid #e2e8f0;
            padding-bottom: 6px;
        }}
        .info-row {{
            display: flex;
            justify-content: space-between;
            margin-bottom: 8px;
            font-size: 14px;
        }}
        .info-label {{
            color: #64748b;
            font-weight: 500;
        }}
        .info-value {{
            font-weight: 600;
            color: #0f172a;
            text-align: right;
        }}
        .checkin-box {{
            background: linear-gradient(135deg, #eff6ff 0%, #dbeafe 100%);
            border: 2px dashed #3b82f6;
            border-radius: 10px;
            padding: 20px;
            margin: 25px 0;
            text-align: center;
        }}
        .checkin-title {{
            font-size: 18px;
            font-weight: 800;
            color: #1d4ed8;
            margin: 0 0 15px 0;
            text-transform: uppercase;
        }}
        .passcode-display {{
            font-size: 28px;
            font-weight: 900;
            letter-spacing: 4px;
            color: #b91c1c;
            background: #ffffff;
            padding: 10px 20px;
            border-radius: 8px;
            display: inline-block;
            margin: 10px 0;
            border: 1px solid #fca5a5;
        }}
        .wifi-info {{
            margin-top: 12px;
            font-size: 14px;
            color: #1e40af;
            background: rgba(255,255,255,0.7);
            padding: 8px 14px;
            border-radius: 6px;
            display: inline-block;
        }}
        .rules-list {{
            padding-left: 20px;
            margin: 10px 0;
            font-size: 13px;
            color: #475569;
            line-height: 1.6;
        }}
        .footer {{
            background: #f1f5f9;
            text-align: center;
            padding: 20px;
            font-size: 13px;
            color: #64748b;
            border-top: 1px solid #e2e8f0;
        }}
        .footer a {{
            color: #2563eb;
            text-decoration: none;
            font-weight: 600;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>SAIGON STAYS HOMESTAY</h1>
            <div class=""badge"">ĐÃ THANH TOÁN THÀNH CÔNG</div>
        </div>

        <div class=""content"">
            <p class=""welcome"">
                Xin chào <strong>{booking.GuestName}</strong>,<br>
                Cảm ơn bạn đã lựa chọn Saigon Stays. Đơn đặt phòng của bạn đã được thanh toán thành công. Dưới đây là thông tin chi tiết và hướng dẫn <strong>Tự Check-in</strong> để vào phòng nhanh chóng:
            </p>

            <!-- Self Check-in Highlight Box -->
            <div class=""checkin-box"">
                <div class=""checkin-title"">🔑 THÔNG TIN TỰ CHECK-IN TỰ ĐỘNG</div>
                <div style=""font-size: 13px; color: #475569;"">Mã khoá cửa điện tử thông minh:</div>
                <div class=""passcode-display"">{passcode}</div>
                <div style=""font-size: 12px; color: #64748b; margin-bottom: 8px;"">(Chạm tay vào bàn phím cho sáng đèn &rarr; Nhập mật mã &rarr; Nhấn phím # để mở)</div>
                <div class=""wifi-info"">
                    📶 <strong>Wifi:</strong> {wifiSsid} | <strong>Mật khẩu:</strong> {wifiPassword}
                </div>
            </div>

            <!-- Booking Info Card -->
            <div class=""card"">
                <div class=""card-title"">📋 CHI TIẾT ĐƠN ĐẶT PHÒNG #{booking.Id}</div>
                <div class=""info-row"">
                    <span class=""info-label"">Chi nhánh:</span>
                    <span class=""info-value"">{branchName}</span>
                </div>
                <div class=""info-row"">
                    <span class=""info-label"">Địa chỉ:</span>
                    <span class=""info-value"">{branchAddress}</span>
                </div>
                <div class=""info-row"">
                    <span class=""info-label"">Phòng:</span>
                    <span class=""info-value"">{roomName}</span>
                </div>
                <div class=""info-row"">
                    <span class=""info-label"">Khung giờ đặt (Slot):</span>
                    <span class=""info-value"">{slotName}</span>
                </div>
                <div class=""info-row"">
                    <span class=""info-label"">Ngày lưu trú:</span>
                    <span class=""info-value"">{bookingDateStr}</span>
                </div>
                <div class=""info-row"">
                    <span class=""info-label"">Số tiền đã thanh toán:</span>
                    <span class=""info-value"" style=""color: #059669; font-size: 16px;"">{booking.TotalPrice:N0} VNĐ</span>
                </div>
            </div>

            <!-- Rules & Guidance -->
            <div class=""card"">
                <div class=""card-title"">📌 NỘI QUY VÀ LƯU Ý LƯU TRÚ</div>
                <ul class=""rules-list"">
                    <li>Vui lòng giữ trật tự chung sau 22:00 để không ảnh hưởng đến các phòng xung quanh.</li>
                    <li>Không hút thuốc lá / thuốc lá điện tử trong phòng ngủ (Vui lòng sử dụng khu vực ban công).</li>
                    <li>Tắt các thiết bị điện, điều hòa và đóng chặt cửa khi ra khỏi phòng.</li>
                    <li>Giờ trả phòng theo đúng khung giờ đã đăng ký. Vui lòng đóng cửa khi rời phòng.</li>
                </ul>
            </div>
        </div>

        <div class=""footer"">
            <p style=""margin: 0 0 6px 0;"">Cần hỗ trợ gấp? Hotline CSKH: <a href=""tel:{hotline}"">{hotline}</a></p>
            <p style=""margin: 0;"">&copy; {DateTime.UtcNow.Year} Saigon Stays Homestay. Chúc bạn có kỳ nghỉ thật tuyệt vời!</p>
        </div>
    </div>
</body>
</html>";

        // Try to send email via SMTP if credentials are configured
        if (!string.IsNullOrEmpty(password) && password != "YOUR_GMAIL_APP_PASSWORD")
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Saigon Stays Homestay", fromEmail));
                message.To.Add(new MailboxAddress(booking.GuestName, guestEmail));
                message.Subject = subject;

                var builder = new BodyBuilder
                {
                    HtmlBody = htmlBody
                };
                message.Body = builder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(smtpServer, port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(username, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Check-in email sent successfully via SMTP to {GuestEmail} for Booking {BookingId}", guestEmail, booking.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMTP email to {GuestEmail} for Booking {BookingId}. Fallback to simulated delivery.", guestEmail, booking.Id);
            }
        }
        else
        {
            _logger.LogWarning("[DEMO MODE] EmailSettings password is not configured or placeholder. Simulating check-in email to {GuestEmail} for booking {BookingId}.", guestEmail, booking.Id);
            _logger.LogInformation("Email Subject: {Subject}", subject);
            _logger.LogInformation("Passcode: {Passcode}, Wifi: {WifiSSID}/{WifiPass}", passcode, wifiSsid, wifiPassword);
        }
    }
}
