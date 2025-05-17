namespace Seagull.API.DTO.auth.Response;

public record UserProfileResponse(
    string Id,
    string Email,
    string UserName,
    string DisplayName,
    string Tag,
    string? AvatarFilename,
    string? BannerFilename,
    string? BannerColor,
    IList<string> Roles);
