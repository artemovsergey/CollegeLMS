using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("courses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Title).HasMaxLength(255);
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(CourseStatus.Draft);
        builder.HasOne(x => x.Teacher)
            .WithMany(t => t.Courses)
            .HasForeignKey(x => x.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Group)
            .WithMany()
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
