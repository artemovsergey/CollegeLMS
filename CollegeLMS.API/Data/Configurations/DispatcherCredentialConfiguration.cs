using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class DispatcherCredentialConfiguration : IEntityTypeConfiguration<DispatcherCredential>
{
    public void Configure(EntityTypeBuilder<DispatcherCredential> builder)
    {
        builder.ToTable("dispatcher_credentials");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.PasswordHash).HasMaxLength(200);
    }
}