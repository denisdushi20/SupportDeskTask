using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SupportDesk.Infrastructure.Persistence;

namespace SupportDesk.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSupportDeskPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<SupportDeskDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<ITicketReferenceGenerator, SqlServerTicketReferenceGenerator>();

        return services;
    }
}
