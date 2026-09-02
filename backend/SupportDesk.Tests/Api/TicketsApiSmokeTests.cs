using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SupportDesk.Api.Application.Common;
using SupportDesk.Domain.Enums;
using SupportDesk.Domain.Time;
using SupportDesk.Infrastructure.Persistence;
using SupportDesk.Infrastructure.Persistence.Seed;
using SupportDesk.Tests.TestSupport;

namespace SupportDesk.Tests.Api;

public class SupportDeskApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString =
        SqlServerTestDatabase.CreateConnectionString("SupportDesk_ApiSmoke");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<SupportDeskDbContext>));
            services.RemoveAll(typeof(SupportDeskDbContext));

            services.AddDbContext<SupportDeskDbContext>(options =>
                options.UseSqlServer(_connectionString));

            services.RemoveAll(typeof(IClock));
            services.AddSingleton<IClock>(new FakeClock(
                new DateTimeOffset(2026, 9, 2, 16, 0, 0, TimeSpan.Zero)));
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SupportDeskDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        await SupportDeskSeedData.EnsureSeededAsync(db);
    }
}

public class TicketsApiSmokeTests : IAsyncLifetime
{
    private readonly SupportDeskApiFactory _factory = new();
    private HttpClient _client = null!;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Create_then_get_returns_201_and_200()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/tickets", new
        {
            title = "API smoke ticket",
            description = "Created via smoke test",
            customerName = "Smoke Customer",
            customerEmail = "smoke@example.com",
            priority = "High"
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createResponse.Headers.Location);

        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var id = created.GetProperty("id").GetGuid();
        Assert.Equal("New", created.GetProperty("status").GetString());

        var get = await _client.GetAsync($"/api/tickets/{id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
    }

    [Fact]
    public async Task Invalid_status_returns_409_with_code()
    {
        var ticketId = Guid.Parse("22222222-2222-2222-2222-222222220001"); // New
        var response = await _client.PostAsJsonAsync($"/api/tickets/{ticketId}/status", new
        {
            status = "Resolved"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(AppErrorCodes.InvalidStatusTransition, body.GetProperty("code").GetString());
        Assert.Equal("New", body.GetProperty("currentStatus").GetString());
        Assert.Equal("Resolved", body.GetProperty("requestedStatus").GetString());
    }

    [Fact]
    public async Task Resolve_without_agent_returns_ASSIGNMENT_REQUIRED()
    {
        // Create unassigned, move to InProgress, try resolve
        var create = await _client.PostAsJsonAsync("/api/tickets", new
        {
            title = "Needs resolve",
            description = "desc",
            customerName = "Cust",
            customerEmail = "cust@example.com",
            priority = "Normal"
        });
        var created = await create.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var id = created.GetProperty("id").GetGuid();

        await _client.PostAsJsonAsync($"/api/tickets/{id}/status", new { status = "InProgress" });

        var resolve = await _client.PostAsJsonAsync($"/api/tickets/{id}/status", new { status = "Resolved" });
        Assert.Equal(HttpStatusCode.Conflict, resolve.StatusCode);
        var body = await resolve.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(AppErrorCodes.AssignmentRequired, body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Inactive_assignment_returns_AGENT_INACTIVE()
    {
        var ticketId = Guid.Parse("22222222-2222-2222-2222-222222220001");
        var response = await _client.PutAsJsonAsync($"/api/tickets/{ticketId}/assignee", new
        {
            agentId = SupportDeskSeedData.Agent5Id
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(AppErrorCodes.AgentInactive, body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Closed_update_returns_TICKET_CLOSED()
    {
        var closedId = Guid.Parse("22222222-2222-2222-2222-222222220015");
        var response = await _client.PutAsJsonAsync($"/api/tickets/{closedId}", new
        {
            title = "Nope",
            description = "Nope",
            customerName = "Nope",
            customerEmail = "nope@example.com",
            priority = "Low"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(AppErrorCodes.TicketClosed, body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Resolved_update_returns_TICKET_NOT_EDITABLE()
    {
        var resolvedId = Guid.Parse("22222222-2222-2222-2222-222222220011");
        var response = await _client.PutAsJsonAsync($"/api/tickets/{resolvedId}", new
        {
            title = "Nope",
            description = "Nope",
            customerName = "Nope",
            customerEmail = "nope@example.com",
            priority = "Low"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(AppErrorCodes.TicketNotEditable, body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Update_whitespace_only_title_returns_400_VALIDATION_ERROR()
    {
        var ticketId = Guid.Parse("22222222-2222-2222-2222-222222220001");
        var response = await _client.PutAsJsonAsync($"/api/tickets/{ticketId}", new
        {
            title = "   ",
            description = "Valid description",
            customerName = "Valid Customer",
            customerEmail = "valid@example.com",
            priority = "High"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(AppErrorCodes.ValidationError, body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Overdue_list_filter_returns_only_overdue_open_tickets()
    {
        var response = await _client.GetAsync("/api/tickets?overdueOnly=true&pageSize=50");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var items = body.GetProperty("items").EnumerateArray().ToList();
        Assert.NotEmpty(items);
        Assert.All(items, item => Assert.True(item.GetProperty("isOverdue").GetBoolean()));
    }

    [Fact]
    public async Task Unknown_ticket_returns_404()
    {
        var response = await _client.GetAsync($"/api/tickets/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(AppErrorCodes.TicketNotFound, body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Invalid_create_request_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/api/tickets", new
        {
            title = "",
            description = "x",
            customerName = "x",
            customerEmail = "not-an-email",
            priority = "High"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_without_priority_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/api/tickets", new
        {
            title = "Missing priority",
            description = "desc",
            customerName = "Cust",
            customerEmail = "cust@example.com"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Agents_list_returns_200()
    {
        var response = await _client.GetAsync("/api/agents");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.True(body.GetArrayLength() >= 5);
    }
}
