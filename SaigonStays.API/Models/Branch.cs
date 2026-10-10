using System.ComponentModel.DataAnnotations;

namespace SaigonStays.API.Models;

public class Branch
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required, MaxLength(255)]
    public string Address { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string? Hotline { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}
