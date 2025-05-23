﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Seagull.API.DTO.island.Request;
using Seagull.API.DTO.island.Response;
using Seagull.API.Extensions;
using Seagull.API.Query.invite;
using Seagull.API.Services;
using Seagull.Core.Entities.General;
using Seagull.Core.Entities.Identity;
using Seagull.Core.Entities.Linker;
using Seagull.Infrastructure.Data;
using Seagull.Infrastructure.Hooks;
using Seagull.Infrastructure.Services;

namespace Seagull.API.Controllers;

[Route("api/island")]
[ApiController]
public class IslandController(MainContext context, S3Hook hook, UserManager<User> userManager, InviteGeneratorService gen, S3Service s3) : ControllerBase
{
    private readonly MainContext _context = context;
    private readonly S3Hook _hook = hook;
    private readonly S3Service _s3 = s3;
    private readonly UserManager<User> _userManager = userManager;
    private readonly InviteGeneratorService _gen = gen;

    private const string _bucketName = "island";

    /// <summary>
    /// Возвращает список островов в которых есть текущий пользователь
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ListMyIslands()
    {
        var user = await this.CurrentUserAsync(_userManager);
        if (user == null) return Unauthorized();

        var my = await _context.UserIsland
            .Include(ui => ui.Island)
            .Where(ui => ui.UserId == user.Id)
            .Select(ui => new IslandDto()
            {
                Id = ui.Island.Id,
                Name = ui.Island.Name,
                Description = ui.Island.Description,
                Status = ui.Island.Status,
                AuthorId = ui.Island.AuthorId,
                OwnerId = ui.Island.OwnerId,
                LogoFilename = ui.Island.LogoFilename,
                BannerFilename = ui.Island.BannerFilename,
                BannerColor = ui.Island.BannerColor
            })
            .ToListAsync();

        return Ok(my);
    }

    /// <summary>
    /// Возвращает подробную информацию об определенном острове
    /// </summary>
    /// <param name="islandId"></param>
    /// <returns></returns>
    [HttpGet("{islandId}")]
    public async Task<IActionResult> GetIsland([FromRoute] int islandId)
    {
        var island = await _context.Island
            .Include(i => i.UsersConns)
                .ThenInclude(ui => ui.User)
            .FirstOrDefaultAsync(i => i.Id == islandId);
        if (island == null) return NotFound($"Island [{islandId}] not found");

        return Ok(new IslandExDto()
        {
            Id = island.Id,
            Name = island.Name,
            Description = island.Description,
            Status = island.Status,
            AuthorId = island.AuthorId,
            OwnerId = island.OwnerId,
            LogoFilename = island.LogoFilename,
            BannerFilename = island.BannerFilename,
            BannerColor = island.BannerColor,
            Users = island.UsersConns.Select(i => new DTO.auth.Response.UserProfileResponse(
                i.User.Id,
                i.User.Email,
                i.User.UserName,
                i.User.DisplayName,
                i.User.Tag,
                i.User.AvatarFilename,
                i.User.BannerFilename,
                i.User.BannerColor,
                []
            )).ToList()
        });
    }

    /// <summary>
    /// Генерирует ссылку для приглашения в остров
    /// </summary>
    /// <param name="islandId"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    [HttpPost("{islandId}/invite")]
    [Authorize]
    public async Task<IActionResult> GenerateInvite([FromRoute] int islandId, [FromQuery] GenerateInviteQuery query)
    {
        var user = await this.CurrentUserAsync(_userManager);
        if (user == null) return Unauthorized();

        var island = await _context.Island.FindAsync(islandId);
        if (island == null) return NotFound($"Island [{islandId}] not found");

        if (island.OwnerId != user.Id) return Forbid();

        var key = _gen.GenerateUniqueId();

        var to = DateTime.UtcNow;
        if (query.Days != null) to = to.AddDays(query.Days.Value);
        if (query.Hours != null) to = to.AddHours(query.Hours.Value);
        if (query.Minutes != null) to = to.AddMinutes(query.Minutes.Value);

        var invite = new IslandInviteLink()
        {
            IslandId = islandId,
            AuthorId = user.Id,
            EffectiveTo = (query.Days != null || query.Hours != null || query.Minutes != null) ? to : null,
            UsagesMax = query.Usages,
            Content = key,
        };

        var entry = _context.IslandInviteLink.Add(invite);
        await _context.SaveChangesAsync();

        return Ok(new IslandInviteDto()
        {
            EffectiveTo = entry.Entity.EffectiveTo,
            UsagesLeft = entry.Entity.UsagesMax,
            Content = entry.Entity.Content,
        });
    }

