using System.ComponentModel.DataAnnotations;

namespace SaigonStays.API.DTOs;

public class BranchDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Hotline { get; set; }
    public bool IsActive { get; set; }
    public int TotalRooms { get; set; }
}

public class BranchDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Hotline { get; set; }
    public bool IsActive { get; set; }
    public List<RoomDto> Rooms { get; set; } = new();
}

public class CreateBranchDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Hotline { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateBranchDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Hotline { get; set; }

    public bool IsActive { get; set; } = true;
}
