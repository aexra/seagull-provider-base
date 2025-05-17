using Microsoft.EntityFrameworkCore;
using Seagull.Core.Entities.General;
using Seagull.Core.Entities.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Seagull.Core.Entities.Linker;

[PrimaryKey(nameof(UserId), nameof(IslandId))]
public class UserIsland
{
    [Required, Key, Column(Order = 0), ForeignKey(nameof(User))]
    required public string UserId { get; set; }

    [Required, Key, Column(Order = 1), ForeignKey(nameof(Island))]
    required public int IslandId { get; set; }

    public User? User { get; set; }
    public Island? Island { get; set; }
}
