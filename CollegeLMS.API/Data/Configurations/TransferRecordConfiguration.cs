using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class TransferRecordConfiguration : IEntityTypeConfiguration<TransferRecord>
{
    public void Configure(EntityTypeBuilder<TransferRecord> builder)
    {
        builder.ToTable("transfer_records");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Reason).HasMaxLength(1000);
        builder
            .HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.StudentId).HasDatabaseName("ix_transfer_records_student_id");
    }
}
