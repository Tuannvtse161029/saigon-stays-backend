using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaigonStays.API.Models;

public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string? GoogleId { get; set; }
    
    [Required, MaxLength(100)]
    public string Email { get; set; } = string.Empty;
    
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
    
    public string? AvatarUrl { get; set; }
    
    [MaxLength(20)]
    public string Role { get; set; } = "GUEST"; // GUEST, STAFF, ADMIN
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
