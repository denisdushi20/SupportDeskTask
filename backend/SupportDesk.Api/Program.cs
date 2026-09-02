using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Api.Application.Agents;
using SupportDesk.Api.Application.Tickets;
using SupportDesk.Api.Infrastructure;
using SupportDesk.Domain.Time;
using SupportDesk.Infrastructure;
using SupportDesk.Infrastructure.Persistence;
using SupportDesk.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers(options =>
    {
        options.Filters.Add<SafeExceptionFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
        AppErrorHttpMapper.ToValidationProblemResult(context.ModelState);
});

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("SupportDesk")
    ?? DesignTimeSupportDeskDbContextFactory.DefaultConnectionString;

builder.Services.AddSupportDeskPersistence(connectionString);
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<TicketService>();
builder.Services.AddScoped<AgentQueryService>();
builder.Services.AddScoped<SafeExceptionFilter>();

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

public partial class Program;
