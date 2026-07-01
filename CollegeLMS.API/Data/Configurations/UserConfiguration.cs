using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Email).HasMaxLength(256);
        builder.HasIndex(x => x.Email).IsUnique().HasDatabaseName("ix_users_email");

        builder.Property(x => x.PasswordHash).HasMaxLength(512);
        builder.Property(x => x.FullName).HasMaxLength(200);

        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(50);

        // CHECK constraints — in Data/DbConstraints.cs (not here)
    }
}