    /// <summary>
    /// Создает новый остров от имени текущего пользователя
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateIsland([FromBody] CreateIslandDto dto)
    {
        var user = await this.CurrentUserAsync(_userManager);
        if (user == null) return Unauthorized();

        var island = new Island()
        {
            Name = dto.Name,
            Description = dto.Description,
            AuthorId = user.Id,
            OwnerId = user.Id
        };

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var entry = _context.Island.Add(island);
            await _context.SaveChangesAsync();

            _context.UserIsland.Add(new UserIsland { IslandId = island.Id, UserId = user.Id });
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return Ok(new IslandDto()
            {
                Id = entry.Entity.Id,
                Name = entry.Entity.Name,
                Description = entry.Entity.Description,
                Status = entry.Entity.Status,
                AuthorId = entry.Entity.AuthorId,
                OwnerId = entry.Entity.OwnerId,
                LogoFilename = entry.Entity.LogoFilename,
                BannerFilename = entry.Entity.BannerFilename,
                BannerColor = entry.Entity.BannerColor,
            });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Владелец острова изменяет его детали
    /// </summary>
    /// <param name="dto"></param>
    /// <param name="islandId"></param>
    /// <returns></returns>
    [HttpPut("{islandId}")]
    [Authorize]
    public async Task<IActionResult> EditIsland([FromBody] EditIslandDto dto, [FromRoute] int islandId)
    {
        var user = await this.CurrentUserAsync(_userManager);
        if (user == null) return Unauthorized();

        var island = await _context.Island.FindAsync(islandId);
        if (island == null) return NotFound($"Island [{islandId}] not found");

        if (island.OwnerId != user.Id) return Forbid();

        island.Name = dto.Name;
        island.Description = dto.Description;
        island.Status = dto.Status;
        island.BannerColor = dto.BannerColor;

        await _context.SaveChangesAsync();

        return Ok(new IslandDto()
        {
            Id = island.Id,
            Name = island.Name,
            Description = island.Description,
            Status = island.Status,
            AuthorId = island.AuthorId,
            OwnerId = island.OwnerId,
            LogoFilename = island.LogoFilename,
            BannerFilename = island.BannerFilename,
            BannerColor = island.BannerColor,
        });
    }

    /// <summary>
    /// Владелец острова удаляет свой остров
    /// </summary>
    /// <param name="islandId"></param>
    /// <returns></returns>
    [HttpDelete("{islandId}")]
    [Authorize]
    public async Task<IActionResult> DeleteIsland([FromRoute] int islandId)
    {
        var user = await this.CurrentUserAsync(_userManager);
        if (user == null) return Unauthorized();

        var island = await _context.Island.FindAsync(islandId);
        if (island == null) return NotFound($"Island [{islandId}] not found");

        if (island.OwnerId != user.Id) return Forbid();

        _context.Island.Remove(island);
        await _context.SaveChangesAsync();

        return Ok();
    }

    /// <summary>
    /// Возвращает список действующих приглашений в остров (только для владельца)
    /// </summary>
    /// <param name="islandId"></param>
    /// <returns></returns>
    [HttpGet("{islandId}/invites")]
    [Authorize]
    public async Task<IActionResult> ListWorkingInvites([FromRoute] int islandId)
    {
        var user = await this.CurrentUserAsync(_userManager);
        if (user == null) return Unauthorized();

        var island = await _context.Island.FindAsync(islandId);
        if (island == null) return NotFound($"Island [{islandId}] not found");

        if (island.OwnerId != user.Id) return Forbid();

        var invites = await _context.IslandInviteLink.Where(x => !(x.EffectiveTo != null && DateTime.UtcNow > x.EffectiveTo || x.UsagesMax != null && x.UsagesCount > x.UsagesMax)).ToListAsync();

        return Ok(invites.Select(i => new IslandInviteDto()
        {
            EffectiveTo = i.EffectiveTo,
            UsagesLeft = i.UsagesMax != null ? i.UsagesMax - i.UsagesCount : null,
            Content = i.Content,
        }));
    }

    /// <summary>
    /// Удаляет приглашение (только для владельца острова)
    /// </summary>
    /// <param name="islandId"></param>
    /// <param name="ticket"></param>
    /// <returns></returns>
    [HttpDelete("{islandId}/invite")]
    [Authorize]
    public async Task<IActionResult> DeleteInvite([FromRoute] int islandId, [FromQuery] string ticket)
    {
        var user = await this.CurrentUserAsync(_userManager);
        if (user == null) return Unauthorized();

        var island = await _context.Island.FindAsync(islandId);
        if (island == null) return NotFound($"Island [{islandId}] not found");

        if (island.OwnerId != user.Id) return Forbid();

        var link = await _context.IslandInviteLink.FindAsync(ticket);
        if (link == null) return NotFound($"Link [{ticket}] not found");

        _context.IslandInviteLink.Remove(link);
        await _context.SaveChangesAsync();

        return Ok();
    }

    /// <summary>
    /// Применяет приглашение для текущего пользователя
    /// </summary>
    /// <param name="ticket"></param>
    /// <returns></returns>
    [HttpPost("/api/invite/{ticket}")]
    [Authorize]
    public async Task<IActionResult> UseInvite([FromRoute] string ticket)
    {
        var user = await this.CurrentUserAsync(_userManager);
        if (user == null) return Unauthorized();

        var link = await _context.IslandInviteLink.FindAsync(ticket);
        if (link == null) return NotFound($"Link [{ticket}] not found");
        if (link.Expired) return BadRequest($"Link [{ticket}] is expired");

        var existingLinker = await _context.UserIsland.FindAsync(user.Id, link.IslandId);
        if (existingLinker != null) return BadRequest($"You are in island already");

        var linker = new UserIsland()
        {
            UserId = user.Id,
            IslandId = link.IslandId
        };

        _context.UserIsland.Add(linker);
        link.UsagesCount += 1;

        await _context.SaveChangesAsync();

        return Ok();
    }

    /// <summary>
    /// Текущий пользователь выходит из острова
    /// </summary>
    /// <param name="islandId"></param>
    /// <returns></returns>
    [HttpPost("{islandId}/leave")]
    [Authorize]
    public async Task<IActionResult> LeaveIsland([FromRoute] string islandId)
    {
        var user = await this.CurrentUserAsync(_userManager);
        if (user == null) return Unauthorized();

        var island = await _context.Island.FindAsync(islandId);
        if (island == null) return NotFound($"Island [{islandId}] not found");

        var ui = await _context.UserIsland.FindAsync(user.Id, island.Id);
        if (ui == null) return BadRequest($"You are not in island [{island.Id}]");

        _context.UserIsland.Remove(ui);
        await _context.SaveChangesAsync();

        return Ok();
    }

    /// <summary>
    /// Владелец острова выгоняет другого пользователя
    /// </summary>
    /// <param name="islandId"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    [HttpDelete("{islandId}/user/{userId}")]
    [Authorize]
    public async Task<IActionResult> RemoveUser([FromRoute] string islandId, [FromRoute] string userId)
    {
        var user = await this.CurrentUserAsync(_userManager);
        if (user == null) return Unauthorized();

        var island = await _context.Island.FindAsync(islandId);
        if (island == null) return NotFound($"Island [{islandId}] not found");

        if (island.OwnerId != user.Id) return Forbid();

        var ui = await _context.UserIsland.FindAsync(userId, island.Id);
        if (ui == null) return BadRequest($"User [{userId}] is not in island [{island.Id}]");

        _context.UserIsland.Remove(ui);
        await _context.SaveChangesAsync();

        return Ok();
    }

    #region Avatars

    /// <summary>
    /// Устанавливает multipart/form-data как аватарку для острова
    /// </summary>
    /// <param name="islandId"></param>
    /// <param name="file"></param>
    /// <returns></returns>
    [HttpPost("{islandId}/avatar")]
    [Authorize]
    public async Task<IActionResult> UploadAvatar([FromRoute] int islandId, IFormFile file)
    {
        var user = await this.CurrentUserAsync(_userManager);
        if (user == null) return Unauthorized();

        var island = await _context.Island.FindAsync(islandId);
        if (island == null) return NotFound($"Island [{islandId}] not found");

        if (island.OwnerId != user.Id) return Forbid();

        if (file == null || file.Length == 0) return BadRequest("No file uploaded");
        if (!file.ContentType.StartsWith("image/")) return BadRequest("Only image files are allowed");
        if (!string.IsNullOrEmpty(island.LogoFilename)) await _s3.DeleteObjectAsync(_bucketName, $"avatar/{island.LogoFilename}");

        var result = await _hook.UploadAsync(_bucketName, "avatar", file);

        if (result.Success)
        {
            island.LogoFilename = result.FileName;
            await _context.SaveChangesAsync();
            return Ok(new IslandDto()
            {
                Id = island.Id,
                Name = island.Name,
                Description = island.Description,
                Status = island.Status,
                AuthorId = island.AuthorId,
                OwnerId = island.OwnerId,
                LogoFilename = island.LogoFilename,
                BannerFilename = island.BannerFilename,
                BannerColor = island.BannerColor,
            });
        }
        else
        {
            return BadRequest(result.ErrorMessage);
        }
    }

    /// <summary>
    /// Возвращает multipart/form-data аватарки острова
    /// </summary>
    /// <param name="islandId"></param>
    /// <returns></returns>
    [HttpGet("{islandId}/avatar")]
    public async Task<IActionResult> GetAvatar([FromRoute] int islandId)
    {
        var island = await _context.Island.FindAsync(islandId);
        if (island == null) return NotFound($"Island [{islandId}] not found");

        if (island.LogoFilename == null) return NotFound($"Island has no logo");

        var result = await _hook.LoadAsync(_bucketName, "avatar", island.LogoFilename);

        if (result.Success && result.Data != null)
        {
            return File(result.Data, $"image/{Path.GetExtension(island.LogoFilename).ToLower()[1..]}");
        }
        else
        {
            return BadRequest(result.ErrorMessage);
        }
    }

    /// <summary>
    /// Удаляет аватарку с острова
    /// </summary>
    /// <param name="islandId"></param>
    /// <returns></returns>
    [HttpDelete("{islandId}/avatar")]
    [Authorize]
    public async Task<IActionResult> DeleteAvatar([FromRoute] int islandId)
    {
        var user = await this.CurrentUserAsync(_userManager);
        if (user == null) return Unauthorized();

        var island = await _context.Island.FindAsync(islandId);
        if (island == null) return NotFound($"Island [{islandId}] not found");

        if (island.OwnerId != user.Id) return Forbid();

        if (island.LogoFilename == null) return NotFound($"Island has no logo");

        var result = await _s3.DeleteObjectAsync(_bucketName, $"avatar/{island.LogoFilename}");

        if (result.Success)
        {
            island.LogoFilename = null;
            await _context.SaveChangesAsync();
            return Ok(new IslandDto()
            {
                Id = island.Id,
                Name = island.Name,
                Description = island.Description,
                Status = island.Status,
                AuthorId = island.AuthorId,
                OwnerId = island.OwnerId,
                LogoFilename = island.LogoFilename,
                BannerFilename = island.BannerFilename,
                BannerColor = island.BannerColor,
            });
        }
        else
        {
            return BadRequest(result.ErrorMessage);
        }
    }

    #endregion

    #region Banners

    /// <summary>
    /// Устанавливает multipart/form-data как баннер для острова
    /// </summary>
    /// <param name="islandId"></param>
    /// <param name="file"></param>
    /// <returns></returns>
    [HttpPost("{islandId}/banner")]
    [Authorize]
    public async Task<IActionResult> UploadBanner([FromRoute] int islandId, IFormFile file)
    {
        var user = await this.CurrentUserAsync(_userManager);
        if (user == null) return Unauthorized();

        var island = await _context.Island.FindAsync(islandId);
        if (island == null) return NotFound($"Island [{islandId}] not found");

        if (island.OwnerId != user.Id) return Forbid();

        if (file == null || file.Length == 0) return BadRequest("No file uploaded");
        if (!file.ContentType.StartsWith("image/")) return BadRequest("Only image files are allowed");
        if (!string.IsNullOrEmpty(island.BannerFilename)) await _s3.DeleteObjectAsync(_bucketName, $"banner/{island.BannerFilename}");

        var result = await _hook.UploadAsync(_bucketName, "banner", file);

        if (result.Success)
        {
            island.BannerFilename = result.FileName;
            await _context.SaveChangesAsync();
            return Ok(new IslandDto()
            {
                Id = island.Id,
                Name = island.Name,
                Description = island.Description,
                Status = island.Status,
                AuthorId = island.AuthorId,
                OwnerId = island.OwnerId,
                LogoFilename = island.LogoFilename,
                BannerFilename = island.BannerFilename,
                BannerColor = island.BannerColor,
            });
        }
        else
        {
            return BadRequest(result.ErrorMessage);
        }
    }

    /// <summary>
    /// Возвращает multipart/form-data баннера острова
    /// </summary>
    /// <param name="islandId"></param>
    /// <returns></returns>
    [HttpGet("{islandId}/banner")]
    public async Task<IActionResult> GetBanner([FromRoute] int islandId)
    {
        var island = await _context.Island.FindAsync(islandId);
        if (island == null) return NotFound($"Island [{islandId}] not found");

        if (island.BannerFilename == null) return NotFound($"Island has no banner");

        var result = await _hook.LoadAsync(_bucketName, "avatar", island.BannerFilename);

        if (result.Success && result.Data != null)
        {
            return File(result.Data, $"image/{Path.GetExtension(island.BannerFilename).ToLower()[1..]}");
        }
        else
        {
            return BadRequest(result.ErrorMessage);
        }
    }

    /// <summary>
    /// Удаляет баннер с острова
    /// </summary>
    /// <param name="islandId"></param>
    /// <returns></returns>
    [HttpDelete("{islandId}/banner")]
    [Authorize]
    public async Task<IActionResult> DeleteBanner([FromRoute] int islandId)
    {
        var user = await this.CurrentUserAsync(_userManager);
        if (user == null) return Unauthorized();

        var island = await _context.Island.FindAsync(islandId);
        if (island == null) return NotFound($"Island [{islandId}] not found");

        if (island.OwnerId != user.Id) return Forbid();

        if (island.BannerFilename == null) return NotFound($"Island has no banner");

        var result = await _s3.DeleteObjectAsync(_bucketName, $"avatar/{island.BannerFilename}");

        if (result.Success)
        {
            island.BannerFilename = null;
            await _context.SaveChangesAsync();
            return Ok(new IslandDto()
            {
                Id = island.Id,
                Name = island.Name,
                Description = island.Description,
                Status = island.Status,
                AuthorId = island.AuthorId,
                OwnerId = island.OwnerId,
                LogoFilename = island.LogoFilename,
                BannerFilename = island.BannerFilename,
                BannerColor = island.BannerColor,
            });
        }
        else
        {
            return BadRequest(result.ErrorMessage);
        }
    }

    #endregion
}
