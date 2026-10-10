using System.ComponentModel.DataAnnotations;

namespace SaigonStays.API.DTOs;

public class CreateBookingRequestDto
{
    public Guid? UserId { get; set; }

    [Required]
    public Guid RoomId { get; set; }

    [Required]
    public Guid PricingSlotId { get; set; }

    [Required, MaxLength(100)]
    public string GuestName { get; set; } = string.Empty;

    [Required, MaxLength(20), Phone]
    public string GuestPhone { get; set; } = string.Empty;

    [Required, MaxLength(100), EmailAddress]
    public string GuestEmail { get; set; } = string.Empty;

    [Required]
    public DateOnly BookingDate { get; set; }
}

public class BookingResponseDto
{
    public string Id { get; set; } = string.Empty; // BK_XXXX
    public Guid? UserId { get; set; }
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string? RoomImage { get; set; }
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string BranchAddress { get; set; } = string.Empty;
    public string? BranchHotline { get; set; }
    public Guid PricingSlotId { get; set; }
    public string SlotName { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string GuestPhone { get; set; } = string.Empty;
    public string GuestEmail { get; set; } = string.Empty;
    public DateOnly BookingDate { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = "PENDING";
    public DateTime CreatedAt { get; set; }
    public string? VietQrImageUrl { get; set; }
    public RoomCheckinInfoDto? CheckinInfo { get; set; } // Provided only if status is PAID or CHECKED_IN
}

public class BookingListFilterDto
{
    public Guid? BranchId { get; set; }
    public Guid? RoomId { get; set; }
    public string? Status { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? Search { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
