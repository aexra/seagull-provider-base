using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Seagull.API.DTO.auth.Request;

public record RegisterDto(
    [EmailAddress]
    [Required] string Email,
    [Required] string DisplayName,
    [Required] string UserName,
    [StringLength(100, MinimumLength = 4)] string Password
);
