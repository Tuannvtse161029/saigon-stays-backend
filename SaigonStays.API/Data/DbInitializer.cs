using Microsoft.EntityFrameworkCore;
using SaigonStays.API.Common;
using SaigonStays.API.Models;

namespace SaigonStays.API.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Check if database has been seeded
        if (await context.Branches.AnyAsync())
        {
            return;
        }

        // Seed Admin & Staff Users
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@saigonstays.vn",
            FullName = "Saigon Stays Admin",
            PhoneNumber = "0901234567",
            Role = Constants.Roles.Admin,
            CreatedAt = DateTime.UtcNow
        };

        var staffUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "staff@saigonstays.vn",
            FullName = "Saigon Stays Reception",
            PhoneNumber = "0909876543",
            Role = Constants.Roles.Staff,
            CreatedAt = DateTime.UtcNow
        };

        await context.Users.AddRangeAsync(adminUser, staffUser);

        // Branch 1: District 1
        var branchD1 = new Branch
        {
            Id = Guid.NewGuid(),
            Name = "Saigon Stays - Ben Thanh Boutique",
            Address = "128/4 Le Lai, Phuong Ben Thanh, Quan 1, TP. Ho Chi Minh",
            Hotline = "0908889991",
            IsActive = true
        };

        // Branch 2: Thao Dien
        var branchTD = new Branch
        {
            Id = Guid.NewGuid(),
            Name = "Saigon Stays - Thao Dien Riverview Retreat",
            Address = "45/2 Duong Xuan Thuy, Phuong Thao Dien, TP. Thu Duc, TP. Ho Chi Minh",
            Hotline = "0908889992",
            IsActive = true
        };

        await context.Branches.AddRangeAsync(branchD1, branchTD);

        // Rooms for Branch D1
        var room101 = new Room
        {
            Id = Guid.NewGuid(),
            BranchId = branchD1.Id,
            Name = "Studio Cozy 101 - Bancong City View",
            Description = "Phong studio am cung, co ban cong ngam pho Tay Bui Vien va cho Ben Thanh. Trang bi Smart TV 4K, giuong King Size, bep mini tu nau nuong.",
            ImageUrl = "https://images.unsplash.com/photo-1522771739844-6a9f6d5f14af?w=800&auto=format&fit=crop&q=60",
            Status = Constants.RoomStatus.Available
        };

        var room102 = new Room
        {
            Id = Guid.NewGuid(),
            BranchId = branchD1.Id,
            Name = "Deluxe Suite 102 - Phong Cach Vintage",
            Description = "Can ho phong cach Dong Duong Indochine, bon tam go ngam hoa thom, may chieu phim Netflix rieng biet.",
            ImageUrl = "https://images.unsplash.com/photo-1598928506311-c55ded91a20c?w=800&auto=format&fit=crop&q=60",
            Status = Constants.RoomStatus.Available
        };

        // Rooms for Branch Thao Dien
        var room201 = new Room
        {
            Id = Guid.NewGuid(),
            BranchId = branchTD.Id,
            Name = "Garden Villa 201 - View Song Sai Gon",
            Description = "Villa mini phong cach Scandinavian, san vuon thoang dang ngam song Sai Gon, khu BBQ ngoai troi.",
            ImageUrl = "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?w=800&auto=format&fit=crop&q=60",
            Status = Constants.RoomStatus.Available
        };

        var room202 = new Room
        {
            Id = Guid.NewGuid(),
            BranchId = branchTD.Id,
            Name = "Penthouse Sky Oasis 202",
            Description = "Can penthouse cao cap voi be boi vo cuc tren mai, phong tap gym mini va bep tieu chuan 5 sao.",
            ImageUrl = "https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?w=800&auto=format&fit=crop&q=60",
            Status = Constants.RoomStatus.Available
        };

        await context.Rooms.AddRangeAsync(room101, room102, room201, room202);

        // Check-in info
        var checkin101 = new RoomCheckinInfo
        {
            Id = Guid.NewGuid(),
            RoomId = room101.Id,
            DoorPasscode = "101888#",
            WifiSSID = "SaigonStays_BT_101",
            WifiPassword = "saigonstays2026"
        };

        var checkin102 = new RoomCheckinInfo
        {
            Id = Guid.NewGuid(),
            RoomId = room102.Id,
            DoorPasscode = "102999#",
            WifiSSID = "SaigonStays_BT_102",
            WifiPassword = "saigonstays2026"
        };

        var checkin201 = new RoomCheckinInfo
        {
            Id = Guid.NewGuid(),
            RoomId = room201.Id,
            DoorPasscode = "201777#",
            WifiSSID = "SaigonStays_TD_201",
            WifiPassword = "riverviewsaigon"
        };

        var checkin202 = new RoomCheckinInfo
        {
            Id = Guid.NewGuid(),
            RoomId = room202.Id,
            DoorPasscode = "202666#",
            WifiSSID = "SaigonStays_TD_202",
            WifiPassword = "riverviewsaigon"
        };

        await context.RoomCheckinInfos.AddRangeAsync(checkin101, checkin102, checkin201, checkin202);

        // Pricing Slots for Room 101
        var slotsRoom101 = new List<PricingSlot>
        {
            new() { Id = Guid.NewGuid(), RoomId = room101.Id, SlotName = "Slot Trưa (11:30 - 14:30)", StartTime = new TimeSpan(11, 30, 0), EndTime = new TimeSpan(14, 30, 0), Price = 250000 },
            new() { Id = Guid.NewGuid(), RoomId = room101.Id, SlotName = "Slot Chiều (15:00 - 19:00)", StartTime = new TimeSpan(15, 0, 0), EndTime = new TimeSpan(19, 0, 0), Price = 350000 },
            new() { Id = Guid.NewGuid(), RoomId = room101.Id, SlotName = "Slot Qua Đêm (20:00 - 10:00 hôm sau)", StartTime = new TimeSpan(20, 0, 0), EndTime = new TimeSpan(10, 0, 0), Price = 600000 }
        };

        // Pricing Slots for Room 102
        var slotsRoom102 = new List<PricingSlot>
        {
            new() { Id = Guid.NewGuid(), RoomId = room102.Id, SlotName = "Slot Trưa (11:30 - 14:30)", StartTime = new TimeSpan(11, 30, 0), EndTime = new TimeSpan(14, 30, 0), Price = 300000 },
            new() { Id = Guid.NewGuid(), RoomId = room102.Id, SlotName = "Slot Chiều (15:00 - 19:00)", StartTime = new TimeSpan(15, 0, 0), EndTime = new TimeSpan(19, 0, 0), Price = 420000 },
            new() { Id = Guid.NewGuid(), RoomId = room102.Id, SlotName = "Slot Qua Đêm (20:00 - 10:00 hôm sau)", StartTime = new TimeSpan(20, 0, 0), EndTime = new TimeSpan(10, 0, 0), Price = 750000 }
        };

        // Pricing Slots for Room 201
        var slotsRoom201 = new List<PricingSlot>
        {
            new() { Id = Guid.NewGuid(), RoomId = room201.Id, SlotName = "Slot Trưa (11:30 - 14:30)", StartTime = new TimeSpan(11, 30, 0), EndTime = new TimeSpan(14, 30, 0), Price = 350000 },
            new() { Id = Guid.NewGuid(), RoomId = room201.Id, SlotName = "Slot Chiều (15:00 - 19:00)", StartTime = new TimeSpan(15, 0, 0), EndTime = new TimeSpan(19, 0, 0), Price = 500000 },
            new() { Id = Guid.NewGuid(), RoomId = room201.Id, SlotName = "Slot Qua Đêm (20:00 - 10:00 hôm sau)", StartTime = new TimeSpan(20, 0, 0), EndTime = new TimeSpan(10, 0, 0), Price = 950000 }
        };

        // Pricing Slots for Room 202
        var slotsRoom202 = new List<PricingSlot>
        {
            new() { Id = Guid.NewGuid(), RoomId = room202.Id, SlotName = "Slot Trưa (11:30 - 14:30)", StartTime = new TimeSpan(11, 30, 0), EndTime = new TimeSpan(14, 30, 0), Price = 500000 },
            new() { Id = Guid.NewGuid(), RoomId = room202.Id, SlotName = "Slot Chiều (15:00 - 19:00)", StartTime = new TimeSpan(15, 0, 0), EndTime = new TimeSpan(19, 0, 0), Price = 750000 },
            new() { Id = Guid.NewGuid(), RoomId = room202.Id, SlotName = "Slot Qua Đêm (20:00 - 10:00 hôm sau)", StartTime = new TimeSpan(20, 0, 0), EndTime = new TimeSpan(10, 0, 0), Price = 1350000 }
        };

        await context.PricingSlots.AddRangeAsync(slotsRoom101);
        await context.PricingSlots.AddRangeAsync(slotsRoom102);
        await context.PricingSlots.AddRangeAsync(slotsRoom201);
        await context.PricingSlots.AddRangeAsync(slotsRoom202);

        // Sample Booking
        var sampleBooking = new Booking
        {
            Id = "BK_9981",
            UserId = adminUser.Id,
            RoomId = room101.Id,
            PricingSlotId = slotsRoom101[0].Id,
            GuestName = "Nguyen Van A",
            GuestPhone = "0912345678",
            GuestEmail = "guest.demo@gmail.com",
            BookingDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            TotalPrice = slotsRoom101[0].Price,
            Status = Constants.BookingStatus.Paid,
            CreatedAt = DateTime.UtcNow
        };

        await context.Bookings.AddAsync(sampleBooking);

        await context.SaveChangesAsync();
    }
}
