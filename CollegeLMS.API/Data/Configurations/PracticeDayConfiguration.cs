using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class PracticeDayConfiguration : IEntityTypeConfiguration<PracticeDay>
{
    public void Configure(EntityTypeBuilder<PracticeDay> builder)
    {
        builder.ToTable("practice_days");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Date).IsRequired();
        builder.Property(x => x.PairNumbers).IsRequired();

        builder.HasIndex(x => x.PracticeId).HasDatabaseName("ix_practice_days_practice_id");
        builder
            .HasIndex(x => new { x.PracticeId, x.Date })
            .IsUnique()
            .HasDatabaseName("ix_practice_days_practice_date");

        builder
            .HasOne(x => x.Practice)
            .WithMany(p => p.Days)
            .HasForeignKey(x => x.PracticeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
