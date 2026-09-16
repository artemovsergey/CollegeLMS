using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class CorrectionPositionConfiguration : IEntityTypeConfiguration<CorrectionPosition>
{
    public void Configure(EntityTypeBuilder<CorrectionPosition> builder)
    {
        builder.ToTable("correction_positions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Row).IsRequired();
        builder.Property(x => x.GroupName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(200);
        builder.Property(x => x.TeacherName).HasMaxLength(200);
        builder.Property(x => x.RemovedSubject).HasMaxLength(200);
        builder.Property(x => x.RemovedTeacherName).HasMaxLength(200);
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.Property(x => x.ChangeType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(x => x.BatchId).HasDatabaseName("ix_correction_positions_batch_id");
        builder.HasIndex(x => x.Status).HasDatabaseName("ix_correction_positions_status");

        builder
            .HasOne(x => x.Batch)
            .WithMany(x => x.Positions)
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
