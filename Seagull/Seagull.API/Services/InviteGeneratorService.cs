namespace Seagull.API.Services;

public class InviteGeneratorService
{
    public string GenerateUniqueId() => Guid.NewGuid().ToString("N");
}
