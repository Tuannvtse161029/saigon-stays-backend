using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaigonStays.API.Models;

public class PricingSlot
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid RoomId { get; set; }
    
    [Required, MaxLength(50)]
    public string SlotName { get; set; } = string.Empty; // VD: 11:30 - 14:30, 15:00 - 18:00, Qua đêm
    
    public TimeSpan StartTime { get; set; }
    
    public TimeSpan EndTime { get; set; }
    
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Price { get; set; }

    [ForeignKey("RoomId")]
    public Room? Room { get; set; }
    
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
