using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class BellSlotConfiguration : IEntityTypeConfiguration<BellSlot>
{
    private static readonly DateTime SeedDate = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<BellSlot> builder)
    {
        builder.ToTable("bell_slots");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.NumberPair).IsRequired();
        builder.Property(x => x.StartTime).IsRequired();
        builder.Property(x => x.EndTime).IsRequired();

        builder.HasIndex(x => x.NumberPair).IsUnique().HasDatabaseName("ix_bell_slots_number_pair");

        builder.HasData(
            new BellSlot
            {
                Id = new Guid("b1000000-0000-0000-0000-000000000001"),
                NumberPair = 1,
                StartTime = new TimeSpan(8, 30, 0),
                EndTime = new TimeSpan(9, 50, 0),
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            new BellSlot
            {
                Id = new Guid("b1000000-0000-0000-0000-000000000002"),
                NumberPair = 2,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(11, 20, 0),
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            new BellSlot
            {
                Id = new Guid("b1000000-0000-0000-0000-000000000003"),
                NumberPair = 3,
                StartTime = new TimeSpan(11, 30, 0),
                EndTime = new TimeSpan(12, 50, 0),
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            new BellSlot
            {
                Id = new Guid("b1000000-0000-0000-0000-000000000004"),
                NumberPair = 4,
                StartTime = new TimeSpan(13, 0, 0),
                EndTime = new TimeSpan(14, 20, 0),
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            new BellSlot
            {
                Id = new Guid("b1000000-0000-0000-0000-000000000005"),
                NumberPair = 5,
                StartTime = new TimeSpan(15, 5, 0),
                EndTime = new TimeSpan(16, 25, 0),
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            new BellSlot
            {
                Id = new Guid("b1000000-0000-0000-0000-000000000006"),
                NumberPair = 6,
                StartTime = new TimeSpan(16, 35, 0),
                EndTime = new TimeSpan(17, 55, 0),
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            new BellSlot
            {
                Id = new Guid("b1000000-0000-0000-0000-000000000007"),
                NumberPair = 7,
                StartTime = new TimeSpan(18, 5, 0),
                EndTime = new TimeSpan(19, 25, 0),
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            new BellSlot
            {
                Id = new Guid("b1000000-0000-0000-0000-000000000008"),
                NumberPair = 8,
                StartTime = new TimeSpan(19, 35, 0),
                EndTime = new TimeSpan(20, 55, 0),
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            }
        );
    }
}

public class BigBreakConfiguration : IEntityTypeConfiguration<BigBreak>
{
    private static readonly DateTime SeedDate = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<BigBreak> builder)
    {
        builder.ToTable("big_breaks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.AfterPair).IsRequired();
        builder.Property(x => x.StartTime).IsRequired();
        builder.Property(x => x.EndTime).IsRequired();

        builder.HasData(
            new BigBreak
            {
                Id = new Guid("b2000000-0000-0000-0000-000000000001"),
                AfterPair = 4,
                StartTime = new TimeSpan(14, 20, 0),
                EndTime = new TimeSpan(15, 5, 0),
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            }
        );
    }
}
