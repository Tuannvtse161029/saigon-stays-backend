using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaigonStays.API.Common;
using SaigonStays.API.Data;
using SaigonStays.API.DTOs;
using SaigonStays.API.Models;
using SaigonStays.API.Services;
using System.Security.Claims;

namespace SaigonStays.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ApplicationDbContext db,
        IJwtService jwtService,
        IConfiguration config,
        ILogger<AuthController> logger)
    {
        _db = db;
        _jwtService = jwtService;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate with Google ID Token (Google Identity Services)
    /// </summary>
    [HttpPost("google-login")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> GoogleLogin([FromBody] GoogleLoginRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.IdToken))
        {
            return BadRequest(ApiResponse<LoginResponseDto>.Fail("IdToken không được để trống."));
        }

        try
        {
            var googleClientId = _config["GoogleAuth:ClientId"];
            GoogleJsonWebSignature.Payload payload;

            // Validate Google Token
            if (!string.IsNullOrEmpty(googleClientId) && !googleClientId.StartsWith("YOUR_"))
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { googleClientId }
                };
                payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken, settings);
            }
            else
            {
                // Fallback for development/testing if Google Client ID is not yet configured in production
                payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken);
            }

            if (payload == null || string.IsNullOrEmpty(payload.Email))
            {
                return BadRequest(ApiResponse<LoginResponseDto>.Fail("Google Token không hợp lệ."));
            }

            var email = payload.Email.ToLowerInvariant();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    GoogleId = payload.Subject,
                    Email = email,
                    FullName = payload.Name ?? email.Split('@')[0],
                    AvatarUrl = payload.Picture,
                    Role = Constants.Roles.Guest,
                    CreatedAt = DateTime.UtcNow
                };
                await _db.Users.AddAsync(user);
            }
            else
            {
                // Update Google ID & Avatar if needed
                user.GoogleId = payload.Subject;
                if (!string.IsNullOrEmpty(payload.Picture)) user.AvatarUrl = payload.Picture;
                if (string.IsNullOrEmpty(user.FullName) && !string.IsNullOrEmpty(payload.Name)) user.FullName = payload.Name;
            }

            await _db.SaveChangesAsync();

            var token = _jwtService.GenerateToken(user);
            var expiresAt = _jwtService.GetExpirationDate();

            var response = new LoginResponseDto
            {
                Token = token,
                ExpiresAt = expiresAt,
                User = MapUserDto(user)
            };

            return Ok(ApiResponse<LoginResponseDto>.Ok(response, "Đăng nhập Google thành công."));
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogError(ex, "Google token validation failed.");
            return BadRequest(ApiResponse<LoginResponseDto>.Fail($"Google token không hợp lệ hoặc đã hết hạn: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Google login.");
            return StatusCode(500, ApiResponse<LoginResponseDto>.Fail($"Lỗi xác thực Google: {ex.Message}"));
        }
    }

    /// <summary>
    /// Dev/Test Login endpoint for easy local testing without external Google Client ID
    /// </summary>
    [HttpPost("dev-login")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> DevLogin([FromBody] DevLoginRequestDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                FullName = dto.FullName ?? email.Split('@')[0],
                Role = !string.IsNullOrEmpty(dto.Role) ? dto.Role.ToUpper() : Constants.Roles.Guest,
                CreatedAt = DateTime.UtcNow
            };
            await _db.Users.AddAsync(user);
            await _db.SaveChangesAsync();
        }

        var token = _jwtService.GenerateToken(user);
        var expiresAt = _jwtService.GetExpirationDate();

        var response = new LoginResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = MapUserDto(user)
        };

        return Ok(ApiResponse<LoginResponseDto>.Ok(response, "Đăng nhập thành công (Dev Mode)."));
    }

    /// <summary>
    /// Get current logged-in user profile
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetMe()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(ApiResponse<UserDto>.Fail("Không xác định được danh tính người dùng."));
        }

        var user = await _db.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(ApiResponse<UserDto>.Fail("Không tìm thấy thông tin người dùng."));
        }

        return Ok(ApiResponse<UserDto>.Ok(MapUserDto(user)));
    }

    /// <summary>
    /// Update current logged-in user profile
    /// </summary>
    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(ApiResponse<UserDto>.Fail("Không xác định được danh tính người dùng."));
        }

        var user = await _db.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(ApiResponse<UserDto>.Fail("Không tìm thấy thông tin người dùng."));
        }

        user.FullName = dto.FullName.Trim();
        if (!string.IsNullOrEmpty(dto.PhoneNumber)) user.PhoneNumber = dto.PhoneNumber.Trim();
        if (!string.IsNullOrEmpty(dto.AvatarUrl)) user.AvatarUrl = dto.AvatarUrl.Trim();

        await _db.SaveChangesAsync();

        return Ok(ApiResponse<UserDto>.Ok(MapUserDto(user), "Cập nhật hồ sơ thành công."));
    }

    private static UserDto MapUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            GoogleId = user.GoogleId,
            Email = user.Email,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            AvatarUrl = user.AvatarUrl,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };
    }
}
