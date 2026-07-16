using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class RetakeConfiguration : IEntityTypeConfiguration<Retake>
{
    public void Configure(EntityTypeBuilder<Retake> builder)
    {
        builder.ToTable("retakes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Reason).HasMaxLength(1000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder
            .HasOne(x => x.Exam)
            .WithMany(e => e.Retakes)
            .HasForeignKey(x => x.ExamId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.ExamId).HasDatabaseName("ix_retakes_exam_id");
    }
}
