namespace Seagull.API.DTO.auth.Request;

public class EditUserProfileDto
{
    required public string DisplayName { get; set; }
    public string? BannerColor { get; set; }
    public string? Status { get; set; }
}
