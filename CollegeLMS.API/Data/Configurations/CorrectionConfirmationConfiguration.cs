using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class CorrectionConfirmationConfiguration : IEntityTypeConfiguration<CorrectionConfirmation>
{
    public void Configure(EntityTypeBuilder<CorrectionConfirmation> builder)
    {
        builder.ToTable("correction_confirmations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(64);

        builder
            .HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("ux_correction_confirmations_idempotency_key");
    }
}
