using SaigonStays.API.DTOs;
using SaigonStays.API.Models;

namespace SaigonStays.API.Services;

public interface IVietQrService
{
    CreateQrResponseDto GenerateQr(Booking booking);
}
