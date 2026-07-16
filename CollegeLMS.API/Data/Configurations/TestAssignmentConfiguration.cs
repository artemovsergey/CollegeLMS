using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class TestAssignmentConfiguration : IEntityTypeConfiguration<TestAssignment>
{
    public void Configure(EntityTypeBuilder<TestAssignment> builder)
    {
        builder.ToTable("test_assignments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder
            .HasOne(x => x.Test)
            .WithMany(t => t.Assignments)
            .HasForeignKey(x => x.TestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.Group)
            .WithMany()
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.TestId).HasDatabaseName("ix_test_assignments_test_id");
        builder.HasIndex(x => x.GroupId).HasDatabaseName("ix_test_assignments_group_id");
    }
}
