using System.ComponentModel.DataAnnotations;

namespace Seagull.API.DTO.island.Request;

public class CreateIslandDto
{
    [Required]
    required public string Name { get; set; }
    public string? Description { get; set; }
}
