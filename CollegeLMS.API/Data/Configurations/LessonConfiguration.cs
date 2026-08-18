using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("lessons");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Title).HasMaxLength(255);
        builder.Property(x => x.Content).HasMaxLength(65535);
        builder.Property(x => x.Order).HasDefaultValue(0);
        builder
            .Property(x => x.Kind)
            .HasColumnName("kind")
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(x => x.IsCurrent).HasDefaultValue(false);
        builder
            .HasOne(x => x.Course)
            .WithMany(c => c.Lessons)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.Test)
            .WithMany()
            .HasForeignKey(x => x.TestId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.TestId).HasDatabaseName("ix_lessons_test_id");

        builder
            .HasIndex(x => new { x.CourseId, x.Order })
            .HasDatabaseName("ix_lessons_course_id_order");
    }
}
