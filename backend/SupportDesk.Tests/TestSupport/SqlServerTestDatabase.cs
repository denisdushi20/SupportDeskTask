using Microsoft.EntityFrameworkCore;
using SupportDesk.Infrastructure.Persistence;

namespace SupportDesk.Tests.TestSupport;

internal static class SqlServerTestDatabase
{
    public static string CreateConnectionString(string prefix) =>
        $"Server=localhost;Database={prefix}_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

    public static SupportDeskDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<SupportDeskDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        var db = new SupportDeskDbContext(options);
        db.Database.EnsureDeleted();
        db.Database.Migrate();
        return db;
    }
}
