using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class PracticeTeacherConfiguration : IEntityTypeConfiguration<PracticeTeacher>
{
    public void Configure(EntityTypeBuilder<PracticeTeacher> builder)
    {
        builder.ToTable("practice_teachers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.HasIndex(x => x.PracticeId).HasDatabaseName("ix_practice_teachers_practice_id");
        builder.HasIndex(x => x.TeacherId).HasDatabaseName("ix_practice_teachers_teacher_id");
        builder
            .HasIndex(x => new { x.PracticeId, x.TeacherId })
            .IsUnique()
            .HasDatabaseName("ix_practice_teachers_practice_teacher");

        builder
            .HasOne(x => x.Practice)
            .WithMany(p => p.Teachers)
            .HasForeignKey(x => x.PracticeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.Teacher)
            .WithMany()
            .HasForeignKey(x => x.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
