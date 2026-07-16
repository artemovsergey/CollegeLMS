using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class StipendListItemConfiguration : IEntityTypeConfiguration<StipendListItem>
{
    public void Configure(EntityTypeBuilder<StipendListItem> builder)
    {
        builder.ToTable("stipend_list_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Amount).HasColumnType("decimal(10,2)");
        builder
            .HasOne(x => x.StipendList)
            .WithMany(l => l.Items)
            .HasForeignKey(x => x.StipendListId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
