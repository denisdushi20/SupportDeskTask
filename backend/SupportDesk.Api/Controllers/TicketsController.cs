using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Api.Application.Common;
using SupportDesk.Api.Application.Tickets;
using SupportDesk.Api.Contracts;
using SupportDesk.Api.Infrastructure;

namespace SupportDesk.Api.Controllers;

[ApiController]
[Route("api/tickets")]
public sealed class TicketsController : ControllerBase
{
    private readonly TicketService _tickets;

    public TicketsController(TicketService tickets)
    {
        _tickets = tickets;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TicketListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] TicketListQuery query, CancellationToken cancellationToken)
    {
        var result = await _tickets.ListAsync(query, cancellationToken);
        return AppErrorHttpMapper.ToActionResult(result, Ok);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TicketDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _tickets.GetByIdAsync(id, cancellationToken);
        return AppErrorHttpMapper.ToActionResult(result, Ok);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TicketDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTicketRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _tickets.CreateAsync(request, cancellationToken);
            return AppErrorHttpMapper.ToActionResult(
                result,
                ticket => CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ticket));
        }
        catch (DbUpdateException)
        {
            return AppErrorHttpMapper.ToProblemResult(
                AppError.Create(AppErrorCodes.Conflict, "A uniqueness conflict occurred while creating the ticket."));
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TicketDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateTicketRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _tickets.UpdateAsync(id, request, cancellationToken);
        return AppErrorHttpMapper.ToActionResult(result, Ok);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _tickets.DeleteAsync(id, cancellationToken);
        return AppErrorHttpMapper.ToActionResult(result);
    }

    [HttpPut("{id:guid}/assignee")]
    [ProducesResponseType(typeof(TicketDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Assign(
        Guid id,
        [FromBody] AssignAgentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _tickets.AssignAsync(id, request.AgentId!.Value, cancellationToken);
        return AppErrorHttpMapper.ToActionResult(result, Ok);
    }

    [HttpDelete("{id:guid}/assignee")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Unassign(Guid id, CancellationToken cancellationToken)
    {
        var result = await _tickets.UnassignAsync(id, cancellationToken);
        if (result.IsFailure)
        {
            return AppErrorHttpMapper.ToProblemResult(result.Error!);
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/status")]
    [ProducesResponseType(typeof(TicketDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangeStatus(
        Guid id,
        [FromBody] ChangeStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _tickets.ChangeStatusAsync(id, request.Status!.Value, cancellationToken);
        return AppErrorHttpMapper.ToActionResult(result, Ok);
    }

    [HttpPost("{id:guid}/comments")]
    [ProducesResponseType(typeof(CommentResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddComment(
        Guid id,
        [FromBody] CreateCommentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _tickets.AddCommentAsync(id, request, cancellationToken);
        return AppErrorHttpMapper.ToActionResult(
            result,
            comment => StatusCode(StatusCodes.Status201Created, comment));
    }
}
