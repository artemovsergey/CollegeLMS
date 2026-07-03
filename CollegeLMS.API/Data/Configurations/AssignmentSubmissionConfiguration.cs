using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class AssignmentSubmissionConfiguration : IEntityTypeConfiguration<AssignmentSubmission>
{
    public void Configure(EntityTypeBuilder<AssignmentSubmission> builder)
    {
        builder.ToTable("assignment_submissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.FilePath).HasMaxLength(500);
        builder.Property(x => x.Comment).HasMaxLength(1000);
        builder.Property(x => x.SubmittedAt).HasColumnType("timestamp with time zone");
        builder.HasOne(x => x.Assignment)
            .WithMany(a => a.Submissions)
            .HasForeignKey(x => x.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Student)
            .WithMany(s => s.Submissions)
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.AssignmentId, x.StudentId }).HasDatabaseName("ix_assignment_submissions_assignment_id_student_id");
    }
}
