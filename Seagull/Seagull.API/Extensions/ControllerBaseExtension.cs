using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Seagull.Core.Entities.Identity;
using System.Security.Claims;

namespace Seagull.API.Extensions;

public static class ControllerBaseExtensions
{
    public static async Task<User?> CurrentUserAsync(this ControllerBase ctrl, UserManager<User> um)
    {
        var userId = ctrl.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return null;

        var user = await um.FindByIdAsync(userId);
        return user;
    }
}
