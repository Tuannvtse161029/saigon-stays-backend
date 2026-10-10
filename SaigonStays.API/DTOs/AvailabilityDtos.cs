namespace SaigonStays.API.DTOs;

public class AvailabilityCalendarResponseDto
{
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public List<RoomAvailabilityDto> Rooms { get; set; } = new();
}

public class RoomAvailabilityDto
{
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string? RoomImage { get; set; }
    public string Status { get; set; } = "AVAILABLE"; // AVAILABLE, CLEANING, MAINTENANCE
    public List<SlotAvailabilityDto> Slots { get; set; } = new();
}

public class SlotAvailabilityDto
{
    public Guid SlotId { get; set; }
    public string SlotName { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
    public string? BookingId { get; set; }
}
