using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("students");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.UserId).ValueGeneratedNever();
        builder.HasIndex(x => x.UserId).IsUnique().HasDatabaseName("ix_students_user_id");
        builder.Property(x => x.RecordBookNumber).HasMaxLength(20);
        builder
            .HasIndex(x => x.RecordBookNumber)
            .IsUnique()
            .HasDatabaseName("ix_students_record_book_number");
        builder
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.Group)
            .WithMany(g => g.Students)
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
