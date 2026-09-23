using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class PracticeConfiguration : IEntityTypeConfiguration<Practice>
{
    public void Configure(EntityTypeBuilder<Practice> builder)
    {
        builder.ToTable("practices");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.Property(x => x.Room).HasMaxLength(20);
        builder.Property(x => x.DateFrom).IsRequired();
        builder.Property(x => x.DateTo).IsRequired();

        builder.HasIndex(x => x.GroupId).HasDatabaseName("ix_practices_group_id");
        builder.HasIndex(x => x.DateFrom).HasDatabaseName("ix_practices_date_from");

        builder
            .HasOne(x => x.Group)
            .WithMany()
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(x => x.Teachers)
            .WithOne(t => t.Practice)
            .HasForeignKey(t => t.PracticeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(x => x.Days)
            .WithOne(d => d.Practice)
            .HasForeignKey(d => d.PracticeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
