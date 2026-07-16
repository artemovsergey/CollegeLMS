using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class TestQuestionConfiguration : IEntityTypeConfiguration<TestQuestion>
{
    public void Configure(EntityTypeBuilder<TestQuestion> builder)
    {
        builder.ToTable("test_questions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Text).HasMaxLength(4000);
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Options).HasMaxLength(8000);
        builder.Property(x => x.CorrectAnswer).HasMaxLength(4000);
        builder
            .HasOne(x => x.Test)
            .WithMany(t => t.Questions)
            .HasForeignKey(x => x.TestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.TestId).HasDatabaseName("ix_test_questions_test_id");
    }
}
