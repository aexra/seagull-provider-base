using System.ComponentModel.DataAnnotations;

namespace Seagull.API.DTO.island.Request;

public class EditIslandDto
{
    [Required]
    required public string Name { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? BannerColor { get; set; }
}
