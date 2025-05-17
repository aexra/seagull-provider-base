using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Seagull.Core.Entities.Identity;

namespace Seagull.Infrastructure.Data;

public class MainContext(DbContextOptions<MainContext> options) : IdentityDbContext<User>(options)
{
    public DbSet<User> User { get; set; }
}
