using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Seagull.API.DTO.island.Response;
using Seagull.API.Extensions;
using Seagull.API.Query.invite;
using Seagull.API.Services;
using Seagull.Core.Entities.General;
using Seagull.Core.Entities.Identity;
using Seagull.Infrastructure.Data;
using Seagull.Infrastructure.Hooks;

namespace Seagull.API.Controllers;

[Route("api/island")]
[ApiController]
public class IslandController(MainContext context, S3Hook hook, UserManager<User> userManager, InviteGeneratorService gen) : ControllerBase
{
    private readonly MainContext _context = context;
    private readonly S3Hook _hook = hook;
    private readonly UserManager<User> _userManager = userManager;
    private readonly InviteGeneratorService _gen = gen;

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

    [HttpGet("{islandId}")]
    public async Task<IActionResult> GetIsland([FromRoute] int islandId)
    {
        var island = await _context.Island
            .Include(i => i.UsersConns)
                .ThenInclude(ui => ui.User)
            .FirstOrDefaultAsync(i => i.Id == islandId);
        if (island == null) return NotFound();

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

    [HttpGet("{islandId}/invite")]
    [Authorize]
    public async Task<IActionResult> GenerateInvite([FromRoute] int islandId, [FromQuery] GenerateInviteQuery query)
    {
        var user = await this.CurrentUserAsync(_userManager);
        if (user == null) return Unauthorized();

        var island = await _context.Island.FindAsync(islandId);
        if (island == null) return NotFound();

        if (island.OwnerId != user.Id) return Forbid();

        var key = _gen.GenerateUniqueId();

        var now = DateTime.UtcNow;
        if (query.Days != null) now.AddDays(query.Days.Value);
        if (query.Hours != null) now.AddHours(query.Hours.Value);
        if (query.Minutes != null) now.AddMinutes(query.Minutes.Value);

        var invite = new IslandInviteLink()
        {
            IslandId = islandId,
            UserId = user.Id,
            EffectiveTo = (query.Days != null || query.Hours != null || query.Minutes != null) ? now : null,
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

    [HttpGet("{islandId}/invites")]
    [Authorize]
    public async Task<IActionResult> ListWorkingInvites([FromRoute] int islandId)
    {
        var user = await this.CurrentUserAsync(_userManager);
        if (user == null) return Unauthorized();

        var island = await _context.Island.FindAsync(islandId);
        if (island == null) return NotFound();

        if (island.OwnerId != user.Id) return Forbid();

        var invites = await _context.IslandInviteLink.Where(x => !(x.EffectiveTo != null && DateTime.UtcNow > x.EffectiveTo || x.UsagesMax != null && x.UsagesCount > x.UsagesMax)).ToListAsync();

        return Ok(invites.Select(i => new IslandInviteDto()
        {
            EffectiveTo = i.EffectiveTo,
            UsagesLeft = i.UsagesMax != null ? i.UsagesMax - i.UsagesCount : null,
            Content = i.Content,
        }));
    }
}
