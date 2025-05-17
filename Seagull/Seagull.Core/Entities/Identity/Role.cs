using Microsoft.AspNetCore.Identity;

namespace Seagull.Core.Entities.Identity;

public class Role : IdentityRole
{
    public Role() : base() { }
    public Role(string roleName) : base(roleName) { }
}
