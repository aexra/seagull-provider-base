using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Seagull.Core.Entities.General;

[PrimaryKey(nameof(IslandId), nameof(UserId), nameof(EffectiveFrom))]
public class IslandInviteLink
{
    [Required, Key, Column(Order = 0)]
    required public int IslandId { get; set; }

    [Required, Key, Column(Order = 1)]
    required public string UserId { get; set; }

    [Required, Key, Column(Order = 2)]
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; set; } = null;

    [DefaultValue(null)]
    public int? UsagesMax { get; set; } = null;

    [DefaultValue(0)]
    public int UsagesCount { get; set; } = 0;

    [Required]
    required public string Content { get; set; }
}
