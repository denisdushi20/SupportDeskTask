using Microsoft.EntityFrameworkCore;
using SupportDesk.Infrastructure;
using SupportDesk.Infrastructure.Persistence;
using SupportDesk.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("SupportDesk")
    ?? DesignTimeSupportDeskDbContextFactory.DefaultConnectionString;

builder.Services.AddSupportDeskPersistence(connectionString);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<SupportDeskDbContext>();
    await db.Database.MigrateAsync();
    await SupportDeskSeedData.EnsureSeededAsync(db);
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
