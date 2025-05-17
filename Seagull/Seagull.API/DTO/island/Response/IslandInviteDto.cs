namespace Seagull.API.DTO.island.Response;

public class IslandInviteDto
{
    public DateTime? EffectiveTo { get; set; } = null;
    public int? UsagesLeft { get; set; } = null;
    required public string Content { get; set; }
}
