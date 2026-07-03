using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class LectureConfiguration : IEntityTypeConfiguration<Lecture>
{
    public void Configure(EntityTypeBuilder<Lecture> builder)
    {
        builder.ToTable("lectures");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Title).HasMaxLength(255);
        builder.Property(x => x.Content).HasMaxLength(65535);
        builder.Property(x => x.Order).HasDefaultValue(0);
        builder
            .HasOne(x => x.Course)
            .WithMany(c => c.Lectures)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasIndex(x => new { x.CourseId, x.Order })
            .HasDatabaseName("ix_lectures_course_id_order");
    }
}
