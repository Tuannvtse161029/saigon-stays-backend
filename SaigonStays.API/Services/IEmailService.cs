using SaigonStays.API.Models;

namespace SaigonStays.API.Services;

public interface IEmailService
{
    Task SendCheckinEmailAsync(string guestEmail, Booking booking, RoomCheckinInfo? checkinInfo);
}
