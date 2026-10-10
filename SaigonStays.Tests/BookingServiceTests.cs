using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SaigonStays.API.Common;
using SaigonStays.API.Data;
using SaigonStays.API.DTOs;
using SaigonStays.API.Models;
using SaigonStays.API.Services;
using Xunit;

namespace SaigonStays.Tests;

public class BookingServiceTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateBooking_ShouldSucceed_WhenValidData()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var branch = new Branch { Id = Guid.NewGuid(), Name = "District 1 Branch", Address = "Ben Thanh, Q1" };
        var room = new Room { Id = Guid.NewGuid(), BranchId = branch.Id, Name = "Cozy Studio 101", Status = Constants.RoomStatus.Available };
        var slot = new PricingSlot { Id = Guid.NewGuid(), RoomId = room.Id, SlotName = "11:30 - 14:30", Price = 300000 };
        var checkin = new RoomCheckinInfo { Id = Guid.NewGuid(), RoomId = room.Id, DoorPasscode = "8888#" };

        await db.Branches.AddAsync(branch);
        await db.Rooms.AddAsync(room);
        await db.PricingSlots.AddAsync(slot);
        await db.RoomCheckinInfos.AddAsync(checkin);
        await db.SaveChangesAsync();

        var mockVietQr = new Mock<IVietQrService>();
        mockVietQr.Setup(x => x.GenerateQr(It.IsAny<Booking>()))
                  .Returns(new CreateQrResponseDto { QrImageUrl = "https://img.vietqr.io/test.png" });

        var mockLogger = new Mock<ILogger<BookingService>>();
        var mockJobClient = new Mock<IBackgroundJobClient>();
        var service = new BookingService(db, mockVietQr.Object, mockJobClient.Object, mockLogger.Object);

        var dto = new CreateBookingRequestDto
        {
            RoomId = room.Id,
            PricingSlotId = slot.Id,
            GuestName = "Tran Van B",
            GuestPhone = "0909111222",
            GuestEmail = "tranvanb@gmail.com",
            BookingDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2))
        };

        // Act
        var result = await service.CreateBookingAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("BK_", result.Id);
        Assert.Equal(300000, result.TotalPrice);
        Assert.Equal(Constants.BookingStatus.Pending, result.Status);
        Assert.Equal("Tran Van B", result.GuestName);
        Assert.Equal("https://img.vietqr.io/test.png", result.VietQrImageUrl);
        Assert.Null(result.CheckinInfo); // Passcode not revealed for pending bookings
    }

    [Fact]
    public async Task CreateBooking_ShouldThrowException_WhenSlotAlreadyBookedOnSameDate()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var branch = new Branch { Id = Guid.NewGuid(), Name = "District 1 Branch", Address = "Ben Thanh" };
        var room = new Room { Id = Guid.NewGuid(), BranchId = branch.Id, Name = "Cozy Studio 101", Status = Constants.RoomStatus.Available };
        var slot = new PricingSlot { Id = Guid.NewGuid(), RoomId = room.Id, SlotName = "11:30 - 14:30", Price = 300000 };

        await db.Branches.AddAsync(branch);
        await db.Rooms.AddAsync(room);
        await db.PricingSlots.AddAsync(slot);

        var existingBooking = new Booking
        {
            Id = "BK_1234",
            RoomId = room.Id,
            PricingSlotId = slot.Id,
            GuestName = "Khach A",
            GuestPhone = "0901234567",
            GuestEmail = "khacha@gmail.com",
            BookingDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            TotalPrice = 300000,
            Status = Constants.BookingStatus.Paid
        };
        await db.Bookings.AddAsync(existingBooking);
        await db.SaveChangesAsync();

        var mockVietQr = new Mock<IVietQrService>();
        var mockLogger = new Mock<ILogger<BookingService>>();
        var mockJobClient = new Mock<IBackgroundJobClient>();
        var service = new BookingService(db, mockVietQr.Object, mockJobClient.Object, mockLogger.Object);

        var duplicateDto = new CreateBookingRequestDto
        {
            RoomId = room.Id,
            PricingSlotId = slot.Id,
            GuestName = "Khach B",
            GuestPhone = "0907654321",
            GuestEmail = "khachb@gmail.com",
            BookingDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)) // Same date and slot
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateBookingAsync(duplicateDto));
        Assert.Contains("đã có khách đặt trước", exception.Message);
    }

    [Fact]
    public async Task ProcessPaymentSuccess_ShouldUpdateStatusToPaidAndEnqueueEmail()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var branch = new Branch { Id = Guid.NewGuid(), Name = "District 1 Branch", Address = "Ben Thanh" };
        var room = new Room { Id = Guid.NewGuid(), BranchId = branch.Id, Name = "Cozy Studio 101", Status = Constants.RoomStatus.Available };
        var slot = new PricingSlot { Id = Guid.NewGuid(), RoomId = room.Id, SlotName = "11:30 - 14:30", Price = 300000 };
        var checkin = new RoomCheckinInfo { Id = Guid.NewGuid(), RoomId = room.Id, DoorPasscode = "9999#" };

        await db.Branches.AddAsync(branch);
        await db.Rooms.AddAsync(room);
        await db.PricingSlots.AddAsync(slot);
        await db.RoomCheckinInfos.AddAsync(checkin);

        var booking = new Booking
        {
            Id = "BK_5566",
            RoomId = room.Id,
            PricingSlotId = slot.Id,
            GuestName = "Khach C",
            GuestPhone = "0901234567",
            GuestEmail = "khachc@gmail.com",
            BookingDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            TotalPrice = 300000,
            Status = Constants.BookingStatus.Pending
        };
        await db.Bookings.AddAsync(booking);
        await db.SaveChangesAsync();

        var mockVietQr = new Mock<IVietQrService>();
        var mockLogger = new Mock<ILogger<BookingService>>();
        var mockJobClient = new Mock<IBackgroundJobClient>();
        var service = new BookingService(db, mockVietQr.Object, mockJobClient.Object, mockLogger.Object);

        // Act
        var result = await service.ProcessPaymentSuccessAsync("BK_5566");

        // Assert
        Assert.True(result);
        var updatedBooking = await db.Bookings.FindAsync("BK_5566");
        Assert.NotNull(updatedBooking);
        Assert.Equal(Constants.BookingStatus.Paid, updatedBooking.Status);

        // Verify Hangfire Job was enqueued
        mockJobClient.Verify(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Once);
    }
}
