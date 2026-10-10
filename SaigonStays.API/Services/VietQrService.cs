using System.Web;
using SaigonStays.API.DTOs;
using SaigonStays.API.Models;

namespace SaigonStays.API.Services;

public class VietQrService : IVietQrService
{
    private readonly IConfiguration _config;

    public VietQrService(IConfiguration config)
    {
        _config = config;
    }

    public CreateQrResponseDto GenerateQr(Booking booking)
    {
        var bankId = _config["VietQR:BankId"] ?? "MB";
        var accountNo = _config["VietQR:AccountNo"] ?? "0987654321";
        var accountName = _config["VietQR:AccountName"] ?? "SAIGON STAYS HOMESTAY";
        var template = _config["VietQR:Template"] ?? "compact";

        var transferContent = $"THANH TOAN {booking.Id}";
        var amount = Convert.ToInt64(booking.TotalPrice);

        var encodedAccountName = Uri.EscapeDataString(accountName);
        var encodedContent = Uri.EscapeDataString(transferContent);

        var qrImageUrl = $"https://img.vietqr.io/image/{bankId}-{accountNo}-{template}.png?amount={amount}&addInfo={encodedContent}&accountName={encodedAccountName}";
        var quickPayUrl = $"https://api.vietqr.io/{bankId}/{accountNo}/{amount}/{encodedContent}/vietqr_net_2.jpg";

        return new CreateQrResponseDto
        {
            BookingId = booking.Id,
            Amount = booking.TotalPrice,
            BankId = bankId,
            AccountNo = accountNo,
            AccountName = accountName,
            TransferContent = transferContent,
            QrImageUrl = qrImageUrl,
            QuickPayUrl = quickPayUrl
        };
    }
}
