using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SupportDesk.Infrastructure.Persistence.Configurations;

public class TicketReferenceCounterConfiguration : IEntityTypeConfiguration<TicketReferenceCounter>
{
    public void Configure(EntityTypeBuilder<TicketReferenceCounter> builder)
    {
        builder.ToTable("TicketReferenceCounters");

        builder.HasKey(c => c.Year);

        builder.Property(c => c.Year)
            .ValueGeneratedNever();

        builder.Property(c => c.LastValue)
            .IsRequired();
    }
}
