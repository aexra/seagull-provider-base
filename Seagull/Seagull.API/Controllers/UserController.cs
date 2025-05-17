using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Minio.DataModel.Tags;
using Seagull.API.DTO.auth.Request;
using Seagull.API.DTO.auth.Response;
using Seagull.API.Extensions;
using Seagull.Core.Entities.Identity;
using Seagull.Infrastructure.Hooks;
using Seagull.Infrastructure.Services;
using System.Data;
using System.Security.Claims;

namespace Seagull.API.Controllers;

[Route("api/user")]
[ApiController]
public class UserController(
UserManager<User> userManager,
S3Hook hook,
S3Service s3) : ControllerBase
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly S3Service _s3 = s3;
    private readonly S3Hook _hook = hook;

    private const string _bucketName = "user";

    [HttpGet("s")]
    public async Task<IActionResult> SearchUsers(
        [FromQuery] string? username,
        [FromQuery] string? tag,
        [FromQuery] string? displayname,
        [FromQuery] string? id,
        [FromQuery] string? email,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10
    )
    {
        if ((username == null || username.Length < 3)
            && (tag == null || tag.Length < 3)
            && (displayname == null || displayname.Length < 3)
            && (id == null || id.Length < 5)
            && (email == null || email.Length < 3)) return Ok(new List<string>());

        if (id != null)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();
            return Ok(user);
        }
        if (email != null)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return NotFound();
            return Ok(user);
        }
        if (username != null) return Ok(
            await _userManager.Users
                .Where(u => u.UserName!.ToLower().StartsWith(username.ToLower()))
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync()
        );
        if (tag != null) return Ok(
            await _userManager.Users
                .Where(u => u.Tag.ToLower().StartsWith(tag.ToLower()))
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync()
        );
        if (displayname != null) return Ok(
            await _userManager.Users
                .Where(u => u.DisplayName.ToLower().StartsWith(displayname.ToLower()))
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync()
        );

        return BadRequest();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMyProfile()
    {
        var user = await this.CurrentUserAsync(_userManager);
        if (user == null) return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new UserProfileResponse(
            Id: user.Id,
            Email: user.Email!,
            UserName: user.UserName!,
            DisplayName: user.UserName!,
            Tag: user.Tag,
            AvatarUrl: user.AvatarUrl,
            BannerUrl: user.BannerUrl,
            BannerColor: user.BannerColor,
            Roles: roles
        ));
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetProfile([FromRoute] string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found");

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new UserProfileResponse(
            Id: user.Id,
            Email: user.Email!,
            UserName: user.UserName!,
            DisplayName: user.UserName!,
            Tag: user.Tag,
            AvatarUrl: user.AvatarUrl,
            BannerUrl: user.BannerUrl,
            BannerColor: user.BannerColor,
            Roles: roles
        ));
    }

    [HttpPut]
    [Authorize]
    public async Task<IActionResult> EditProfileSelf([FromBody] EditUserProfileDto dto)
    {
        var user = await this.CurrentUserAsync(_userManager);
        if (user == null) return Unauthorized();

        user.DisplayName = dto.DisplayName;
        user.BannerColor = dto.BannerColor;
        user.Status = dto.Status;

        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new UserProfileResponse(
            Id: user.Id,
            Email: user.Email!,
            UserName: user.UserName!,
            DisplayName: user.UserName!,
            Tag: user.Tag,
            AvatarUrl: user.AvatarUrl,
            BannerUrl: user.BannerUrl,
            BannerColor: user.BannerColor,
            Roles: roles
        ));
    }

    [HttpPut("{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> EditProfile([FromRoute] string userId, [FromBody] EditUserProfileDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized();

        user.DisplayName = dto.DisplayName;
        user.BannerColor = dto.BannerColor;
        user.Status = dto.Status;

        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new UserProfileResponse(
            Id: user.Id,
            Email: user.Email!,
            UserName: user.UserName!,
            DisplayName: user.UserName!,
            Tag: user.Tag,
            AvatarUrl: user.AvatarUrl,
            BannerUrl: user.BannerUrl,
            BannerColor: user.BannerColor,
            Roles: roles
        ));
    }

    #region Avatars

    [HttpPost("avatar")]
    [Authorize]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        var user = await this.CurrentUserAsync(_userManager);
        if (user == null) return Unauthorized();

        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded");
        }

        if (!file.ContentType.StartsWith("image/"))
        {
            return BadRequest("Only image files are allowed");
        }

        if (!string.IsNullOrEmpty(user.AvatarUrl))
        {
            await _s3.DeleteObjectAsync(_bucketName, $"avatar/{user.AvatarUrl}");
        }

        var result = await _hook.UploadAsync(_bucketName, "avatar", file);

        if (result.Success)
        {
            user.AvatarUrl = result.FileName;
            await _userManager.UpdateAsync(user);
            return Ok(user);
        }
        else
        {
            return BadRequest(result.ErrorMessage);
        }
    }

    [HttpGet("avatar")]
    [Authorize]
    public async Task<IActionResult> GetMyAvatar()
    {
        var user = await this.CurrentUserAsync(_userManager);
        if (user == null) return Unauthorized();

        if (user.AvatarUrl == null)
        {
            return NotFound();
        }

        var result = await _hook.LoadAsync(_bucketName, "avatar", user.AvatarUrl);

        if (result.Success && result.Data != null)
        {
            return File(result.Data, $"image/{Path.GetExtension(user.AvatarUrl).ToLower()[1..]}");
        }
        else
        {
            return BadRequest(result.ErrorMessage);
        }
    }

    [HttpGet("{userId}/avatar")]
    public async Task<IActionResult> GetAvatar([FromRoute] string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null || user.AvatarUrl == null)
        {
            return NotFound();
        }

        var result = await _hook.LoadAsync(_bucketName, "avatar", user.AvatarUrl);

        if (result.Success && result.Data != null)
        {
            return File(result.Data, $"image/{Path.GetExtension(user.AvatarUrl).ToLower()[1..]}");
        }
        else
        {
            return BadRequest(result.ErrorMessage);
        }
    }

    [HttpDelete("avatar")]
    [Authorize]
    public async Task<IActionResult> DeleteMyAvatar()
    {
        var user = await this.CurrentUserAsync(_userManager);
        if (user == null) return Unauthorized();

        if (string.IsNullOrEmpty(user.AvatarUrl))
        {
            return NotFound("Avatar not found");
        }

        var result = await _s3.DeleteObjectAsync(_bucketName, $"avatar/{user.AvatarUrl}");

        if (result.Success)
        {
            user.AvatarUrl = null;
            await _userManager.UpdateAsync(user);
            return Ok();
        }
        else
        {
            return BadRequest(result.ErrorMessage);
        }
    }

    [HttpDelete("{userId}/avatar")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUserAvatar(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.AvatarUrl))
        {
            return NotFound("Avatar not found");
        }

        var result = await _s3.DeleteObjectAsync(_bucketName, $"avatar/{user.AvatarUrl}");

        if (result.Success)
        {
            user.AvatarUrl = null;
            await _userManager.UpdateAsync(user);
            return Ok();
        }
        else
        {
            return BadRequest(result.ErrorMessage);
        }
    }

    #endregion

    #region Banners

    [HttpPost("banner")]
    [Authorize]
    public async Task<IActionResult> UploadBanner(IFormFile file)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded");
        }

        if (!file.ContentType.StartsWith("image/"))
        {
            return BadRequest("Only image files are allowed");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound("User not found");
        }

        if (!string.IsNullOrEmpty(user.BannerUrl))
        {
            await _s3.DeleteObjectAsync(_bucketName, $"banner/{user.BannerUrl}");
        }

        var result = await _hook.UploadAsync(_bucketName, "banner", file);

        if (result.Success)
        {
            user.BannerUrl = result.FileName;
            await _userManager.UpdateAsync(user);
            return Ok(user);
        }
        else
        {
            return BadRequest(result.ErrorMessage);
        }
    }

    [HttpGet("banner")]
    [Authorize]
    public async Task<IActionResult> GetMyBanner()
    {
        var user = await this.CurrentUserAsync(_userManager);
        if (user == null) return Unauthorized();

        if (user.BannerUrl == null)
        {
            return NotFound();
        }

        var result = await _hook.LoadAsync(_bucketName, "banner", user.BannerUrl);

        if (result.Success && result.Data != null)
        {
            return File(result.Data, $"image/{Path.GetExtension(user.BannerUrl).ToLower()[1..]}");
        }
        else
        {
            return BadRequest(result.ErrorMessage);
        }
    }

    [HttpGet("{userId}/banner")]
    public async Task<IActionResult> GetBanner([FromRoute] string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null || user.BannerUrl == null)
        {
            return NotFound();
        }

        var result = await _hook.LoadAsync(_bucketName, "banner", user.BannerUrl);

        if (result.Success && result.Data != null)
        {
            return File(result.Data, $"image/{Path.GetExtension(user.BannerUrl).ToLower()[1..]}");
        }
        else
        {
            return BadRequest(result.ErrorMessage);
        }
    }

    [HttpDelete("banner")]
    [Authorize]
    public async Task<IActionResult> DeleteMyBanner()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.BannerUrl))
        {
            return NotFound("Avatar not found");
        }

        var result = await _s3.DeleteObjectAsync(_bucketName, $"banner/{user.BannerUrl}");

        if (result.Success)
        {
            user.BannerUrl = null;
            await _userManager.UpdateAsync(user);
            return Ok();
        }
        else
        {
            return BadRequest(result.ErrorMessage);
        }
    }

    [HttpDelete("{userId}/banner")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUserBanner(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.BannerUrl))
        {
            return NotFound("Avatar not found");
        }

        var result = await _s3.DeleteObjectAsync(_bucketName, $"banner/{user.BannerUrl}");

        if (result.Success)
        {
            user.BannerUrl = null;
            await _userManager.UpdateAsync(user);
            return Ok();
        }
        else
        {
            return BadRequest(result.ErrorMessage);
        }
    }

    #endregion
}