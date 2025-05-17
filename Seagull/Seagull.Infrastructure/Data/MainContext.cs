using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Seagull.Core.Entities.Identity;

namespace Seagull.Infrastructure.Data;

public class MainContext : IdentityDbContext<User>
{
    public DbSet<User> User { get; set; }
}
