namespace Seagull.API.DTO.auth.Response;

public record AuthResponse(
    string AccessToken,
    string RefreshToken
);
