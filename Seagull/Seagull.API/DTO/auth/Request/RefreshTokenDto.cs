using System.ComponentModel.DataAnnotations;

namespace Seagull.API.DTO.auth.Request;

public record RefreshTokenDto(
    [Required] string AccessToken,
    [Required] string RefreshToken
);
