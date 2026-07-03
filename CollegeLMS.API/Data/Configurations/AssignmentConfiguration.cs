using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("assignments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Title).HasMaxLength(255);
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.MaxScore).HasDefaultValue(100);
        builder.Property(x => x.Order).HasDefaultValue(0);
        builder.HasOne(x => x.Course)
            .WithMany(c => c.Assignments)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.CourseId, x.Order }).HasDatabaseName("ix_assignments_course_id_order");
    }
}
