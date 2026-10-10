using Microsoft.Extensions.Configuration;
using SaigonStays.API.Models;
using SaigonStays.API.Services;
using Xunit;

namespace SaigonStays.Tests;

public class VietQrServiceTests
{
    [Fact]
    public void GenerateQr_ShouldProduceValidVietQrUrls()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"VietQR:BankId", "MB"},
            {"VietQR:AccountNo", "0987654321"},
            {"VietQR:AccountName", "SAIGON STAYS HOMESTAY"},
            {"VietQR:Template", "compact"}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var service = new VietQrService(configuration);

        var booking = new Booking
        {
            Id = "BK_9981",
            TotalPrice = 350000
        };

        // Act
        var result = service.GenerateQr(booking);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("BK_9981", result.BookingId);
        Assert.Equal(350000, result.Amount);
        Assert.Equal("MB", result.BankId);
        Assert.Equal("0987654321", result.AccountNo);
        Assert.Equal("THANH TOAN BK_9981", result.TransferContent);
        Assert.Contains("img.vietqr.io/image/MB-0987654321-compact.png", result.QrImageUrl);
        Assert.Contains("amount=350000", result.QrImageUrl);
    }
}

public class JwtServiceTests
{
    [Fact]
    public void GenerateToken_ShouldGenerateValidJwtToken()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"JwtSettings:Secret", "SaigonStaysSuperSecretSecurityKey2026!@#$MustBeAtLeast32BytesLongForHmacSha256"},
            {"JwtSettings:Issuer", "SaigonStaysAPI"},
            {"JwtSettings:Audience", "SaigonStaysClient"},
            {"JwtSettings:ExpiryMinutes", "60"}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var service = new JwtService(configuration);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@saigonstays.vn",
            FullName = "Saigon Stays Admin",
            Role = "ADMIN"
        };

        // Act
        var token = service.GenerateToken(user);
        var expiry = service.GetExpirationDate();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Contains(".", token); // Standard JWT format: header.payload.signature
        Assert.True(expiry > DateTime.UtcNow);
    }
}
