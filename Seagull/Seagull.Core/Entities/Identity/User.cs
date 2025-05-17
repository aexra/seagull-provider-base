using Microsoft.AspNetCore.Identity;

namespace Seagull.Core.Entities.Identity;

public class User : IdentityUser
{
    required public string DisplayName { get; set; }
    required public string FirstName { get; set; }
    required public string LastName { get; set; }
    public string? MiddleName { get; set; }
    required public string Tag { get; set; }

    public string? AvatarUrl { get; set; }
    public string? BannerUrl { get; set; }
}
