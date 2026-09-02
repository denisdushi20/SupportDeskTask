using Microsoft.EntityFrameworkCore;
using SupportDesk.Domain.Entities;

namespace SupportDesk.Infrastructure.Persistence;

public class SupportDeskDbContext : DbContext
{
    public SupportDeskDbContext(DbContextOptions<SupportDeskDbContext> options)
        : base(options)
    {
    }

    public DbSet<Agent> Agents => Set<Agent>();

    public DbSet<Ticket> Tickets => Set<Ticket>();

    public DbSet<Comment> Comments => Set<Comment>();

    public DbSet<TicketReferenceCounter> TicketReferenceCounters => Set<TicketReferenceCounter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SupportDeskDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
