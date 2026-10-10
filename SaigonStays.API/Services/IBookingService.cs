using SaigonStays.API.DTOs;

namespace SaigonStays.API.Services;

public interface IBookingService
{
    Task<BookingResponseDto> CreateBookingAsync(CreateBookingRequestDto dto);
    Task<BookingResponseDto?> GetBookingByIdAsync(string id, bool includeCheckinInfo = false);
    Task<List<BookingResponseDto>> GetBookingsAsync(BookingListFilterDto filter);
    Task<bool> ProcessPaymentSuccessAsync(string bookingId);
    Task<AvailabilityCalendarResponseDto> GetAvailabilityCalendarAsync(Guid branchId, DateOnly date);
    Task<bool> CancelBookingAsync(string bookingId, string? reason = null);
}
