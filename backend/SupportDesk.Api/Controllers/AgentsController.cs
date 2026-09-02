using Microsoft.AspNetCore.Mvc;
using SupportDesk.Api.Application.Agents;
using SupportDesk.Api.Contracts;
using SupportDesk.Api.Infrastructure;

namespace SupportDesk.Api.Controllers;

[ApiController]
[Route("api/agents")]
public sealed class AgentsController : ControllerBase
{
    private readonly AgentQueryService _agents;

    public AgentsController(AgentQueryService agents)
    {
        _agents = agents;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AgentListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] AgentListQuery query, CancellationToken cancellationToken)
    {
        var result = await _agents.ListAsync(query.Search, cancellationToken);
        return AppErrorHttpMapper.ToActionResult(result, Ok);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AgentListItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _agents.GetByIdAsync(id, cancellationToken);
        return AppErrorHttpMapper.ToActionResult(result, Ok);
    }
}
