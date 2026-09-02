using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportDesk.Domain.Entities;

namespace SupportDesk.Infrastructure.Persistence.Configurations;

public class AgentConfiguration : IEntityTypeConfiguration<Agent>
{
    public void Configure(EntityTypeBuilder<Agent> builder)
    {
        builder.ToTable("Agents");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.HasIndex(a => a.Email)
            .IsUnique()
            .HasDatabaseName("UX_Agents_Email");

        builder.Property(a => a.Department)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(a => a.Active)
            .IsRequired();

        builder.HasMany(a => a.Tickets)
            .WithOne(t => t.AssignedAgent)
            .HasForeignKey(t => t.AssignedAgentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
