using System.ComponentModel.DataAnnotations;

namespace SaigonStays.API.DTOs;

public class CreateQrRequestDto
{
    [Required]
    public string BookingId { get; set; } = string.Empty;
}

public class CreateQrResponseDto
{
    public string BookingId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string BankId { get; set; } = string.Empty;
    public string AccountNo { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string TransferContent { get; set; } = string.Empty;
    public string QrImageUrl { get; set; } = string.Empty;
    public string QuickPayUrl { get; set; } = string.Empty;
}

public class PaymentStatusDto
{
    public string BookingId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
}

public class BankWebhookDto
{
    public string? TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string? Content { get; set; } // May contain "BK_9981" or "BK9981"
    public string? BookingId { get; set; } // Optional if direct
    public string? Gateway { get; set; }
    public DateTime? TransactionDate { get; set; }
}
