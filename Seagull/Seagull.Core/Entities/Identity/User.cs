using Microsoft.AspNetCore.Identity;

namespace Seagull.Core.Entities.Identity;

public class User : IdentityUser
{
    required public string DisplayName { get; set; }
    required public string Tag { get; set; }

    public string? AvatarUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? BannerColor { get; set; }
    public string? Status { get; set; }
}
