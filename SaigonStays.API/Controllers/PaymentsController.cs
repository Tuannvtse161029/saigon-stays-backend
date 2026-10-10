using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaigonStays.API.Common;
using SaigonStays.API.Data;
using SaigonStays.API.DTOs;
using SaigonStays.API.Services;

namespace SaigonStays.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IBookingService _bookingService;
    private readonly IVietQrService _vietQrService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        ApplicationDbContext db,
        IBookingService bookingService,
        IVietQrService vietQrService,
        ILogger<PaymentsController> logger)
    {
        _db = db;
        _bookingService = bookingService;
        _vietQrService = vietQrService;
        _logger = logger;
    }

    /// <summary>
    /// Generate VietQR payment info & QR Code URL for a booking
    /// </summary>
    [HttpPost("create-qr")]
    public async Task<ActionResult<ApiResponse<CreateQrResponseDto>>> CreateQr([FromBody] CreateQrRequestDto dto)
    {
        var booking = await _db.Bookings.FindAsync(dto.BookingId);
        if (booking == null)
        {
            return NotFound(ApiResponse<CreateQrResponseDto>.Fail($"Không tìm thấy đơn đặt phòng '{dto.BookingId}'."));
        }

        if (booking.Status == Constants.BookingStatus.Paid)
        {
            return BadRequest(ApiResponse<CreateQrResponseDto>.Fail($"Đơn đặt phòng '{dto.BookingId}' đã được thanh toán rồi."));
        }

        var qrDto = _vietQrService.GenerateQr(booking);
        return Ok(ApiResponse<CreateQrResponseDto>.Ok(qrDto, "Tạo mã QR thanh toán VietQR thành công."));
    }

    /// <summary>
    /// Check payment status for a booking
    /// </summary>
    [HttpGet("{bookingId}/status")]
    public async Task<ActionResult<ApiResponse<PaymentStatusDto>>> GetPaymentStatus(string bookingId)
    {
        var booking = await _db.Bookings.FindAsync(bookingId);
        if (booking == null)
        {
            return NotFound(ApiResponse<PaymentStatusDto>.Fail($"Không tìm thấy đơn đặt phòng '{bookingId}'."));
        }

        var isPaid = booking.Status == Constants.BookingStatus.Paid || booking.Status == Constants.BookingStatus.CheckedIn;

        var statusDto = new PaymentStatusDto
        {
            BookingId = booking.Id,
            Status = booking.Status,
            TotalPrice = booking.TotalPrice,
            IsPaid = isPaid,
            PaidAt = isPaid ? DateTime.UtcNow : null
        };

        return Ok(ApiResponse<PaymentStatusDto>.Ok(statusDto));
    }

    /// <summary>
    /// Bank/Payment Gateway Webhook (Auto reconciliation & trigger Hangfire email job)
    /// </summary>
    [HttpPost("webhook")]
    public async Task<ActionResult<ApiResponse<object>>> Webhook([FromBody] BankWebhookDto dto)
    {
        _logger.LogInformation("Payment Webhook received: Amount={Amount}, Content={Content}, BookingId={BookingId}",
            dto.Amount, dto.Content, dto.BookingId);

        string? targetBookingId = dto.BookingId;

        // If BookingId is not directly provided in payload, extract from transfer content (e.g. "THANH TOAN BK_9981" or "BK9981")
        if (string.IsNullOrEmpty(targetBookingId) && !string.IsNullOrEmpty(dto.Content))
        {
            var match = Regex.Match(dto.Content, @"BK_?(\d{4,})", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var digits = match.Groups[1].Value;
                targetBookingId = $"BK_{digits}";
            }
        }

        if (string.IsNullOrEmpty(targetBookingId))
        {
            _logger.LogWarning("Webhook received but no valid Booking ID could be extracted from content: {Content}", dto.Content);
            return BadRequest(ApiResponse<object>.Fail("Không tìm thấy mã đơn đặt phòng (BK_XXXX) trong nội dung chuyển khoản."));
        }

        var success = await _bookingService.ProcessPaymentSuccessAsync(targetBookingId);

        if (!success)
        {
            return NotFound(ApiResponse<object>.Fail($"Không tìm thấy đơn đặt phòng '{targetBookingId}' để gạch nợ."));
        }

        return Ok(ApiResponse<object>.Ok(new { BookingId = targetBookingId, Status = "PAID", Message = "Gạch nợ thành công & Đã gửi email check-in." }));
    }

    /// <summary>
    /// Manual Payment Confirmation (Admin/Staff/Dev endpoint to confirm payment and trigger Hangfire Check-in Email Job)
    /// </summary>
    [HttpPost("manual-confirm/{bookingId}")]
    public async Task<ActionResult<ApiResponse<object>>> ManualConfirmPayment(string bookingId)
    {
        var success = await _bookingService.ProcessPaymentSuccessAsync(bookingId);

        if (!success)
        {
            return NotFound(ApiResponse<object>.Fail($"Không tìm thấy đơn đặt phòng '{bookingId}'."));
        }

        return Ok(ApiResponse<object>.Ok(new { BookingId = bookingId, Status = "PAID", Message = "Xác nhận thanh toán thủ công thành công & Hangfire Job gửi email đã được kích hoạt." }));
    }
}
