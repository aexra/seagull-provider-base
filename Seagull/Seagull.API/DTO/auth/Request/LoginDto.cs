using System.ComponentModel.DataAnnotations;

namespace Seagull.API.DTO.auth.Request;

public record LoginDto(
    [Required] string Login,
    [Required] string Password
);
