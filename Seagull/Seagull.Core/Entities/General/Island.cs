using Seagull.Core.Entities.Identity;
using Seagull.Core.Entities.Linker;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Seagull.Core.Entities.General;

public class Island
{
    [Key]
    public int Id { get; set; }

    [Required]
    required public string Name { get; set; }
    
    public string? Description { get; set; }
    public string? Status { get; set; }

    [ForeignKey(nameof(Author))]
    required public string AuthorId { get; set; }

    [ForeignKey(nameof(Owner))]
    required public string OwnerId { get; set; }

    public string? LogoFilename { get; set; }
    public string? BannerFilename { get; set; }
    public string? BannerColor { get; set; }

    public User? Author { get; set; }
    public User? Owner { get; set; }
    public ICollection<UserIsland> UsersConns { get; set; } = [];
}
