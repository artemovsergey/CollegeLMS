using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class NonWorkingDayConfiguration : IEntityTypeConfiguration<NonWorkingDay>
{
    public void Configure(EntityTypeBuilder<NonWorkingDay> builder)
    {
        builder.ToTable("non_working_days");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.DateFrom).IsRequired();
        builder.Property(x => x.DateTo).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();

        builder.HasIndex(x => x.DateFrom).HasDatabaseName("ix_non_working_days_date_from");
        builder.HasIndex(x => x.DateTo).HasDatabaseName("ix_non_working_days_date_to");
    }
}
