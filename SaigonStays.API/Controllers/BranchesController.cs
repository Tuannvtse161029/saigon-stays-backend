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
public class BranchesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<BranchesController> _logger;

    public BranchesController(ApplicationDbContext db, ILogger<BranchesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Get list of active homestay branches
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<BranchDto>>>> GetBranches([FromQuery] bool includeInactive = false)
    {
        var query = _db.Branches.AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(b => b.IsActive);
        }

        var branches = await query
            .Include(b => b.Rooms)
            .OrderBy(b => b.Name)
            .Select(b => new BranchDto
            {
                Id = b.Id,
                Name = b.Name,
                Address = b.Address,
                Hotline = b.Hotline,
                IsActive = b.IsActive,
                TotalRooms = b.Rooms.Count
            })
            .ToListAsync();

        return Ok(ApiResponse<List<BranchDto>>.Ok(branches));
    }

    /// <summary>
    /// Get details of a branch with all its rooms and pricing slots
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<BranchDetailDto>>> GetBranchById(Guid id)
    {
        var branch = await _db.Branches
            .Include(b => b.Rooms)
                .ThenInclude(r => r.PricingSlots)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (branch == null)
        {
            return NotFound(ApiResponse<BranchDetailDto>.Fail("Không tìm thấy chi nhánh."));
        }

        var detail = new BranchDetailDto
        {
            Id = branch.Id,
            Name = branch.Name,
            Address = branch.Address,
            Hotline = branch.Hotline,
            IsActive = branch.IsActive,
            Rooms = branch.Rooms.Select(r => new RoomDto
            {
                Id = r.Id,
                BranchId = r.BranchId,
                BranchName = branch.Name,
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
            }).ToList()
        };

        return Ok(ApiResponse<BranchDetailDto>.Ok(detail));
    }

    /// <summary>
    /// Create new branch (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = $"{Constants.Roles.Admin},{Constants.Roles.Staff}")]
    public async Task<ActionResult<ApiResponse<BranchDto>>> CreateBranch([FromBody] CreateBranchDto dto)
    {
        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Address = dto.Address.Trim(),
            Hotline = dto.Hotline?.Trim(),
            IsActive = dto.IsActive
        };

        await _db.Branches.AddAsync(branch);
        await _db.SaveChangesAsync();

        var result = new BranchDto
        {
            Id = branch.Id,
            Name = branch.Name,
            Address = branch.Address,
            Hotline = branch.Hotline,
            IsActive = branch.IsActive,
            TotalRooms = 0
        };

        return CreatedAtAction(nameof(GetBranchById), new { id = branch.Id }, ApiResponse<BranchDto>.Ok(result, "Tạo chi nhánh mới thành công."));
    }

    /// <summary>
    /// Update branch (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = $"{Constants.Roles.Admin},{Constants.Roles.Staff}")]
    public async Task<ActionResult<ApiResponse<BranchDto>>> UpdateBranch(Guid id, [FromBody] UpdateBranchDto dto)
    {
        var branch = await _db.Branches.FindAsync(id);
        if (branch == null)
        {
            return NotFound(ApiResponse<BranchDto>.Fail("Không tìm thấy chi nhánh."));
        }

        branch.Name = dto.Name.Trim();
        branch.Address = dto.Address.Trim();
        branch.Hotline = dto.Hotline?.Trim();
        branch.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();

        var result = new BranchDto
        {
            Id = branch.Id,
            Name = branch.Name,
            Address = branch.Address,
            Hotline = branch.Hotline,
            IsActive = branch.IsActive
        };

        return Ok(ApiResponse<BranchDto>.Ok(result, "Cập nhật chi nhánh thành công."));
    }

    /// <summary>
    /// Delete or deactivate branch (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = Constants.Roles.Admin)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteBranch(Guid id)
    {
        var branch = await _db.Branches
            .Include(b => b.Rooms)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (branch == null)
        {
            return NotFound(ApiResponse<bool>.Fail("Không tìm thấy chi nhánh."));
        }

        // Soft delete / deactivate
        branch.IsActive = false;
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<bool>.Ok(true, "Đã ngưng hoạt động chi nhánh thành công."));
    }
}
