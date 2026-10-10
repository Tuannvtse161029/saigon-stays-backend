using System.ComponentModel.DataAnnotations;

namespace SaigonStays.API.DTOs;

public class RoomDto
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string Status { get; set; } = "AVAILABLE";
    public decimal MinPrice { get; set; }
    public List<PricingSlotDto> PricingSlots { get; set; } = new();
}

public class RoomDetailDto
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string BranchAddress { get; set; } = string.Empty;
    public string? BranchHotline { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string Status { get; set; } = "AVAILABLE";
    public List<PricingSlotDto> PricingSlots { get; set; } = new();
    public RoomCheckinInfoDto? CheckinInfo { get; set; } // Only exposed to authorized users / paid bookings
}

public class CreateRoomDto
{
    [Required]
    public Guid BranchId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string Status { get; set; } = "AVAILABLE";

    public CreateRoomCheckinInfoDto? CheckinInfo { get; set; }
    public List<CreatePricingSlotDto>? PricingSlots { get; set; }
}

public class UpdateRoomDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string Status { get; set; } = "AVAILABLE";

    public UpdateRoomCheckinInfoDto? CheckinInfo { get; set; }
}

public class PricingSlotDto
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public string SlotName { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public decimal Price { get; set; }
}

public class CreatePricingSlotDto
{
    [Required, MaxLength(50)]
    public string SlotName { get; set; } = string.Empty;

    [Required]
    public TimeSpan StartTime { get; set; }

    [Required]
    public TimeSpan EndTime { get; set; }

    [Required, Range(0, 100000000)]
    public decimal Price { get; set; }
}

public class RoomCheckinInfoDto
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public string DoorPasscode { get; set; } = string.Empty;
    public string? WifiSSID { get; set; }
    public string? WifiPassword { get; set; }
}

public class CreateRoomCheckinInfoDto
{
    [Required, MaxLength(50)]
    public string DoorPasscode { get; set; } = string.Empty;
    [MaxLength(50)]
    public string? WifiSSID { get; set; }
    [MaxLength(50)]
    public string? WifiPassword { get; set; }
}

public class UpdateRoomCheckinInfoDto
{
    [Required, MaxLength(50)]
    public string DoorPasscode { get; set; } = string.Empty;
    [MaxLength(50)]
    public string? WifiSSID { get; set; }
    [MaxLength(50)]
    public string? WifiPassword { get; set; }
}
