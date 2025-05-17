using Microsoft.AspNetCore.Identity;
using Seagull.Core.Entities.General;
using System.ComponentModel.DataAnnotations.Schema;

namespace Seagull.Core.Entities.Identity;

public class User : IdentityUser
{
    required public string DisplayName { get; set; }
    required public string Tag { get; set; }

    public string? AvatarFilename { get; set; }
    public string? BannerFilename { get; set; }
    public string? BannerColor { get; set; }
    public string? Status { get; set; }

    [InverseProperty(nameof(Island.Author))]
    public ICollection<Island> AuthoredIslands { get; set; } = [];

    [InverseProperty(nameof(Island.Owner))]
    public ICollection<Island> OwnedIslands { get; set; } = [];
}
