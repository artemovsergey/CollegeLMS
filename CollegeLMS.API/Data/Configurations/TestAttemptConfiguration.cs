using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class TestAttemptConfiguration : IEntityTypeConfiguration<TestAttempt>
{
    public void Configure(EntityTypeBuilder<TestAttempt> builder)
    {
        builder.ToTable("test_attempts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder
            .HasOne(x => x.Test)
            .WithMany(t => t.Attempts)
            .HasForeignKey(x => x.TestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.Student)
            .WithMany(s => s.TestAttempts)
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.TestId).HasDatabaseName("ix_test_attempts_test_id");
        builder.HasIndex(x => x.StudentId).HasDatabaseName("ix_test_attempts_student_id");
    }
}
