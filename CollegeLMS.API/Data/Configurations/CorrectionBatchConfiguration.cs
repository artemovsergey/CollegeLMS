using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class CorrectionBatchConfiguration : IEntityTypeConfiguration<CorrectionBatch>
{
    public void Configure(EntityTypeBuilder<CorrectionBatch> builder)
    {
        builder.ToTable("correction_batches");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.CorrectionDate).IsRequired();
        builder.Property(x => x.Week).IsRequired();
        builder.Property(x => x.DayOfWeek).IsRequired();
        builder
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .IsConcurrencyToken();

        builder.HasIndex(x => x.Status).HasDatabaseName("ix_correction_batches_status");
        builder
            .HasIndex(x => x.CorrectionDate)
            .HasDatabaseName("ix_correction_batches_correction_date");

        builder
            .HasMany(x => x.Positions)
            .WithOne(x => x.Batch)
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
