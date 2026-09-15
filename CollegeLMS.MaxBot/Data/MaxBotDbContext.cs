using CollegeLMS.MaxBot.Models;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.MaxBot.Data;

public class MaxBotDbContext : DbContext
{
    public MaxBotDbContext(DbContextOptions<MaxBotDbContext> options)
        : base(options) { }

    public DbSet<UserSettings> UserSettings => Set<UserSettings>();

    public DbSet<ScheduleRevision> ScheduleRevisions => Set<ScheduleRevision>();

    public DbSet<BotFavorite> BotFavorites => Set<BotFavorite>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MaxBotDbContext).Assembly);
    }
}
