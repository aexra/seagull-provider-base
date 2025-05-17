using Microsoft.EntityFrameworkCore;
using Seagull.Core.Entities.Identity;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Seagull.Core.Entities.General;

public class IslandInviteLink
{
    [Key]
    required public string Content { get; set; }

    [Required, ForeignKey(nameof(Island))]
    required public int IslandId { get; set; }

    [Required, ForeignKey(nameof(Author))]
    required public string AuthorId { get; set; }

    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; set; } = null;

    [DefaultValue(null)]
    public int? UsagesMax { get; set; } = null;

    [DefaultValue(0)]
    public int UsagesCount { get; set; } = 0;

    public Island? Island { get; set; }
    public User? Author { get; set; }
}
