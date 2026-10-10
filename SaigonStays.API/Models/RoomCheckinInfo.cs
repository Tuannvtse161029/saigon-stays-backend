using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaigonStays.API.Models;

public class RoomCheckinInfo
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid RoomId { get; set; }
    
    [Required, MaxLength(50)]
    public string DoorPasscode { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? WifiSSID { get; set; }
    
    [MaxLength(50)]
    public string? WifiPassword { get; set; }

    [ForeignKey("RoomId")]
    public Room? Room { get; set; }
}
