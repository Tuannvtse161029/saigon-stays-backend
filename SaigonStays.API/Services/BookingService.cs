using Hangfire;
using Microsoft.EntityFrameworkCore;
using SaigonStays.API.Common;
using SaigonStays.API.Data;
using SaigonStays.API.DTOs;
using SaigonStays.API.Models;

namespace SaigonStays.API.Services;

public class BookingService : IBookingService
{
    private readonly ApplicationDbContext _db;
    private readonly IVietQrService _vietQrService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        ApplicationDbContext db,
        IVietQrService vietQrService,
        IBackgroundJobClient backgroundJobClient,
        ILogger<BookingService> logger)
    {
        _db = db;
        _vietQrService = vietQrService;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    public async Task<BookingResponseDto> CreateBookingAsync(CreateBookingRequestDto dto)
    {
        // 1. Validate Room
        var room = await _db.Rooms
            .Include(r => r.Branch)
            .Include(r => r.CheckinInfo)
            .FirstOrDefaultAsync(r => r.Id == dto.RoomId);

        if (room == null)
        {
            throw new InvalidOperationException("Phòng không tồn tại trong hệ thống.");
        }

        if (room.Status != Constants.RoomStatus.Available)
        {
            throw new InvalidOperationException($"Phòng hiện đang ở trạng thái {room.Status}, không thể đặt lúc này.");
        }

        // 2. Validate Pricing Slot
        var slot = await _db.PricingSlots
            .FirstOrDefaultAsync(s => s.Id == dto.PricingSlotId && s.RoomId == dto.RoomId);

        if (slot == null)
        {
            throw new InvalidOperationException("Khung giờ (Pricing Slot) không hợp lệ cho phòng này.");
        }

        // 3. Check conflict (Slot already booked on this date)
        var hasConflict = await _db.Bookings.AnyAsync(b =>
            b.RoomId == dto.RoomId &&
            b.PricingSlotId == dto.PricingSlotId &&
            b.BookingDate == dto.BookingDate &&
            b.Status != Constants.BookingStatus.Cancelled);

        if (hasConflict)
        {
            throw new InvalidOperationException($"Khung giờ '{slot.SlotName}' ngày {dto.BookingDate:dd/MM/yyyy} đã có khách đặt trước.");
        }

        // 4. Generate unique Booking ID (BK_XXXX)
        string bookingId;
        var random = new Random();
        do
        {
            var code = random.Next(1000, 9999);
            bookingId = $"BK_{code}";
        } while (await _db.Bookings.AnyAsync(b => b.Id == bookingId));

        // 5. Create Booking Entity
        var booking = new Booking
        {
            Id = bookingId,
            UserId = dto.UserId,
            RoomId = dto.RoomId,
            PricingSlotId = dto.PricingSlotId,
            GuestName = dto.GuestName.Trim(),
            GuestPhone = dto.GuestPhone.Trim(),
            GuestEmail = dto.GuestEmail.Trim().ToLowerInvariant(),
            BookingDate = dto.BookingDate,
            TotalPrice = slot.Price,
            Status = Constants.BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Room = room,
            PricingSlot = slot
        };

        await _db.Bookings.AddAsync(booking);
        await _db.SaveChangesAsync();

        var qrResult = _vietQrService.GenerateQr(booking);

        return MapToDto(booking, room, slot, qrResult.QrImageUrl, includeCheckinInfo: false);
    }

    public async Task<BookingResponseDto?> GetBookingByIdAsync(string id, bool includeCheckinInfo = false)
    {
        var booking = await _db.Bookings
            .Include(b => b.Room)
                .ThenInclude(r => r!.Branch)
            .Include(b => b.Room)
                .ThenInclude(r => r!.CheckinInfo)
            .Include(b => b.PricingSlot)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null) return null;

        var qrResult = _vietQrService.GenerateQr(booking);
        var canShowCheckin = includeCheckinInfo || booking.Status == Constants.BookingStatus.Paid || booking.Status == Constants.BookingStatus.CheckedIn;

        return MapToDto(booking, booking.Room!, booking.PricingSlot!, qrResult.QrImageUrl, canShowCheckin);
    }

    public async Task<List<BookingResponseDto>> GetBookingsAsync(BookingListFilterDto filter)
    {
        var query = _db.Bookings
            .Include(b => b.Room)
                .ThenInclude(r => r!.Branch)
            .Include(b => b.Room)
                .ThenInclude(r => r!.CheckinInfo)
            .Include(b => b.PricingSlot)
            .AsQueryable();

        if (filter.BranchId.HasValue)
        {
            query = query.Where(b => b.Room!.BranchId == filter.BranchId.Value);
        }

        if (filter.RoomId.HasValue)
        {
            query = query.Where(b => b.RoomId == filter.RoomId.Value);
        }

        if (!string.IsNullOrEmpty(filter.Status))
        {
            query = query.Where(b => b.Status == filter.Status);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(b => b.BookingDate >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(b => b.BookingDate <= filter.ToDate.Value);
        }

        if (!string.IsNullOrEmpty(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(b =>
                b.Id.ToLower().Contains(search) ||
                b.GuestName.ToLower().Contains(search) ||
                b.GuestPhone.ToLower().Contains(search) ||
                b.GuestEmail.ToLower().Contains(search));
        }

        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return items.Select(b =>
        {
            var qr = _vietQrService.GenerateQr(b);
            var isPaid = b.Status == Constants.BookingStatus.Paid || b.Status == Constants.BookingStatus.CheckedIn;
            return MapToDto(b, b.Room!, b.PricingSlot!, qr.QrImageUrl, isPaid);
        }).ToList();
    }

    public async Task<bool> ProcessPaymentSuccessAsync(string bookingId)
    {
        var booking = await _db.Bookings
            .Include(b => b.Room)
                .ThenInclude(r => r!.Branch)
            .Include(b => b.PricingSlot)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
        {
            _logger.LogWarning("Booking with ID {BookingId} not found during payment processing.", bookingId);
            return false;
        }

        if (booking.Status == Constants.BookingStatus.Paid || booking.Status == Constants.BookingStatus.CheckedIn)
        {
            _logger.LogInformation("Booking {BookingId} is already marked as {Status}.", bookingId, booking.Status);
            return true;
        }

        var checkinInfo = await _db.RoomCheckinInfos
            .FirstOrDefaultAsync(c => c.RoomId == booking.RoomId);

        booking.Status = Constants.BookingStatus.Paid;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Booking {BookingId} updated to PAID. Enqueuing email job via Hangfire.", bookingId);

        // Enqueue background email job
        _backgroundJobClient.Enqueue<IEmailService>(x =>
            x.SendCheckinEmailAsync(booking.GuestEmail, booking, checkinInfo));

        return true;
    }

    public async Task<AvailabilityCalendarResponseDto> GetAvailabilityCalendarAsync(Guid branchId, DateOnly date)
    {
        var branch = await _db.Branches
            .Include(b => b.Rooms)
                .ThenInclude(r => r.PricingSlots)
            .FirstOrDefaultAsync(b => b.Id == branchId && b.IsActive);

        if (branch == null)
        {
            throw new InvalidOperationException("Chi nhánh không tồn tại hoặc đã ngừng hoạt động.");
        }

        // Get all active bookings for this branch on the given date
        var roomIds = branch.Rooms.Select(r => r.Id).ToList();
        var activeBookings = await _db.Bookings
            .Where(b => roomIds.Contains(b.RoomId) &&
                        b.BookingDate == date &&
                        b.Status != Constants.BookingStatus.Cancelled)
            .ToListAsync();

        var response = new AvailabilityCalendarResponseDto
        {
            BranchId = branch.Id,
            BranchName = branch.Name,
            Date = date,
            Rooms = branch.Rooms.Select(room =>
            {
                var roomBookings = activeBookings.Where(b => b.RoomId == room.Id).ToList();

                return new RoomAvailabilityDto
                {
                    RoomId = room.Id,
                    RoomName = room.Name,
                    RoomImage = room.ImageUrl,
                    Status = room.Status,
                    Slots = room.PricingSlots.OrderBy(s => s.StartTime).Select(slot =>
                    {
                        var matchingBooking = roomBookings.FirstOrDefault(b => b.PricingSlotId == slot.Id);
                        var isAvailable = room.Status == Constants.RoomStatus.Available && matchingBooking == null;

                        return new SlotAvailabilityDto
                        {
                            SlotId = slot.Id,
                            SlotName = slot.SlotName,
                            StartTime = slot.StartTime,
                            EndTime = slot.EndTime,
                            Price = slot.Price,
                            IsAvailable = isAvailable,
                            BookingId = matchingBooking?.Id
                        };
                    }).ToList()
                };
            }).ToList()
        };

        return response;
    }

    public async Task<bool> CancelBookingAsync(string bookingId, string? reason = null)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking == null) return false;

        booking.Status = Constants.BookingStatus.Cancelled;
        await _db.SaveChangesAsync();
        return true;
    }

    private static BookingResponseDto MapToDto(
        Booking booking,
        Room room,
        PricingSlot slot,
        string? qrCodeUrl,
        bool includeCheckinInfo)
    {
        return new BookingResponseDto
        {
            Id = booking.Id,
            UserId = booking.UserId,
            RoomId = booking.RoomId,
            RoomName = room.Name,
            RoomImage = room.ImageUrl,
            BranchId = room.BranchId,
            BranchName = room.Branch?.Name ?? string.Empty,
            BranchAddress = room.Branch?.Address ?? string.Empty,
            BranchHotline = room.Branch?.Hotline,
            PricingSlotId = booking.PricingSlotId,
            SlotName = slot.SlotName,
            StartTime = slot.StartTime,
            EndTime = slot.EndTime,
            GuestName = booking.GuestName,
            GuestPhone = booking.GuestPhone,
            GuestEmail = booking.GuestEmail,
            BookingDate = booking.BookingDate,
            TotalPrice = booking.TotalPrice,
            Status = booking.Status,
            CreatedAt = booking.CreatedAt,
            VietQrImageUrl = qrCodeUrl,
            CheckinInfo = includeCheckinInfo && room.CheckinInfo != null
                ? new RoomCheckinInfoDto
                {
                    Id = room.CheckinInfo.Id,
                    RoomId = room.CheckinInfo.RoomId,
                    DoorPasscode = room.CheckinInfo.DoorPasscode,
                    WifiSSID = room.CheckinInfo.WifiSSID,
                    WifiPassword = room.CheckinInfo.WifiPassword
                }
                : null
        };
    }
}
