using Seagull.API.DTO.auth.Response;

namespace Seagull.API.DTO.island.Response;

public class IslandExDto : IslandDto
{
    public ICollection<UserProfileResponse> Users { get; set; } = [];
}
