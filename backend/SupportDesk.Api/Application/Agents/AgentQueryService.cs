using Microsoft.EntityFrameworkCore;
using SupportDesk.Api.Application.Common;
using SupportDesk.Api.Application.Mapping;
using SupportDesk.Api.Contracts;
using SupportDesk.Infrastructure.Persistence;

namespace SupportDesk.Api.Application.Agents;

public sealed class AgentQueryService
{
    private readonly SupportDeskDbContext _db;

    public AgentQueryService(SupportDeskDbContext db)
    {
        _db = db;
    }

    public async Task<AppResult<IReadOnlyList<AgentListItemResponse>>> ListAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Agents.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(a =>
                a.FullName.Contains(term)
                || a.Email.Contains(term));
        }

        var items = await query
            .OrderBy(a => a.FullName)
            .ThenBy(a => a.Id)
            .Select(a => new AgentListItemResponse
            {
                Id = a.Id,
                FullName = a.FullName,
                Email = a.Email,
                Department = a.Department,
                Active = a.Active
            })
            .ToListAsync(cancellationToken);

        return AppResult<IReadOnlyList<AgentListItemResponse>>.Success(items);
    }

    public async Task<AppResult<AgentListItemResponse>> GetByIdAsync(
        Guid agentId,
        CancellationToken cancellationToken = default)
    {
        var agent = await _db.Agents
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == agentId, cancellationToken);

        if (agent is null)
        {
            return AppResult<AgentListItemResponse>.Failure(
                AppErrorCodes.AgentNotFound,
                $"Agent '{agentId}' was not found.");
        }

        return AppResult<AgentListItemResponse>.Success(AgentMapper.ToListItem(agent));
    }
}
