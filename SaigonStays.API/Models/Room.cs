using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaigonStays.API.Models;

public class Room
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid BranchId { get; set; }
    
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public string? ImageUrl { get; set; }
    
    [MaxLength(20)]
    public string Status { get; set; } = "AVAILABLE"; // AVAILABLE, CLEANING, MAINTENANCE

    [ForeignKey("BranchId")]
    public Branch? Branch { get; set; }
    
    public RoomCheckinInfo? CheckinInfo { get; set; }
    
    public ICollection<PricingSlot> PricingSlots { get; set; } = new List<PricingSlot>();
    
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
