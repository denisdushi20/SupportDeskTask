using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportDesk.Domain.Entities;

namespace SupportDesk.Infrastructure.Persistence.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comments");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.AuthorName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Body)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(c => c.CreatedDate)
            .IsRequired();

        builder.HasIndex(c => c.TicketId)
            .HasDatabaseName("IX_Comments_TicketId");
    }
}
