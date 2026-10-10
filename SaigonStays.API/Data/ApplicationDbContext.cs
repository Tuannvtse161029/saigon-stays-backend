using Microsoft.EntityFrameworkCore;
using SaigonStays.API.Models;

namespace SaigonStays.API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<PricingSlot> PricingSlots => Set<PricingSlot>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<RoomCheckinInfo> RoomCheckinInfos => Set<RoomCheckinInfo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Role).HasDefaultValue("GUEST");
        });

        // Branch
        modelBuilder.Entity<Branch>(entity =>
        {
            entity.Property(b => b.IsActive).HasDefaultValue(true);
            entity.HasMany(b => b.Rooms)
                  .WithOne(r => r.Branch)
                  .HasForeignKey(r => r.BranchId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Room
        modelBuilder.Entity<Room>(entity =>
        {
            entity.Property(r => r.Status).HasDefaultValue("AVAILABLE");
            
            entity.HasOne(r => r.CheckinInfo)
                  .WithOne(c => c.Room)
                  .HasForeignKey<RoomCheckinInfo>(c => c.RoomId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(r => r.PricingSlots)
                  .WithOne(s => s.Room)
                  .HasForeignKey(s => s.RoomId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(r => r.Bookings)
                  .WithOne(b => b.Room)
                  .HasForeignKey(b => b.RoomId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // PricingSlot
        modelBuilder.Entity<PricingSlot>(entity =>
        {
            entity.Property(p => p.Price).HasColumnType("decimal(18, 2)");
            entity.HasMany(p => p.Bookings)
                  .WithOne(b => b.PricingSlot)
                  .HasForeignKey(b => b.PricingSlotId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Booking
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.Property(b => b.TotalPrice).HasColumnType("decimal(18, 2)");
            entity.Property(b => b.Status).HasDefaultValue("PENDING");
            entity.HasIndex(b => b.BookingDate);
            entity.HasIndex(b => b.Status);
            entity.HasIndex(b => new { b.RoomId, b.PricingSlotId, b.BookingDate });
        });
    }
}
