using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SupportDesk.Infrastructure.Persistence;

public sealed class DesignTimeSupportDeskDbContextFactory : IDesignTimeDbContextFactory<SupportDeskDbContext>
{
    public const string DefaultConnectionString =
        "Server=localhost;Database=SupportDesk;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

    public SupportDeskDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SupportDeskDbContext>()
            .UseSqlServer(DefaultConnectionString)
            .Options;

        return new SupportDeskDbContext(options);
    }
}
