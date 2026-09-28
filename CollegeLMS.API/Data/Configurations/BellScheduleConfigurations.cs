using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class BellSlotConfiguration : IEntityTypeConfiguration<BellSlot>
{
    public void Configure(EntityTypeBuilder<BellSlot> builder)
    {
        builder.ToTable("bell_slots");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ProfileId).IsRequired();
        builder.Property(x => x.NumberPair).IsRequired();
        builder.Property(x => x.StartTime).IsRequired();
        builder.Property(x => x.EndTime).IsRequired();

        builder
            .HasOne(x => x.Profile)
            .WithMany(x => x.Slots)
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(x => new { x.ProfileId, x.NumberPair })
            .IsUnique()
            .HasDatabaseName("ix_bell_slots_profile_pair");

        builder.HasData(BellSeedIds.SeededSlots.ToArray());
    }
}

public class BigBreakConfiguration : IEntityTypeConfiguration<BigBreak>
{
    public void Configure(EntityTypeBuilder<BigBreak> builder)
    {
        builder.ToTable("big_breaks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ProfileId).IsRequired();
        builder.Property(x => x.AfterPair).IsRequired();
        builder.Property(x => x.StartTime).IsRequired();
        builder.Property(x => x.EndTime).IsRequired();

        builder
            .HasOne(x => x.Profile)
            .WithMany(x => x.BigBreaks)
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(BellSeedIds.SeededBigBreaks.ToArray());
    }
}
