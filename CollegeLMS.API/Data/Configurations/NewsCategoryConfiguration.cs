using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class NewsCategoryConfiguration : IEntityTypeConfiguration<NewsCategory>
{
    public void Configure(EntityTypeBuilder<NewsCategory> builder)
    {
        builder.ToTable("news_categories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Slug).HasDatabaseName("ix_news_categories_slug").IsUnique();
    }
}
