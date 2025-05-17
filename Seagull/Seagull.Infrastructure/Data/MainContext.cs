using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Seagull.Core.Entities.General;
using Seagull.Core.Entities.Identity;
using Seagull.Core.Entities.Linker;

namespace Seagull.Infrastructure.Data;

public class MainContext(DbContextOptions<MainContext> options) : IdentityDbContext<User>(options)
{
    public DbSet<User> User { get; set; }
    public DbSet<Role> Role { get; set; }
    public DbSet<Island> Island { get; set; }

    public DbSet<UserIsland> UserIsland { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
