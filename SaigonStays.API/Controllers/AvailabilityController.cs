using Microsoft.AspNetCore.Mvc;
using SaigonStays.API.Common;
using SaigonStays.API.DTOs;
using SaigonStays.API.Services;

namespace SaigonStays.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AvailabilityController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly ILogger<AvailabilityController> _logger;

    public AvailabilityController(IBookingService bookingService, ILogger<AvailabilityController> logger)
    {
        _bookingService = bookingService;
        _logger = logger;
    }

    /// <summary>
    /// Check room and pricing slot availability for a branch on a specific date
    /// </summary>
    /// <param name="branchId">Branch ID</param>
    /// <param name="date">Date in format YYYY-MM-DD (e.g. 2026-10-10). If omitted, defaults to today</param>
    [HttpGet("calendar")]
    public async Task<ActionResult<ApiResponse<AvailabilityCalendarResponseDto>>> GetCalendarAvailability(
        [FromQuery] Guid branchId,
        [FromQuery] DateOnly? date)
    {
        if (branchId == Guid.Empty)
        {
            return BadRequest(ApiResponse<AvailabilityCalendarResponseDto>.Fail("Vui lòng cung cấp branchId hợp lệ."));
        }

        var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);

        try
        {
            var result = await _bookingService.GetAvailabilityCalendarAsync(branchId, targetDate);
            return Ok(ApiResponse<AvailabilityCalendarResponseDto>.Ok(result));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<AvailabilityCalendarResponseDto>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching availability calendar for Branch: {BranchId}, Date: {Date}", branchId, targetDate);
            return StatusCode(500, ApiResponse<AvailabilityCalendarResponseDto>.Fail($"Lỗi hệ thống: {ex.Message}"));
        }
    }
}
