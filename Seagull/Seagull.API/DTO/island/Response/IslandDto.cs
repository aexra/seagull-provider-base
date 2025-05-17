namespace Seagull.API.DTO.island.Response;

public class IslandDto
{
    required public int Id { get; set; }
    required public string Name { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    required public string AuthorId { get; set; }
    required public string OwnerId { get; set; }
    public string? LogoFilename { get; set; }
    public string? BannerFilename { get; set; }
    public string? BannerColor { get; set; }
}
