using CollegeLMS.MaxBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.MaxBot.Data.Configurations;

public class BotFavoriteConfiguration : IEntityTypeConfiguration<BotFavorite>
{
    public void Configure(EntityTypeBuilder<BotFavorite> builder)
    {
        builder.ToTable("bot_favorites");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.MaxUserId).HasColumnName("max_user_id");
        builder.Property(x => x.TargetType).HasColumnName("target_type").HasMaxLength(20);
        builder.Property(x => x.TargetId).HasColumnName("target_id");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder
            .HasIndex(x => new
            {
                x.MaxUserId,
                x.TargetType,
                x.TargetId,
            })
            .IsUnique()
            .HasDatabaseName("ix_bot_favorites_user_type_target");
    }
}
