using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaigonStays.API.Common;
using SaigonStays.API.Data;
using SaigonStays.API.DTOs;
using SaigonStays.API.Models;

namespace SaigonStays.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<RoomsController> _logger;

    public RoomsController(ApplicationDbContext db, ILogger<RoomsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Get rooms list, optionally filtered by Branch ID
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<RoomDto>>>> GetRooms([FromQuery] Guid? branchId, [FromQuery] string? status)
    {
        var query = _db.Rooms
            .Include(r => r.Branch)
            .Include(r => r.PricingSlots)
            .AsQueryable();

        if (branchId.HasValue)
        {
            query = query.Where(r => r.BranchId == branchId.Value);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(r => r.Status == status);
        }

        var rooms = await query
            .OrderBy(r => r.Name)
            .Select(r => new RoomDto
            {
                Id = r.Id,
                BranchId = r.BranchId,
                BranchName = r.Branch != null ? r.Branch.Name : string.Empty,
                Name = r.Name,
                Description = r.Description,
                ImageUrl = r.ImageUrl,
                Status = r.Status,
                MinPrice = r.PricingSlots.Any() ? r.PricingSlots.Min(s => s.Price) : 0,
                PricingSlots = r.PricingSlots.Select(s => new PricingSlotDto
                {
                    Id = s.Id,
                    RoomId = s.RoomId,
                    SlotName = s.SlotName,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    Price = s.Price
                }).ToList()
            })
            .ToListAsync();

        return Ok(ApiResponse<List<RoomDto>>.Ok(rooms));
    }

    /// <summary>
    /// Get detailed room information
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<RoomDetailDto>>> GetRoomById(Guid id)
    {
        var room = await _db.Rooms
            .Include(r => r.Branch)
            .Include(r => r.PricingSlots)
            .Include(r => r.CheckinInfo)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (room == null)
        {
            return NotFound(ApiResponse<RoomDetailDto>.Fail("Không tìm thấy phòng."));
        }

        // Only include check-in info if caller is Admin/Staff
        var isStaffOrAdmin = User.IsInRole(Constants.Roles.Admin) || User.IsInRole(Constants.Roles.Staff);

        var dto = new RoomDetailDto
        {
            Id = room.Id,
            BranchId = room.BranchId,
            BranchName = room.Branch?.Name ?? string.Empty,
            BranchAddress = room.Branch?.Address ?? string.Empty,
            BranchHotline = room.Branch?.Hotline,
            Name = room.Name,
            Description = room.Description,
            ImageUrl = room.ImageUrl,
            Status = room.Status,
            PricingSlots = room.PricingSlots.Select(s => new PricingSlotDto
            {
                Id = s.Id,
                RoomId = s.RoomId,
                SlotName = s.SlotName,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Price = s.Price
            }).ToList(),
            CheckinInfo = (isStaffOrAdmin && room.CheckinInfo != null) ? new RoomCheckinInfoDto
            {
                Id = room.CheckinInfo.Id,
                RoomId = room.CheckinInfo.RoomId,
                DoorPasscode = room.CheckinInfo.DoorPasscode,
                WifiSSID = room.CheckinInfo.WifiSSID,
                WifiPassword = room.CheckinInfo.WifiPassword
            } : null
        };

        return Ok(ApiResponse<RoomDetailDto>.Ok(dto));
    }

    /// <summary>
    /// Create new room (Admin/Staff only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = $"{Constants.Roles.Admin},{Constants.Roles.Staff}")]
    public async Task<ActionResult<ApiResponse<RoomDto>>> CreateRoom([FromBody] CreateRoomDto dto)
    {
        var branch = await _db.Branches.FindAsync(dto.BranchId);
        if (branch == null)
        {
            return BadRequest(ApiResponse<RoomDto>.Fail("Chi nhánh không tồn tại."));
        }

        var room = new Room
        {
            Id = Guid.NewGuid(),
            BranchId = dto.BranchId,
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            ImageUrl = dto.ImageUrl?.Trim(),
            Status = !string.IsNullOrEmpty(dto.Status) ? dto.Status : Constants.RoomStatus.Available
        };

        await _db.Rooms.AddAsync(room);

        // Checkin info
        if (dto.CheckinInfo != null)
        {
            var checkinInfo = new RoomCheckinInfo
            {
                Id = Guid.NewGuid(),
                RoomId = room.Id,
                DoorPasscode = dto.CheckinInfo.DoorPasscode.Trim(),
                WifiSSID = dto.CheckinInfo.WifiSSID?.Trim(),
                WifiPassword = dto.CheckinInfo.WifiPassword?.Trim()
            };
            await _db.RoomCheckinInfos.AddAsync(checkinInfo);
        }

        // Pricing slots
        if (dto.PricingSlots != null && dto.PricingSlots.Any())
        {
            foreach (var slotDto in dto.PricingSlots)
            {
                var slot = new PricingSlot
                {
                    Id = Guid.NewGuid(),
                    RoomId = room.Id,
                    SlotName = slotDto.SlotName.Trim(),
                    StartTime = slotDto.StartTime,
                    EndTime = slotDto.EndTime,
                    Price = slotDto.Price
                };
                await _db.PricingSlots.AddAsync(slot);
            }
        }

        await _db.SaveChangesAsync();

        var createdDto = new RoomDto
        {
            Id = room.Id,
            BranchId = room.BranchId,
            BranchName = branch.Name,
            Name = room.Name,
            Description = room.Description,
            ImageUrl = room.ImageUrl,
            Status = room.Status
        };

        return CreatedAtAction(nameof(GetRoomById), new { id = room.Id }, ApiResponse<RoomDto>.Ok(createdDto, "Tạo phòng mới thành công."));
    }

    /// <summary>
    /// Update room info (Admin/Staff only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = $"{Constants.Roles.Admin},{Constants.Roles.Staff}")]
    public async Task<ActionResult<ApiResponse<RoomDto>>> UpdateRoom(Guid id, [FromBody] UpdateRoomDto dto)
    {
        var room = await _db.Rooms
            .Include(r => r.Branch)
            .Include(r => r.CheckinInfo)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (room == null)
        {
            return NotFound(ApiResponse<RoomDto>.Fail("Không tìm thấy phòng."));
        }

        room.Name = dto.Name.Trim();
        room.Description = dto.Description?.Trim();
        room.ImageUrl = dto.ImageUrl?.Trim();
        room.Status = dto.Status;

        if (dto.CheckinInfo != null)
        {
            if (room.CheckinInfo == null)
            {
                room.CheckinInfo = new RoomCheckinInfo
                {
                    Id = Guid.NewGuid(),
                    RoomId = room.Id,
                    DoorPasscode = dto.CheckinInfo.DoorPasscode.Trim(),
                    WifiSSID = dto.CheckinInfo.WifiSSID?.Trim(),
                    WifiPassword = dto.CheckinInfo.WifiPassword?.Trim()
                };
                await _db.RoomCheckinInfos.AddAsync(room.CheckinInfo);
            }
            else
            {
                room.CheckinInfo.DoorPasscode = dto.CheckinInfo.DoorPasscode.Trim();
                room.CheckinInfo.WifiSSID = dto.CheckinInfo.WifiSSID?.Trim();
                room.CheckinInfo.WifiPassword = dto.CheckinInfo.WifiPassword?.Trim();
            }
        }

        await _db.SaveChangesAsync();

        var result = new RoomDto
        {
            Id = room.Id,
            BranchId = room.BranchId,
            BranchName = room.Branch?.Name ?? string.Empty,
            Name = room.Name,
            Description = room.Description,
            ImageUrl = room.ImageUrl,
            Status = room.Status
        };

        return Ok(ApiResponse<RoomDto>.Ok(result, "Cập nhật thông tin phòng thành công."));
    }

    /// <summary>
    /// Add pricing slot for a room (Admin/Staff only)
    /// </summary>
    [HttpPost("{id}/slots")]
    [Authorize(Roles = $"{Constants.Roles.Admin},{Constants.Roles.Staff}")]
    public async Task<ActionResult<ApiResponse<PricingSlotDto>>> AddPricingSlot(Guid id, [FromBody] CreatePricingSlotDto dto)
    {
        var room = await _db.Rooms.FindAsync(id);
        if (room == null)
        {
            return NotFound(ApiResponse<PricingSlotDto>.Fail("Không tìm thấy phòng."));
        }

        var slot = new PricingSlot
        {
            Id = Guid.NewGuid(),
            RoomId = id,
            SlotName = dto.SlotName.Trim(),
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Price = dto.Price
        };

        await _db.PricingSlots.AddAsync(slot);
        await _db.SaveChangesAsync();

        var slotDto = new PricingSlotDto
        {
            Id = slot.Id,
            RoomId = slot.RoomId,
            SlotName = slot.SlotName,
            StartTime = slot.StartTime,
            EndTime = slot.EndTime,
            Price = slot.Price
        };

        return Ok(ApiResponse<PricingSlotDto>.Ok(slotDto, "Thêm khung giờ giá thành công."));
    }

    /// <summary>
    /// Delete pricing slot (Admin/Staff only)
    /// </summary>
    [HttpDelete("{roomId}/slots/{slotId}")]
    [Authorize(Roles = $"{Constants.Roles.Admin},{Constants.Roles.Staff}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeletePricingSlot(Guid roomId, Guid slotId)
    {
        var slot = await _db.PricingSlots.FirstOrDefaultAsync(s => s.Id == slotId && s.RoomId == roomId);
        if (slot == null)
        {
            return NotFound(ApiResponse<bool>.Fail("Không tìm thấy khung giờ."));
        }

        _db.PricingSlots.Remove(slot);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<bool>.Ok(true, "Xoá khung giờ thành công."));
    }
}
