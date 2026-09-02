using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportDesk.Domain.Entities;

namespace SupportDesk.Infrastructure.Persistence.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Reference)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(t => t.Reference)
            .IsUnique()
            .HasDatabaseName("UX_Tickets_Reference");

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(t => t.CustomerName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.CustomerEmail)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(t => t.Priority)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(t => t.CreatedDate)
            .IsRequired();

        builder.Property(t => t.LastModifiedDate)
            .IsRequired();

        builder.Property(t => t.DueDate)
            .IsRequired();

        builder.Property(t => t.ResolvedDate);

        builder.Property(t => t.ClosedDate);

        builder.HasIndex(t => t.Status)
            .HasDatabaseName("IX_Tickets_Status");

        builder.HasIndex(t => t.AssignedAgentId)
            .HasDatabaseName("IX_Tickets_AssignedAgentId");

        builder.HasIndex(t => new { t.Status, t.DueDate })
            .HasDatabaseName("IX_Tickets_Status_DueDate");

        builder.HasIndex(t => t.CreatedDate)
            .HasDatabaseName("IX_Tickets_CreatedDate");

        builder.HasMany(t => t.Comments)
            .WithOne(c => c.Ticket)
            .HasForeignKey(c => c.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
