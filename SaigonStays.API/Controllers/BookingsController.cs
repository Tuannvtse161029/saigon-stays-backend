using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaigonStays.API.Common;
using SaigonStays.API.DTOs;
using SaigonStays.API.Services;
using System.Security.Claims;

namespace SaigonStays.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly ILogger<BookingsController> _logger;

    public BookingsController(IBookingService bookingService, ILogger<BookingsController> logger)
    {
        _bookingService = bookingService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new booking (Generates BK_XXXX code, calculates price, returns QR Code for VietQR payment)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<BookingResponseDto>>> CreateBooking([FromBody] CreateBookingRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<BookingResponseDto>.Fail("Dữ liệu đặt phòng không hợp lệ.", 
                ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()));
        }

        // If user is authenticated, attach UserId
        if (User.Identity?.IsAuthenticated == true && !dto.UserId.HasValue)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdStr, out var userId))
            {
                dto.UserId = userId;
            }
        }

        try
        {
            var booking = await _bookingService.CreateBookingAsync(dto);
            return CreatedAtAction(nameof(GetBookingById), new { id = booking.Id }, ApiResponse<BookingResponseDto>.Ok(booking, "Tạo đơn đặt phòng thành công. Vui lòng thanh toán để nhận mã check-in."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<BookingResponseDto>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking.");
            return StatusCode(500, ApiResponse<BookingResponseDto>.Fail($"Lỗi khi tạo đơn đặt phòng: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get booking details by Booking ID (e.g. BK_9981). Check-in passcode will be included only if paid.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<BookingResponseDto>>> GetBookingById(string id)
    {
        var isStaffOrAdmin = User.IsInRole(Constants.Roles.Admin) || User.IsInRole(Constants.Roles.Staff);
        var booking = await _bookingService.GetBookingByIdAsync(id, includeCheckinInfo: isStaffOrAdmin);

        if (booking == null)
        {
            return NotFound(ApiResponse<BookingResponseDto>.Fail($"Không tìm thấy đơn đặt phòng có mã '{id}'."));
        }

        return Ok(ApiResponse<BookingResponseDto>.Ok(booking));
    }

    /// <summary>
    /// Get list of bookings with filtering (Admin/Staff or query)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = $"{Constants.Roles.Admin},{Constants.Roles.Staff}")]
    public async Task<ActionResult<ApiResponse<List<BookingResponseDto>>>> GetBookings([FromQuery] BookingListFilterDto filter)
    {
        var bookings = await _bookingService.GetBookingsAsync(filter);
        return Ok(ApiResponse<List<BookingResponseDto>>.Ok(bookings));
    }

    /// <summary>
    /// Cancel a booking
    /// </summary>
    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<ApiResponse<bool>>> CancelBooking(string id)
    {
        var result = await _bookingService.CancelBookingAsync(id);
        if (!result)
        {
            return NotFound(ApiResponse<bool>.Fail($"Không tìm thấy đơn đặt phòng '{id}'."));
        }

        return Ok(ApiResponse<bool>.Ok(true, "Đã hủy đơn đặt phòng thành công."));
    }
}
