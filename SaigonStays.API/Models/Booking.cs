using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaigonStays.API.Models;

public class Booking
{
    [Key, MaxLength(20)]
    public string Id { get; set; } = string.Empty; // Mã đơn: BK_9981
    
    public Guid? UserId { get; set; }
    
    public Guid RoomId { get; set; }
    
    public Guid PricingSlotId { get; set; }
    
    [Required, MaxLength(100)]
    public string GuestName { get; set; } = string.Empty;
    
    [Required, MaxLength(20)]
    public string GuestPhone { get; set; } = string.Empty;
    
    [Required, MaxLength(100)]
    public string GuestEmail { get; set; } = string.Empty;
    
    public DateOnly BookingDate { get; set; }
    
    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalPrice { get; set; }
    
    [MaxLength(20)]
    public string Status { get; set; } = "PENDING"; // PENDING, PAID, CHECKED_IN, CANCELLED
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("UserId")]
    public User? User { get; set; }

    [ForeignKey("RoomId")]
    public Room? Room { get; set; }

    [ForeignKey("PricingSlotId")]
    public PricingSlot? PricingSlot { get; set; }
}
