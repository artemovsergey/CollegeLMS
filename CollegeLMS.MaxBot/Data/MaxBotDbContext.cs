using CollegeLMS.MaxBot.Models;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.MaxBot.Data;

public class MaxBotDbContext : DbContext
{
    public MaxBotDbContext(DbContextOptions<MaxBotDbContext> options)
        : base(options) { }

    public DbSet<UserSettings> UserSettings => Set<UserSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MaxBotDbContext).Assembly);
    }
}
