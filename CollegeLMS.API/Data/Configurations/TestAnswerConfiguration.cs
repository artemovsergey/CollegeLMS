using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class TestAnswerConfiguration : IEntityTypeConfiguration<TestAnswer>
{
    public void Configure(EntityTypeBuilder<TestAnswer> builder)
    {
        builder.ToTable("test_answers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.GivenAnswer).HasMaxLength(4000);
        builder
            .HasOne(x => x.Attempt)
            .WithMany(a => a.Answers)
            .HasForeignKey(x => x.AttemptId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.Question)
            .WithMany()
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.AttemptId).HasDatabaseName("ix_test_answers_attempt_id");
    }
}
