using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> builder)
    {
        builder.ToTable("favorites");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.TargetType).HasConversion<string>().HasMaxLength(20);

        builder
            .HasIndex(x => new
            {
                x.UserId,
                x.TargetType,
                x.TargetId,
            })
            .IsUnique()
            .HasDatabaseName("ux_favorites_user_target");
    }
}
