using SaigonStays.API.Models;

namespace SaigonStays.API.Services;

public interface IJwtService
{
    string GenerateToken(User user);
    DateTime GetExpirationDate();
}
