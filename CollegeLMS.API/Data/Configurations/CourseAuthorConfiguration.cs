using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class CourseAuthorConfiguration : IEntityTypeConfiguration<CourseAuthor>
{
    public void Configure(EntityTypeBuilder<CourseAuthor> builder)
    {
        builder.ToTable("course_authors");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder
            .HasIndex(x => new { x.CourseId, x.TeacherId })
            .IsUnique()
            .HasDatabaseName("IX_course_authors_course_id_teacher_id");

        builder
            .HasOne(x => x.Course)
            .WithMany(c => c.CourseAuthors)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.Teacher)
            .WithMany()
            .HasForeignKey(x => x.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.CreatedAt);
        builder.Property(x => x.UpdatedAt);
    }
}
